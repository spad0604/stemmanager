using System.IO.Compression;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using STEMLabManager.Domain;
namespace STEMLabManager.Infrastructure;

public sealed partial class LabService
{
    public Task<string> BackupAsync(string? directory=null)=>BackupCoreAsync(directory,false);
    public async Task RunScheduledBackupAsync()
    {
        await using(var db=Db())
        {
            await VerifyActorAsync(db,Permission.Read);var last=(await db.Settings.SingleOrDefaultAsync(x=>x.Key=="backup.lastUtc"))?.Value;
            if(DateTime.TryParse(last,out var time) && time.ToUniversalTime().Date>=DateTime.UtcNow.Date)return;
        }
        await BackupCoreAsync(null,true);
    }
    private Task<string> BackupCoreAsync(string? directory,bool scheduled)=>Task.Run(()=>BackupWorkerAsync(directory,scheduled));
    private async Task<string> BackupWorkerAsync(string? directory,bool scheduled)
    {
        var permission=scheduled?Permission.Read:Permission.Backup;Require(permission); await gate.WaitAsync();
        string? temporary=null;
        try
        {
            await using var db=Db();await VerifyActorAsync(db,permission);
            var target=directory??Path.Combine(DataDirectory,"Backups"); Directory.CreateDirectory(target);
            var filename=Path.Combine(target,$"STEM-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N")[..6]}.zip");
            temporary=Path.Combine(DataDirectory,$"backup-{Guid.NewGuid():N}.db");
            await db.Database.OpenConnectionAsync();
            using(var destination=new SqliteConnection($"Data Source={temporary};Pooling=False")) { await destination.OpenAsync(); ((SqliteConnection)db.Database.GetDbConnection()).BackupDatabase(destination); }
            using(var archive=ZipFile.Open(filename,ZipArchiveMode.Create))
            {
                archive.CreateEntryFromFile(temporary,"stem.db");
                var key=Path.Combine(DataDirectory,"secrets.key"); if(File.Exists(key))archive.CreateEntryFromFile(key,"secrets.key");
                var imageDirectory=Path.Combine(DataDirectory,"Images");if(Directory.Exists(imageDirectory))foreach(var image in Directory.GetFiles(imageDirectory,"*.png"))archive.CreateEntryFromFile(image,"Images/"+Path.GetFileName(image));
                using var writer=new StreamWriter(archive.CreateEntry("manifest.json").Open());await writer.WriteAsync(JsonSerializer.Serialize(new { format=1, createdUtc=DateTime.UtcNow, schema=(await db.Database.GetAppliedMigrationsAsync()).LastOrDefault(), includesEncryptionKey=File.Exists(key) }));
            }
            if(!OperatingSystem.IsWindows())File.SetUnixFileMode(filename,UnixFileMode.UserRead|UnixFileMode.UserWrite);
            var setting=await db.Settings.SingleOrDefaultAsync(x=>x.Key=="backup.lastUtc");if(setting==null){setting=new AppSetting {Key="backup.lastUtc"};db.Settings.Add(setting);}setting.Value=DateTime.UtcNow.ToString("O");
            if(directory!=null){var external=await db.Settings.SingleOrDefaultAsync(x=>x.Key=="backup.lastExternalUtc");if(external==null){external=new AppSetting {Key="backup.lastExternalUtc"};db.Settings.Add(external);}external.Value=DateTime.UtcNow.ToString("O");}
            if(scheduled)db.Audit.Add(new AuditEvent {Action="ScheduledBackup",EntityType="System",CreatedUtc=DateTime.UtcNow,DeviceId=Environment.MachineName});else Audit(db,"Backup","System",null);await db.SaveChangesAsync();
            // Retain the latest 14 automatic backups; move older archives into a recoverable folder.
            if(directory==null)
            foreach(var old in Directory.GetFiles(target,"STEM-*.zip").OrderByDescending(File.GetLastWriteTimeUtc).Skip(14))
            { var retired=Path.Combine(target,"Retired");Directory.CreateDirectory(retired);File.Move(old,Path.Combine(retired,Path.GetFileName(old)),true); }
            return filename;
        }
        finally { if(temporary!=null && File.Exists(temporary))File.Delete(temporary); gate.Release(); }
    }
    public async Task<IReadOnlyList<string>> BackupsAsync()
    {
        await using var db=Db();await VerifyActorAsync(db,Permission.Backup);var path=Path.Combine(DataDirectory,"Backups");return Directory.Exists(path)?Directory.GetFiles(path,"STEM-*.zip").OrderByDescending(File.GetLastWriteTimeUtc).ToList():[];
    }
    public Task RestoreAsync(string path)=>Task.Run(()=>RestoreCoreAsync(path));
    private async Task RestoreCoreAsync(string path)
    {
        Require(Permission.Backup);await gate.WaitAsync();
        var temporary=Path.Combine(DataDirectory,$"restore-{Guid.NewGuid():N}.db");
        try
        {
            await using(var db=Db()){await VerifyActorAsync(db,Permission.Backup);}
            using var zip=ZipFile.OpenRead(path);
            Rules.Require(zip.Entries.Sum(e=>e.Length)<2L*1024*1024*1024,"Backup quá lớn.");
            var manifest=zip.GetEntry("manifest.json");Rules.Require(manifest!=null,"Không có manifest backup.");
            using(var reader=new StreamReader(manifest!.Open())){using var json=JsonDocument.Parse(await reader.ReadToEndAsync());Rules.Require(json.RootElement.GetProperty("format").GetInt32()==1,"Định dạng backup không hỗ trợ.");}
            var entry=zip.GetEntry("stem.db");Rules.Require(entry!=null && entry.Length<2L*1024*1024*1024,"Backup thiếu DB hoặc quá lớn.");entry!.ExtractToFile(temporary);
            byte[]? key=null; if(zip.GetEntry("secrets.key") is { } keyEntry){Rules.Require(keyEntry.Length==32,"Khóa mã hóa không hợp lệ.");using var stream=keyEntry.Open();using var ms=new MemoryStream();await stream.CopyToAsync(ms);key=ms.ToArray();Rules.Require(key.Length==32,"Khóa mã hóa không hợp lệ.");}
            using(var check=new SqliteConnection($"Data Source={temporary};Pooling=False"))
            {
                await check.OpenAsync();using var command=check.CreateCommand();command.CommandText="PRAGMA integrity_check;";Rules.Require((string?)await command.ExecuteScalarAsync()=="ok","Backup hỏng: integrity check thất bại.");
                command.CommandText="PRAGMA foreign_key_check;";using(var reader=await command.ExecuteReaderAsync())Rules.Require(!await reader.ReadAsync(),"Backup có khóa ngoại lỗi.");
                command.CommandText="SELECT COUNT(*) FROM Users WHERE IsActive = 1 AND Role = 0";Rules.Require(Convert.ToInt32(await command.ExecuteScalarAsync())>0,"Backup không có quản trị viên hoạt động.");
                command.CommandText="SELECT COUNT(*) FROM Faces";var faces=Convert.ToInt32(await command.ExecuteScalarAsync());
                command.CommandText="SELECT COUNT(*) FROM Settings WHERE Key LIKE '%secret%' AND Value <> ''";Rules.Require((faces==0 && Convert.ToInt32(await command.ExecuteScalarAsync())==0)||key!=null,"Backup thiếu khóa giải mã.");
            }
            // Migrate and validate candidate before touching the active database.
            await using(var candidate=new LabDbContext(new DbContextOptionsBuilder<LabDbContext>().UseSqlite($"Data Source={temporary};Foreign Keys=True;Pooling=False").Options))
            {
                var known=candidate.Database.GetMigrations().ToHashSet();var applied=await candidate.Database.GetAppliedMigrationsAsync();Rules.Require(applied.All(known.Contains),"Backup dùng schema mới hơn ứng dụng.");
                await candidate.Database.MigrateAsync(); _=await candidate.Assets.CountAsync();
                foreach(var image in await candidate.Assets.Where(x=>x.ImagePath!=null).Select(x=>x.ImagePath!).ToListAsync())
                    Rules.Require(System.Text.RegularExpressions.Regex.IsMatch(image,@"^Images/[a-f0-9]{32}\.png$") && zip.GetEntry(image)!=null,"Backup thiếu ảnh hoặc có đường dẫn ảnh không hợp lệ.");
                var faceEnabled=await candidate.Settings.SingleOrDefaultAsync(x=>x.Key=="face.enabled");if(faceEnabled!=null)faceEnabled.Value="false";
                candidate.Audit.Add(new AuditEvent {Action="Restore",EntityType="System",CreatedUtc=DateTime.UtcNow,DeviceId=Environment.MachineName});await candidate.SaveChangesAsync();await candidate.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(TRUNCATE);");
            }
            var rollback=Path.Combine(DataDirectory,$"before-restore-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}.db");
            using(var source=new SqliteConnection($"Data Source={databasePath};Pooling=False"))
            using(var destination=new SqliteConnection($"Data Source={rollback};Pooling=False"))
            {await source.OpenAsync();await destination.OpenAsync();source.BackupDatabase(destination);using var cmd=source.CreateCommand();cmd.CommandText="PRAGMA wal_checkpoint(TRUNCATE);";await cmd.ExecuteNonQueryAsync();}
            var keyPath=Path.Combine(DataDirectory,"secrets.key");var oldKey=File.Exists(keyPath)?File.ReadAllBytes(keyPath):null;
            try
            {
                // No pooled connections survive this point; replacement is local and recoverable.
                foreach(var suffix in new[]{"-wal","-shm"})if(File.Exists(databasePath+suffix))File.Delete(databasePath+suffix);
                if(key!=null)File.WriteAllBytes(keyPath,key);
                if(File.Exists(keyPath) && !OperatingSystem.IsWindows())File.SetUnixFileMode(keyPath,UnixFileMode.UserRead|UnixFileMode.UserWrite);
                foreach(var image in zip.Entries.Where(e=>e.FullName.StartsWith("Images/",StringComparison.Ordinal)))
                {
                    Rules.Require(System.Text.RegularExpressions.Regex.IsMatch(image.FullName,@"^Images/[a-f0-9]{32}\.png$") && image.Length<=10*1024*1024,"Ảnh backup không hợp lệ.");
                    var imageDirectory=Path.Combine(DataDirectory,"Images");Directory.CreateDirectory(imageDirectory);image.ExtractToFile(Path.Combine(imageDirectory,Path.GetFileName(image.FullName)),true);
                }
                File.Move(temporary,databasePath,true);CurrentActor=null;
            }
            catch {File.Copy(rollback,databasePath,true);if(oldKey!=null)File.WriteAllBytes(keyPath,oldKey);throw;}
        }
        catch(Exception e) when(e is InvalidDataException or SqliteException or JsonException){throw new BusinessException($"Không thể phục hồi backup: {e.Message}");}
        finally {if(File.Exists(temporary))File.Delete(temporary);gate.Release();}
    }
    public Task<long> GrantConsentAsync(long borrowerId,string version,string basis) => WriteAsync(Permission.Biometrics,async db=>
    {
        Rules.Text(version,"Phiên bản consent");Rules.Text(basis,"Căn cứ/sự đồng ý",1000);Rules.Require(await db.Borrowers.AnyAsync(x=>x.Id==borrowerId && x.IsActive),"Người mượn không hoạt động.");
        var consent=new Consent {BorrowerId=borrowerId,Version=version,Basis=basis,CreatedUtc=DateTime.UtcNow};db.Consents.Add(consent);await db.SaveChangesAsync();Audit(db,"GrantConsent","Consent",consent.Id);return consent.Id;
    });
    public Task EnrollFaceAsync(long consentId,float[] embedding,string modelVersion) => WriteAsync(Permission.Biometrics,async db=>
    {
        Rules.Require((await db.Settings.SingleOrDefaultAsync(x=>x.Key=="face.enabled"))?.Value=="true","Nhận diện khuôn mặt đang tắt.");
        Rules.Require(embedding.Length>=64 && embedding.Length<=4096 && embedding.All(float.IsFinite) && embedding.Any(x=>x!=0),"Embedding không hợp lệ.");Rules.Text(modelVersion,"Phiên bản model");
        var consent=await db.Consents.SingleAsync(x=>x.Id==consentId);Rules.Require(consent.RevokedUtc==null,"Consent đã thu hồi.");
        var bytes=new byte[embedding.Length*sizeof(float)];Buffer.BlockCopy(embedding,0,bytes,0,bytes.Length);
        var face=await db.Faces.SingleOrDefaultAsync(x=>x.BorrowerId==consent.BorrowerId);
        if(face==null){face=new FaceProfile {BorrowerId=consent.BorrowerId};db.Faces.Add(face);}
        face.ConsentId=consent.Id;face.EncryptedEmbedding=secrets.Encrypt(bytes);face.ModelVersion=modelVersion;face.CreatedUtc=DateTime.UtcNow;Array.Clear(bytes);
        Audit(db,"EnrollFace","Consent",consentId);
    });
    public async Task<IReadOnlyList<(long BorrowerId,float[] Embedding)>> FaceProfilesAsync(string modelVersion)
    {
        await using var db=Db();await VerifyActorAsync(db,Permission.Biometrics);Rules.Require((await db.Settings.SingleOrDefaultAsync(x=>x.Key=="face.enabled"))?.Value=="true","Nhận diện khuôn mặt đang tắt.");
        var profiles=await db.Faces.AsNoTracking().Where(x=>x.ModelVersion==modelVersion && db.Consents.Any(c=>c.Id==x.ConsentId && c.RevokedUtc==null)).ToListAsync();var result=new List<(long,float[])>();
        foreach(var p in profiles){var bytes=secrets.Decrypt(p.EncryptedEmbedding);var vector=new float[bytes.Length/4];Buffer.BlockCopy(bytes,0,vector,0,bytes.Length);Array.Clear(bytes);result.Add((p.BorrowerId,vector));}
        Audit(db,"ReadFaceProfiles","FaceProfile",null,$"Count={profiles.Count}");await db.SaveChangesAsync();return result;
    }
    public Task RevokeConsentAsync(long borrowerId) => WriteAsync(Permission.Biometrics,async db=>
    {
        var list=await db.Consents.Where(x=>x.BorrowerId==borrowerId && x.RevokedUtc==null).ToListAsync();foreach(var c in list)c.RevokedUtc=DateTime.UtcNow;
        db.Faces.RemoveRange(await db.Faces.Where(x=>x.BorrowerId==borrowerId).ToListAsync());Audit(db,"RevokeConsentDeleteEmbedding","Borrower",borrowerId);
    });
}
