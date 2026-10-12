using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Data.Sqlite;
using STEMLabManager.Infrastructure;
using STEMLabManager.Domain;
using STEMLabManager.Application;
using Xunit;
namespace STEMLabManager.Tests;

public sealed class AdditionalTests
{
    [Fact] public async Task OldSchemaMigratesWithoutLosingData()
    {
        var directory=Path.Combine(Path.GetTempPath(),"stem-migrate-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);var path=Path.Combine(directory,"stem.db");
        try
        {
            await using(var db=new LabDbContext(new DbContextOptionsBuilder<LabDbContext>().UseSqlite($"Data Source={path};Pooling=False").Options))
            {
                var first=db.Database.GetMigrations().First();await db.GetService<IMigrator>().MigrateAsync(first);
                await db.Database.ExecuteSqlRawAsync("INSERT INTO Categories (Name) VALUES ('Existing category')");
            }
            var service=new LabService(directory);await service.InitializeAsync();await service.SetupAsync("admin","MigrationPassword123!","Admin","Room");
            Assert.Contains(await service.CategoriesAsync(),x=>x.Name=="Existing category");Assert.NotEmpty(Directory.GetFiles(directory,"before-migration-*.db"));
            await using var current=service.Db();Assert.Empty(await current.Database.GetPendingMigrationsAsync());
        }
        finally{SqliteConnection.ClearAllPools();Directory.Delete(directory,true);}
    }
    [Fact] public async Task QrInventoryMarksMissingUnitAndCanRecoverIt()
    {
        var directory=Path.Combine(Path.GetTempPath(),"stem-inventory-"+Guid.NewGuid().ToString("N"));var service=new LabService(directory);
        try
        {
            await service.InitializeAsync();await service.SetupAsync("admin","InventoryPassword123!","Admin","Room");var c=(await service.CategoriesAsync())[0].Id;var l=(await service.LocationsAsync())[0].Id;var a=await service.SaveAssetAsync(null,new("A1","Unit",AssetKind.Unit,c,l,"cái",2));var units=await service.UnitsAsync(a);
            var inventory=await service.CreateUnitInventoryAsync(l,new HashSet<string>{units[0].QrValue},"Thiếu một cá thể");await service.ConfirmInventoryAsync(inventory);Assert.Equal(UnitStatus.Lost,(await service.UnitsAsync(a)).Single(u=>u.Id==units[1].Id).Status);
            var found=await service.CreateUnitInventoryAsync(l,units.Select(u=>u.QrValue).ToHashSet(),"Tìm lại cá thể");await service.ConfirmInventoryAsync(found);Assert.All(await service.UnitsAsync(a),u=>Assert.Equal(UnitStatus.Available,u.Status));
        }
        finally{service.Logout();SqliteConnection.ClearAllPools();Directory.Delete(directory,true);}
    }
    [Fact] public async Task FutureConsumableReservationProtectsStockFromImmediateConsumption()
    {
        var directory=Path.Combine(Path.GetTempPath(),"stem-consumable-"+Guid.NewGuid().ToString("N"));var service=new LabService(directory);
        try
        {
            await service.InitializeAsync();await service.SetupAsync("admin","ConsumablePassword123!","Admin","Room");var c=(await service.CategoriesAsync())[0].Id;var l=(await service.LocationsAsync())[0].Id;var b=await service.SaveBorrowerAsync(null,new("B1","Borrower",""));var a=await service.SaveAssetAsync(null,new("A1","Consumable",AssetKind.Consumable,c,l,"cái",3));var start=DateTime.UtcNow.AddDays(10);
            var r=await service.ReserveAsync(new(b,start,start.AddHours(1),[new(a,null,3)]));await service.SetReservationStatusAsync(r,ReservationStatus.Approved);
            await Assert.ThrowsAsync<BusinessException>(()=>service.IssueAsync(new(b,DateTime.UtcNow.AddDays(1),"Current",[new(a,null,1)])));await Assert.ThrowsAsync<BusinessException>(()=>service.AdjustStockAsync(a,-1,"Reduce"));Assert.Equal(3,(await service.AssetsAsync()).Single().Total);
        }
        finally{service.Logout();SqliteConnection.ClearAllPools();Directory.Delete(directory,true);}
    }
    [Fact] public async Task DatabaseConstraintRejectsDuplicateOpenUnitEvenWithoutService()
    {
        var directory=Path.Combine(Path.GetTempPath(),"stem-constraint-"+Guid.NewGuid().ToString("N"));var service=new LabService(directory);
        try
        {
            await service.InitializeAsync();await service.SetupAsync("admin","ConstraintPassword123!","Admin","Room");var c=(await service.CategoriesAsync())[0].Id;var l=(await service.LocationsAsync())[0].Id;var b=await service.SaveBorrowerAsync(null,new("B1","Borrower",""));var a=await service.SaveAssetAsync(null,new("A1","Unit",AssetKind.Unit,c,l,"cái",1));var u=(await service.UnitsAsync(a))[0];var loan=await service.IssueAsync(new(b,DateTime.UtcNow.AddDays(1),"Loan",[new(a,u.Id,1)]));
            await using var db=service.Db();db.LoanLines.Add(new LoanLine {LoanId=loan,AssetId=a,UnitId=u.Id,QuantityIssued=1});await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveChangesAsync());
        }
        finally{service.Logout();SqliteConnection.ClearAllPools();Directory.Delete(directory,true);}
    }
}
