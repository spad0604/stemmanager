namespace STEMLabManager.Domain;

public enum Role { SuperAdmin, LabManager, Teacher, Viewer }
public enum AssetKind { Unit, Bulk, Consumable }
public enum UnitStatus { Available, Reserved, CheckedOut, Maintenance, Lost, Retired }
public enum ReservationStatus { Draft, Pending, Approved, Rejected, Cancelled, Fulfilled, Expired }
public enum LoanStatus { Open, Closed }
public enum ReturnCondition { Good, Damaged, Lost }
public enum Permission { Read, ManageAssets, ManageBorrowers, RequestReservation, ApproveReservation, IssueLoan, ReturnLoan, Reports, PersonalData, ManageUsers, Backup, Audit, Biometrics, Settings }
public abstract class Entity { public long Id { get; set; } }
public sealed class UserAccount : Entity
{
    public string Username { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public Role Role { get; set; }
    public bool IsActive { get; set; } = true;
    public int FailedAttempts { get; set; }
    public DateTime? LockedUntilUtc { get; set; }
}
public sealed class Borrower : Entity
{
    public string Code { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string ClassName { get; set; } = "";
    public string Type { get; set; } = "Học sinh";
    public string? Contact { get; set; }
    public bool IsActive { get; set; } = true;
}
public sealed class Category : Entity { public string Name { get; set; } = ""; }
public sealed class Location : Entity { public string Name { get; set; } = ""; }
public sealed class Asset : Entity
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public AssetKind Kind { get; set; }
    public long CategoryId { get; set; }
    public long LocationId { get; set; }
    public string UnitOfMeasure { get; set; } = "cái";
    public string Description { get; set; } = "";
    public string Manufacturer { get; set; } = "";
    public string Model { get; set; } = "";
    public string? ImagePath { get; set; }
    public DateTime? PurchasedUtc { get; set; }
    public decimal? ReferencePrice { get; set; }
    public int MinimumStock { get; set; }
    public bool IsArchived { get; set; }
}
public sealed class AssetUnit : Entity
{
    public long AssetId { get; set; }
    public string? Serial { get; set; }
    public string QrValue { get; set; } = "";
    public long LocationId { get; set; }
    public UnitStatus Status { get; set; }
    public string Condition { get; set; } = "Tốt";
}
public sealed class InventoryEntry : Entity
{
    public long AssetId { get; set; }
    public int QuantityDelta { get; set; }
    public string Reason { get; set; } = "";
    public string ReferenceType { get; set; } = "";
    public long? ReferenceId { get; set; }
    public long ActorId { get; set; }
    public DateTime CreatedUtc { get; set; }
}
public sealed class Loan : Entity
{
    public string Code { get; set; } = "";
    public long BorrowerId { get; set; }
    public long IssuedById { get; set; }
    public DateTime IssuedUtc { get; set; }
    public DateTime DueUtc { get; set; }
    public LoanStatus Status { get; set; }
    public string Purpose { get; set; } = "";
    public long? ReservationId { get; set; }
    public List<LoanLine> Lines { get; set; } = [];
}
public sealed class LoanLine : Entity
{
    public long LoanId { get; set; }
    public long AssetId { get; set; }
    public long? UnitId { get; set; }
    public int QuantityIssued { get; set; }
    public int QuantityReturned { get; set; }
    public bool IsConsumable { get; set; }
}
public sealed class ReturnTransaction : Entity
{
    public long LoanId { get; set; }
    public long ReceivedById { get; set; }
    public DateTime CreatedUtc { get; set; }
    public string Note { get; set; } = "";
    public List<ReturnLine> Lines { get; set; } = [];
}
public sealed class ReturnLine : Entity
{
    public long ReturnTransactionId { get; set; }
    public long LoanLineId { get; set; }
    public int Quantity { get; set; }
    public ReturnCondition Condition { get; set; }
}
public sealed class Reservation : Entity
{
    public string Code { get; set; } = "";
    public long BorrowerId { get; set; }
    public long RequestedById { get; set; }
    public long? ApprovedById { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public ReservationStatus Status { get; set; }
    public string Note { get; set; } = "";
    public List<ReservationLine> Lines { get; set; } = [];
}
public sealed class ReservationLine : Entity
{
    public long ReservationId { get; set; }
    public long AssetId { get; set; }
    public long? UnitId { get; set; }
    public int Quantity { get; set; }
}
public sealed class MaintenanceCase : Entity
{
    public long UnitId { get; set; }
    public long OpenedById { get; set; }
    public DateTime OpenedUtc { get; set; }
    public DateTime? ClosedUtc { get; set; }
    public string Reason { get; set; } = "";
    public string Resolution { get; set; } = "";
    public decimal Cost { get; set; }
}
public sealed class InventorySession : Entity
{
    public long LocationId { get; set; }
    public long ActorId { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? ConfirmedUtc { get; set; }
    public string Note { get; set; } = "";
    public List<InventoryCount> Counts { get; set; } = [];
    public List<InventoryUnitCount> Units { get; set; } = [];
}
public sealed class InventoryUnitCount : Entity
{
    public long InventorySessionId { get; set; }
    public long UnitId { get; set; }
    public UnitStatus ExpectedStatus { get; set; }
    public bool Present { get; set; }
}
public sealed class InventoryCount : Entity
{
    public long InventorySessionId { get; set; }
    public long AssetId { get; set; }
    public int Expected { get; set; }
    public int Actual { get; set; }
}
public sealed class AuditEvent : Entity
{
    public long? ActorId { get; set; }
    public string Action { get; set; } = "";
    public string EntityType { get; set; } = "";
    public long? EntityId { get; set; }
    public string Details { get; set; } = "";
    public DateTime CreatedUtc { get; set; }
    public string DeviceId { get; set; } = "";
}
public sealed class AppSetting : Entity { public string Key { get; set; } = ""; public string Value { get; set; } = ""; }
public sealed class Consent : Entity
{
    public long BorrowerId { get; set; }
    public string Version { get; set; } = "";
    public string Basis { get; set; } = "";
    public DateTime CreatedUtc { get; set; }
    public DateTime? RevokedUtc { get; set; }
}
public sealed class FaceProfile : Entity
{
    public long BorrowerId { get; set; }
    public long ConsentId { get; set; }
    public byte[] EncryptedEmbedding { get; set; } = [];
    public string ModelVersion { get; set; } = "";
    public DateTime CreatedUtc { get; set; }
}
