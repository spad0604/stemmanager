using System.Globalization;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using STEMLabManager.Application;
using STEMLabManager.Domain;
namespace STEMLabManager.Infrastructure;

public sealed partial class LabService : ILabService
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly string databasePath;
    private readonly SecretStore secrets;
    private readonly bool demoMode;
    private readonly IExternalInsights? externalInsights;
    public Actor? CurrentActor { get; private set; }
    public string DataDirectory { get; }
    public bool IsDemo=>demoMode;
    public LabService(string? directory = null, bool demoMode = false,IExternalInsights? externalInsights = null)
    {
        DataDirectory = directory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "STEMManager", "Data");
        Directory.CreateDirectory(DataDirectory);
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(DataDirectory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        databasePath = Path.Combine(DataDirectory, "stem.db"); secrets = new(DataDirectory); this.demoMode=demoMode;this.externalInsights=externalInsights;
    }
    internal LabDbContext Db()
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = databasePath, ForeignKeys = true, DefaultTimeout = 30, Pooling = false }.ToString());
        return new(new DbContextOptionsBuilder<LabDbContext>().UseSqlite(connection).Options);
    }
    public async Task InitializeAsync()
    {
        await using var db = Db();
        if(File.Exists(databasePath) && new FileInfo(databasePath).Length>0 && (await db.Database.GetPendingMigrationsAsync()).Any())
        {
            var rollback=Path.Combine(DataDirectory,$"before-migration-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}.db");
            await db.Database.OpenConnectionAsync();using var destination=new SqliteConnection($"Data Source={rollback};Pooling=False");await destination.OpenAsync();((SqliteConnection)db.Database.GetDbConnection()).BackupDatabase(destination);
            if(File.Exists(Path.Combine(DataDirectory,"secrets.key")))File.Copy(Path.Combine(DataDirectory,"secrets.key"),rollback+".key");
        }
        await db.Database.MigrateAsync();
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
        await db.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=30000;");
    }
    private void Require(Permission permission) => Access.Require(CurrentActor, permission);
    private async Task VerifyActorAsync(LabDbContext db, Permission permission)
    {
        Require(permission);
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == CurrentActor!.Id);
        Rules.Require(user is { IsActive: true } && user.Role == CurrentActor!.Role, "Phiên đăng nhập đã hết hiệu lực. Hãy đăng nhập lại.");
    }
    private async Task<T> WriteAsync<T>(Permission permission, Func<LabDbContext, Task<T>> work)
    {
        Require(permission); await gate.WaitAsync();
        try
        {
            // Microsoft.Data.Sqlite async I/O often executes synchronously; keep import/seed/transactions off the UI thread.
            return await Task.Run(async ()=>
            {
                await using var db = Db(); await db.Database.OpenConnectionAsync();
                await using var tx = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: false);
                await db.Database.UseTransactionAsync(tx);
                await VerifyActorAsync(db, permission);
                var result = await work(db); await db.SaveChangesAsync(); await tx.CommitAsync(); return result;
            });
        }
        catch (DbUpdateException) { throw new BusinessException("Không thể ghi dữ liệu: mã trùng, quan hệ không hợp lệ hoặc dữ liệu đã thay đổi. Hãy tải lại."); }
        finally { gate.Release(); }
    }
    private Task WriteAsync(Permission p, Func<LabDbContext, Task> work) => WriteAsync(p, async db => { await work(db); return true; });
    private void Audit(LabDbContext db, string action, string entity, long? id, string details = "") => db.Audit.Add(new AuditEvent { ActorId = CurrentActor?.Id, Action = action, EntityType = entity, EntityId = id, Details = details, CreatedUtc = DateTime.UtcNow, DeviceId = Environment.MachineName });
    private static string Code(string prefix) => $"{prefix}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..10].ToUpperInvariant()}";
    private static string Normalize(string value) => string.Concat(value.Normalize(NormalizationForm.FormD).Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)).Replace('đ','d').Replace('Đ','D').ToLowerInvariant();
    private static bool Matches(string search, params string[] values) => string.IsNullOrWhiteSpace(search) || values.Any(v => Normalize(v).Contains(Normalize(search.Trim())));
    private static string CleanCode(string value) { Rules.Text(value, "Mã", 80); return value.Trim().ToUpperInvariant(); }
    public async Task<bool> NeedsSetupAsync() { await using var db = Db(); return !await db.Users.AnyAsync(); }
    public async Task SetupAsync(string username, string password, string displayName, string room)
    {
        Rules.Text(username, "Tài khoản", 80); Rules.Text(displayName, "Tên"); Rules.Text(room, "Tên phòng"); var hash = Passwords.Hash(password);
        await gate.WaitAsync();
        try
        {
            await using var db = Db(); await db.Database.OpenConnectionAsync();
            await using var tx = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: false); await db.Database.UseTransactionAsync(tx);
            Rules.Require(!await db.Users.AnyAsync(), "Hệ thống đã được thiết lập.");
            var user = new UserAccount { Username = username.Trim().ToLowerInvariant(), DisplayName = displayName.Trim(), PasswordHash = hash, Role = Role.SuperAdmin };
            db.Users.Add(user); db.Categories.Add(new Category { Name = "Thiết bị chung" }); db.Locations.Add(new Location { Name = "Phòng STEM / Kệ 1" });
            db.Settings.AddRange(new AppSetting { Key = "room", Value = room.Trim() }, new AppSetting { Key = "face.enabled", Value = "false" }, new AppSetting { Key = "llm.enabled", Value = "false" }, new AppSetting { Key = "teacher.issue", Value = "true" });
            await db.SaveChangesAsync(); CurrentActor = new(user.Id,user.Username,user.DisplayName,user.Role); Audit(db,"Setup","System",null); await db.SaveChangesAsync(); await tx.CommitAsync();
        }
        finally { gate.Release(); }
    }
    public async Task<Actor> LoginAsync(string username, string password)
    {
        CurrentActor=null;
        await gate.WaitAsync();
        try
        {
            await using var db = Db(); var user = await db.Users.SingleOrDefaultAsync(x => x.Username == username.Trim().ToLowerInvariant());
            if (user is null) { Passwords.Verify(password, PasswordsDummy); throw new BusinessException("Tài khoản hoặc mật khẩu không đúng."); }
            if (!user.IsActive || user.LockedUntilUtc > DateTime.UtcNow) throw new BusinessException("Tài khoản bị khóa hoặc tạm khóa. Thử lại sau 15 phút.");
            if (!Passwords.Verify(password,user.PasswordHash))
            {
                user.FailedAttempts++; if (user.FailedAttempts >= 5) user.LockedUntilUtc = DateTime.UtcNow.AddMinutes(15);
                db.Audit.Add(new AuditEvent { ActorId = user.Id, Action = "LoginFailed", EntityType = "UserAccount", EntityId = user.Id, CreatedUtc = DateTime.UtcNow, DeviceId = Environment.MachineName });
                await db.SaveChangesAsync(); throw new BusinessException("Tài khoản hoặc mật khẩu không đúng.");
            }
            user.FailedAttempts = 0; user.LockedUntilUtc = null; CurrentActor = new(user.Id,user.Username,user.DisplayName,user.Role);
            Audit(db,"Login","UserAccount",user.Id); await db.SaveChangesAsync(); return CurrentActor;
        }
        finally { gate.Release(); }
    }
    private static readonly string PasswordsDummy = Passwords.Hash("InvalidAccount123!");
    public void Logout() { CurrentActor = null; }
    public Task ChangePasswordAsync(string oldPassword, string newPassword) => WriteAsync(Permission.Read, async db =>
    {
        var user = await db.Users.SingleAsync(x => x.Id == CurrentActor!.Id); Rules.Require(Passwords.Verify(oldPassword,user.PasswordHash), "Mật khẩu hiện tại không đúng.");
        user.PasswordHash = Passwords.Hash(newPassword); Audit(db,"ChangePassword","UserAccount",user.Id);
    });
    public async Task<IReadOnlyList<UserAccount>> UsersAsync()
    {
        await using var db = Db(); await VerifyActorAsync(db,Permission.ManageUsers);
        return await db.Users.Select(x => new UserAccount { Id = x.Id, Username = x.Username, DisplayName = x.DisplayName, Role = x.Role, IsActive = x.IsActive }).ToListAsync();
    }
    public Task SaveUserAsync(long? id, string username, string name, Role role, string? password, bool active) => WriteAsync(Permission.ManageUsers, async db =>
    {
        Rules.Text(username,"Tài khoản",80); Rules.Text(name,"Tên"); Rules.Require(Enum.IsDefined(role), "Vai trò không hợp lệ.");
        var user = id is null ? new UserAccount() : await db.Users.SingleAsync(x => x.Id == id);
        Rules.Require(id != CurrentActor!.Id || (active && role == Role.SuperAdmin), "Không thể tự vô hiệu hóa hoặc hạ quyền tài khoản hiện tại.");
        Rules.Require(user.Role != Role.SuperAdmin || (active && role == Role.SuperAdmin) || await db.Users.CountAsync(x => x.Role == Role.SuperAdmin && x.IsActive && x.Id != id) > 0,"Phải giữ ít nhất một quản trị viên.");
        user.Username = username.Trim().ToLowerInvariant(); user.DisplayName = name.Trim(); user.Role = role; user.IsActive = active;
        if (!string.IsNullOrEmpty(password)) { user.PasswordHash = Passwords.Hash(password); user.FailedAttempts=0; user.LockedUntilUtc=null; }
        Rules.Require(!string.IsNullOrEmpty(user.PasswordHash),"Cần mật khẩu cho tài khoản mới.");
        if (id is null) db.Users.Add(user); await db.SaveChangesAsync(); Audit(db,"SaveUser","UserAccount",user.Id);
    });
    public async Task<IReadOnlyList<Category>> CategoriesAsync() { await using var db=Db(); await VerifyActorAsync(db,Permission.Read); return await db.Categories.AsNoTracking().OrderBy(x=>x.Name).ToListAsync(); }
    public async Task<IReadOnlyList<Location>> LocationsAsync() { await using var db=Db(); await VerifyActorAsync(db,Permission.Read); return await db.Locations.AsNoTracking().OrderBy(x=>x.Name).ToListAsync(); }
    public Task SaveLookupAsync(bool category, long? id, string name) => WriteAsync(Permission.ManageAssets, async db =>
    {
        Rules.Text(name,"Tên");
        if (category) { var c=id is null ? new Category() : await db.Categories.SingleAsync(x=>x.Id==id); c.Name=name.Trim(); if(id is null) db.Categories.Add(c); }
        else { var l=id is null ? new Location() : await db.Locations.SingleAsync(x=>x.Id==id); l.Name=name.Trim(); if(id is null) db.Locations.Add(l); }
        Audit(db,"SaveLookup",category?"Category":"Location",id);
    });
    private static async Task<int> BalanceAsync(LabDbContext db, long assetId) => await db.Ledger.Where(x=>x.AssetId==assetId).SumAsync(x=>(int?)x.QuantityDelta) ?? 0;
    private static async Task<int> OpenQuantityAsync(LabDbContext db, long assetId) => await db.LoanLines.Where(x=>x.AssetId==assetId && !x.IsConsumable).SumAsync(x=>(int?)(x.QuantityIssued-x.QuantityReturned)) ?? 0;
    public async Task<IReadOnlyList<AssetRow>> AssetsAsync(string search = "", bool includeArchived = false)
    {
        await using var db = Db(); await VerifyActorAsync(db,Permission.Read);
        var assets = await db.Assets.AsNoTracking().Where(x=>includeArchived || !x.IsArchived).OrderBy(x=>x.Code).ToListAsync();
        var categories = await db.Categories.ToDictionaryAsync(x=>x.Id,x=>x.Name); var locations = await db.Locations.ToDictionaryAsync(x=>x.Id,x=>x.Name);
        var result = new List<AssetRow>();
        foreach(var a in assets.Where(a=>Matches(search,a.Code,a.Name,categories[a.CategoryId],locations[a.LocationId])))
        {
            int total,available;
            if(a.Kind==AssetKind.Unit) { total=await db.Units.CountAsync(x=>x.AssetId==a.Id && x.Status!=UnitStatus.Retired); available=await db.Units.CountAsync(x=>x.AssetId==a.Id && x.Status==UnitStatus.Available); }
            else { total=await BalanceAsync(db,a.Id); available=total-await OpenQuantityAsync(db,a.Id); }
            result.Add(new(a.Id,a.Code,a.Name,a.Kind,categories[a.CategoryId],locations[a.LocationId],a.UnitOfMeasure,total,available,a.IsArchived));
        }
        return result;
    }
    public async Task<IReadOnlyList<AssetUnit>> UnitsAsync(long assetId) { await using var db=Db(); await VerifyActorAsync(db,Permission.Read); return await db.Units.AsNoTracking().Where(x=>x.AssetId==assetId).ToListAsync(); }
    public async Task<Asset> AssetDetailsAsync(long assetId) { await using var db=Db(); await VerifyActorAsync(db,Permission.Read); return await db.Assets.AsNoTracking().SingleAsync(x=>x.Id==assetId); }
    public Task<long> SaveAssetAsync(long? id, AssetInput input) => WriteAsync(Permission.ManageAssets,db=>SaveAssetCoreAsync(db,id,input));
    private async Task<long> SaveAssetCoreAsync(LabDbContext db,long? id,AssetInput input)
    {
        Rules.Text(input.Name,"Tên thiết bị"); Rules.Text(input.Unit,"Đơn vị",30); Rules.Require(Enum.IsDefined(input.Kind),"Loại thiết bị không hợp lệ.");
        Rules.Require(input.InitialQuantity>=0 && input.InitialQuantity<=10000 && input.MinimumStock>=0,"Số lượng không hợp lệ.");
        Rules.Require(input.ReferencePrice is null or >=0,"Giá tham khảo không được âm.");
        Rules.Require(input.PurchasedUtc is null || (input.PurchasedUtc.Value.Kind==DateTimeKind.Utc && input.PurchasedUtc<=DateTime.UtcNow),"Ngày mua phải là UTC và không ở tương lai.");
        var a=id is null ? new Asset() : await db.Assets.SingleAsync(x=>x.Id==id);
        Rules.Require(id is null || a.Kind==input.Kind,"Không thể đổi loại quản lý của thiết bị đã tạo.");
        a.Code=CleanCode(input.Code); a.Name=input.Name.Trim(); a.Kind=input.Kind; a.CategoryId=input.CategoryId; a.LocationId=input.LocationId;
        a.UnitOfMeasure=input.Unit.Trim(); a.MinimumStock=input.MinimumStock; a.Description=input.Description; a.Manufacturer=input.Manufacturer; a.Model=input.Model; a.ImagePath=input.ImagePath;
        a.PurchasedUtc=input.PurchasedUtc;a.ReferencePrice=input.ReferencePrice;
        if(id is null) db.Assets.Add(a); await db.SaveChangesAsync();
        if(id is null && input.InitialQuantity>0)
        {
            if(a.Kind==AssetKind.Unit) for(int i=0;i<input.InitialQuantity;i++) db.Units.Add(new AssetUnit { AssetId=a.Id, LocationId=a.LocationId, QrValue=Code("UNIT"), Status=UnitStatus.Available });
            else db.Ledger.Add(new InventoryEntry { AssetId=a.Id, QuantityDelta=input.InitialQuantity, Reason="Tồn ban đầu", ReferenceType="Initial", ActorId=CurrentActor!.Id, CreatedUtc=DateTime.UtcNow });
        }
        Audit(db,id is null?"CreateAsset":"UpdateAsset","Asset",a.Id); return a.Id;
    }
    public Task ArchiveAssetAsync(long id) => WriteAsync(Permission.ManageAssets,async db=>
    {
        var a=await db.Assets.SingleAsync(x=>x.Id==id);
        Rules.Require(!await db.LoanLines.AnyAsync(x=>x.AssetId==id && x.QuantityReturned<x.QuantityIssued && !x.IsConsumable),"Thiết bị còn đang mượn.");
        Rules.Require(!await db.Reservations.AnyAsync(r=>r.Status==ReservationStatus.Approved && r.EndUtc>DateTime.UtcNow && r.Lines.Any(l=>l.AssetId==id)),"Thiết bị còn lịch đặt đã duyệt.");
        a.IsArchived=true; Audit(db,"ArchiveAsset","Asset",id);
    });
    public Task AdjustStockAsync(long assetId,int delta,string reason) => WriteAsync(Permission.ManageAssets,async db=>
    {
        Rules.Text(reason,"Lý do",1000); Rules.Require(delta!=0 && Math.Abs((long)delta)<=10000,"Lượng điều chỉnh không hợp lệ."); var a=await db.Assets.SingleAsync(x=>x.Id==assetId);
        Rules.Require(!a.IsArchived,"Thiết bị đã ngừng sử dụng.");
        if(a.Kind==AssetKind.Unit)
        {
            Rules.Require(delta>0,"Giảm thiết bị cá thể qua quy trình ngừng sử dụng từng cá thể.");
            for(int i=0;i<delta;i++) db.Units.Add(new AssetUnit { AssetId=assetId,LocationId=a.LocationId,QrValue=Code("UNIT"),Status=UnitStatus.Available });
        }
        else
        {
            var balance=await BalanceAsync(db,assetId); var outstanding=await OpenQuantityAsync(db,assetId);
            Rules.Require(balance+delta>=outstanding,"Điều chỉnh làm âm kho khả dụng.");
            await EnsureCapacityAsync(db,[new(assetId,null,1)],DateTime.UtcNow,DateTime.UtcNow.AddYears(20),capacityDelta:delta,checkRequested:false);
            db.Ledger.Add(new InventoryEntry { AssetId=assetId,QuantityDelta=delta,Reason=reason,ReferenceType="Adjustment",ActorId=CurrentActor!.Id,CreatedUtc=DateTime.UtcNow });
        }
        Audit(db,"AdjustStock","Asset",assetId,$"Delta={delta}; Reason={reason}");
    });
    public Task MoveUnitAsync(long unitId,long locationId) => WriteAsync(Permission.ManageAssets,async db=> { var u=await db.Units.SingleAsync(x=>x.Id==unitId); Rules.Require(await db.Locations.AnyAsync(x=>x.Id==locationId),"Vị trí không tồn tại."); u.LocationId=locationId; Audit(db,"MoveUnit","AssetUnit",unitId,$"Location={locationId}"); });
    public Task SaveUnitDetailsAsync(long unitId,string? serial,string condition)=>WriteAsync(Permission.ManageAssets,async db=>
    {
        Rules.Text(condition,"Tình trạng",200);Rules.Require(serial is null || serial.Length<=200,"Serial tối đa 200 ký tự.");var unit=await db.Units.SingleAsync(x=>x.Id==unitId);unit.Serial=string.IsNullOrWhiteSpace(serial)?null:serial.Trim();unit.Condition=condition.Trim();Audit(db,"SaveUnitDetails","AssetUnit",unitId);
    });
    public Task RetireUnitAsync(long unitId,string reason) => WriteAsync(Permission.ManageAssets,async db=>
    {
        Rules.Text(reason,"Lý do"); var u=await db.Units.SingleAsync(x=>x.Id==unitId); Rules.Require(u.Status==UnitStatus.Available || u.Status==UnitStatus.Lost,"Chỉ ngừng sử dụng cá thể sẵn sàng hoặc mất.");
        Rules.Require(!await db.Reservations.AnyAsync(r=>r.Status==ReservationStatus.Approved && r.EndUtc>DateTime.UtcNow && r.Lines.Any(l=>l.UnitId==unitId)),"Cá thể có lịch đặt đã duyệt.");
        u.Status=UnitStatus.Retired; Audit(db,"RetireUnit","AssetUnit",unitId,reason);
    });
    public async Task<IReadOnlyList<BorrowerRow>> BorrowersAsync(string search="")
    {
        await using var db=Db(); await VerifyActorAsync(db,Permission.Read); var list=await db.Borrowers.AsNoTracking().OrderBy(x=>x.Code).ToListAsync(); var loans=await db.Loans.AsNoTracking().ToListAsync();
        return list.Where(x=>Matches(search,x.Code,x.DisplayName,x.ClassName)).Select(x=>new BorrowerRow(x.Id,CurrentActor!.Role==Role.Viewer?"Ẩn":x.Code,CurrentActor.Role==Role.Viewer?$"Người mượn #{x.Id}":x.DisplayName,CurrentActor.Role==Role.Viewer?"Ẩn":x.ClassName,x.Type,Access.Allows(CurrentActor.Role,Permission.PersonalData)?x.Contact:null,x.IsActive,loans.Count(l=>l.BorrowerId==x.Id && l.Status==LoanStatus.Open),loans.Count(l=>l.BorrowerId==x.Id && l.DueUtc<DateTime.UtcNow && l.Status==LoanStatus.Open))).ToList();
    }
    public Task<long> SaveBorrowerAsync(long? id,BorrowerInput input) => WriteAsync(Permission.ManageBorrowers,async db=>
    {
        Rules.Text(input.Name,"Họ tên"); var b=id is null?new Borrower():await db.Borrowers.SingleAsync(x=>x.Id==id); b.Code=CleanCode(input.Code); b.DisplayName=input.Name.Trim(); b.ClassName=input.ClassName.Trim(); b.Type=input.Type; b.Contact=input.Contact;
        if(id is null)db.Borrowers.Add(b); await db.SaveChangesAsync(); Audit(db,"SaveBorrower","Borrower",b.Id); return b.Id;
    });
    public Task DeactivateBorrowerAsync(long id) => WriteAsync(Permission.ManageBorrowers,async db=> { var b=await db.Borrowers.SingleAsync(x=>x.Id==id); Rules.Require(!await db.Loans.AnyAsync(x=>x.BorrowerId==id && x.Status==LoanStatus.Open),"Người mượn còn phiếu chưa trả."); b.IsActive=false; Audit(db,"DeactivateBorrower","Borrower",id); });
}
