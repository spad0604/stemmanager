using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using STEMLabManager.Application;
using STEMLabManager.Domain;
using STEMLabManager.Infrastructure;
using STEMLabManager.AI;
using Xunit;

namespace STEMLabManager.Tests;

public sealed class LabTests : IAsyncLifetime
{
    private readonly string directory=Path.Combine(Path.GetTempPath(),"stem-tests-"+Guid.NewGuid().ToString("N"));
    private LabService service=null!;
    private long category,location,borrower;
    private const string Password="StrongPassword123!";
    public async Task InitializeAsync()
    {
        service=new(directory);await service.InitializeAsync();await service.SetupAsync("admin",Password,"Admin test","Test room");
        category=(await service.CategoriesAsync())[0].Id;location=(await service.LocationsAsync())[0].Id;borrower=await service.SaveBorrowerAsync(null,new("B001","Người thử","7A","Học sinh","private@example.invalid"));
    }
    public Task DisposeAsync(){service.Logout();Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();Directory.Delete(directory,true);return Task.CompletedTask;}
    private Task<long> Asset(AssetKind kind,int quantity=3,string code="A001")=>service.SaveAssetAsync(null,new(code,"Cảm biến điện",kind,category,location,"cái",quantity));
    private async Task<long> Issue(long asset,int qty=1,long? unit=null)=>await service.IssueAsync(new(borrower,DateTime.UtcNow.AddDays(1),"Bài học",[new(asset,unit,qty)]));
    [Fact] public async Task SetupAndMigrationAreReal()
    {Assert.False(await service.NeedsSetupAsync());await using var db=service.Db();Assert.NotEmpty(await db.Database.GetAppliedMigrationsAsync());Assert.True(await db.Users.AnyAsync());Assert.NotEqual(Password,(await db.Users.FirstAsync()).PasswordHash);await Assert.ThrowsAsync<BusinessException>(()=>service.SetupAsync("another",Password,"Name","Room"));}
    [Fact] public async Task UnitsCannotBeBorrowedTwiceAndAuditIsAtomic()
    {
        var a=await Asset(AssetKind.Unit,1);var u=(await service.UnitsAsync(a))[0];var loan=await Issue(a,unit:u.Id);
        await Assert.ThrowsAsync<BusinessException>(()=>Issue(a,unit:u.Id));Assert.Single(await service.LoansAsync());Assert.Equal(UnitStatus.CheckedOut,(await service.UnitsAsync(a))[0].Status);
        await using var db=service.Db();Assert.Contains(await db.Audit.ToListAsync(),e=>e.Action=="IssueLoan" && e.EntityId==loan);
    }
    [Fact] public async Task PartialReturnsAndDamagePreserveState()
    {
        var a=await Asset(AssetKind.Unit,2);var id=await Issue(a,2);var l=(await service.LoansAsync()).Single();
        await service.ReturnAsync(id,[new(l.Lines[0].Id,1,ReturnCondition.Damaged)],"Hỏng");Assert.Equal(LoanStatus.Open,(await service.LoansAsync()).Single().Status);Assert.Single(await service.MaintenanceAsync());
        Assert.Equal(0,(await service.AssetsAsync()).Single().Available);
    }
    [Fact] public async Task ReturnMoreThanOutstandingRollsBack()
    {
        var a=await Asset(AssetKind.Bulk,5);var id=await Issue(a,3);var line=(await service.LoansAsync()).Single().Lines[0];
        await Assert.ThrowsAsync<BusinessException>(()=>service.ReturnAsync(id,[new(line.Id,4,ReturnCondition.Good)],""));
        Assert.Equal(0,(await service.LoansAsync()).Single().Lines[0].QuantityReturned);await using var db=service.Db();Assert.Empty(await db.Returns.ToListAsync());
        await service.ReturnAsync(id,[new(line.Id,2,ReturnCondition.Good)],"");Assert.Single(await service.LoansAsync());
        await service.ReturnAsync(id,[new(line.Id,1,ReturnCondition.Good)],"");Assert.Empty(await service.LoansAsync());Assert.Equal(5,(await service.AssetsAsync()).Single().Available);
    }
    [Fact] public async Task ConsumableDecreasesLedgerAndCannotBeReturned()
    {
        var a=await Asset(AssetKind.Consumable,4);var id=await Issue(a,3);Assert.Equal(1,(await service.AssetsAsync()).Single().Total);var line=(await service.LoansAsync(true)).Single().Lines[0];Assert.True(line.Consumable);
        await Assert.ThrowsAsync<BusinessException>(()=>service.ReturnAsync(id,[new(line.Id,1,ReturnCondition.Good)],""));await Assert.ThrowsAsync<BusinessException>(()=>Issue(a,2));
    }
    [Fact] public async Task BulkStockCannotBecomeNegativeOrBelowOutstanding()
    {var a=await Asset(AssetKind.Bulk,3);await Issue(a,2);await Assert.ThrowsAsync<BusinessException>(()=>service.AdjustStockAsync(a,-2,"Điều chỉnh"));Assert.Equal(3,(await service.AssetsAsync()).Single().Total);}
    [Fact] public async Task MultipleLinesFailAsOneTransaction()
    {var a=await Asset(AssetKind.Unit,1);var b=await Asset(AssetKind.Bulk,1,"A002");await Assert.ThrowsAsync<BusinessException>(()=>service.IssueAsync(new(borrower,DateTime.UtcNow.AddDays(1),"Test",[new(a,null,1),new(b,null,2)])));Assert.Equal(UnitStatus.Available,(await service.UnitsAsync(a))[0].Status);Assert.Empty(await service.LoansAsync());}
    [Fact] public async Task ConcurrentIssuingUsesSqliteTransactionAcrossServiceInstances()
    {
        var a=await Asset(AssetKind.Unit,1);var other=new LabService(directory);await other.LoginAsync("admin",Password);
        async Task<bool> Try(LabService s){try{await s.IssueAsync(new(borrower,DateTime.UtcNow.AddDays(1),"Race",[new(a,null,1)]));return true;}catch(BusinessException){return false;}}
        var results=await Task.WhenAll(Try(service),Task.Run(()=>Try(other)));Assert.Equal(1,results.Count(x=>x));Assert.Single(await service.LoansAsync());
    }
    [Fact] public async Task OnlyOneOverlappingReservationCanBeApproved()
    {
        var a=await Asset(AssetKind.Unit,1);var start=DateTime.UtcNow.AddDays(2);var end=start.AddHours(1);
        var one=await service.ReserveAsync(new(borrower,start,end,[new(a,null,1)]));var two=await service.ReserveAsync(new(borrower,start,end,[new(a,null,1)]));
        await service.SetReservationStatusAsync(one,ReservationStatus.Approved);await Assert.ThrowsAsync<BusinessException>(()=>service.SetReservationStatusAsync(two,ReservationStatus.Approved));
        Assert.Equal(ReservationStatus.Pending,(await service.ReservationsAsync()).Single(x=>x.Id==two).Status);
    }
    [Fact] public async Task BackToBackReservationsDoNotOverlap()
    {var a=await Asset(AssetKind.Unit,1);var start=DateTime.UtcNow.AddDays(2);var one=await service.ReserveAsync(new(borrower,start,start.AddHours(1),[new(a,null,1)]));await service.SetReservationStatusAsync(one,ReservationStatus.Approved);var two=await service.ReserveAsync(new(borrower,start.AddHours(1),start.AddHours(2),[new(a,null,1)]));await service.SetReservationStatusAsync(two,ReservationStatus.Approved);}
    [Fact] public async Task ReservationCapacityUsesPeakRatherThanSumOfNonoverlappingBookings()
    {
        var a=await Asset(AssetKind.Bulk,2);var start=DateTime.UtcNow.AddDays(2);
        foreach(int offset in new[]{0,1}){var r=await service.ReserveAsync(new(borrower,start.AddHours(offset),start.AddHours(offset+1),[new(a,null,1)]));await service.SetReservationStatusAsync(r,ReservationStatus.Approved);}
        var spanning=await service.ReserveAsync(new(borrower,start,start.AddHours(2),[new(a,null,1)]));await service.SetReservationStatusAsync(spanning,ReservationStatus.Approved);
    }
    [Fact] public async Task ExtensionConflictingWithBookingIsRejected()
    {var a=await Asset(AssetKind.Unit,1);var loan=await Issue(a);var start=DateTime.UtcNow.AddDays(2);var r=await service.ReserveAsync(new(borrower,start,start.AddHours(2),[new(a,null,1)]));await service.SetReservationStatusAsync(r,ReservationStatus.Approved);await Assert.ThrowsAsync<BusinessException>(()=>service.ExtendAsync(loan,start.AddHours(1),"Gia hạn"));}
    [Fact] public async Task MaintenanceAndRetiredUnitsAreNotAvailable()
    {var a=await Asset(AssetKind.Unit,2);var units=await service.UnitsAsync(a);await service.OpenMaintenanceAsync(units[0].Id,"Hỏng");await service.RetireUnitAsync(units[1].Id,"Cũ");await Assert.ThrowsAsync<BusinessException>(()=>Issue(a));var maintenance=(await service.MaintenanceAsync())[0];await service.CloseMaintenanceAsync(maintenance.Id,"Đã sửa",10);Assert.Equal(1,(await service.AssetsAsync()).Single().Available);}
    [Fact] public async Task ArchiveDoesNotDestroyHistory()
    {var a=await Asset(AssetKind.Unit,1);var id=await Issue(a);await Assert.ThrowsAsync<BusinessException>(()=>service.ArchiveAssetAsync(a));var line=(await service.LoansAsync()).Single().Lines[0];await service.ReturnAsync(id,[new(line.Id,1,ReturnCondition.Good)],"");await service.ArchiveAssetAsync(a);Assert.Empty(await service.AssetsAsync());Assert.Single(await service.LoansAsync(true));}
    [Theory] [InlineData(Role.Viewer)] [InlineData(Role.Teacher)]
    public async Task ServicesEnforceRoleWithoutUI(Role role)
    {
        await service.SaveUserAsync(null,"low","Low",role,Password,true);service.Logout();await service.LoginAsync("low",Password);
        await Assert.ThrowsAsync<BusinessException>(()=>Asset(AssetKind.Unit));await Assert.ThrowsAsync<BusinessException>(()=>service.BackupAsync());await Assert.ThrowsAsync<BusinessException>(()=>service.UsersAsync());await Assert.ThrowsAsync<BusinessException>(()=>service.SaveSettingAsync("room","Other"));await Assert.ThrowsAsync<BusinessException>(()=>service.GrantConsentAsync(borrower,"1","Yes"));
        Assert.Null((await service.BorrowersAsync()).Single().Contact);
    }
    [Fact] public async Task RevokedAccountSessionCannotContinueWriting()
    {await service.SaveUserAsync(null,"manager","Manager",Role.LabManager,Password,true);var other=new LabService(directory);await other.LoginAsync("manager",Password);var user=(await service.UsersAsync()).Single(x=>x.Username=="manager");await service.SaveUserAsync(user.Id,user.Username,user.DisplayName,user.Role,null,false);await Assert.ThrowsAsync<BusinessException>(()=>other.SaveBorrowerAsync(null,new("B2","Other","")));}
    [Fact] public async Task TeacherIssueCanBeDisabledByConfiguration()
    {var a=await Asset(AssetKind.Unit);await service.SaveUserAsync(null,"teacher","Teacher",Role.Teacher,Password,true);await service.SaveSettingAsync("teacher.issue","false");await service.LoginAsync("teacher",Password);await Assert.ThrowsAsync<BusinessException>(()=>Issue(a));}
    [Fact] public async Task LoginLockoutAndPasswordChangeWork()
    {for(int i=0;i<5;i++)await Assert.ThrowsAsync<BusinessException>(()=>service.LoginAsync("admin","wrong"));await Assert.ThrowsAsync<BusinessException>(()=>service.LoginAsync("admin",Password));}
    [Fact] public async Task PasswordChangeInvalidatesOldPassword()
    {await service.ChangePasswordAsync(Password,"AnotherPassword123!");service.Logout();await Assert.ThrowsAsync<BusinessException>(()=>service.LoginAsync("admin",Password));await service.LoginAsync("admin","AnotherPassword123!");}
    [Fact] public async Task BorrowerImportIsAllOrNothingAndHandlesQuotedCsv()
    {
        var file=Path.Combine(directory,"import.csv");await File.WriteAllTextAsync(file,"Code,Name,Class\nB002,\"Tên, có dấu\",7B\nB001,Trùng,7A");var preview=await service.PreviewBorrowersAsync(file);Assert.NotEmpty(preview.Errors);await Assert.ThrowsAsync<BusinessException>(()=>service.ImportBorrowersAsync(preview));Assert.Single(await service.BorrowersAsync());
        await File.WriteAllTextAsync(file,"Code,Name,Class\nB002,\"Tên, có dấu\",7B");preview=await service.PreviewBorrowersAsync(file);await service.ImportBorrowersAsync(preview);Assert.Equal(2,(await service.BorrowersAsync()).Count);
    }
    [Fact] public async Task VietnameseSearchWorksWithoutAccents()
    {await Asset(AssetKind.Unit);Assert.Single(await service.AssetsAsync("cam bien dien"));Assert.Single(await service.BorrowersAsync("nguoi thu"));}
    [Fact] public async Task BackupRestorePreservesDataAndRequiresRelogin()
    {await Asset(AssetKind.Bulk,5);var backup=await service.BackupAsync();await Asset(AssetKind.Unit,1,"A002");await service.RestoreAsync(backup);Assert.Null(service.CurrentActor);await service.LoginAsync("admin",Password);Assert.Single(await service.AssetsAsync());Assert.Equal(5,(await service.AssetsAsync()).Single().Total);}
    [Fact] public async Task BadBackupLeavesActiveDatabaseIntact()
    {await Asset(AssetKind.Unit);var invalid=Path.Combine(directory,"bad.zip");await File.WriteAllTextAsync(invalid,"bad");await Assert.ThrowsAsync<BusinessException>(()=>service.RestoreAsync(invalid));Assert.Single(await service.AssetsAsync());}
    [Fact] public async Task InventoryConfirmsLedgerAdjustmentAndRejectsStaleCount()
    {var a=await Asset(AssetKind.Bulk,5);var session=await service.CreateInventoryAsync(location,new Dictionary<long,int>{{a,4}},"Thiếu 1");await service.ConfirmInventoryAsync(session);Assert.Equal(4,(await service.AssetsAsync()).Single().Total);var stale=await service.CreateInventoryAsync(location,new Dictionary<long,int>{{a,3}},"Thiếu");await service.AdjustStockAsync(a,1,"Nhập");await Assert.ThrowsAsync<BusinessException>(()=>service.ConfirmInventoryAsync(stale));}
    [Fact] public async Task ConsentRevocationDeletesEncryptedEmbeddingAndNeverLogsIt()
    {await service.SaveSettingAsync("face.enabled","true");var c=await service.GrantConsentAsync(borrower,"1.0","Tự nguyện");var embedding=Enumerable.Repeat(0.12345f,128).ToArray();await service.EnrollFaceAsync(c,embedding,"test-model");var profiles=await service.FaceProfilesAsync("test-model");Assert.Equal(embedding,profiles.Single().Embedding);await using(var db=service.Db()){var bytes=(await db.Faces.SingleAsync()).EncryptedEmbedding;Assert.NotEqual(embedding.Length*4,bytes.Length);Assert.DoesNotContain(await db.Audit.ToListAsync(),x=>x.Details.Contains("0.12345"));}await service.RevokeConsentAsync(borrower);Assert.Empty(await service.FaceProfilesAsync("test-model"));await Assert.ThrowsAsync<BusinessException>(()=>service.EnrollFaceAsync(c,embedding,"test-model"));}
    [Fact] public async Task EncryptedLlmSecretIsNotReturnedInSettings()
    {await service.SaveSettingAsync("llm.secret","private-key-123");Assert.DoesNotContain("llm.secret",(await service.SettingsAsync()).Keys);Assert.Equal("private-key-123",await service.ReadLlmKeyAsync());await using var db=service.Db();Assert.DoesNotContain("private-key-123",(await db.Settings.SingleAsync(x=>x.Key=="llm.secret")).Value);}
    [Fact] public async Task ReportsExcelPdfQrAndImportUseRealFiles()
    {await Asset(AssetKind.Unit);foreach(var extension in new[]{"csv","xlsx","pdf"}){var file=Path.Combine(directory,"report."+extension);await service.ExportAsync("assets",file,DateTime.UtcNow.AddDays(-1),DateTime.UtcNow.AddDays(1));Assert.True(new FileInfo(file).Length>100);}var qr=Path.Combine(directory,"qr.png");await service.ExportQrAsync("B001",qr);Assert.Equal(new byte[]{137,80,78,71},File.ReadAllBytes(qr).Take(4));}
    [Fact] public async Task AnalyticsChangesWithDateRangeAndUsesActualLoans()
    {var a=await Asset(AssetKind.Unit);await Issue(a);var current=await service.AnalyticsAsync(DateTime.UtcNow.AddDays(-1),DateTime.UtcNow.AddDays(1));Assert.Equal(1,current.TotalLoans);var past=await service.AnalyticsAsync(DateTime.UtcNow.AddDays(-30),DateTime.UtcNow.AddDays(-20));Assert.Equal(0,past.TotalLoans);}
    [Fact] public async Task DemoIsForbiddenInProduction()
    {await Assert.ThrowsAsync<BusinessException>(()=>service.SeedDemoAsync());}
    [Fact] public void FaceMatcherCanReturnUnknown()
    {Assert.Empty(FaceEngine.Match([1,0],[(1,new float[]{0,1})],0.5));Assert.Single(FaceEngine.Match([1,0],[(1,new float[]{1,0})],0.5));}
    [Fact] public void AggregateContainsNoNamesOrContact()
    {var a=new Analytics(DateTime.UtcNow.AddDays(-1),DateTime.UtcNow,1,0,0,0,1,1,0,[],[new("PII school student",1)],[]);var json=Insights.Preview(a);Assert.DoesNotContain("PII",json);Assert.DoesNotContain("student",json);Assert.Contains("asset-1",json);}
    [Fact] public async Task LlmNetworkFailureIsControlled()
    {using var client=new HttpClient(new FailHandler());var adapter=new Insights(client);var a=new Analytics(DateTime.UtcNow.AddDays(-1),DateTime.UtcNow,1,0,0,0,1,1,0,[],[],[]);var error=await Assert.ThrowsAsync<BusinessException>(()=>adapter.AnalyzeAsync(a,"https://example.invalid/chat","test","key"));Assert.Contains("Internet",error.Message);}
    private sealed class FailHandler : HttpMessageHandler {protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)=>throw new HttpRequestException();}
}
