using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using STEMLabManager.Domain;
namespace STEMLabManager.Infrastructure;

public sealed class LabDbContext(DbContextOptions<LabDbContext> options) : DbContext(options)
{
    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<Borrower> Borrowers => Set<Borrower>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<AssetUnit> Units => Set<AssetUnit>();
    public DbSet<InventoryEntry> Ledger => Set<InventoryEntry>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<LoanLine> LoanLines => Set<LoanLine>();
    public DbSet<ReturnTransaction> Returns => Set<ReturnTransaction>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<MaintenanceCase> Maintenance => Set<MaintenanceCase>();
    public DbSet<InventorySession> Inventory => Set<InventorySession>();
    public DbSet<AuditEvent> Audit => Set<AuditEvent>();
    public DbSet<AppSetting> Settings => Set<AppSetting>();
    public DbSet<Consent> Consents => Set<Consent>();
    public DbSet<FaceProfile> Faces => Set<FaceProfile>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<UserAccount>().HasIndex(x => x.Username).IsUnique();
        b.Entity<Borrower>().HasIndex(x => x.Code).IsUnique();
        b.Entity<Category>().HasIndex(x => x.Name).IsUnique();
        b.Entity<Location>().HasIndex(x => x.Name).IsUnique();
        b.Entity<Asset>().HasIndex(x => x.Code).IsUnique();
        b.Entity<Asset>().HasIndex(x => new { x.CategoryId, x.IsArchived });
        b.Entity<Asset>().HasOne<Category>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Asset>().HasOne<Location>().WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<AssetUnit>().HasIndex(x => x.Serial).IsUnique().HasFilter("\"Serial\" IS NOT NULL");
        b.Entity<AssetUnit>().HasIndex(x => x.QrValue).IsUnique();
        b.Entity<AssetUnit>().HasOne<Asset>().WithMany().HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<AssetUnit>().HasOne<Location>().WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<InventoryEntry>().HasIndex(x => new { x.AssetId, x.CreatedUtc });
        b.Entity<InventoryEntry>().HasOne<Asset>().WithMany().HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<InventoryEntry>().HasOne<UserAccount>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Loan>().HasIndex(x => x.Code).IsUnique();
        b.Entity<Loan>().HasIndex(x => new { x.Status, x.DueUtc });
        b.Entity<Loan>().HasOne<Borrower>().WithMany().HasForeignKey(x => x.BorrowerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Loan>().HasOne<UserAccount>().WithMany().HasForeignKey(x => x.IssuedById).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Loan>().HasOne<Reservation>().WithMany().HasForeignKey(x => x.ReservationId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Loan>().HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.LoanId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<LoanLine>().HasIndex(x => x.UnitId).IsUnique().HasFilter("\"UnitId\" IS NOT NULL AND \"QuantityReturned\" < \"QuantityIssued\"");
        b.Entity<LoanLine>().HasOne<Asset>().WithMany().HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<LoanLine>().HasOne<AssetUnit>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<LoanLine>().ToTable(t => t.HasCheckConstraint("CK_LoanQuantity", "\"QuantityIssued\" > 0 AND \"QuantityReturned\" >= 0 AND \"QuantityReturned\" <= \"QuantityIssued\""));
        b.Entity<ReturnTransaction>().HasOne<Loan>().WithMany().HasForeignKey(x => x.LoanId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ReturnTransaction>().HasOne<UserAccount>().WithMany().HasForeignKey(x => x.ReceivedById).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ReturnTransaction>().HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.ReturnTransactionId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ReturnLine>().HasOne<LoanLine>().WithMany().HasForeignKey(x => x.LoanLineId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ReturnLine>().ToTable(t => t.HasCheckConstraint("CK_ReturnQuantity", "\"Quantity\" > 0"));
        b.Entity<Reservation>().HasIndex(x => x.Code).IsUnique();
        b.Entity<Reservation>().HasIndex(x => new { x.Status, x.StartUtc, x.EndUtc });
        b.Entity<Reservation>().HasOne<Borrower>().WithMany().HasForeignKey(x => x.BorrowerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Reservation>().HasOne<UserAccount>().WithMany().HasForeignKey(x => x.RequestedById).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Reservation>().HasOne<UserAccount>().WithMany().HasForeignKey(x => x.ApprovedById).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Reservation>().HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.ReservationId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Reservation>().ToTable(t => t.HasCheckConstraint("CK_ReservationInterval", "\"StartUtc\" < \"EndUtc\""));
        b.Entity<ReservationLine>().HasOne<Asset>().WithMany().HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ReservationLine>().HasOne<AssetUnit>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ReservationLine>().ToTable(t => t.HasCheckConstraint("CK_ReservationQuantity", "\"Quantity\" > 0"));
        b.Entity<MaintenanceCase>().HasOne<AssetUnit>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<MaintenanceCase>().HasOne<UserAccount>().WithMany().HasForeignKey(x => x.OpenedById).OnDelete(DeleteBehavior.Restrict);
        b.Entity<InventorySession>().HasOne<Location>().WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<InventorySession>().HasOne<UserAccount>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<InventorySession>().HasMany(x => x.Counts).WithOne().HasForeignKey(x => x.InventorySessionId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<InventorySession>().HasMany(x=>x.Units).WithOne().HasForeignKey(x=>x.InventorySessionId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<InventoryUnitCount>().HasOne<AssetUnit>().WithMany().HasForeignKey(x=>x.UnitId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<InventoryUnitCount>().HasIndex(x=>new {x.InventorySessionId,x.UnitId}).IsUnique();
        b.Entity<InventoryCount>().HasOne<Asset>().WithMany().HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<AuditEvent>().HasIndex(x => x.CreatedUtc);
        b.Entity<AuditEvent>().HasOne<UserAccount>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<AppSetting>().HasIndex(x => x.Key).IsUnique();
        b.Entity<Consent>().HasOne<Borrower>().WithMany().HasForeignKey(x => x.BorrowerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<FaceProfile>().HasIndex(x => x.BorrowerId).IsUnique();
        b.Entity<FaceProfile>().HasOne<Borrower>().WithMany().HasForeignKey(x => x.BorrowerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<FaceProfile>().HasOne<Consent>().WithMany().HasForeignKey(x => x.ConsentId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<FaceProfile>().HasIndex(x=>x.ModelVersion);
        b.Entity<Consent>().HasIndex(x=>new {x.BorrowerId,x.RevokedUtc});
        foreach(var entity in b.Model.GetEntityTypes())
        foreach(var property in entity.GetProperties())
        {
            if(property.ClrType==typeof(DateTime))property.SetValueConverter(new ValueConverter<DateTime,DateTime>(v=>v,v=>DateTime.SpecifyKind(v,DateTimeKind.Utc)));
            if(property.ClrType==typeof(DateTime?))property.SetValueConverter(new ValueConverter<DateTime?,DateTime?>(v=>v,v=>v.HasValue?DateTime.SpecifyKind(v.Value,DateTimeKind.Utc):null));
        }
    }
}
