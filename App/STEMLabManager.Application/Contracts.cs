using STEMLabManager.Domain;
namespace STEMLabManager.Application;

public sealed record AssetRow(long Id, string Code, string Name, AssetKind Kind, string Category, string Location, string Unit, int Total, int Available, bool Archived, string? ImagePath = null);
public sealed record BorrowerRow(long Id, string Code, string Name, string ClassName, string Type, string? Contact, bool Active, int OpenLoans, int OverdueLoans);
public sealed record LoanRow(long Id, string Code, string Borrower, string Staff, DateTime IssuedUtc, DateTime DueUtc, LoanStatus Status, string Purpose, IReadOnlyList<LoanLineRow> Lines, long? BorrowerId = null)
{
    public bool Overdue => Status == LoanStatus.Open && DueUtc < DateTime.UtcNow;
    public string Summary => string.Join("; ", Lines.Select(l => l.Consumable?$"{l.Asset}: {l.QuantityIssued} tiêu hao":$"{l.Asset}: {l.QuantityIssued} mượn, {l.QuantityReturned} đã trả"));
}
public sealed record LoanLineRow(long Id, long AssetId, long? UnitId, string Asset, int QuantityIssued, int QuantityReturned, bool Consumable);
public sealed record ReservationRow(long Id, string Code, string Borrower, DateTime StartUtc, DateTime EndUtc, ReservationStatus Status, string Note, IReadOnlyList<ReservationLine> Lines, long BorrowerId);
public sealed record Analytics(DateTime StartUtc, DateTime EndUtc, int Available, int OpenLoans, int Overdue, int TodayReservations, int TotalLoans, int UniqueBorrowers, double OverdueRate, IReadOnlyList<MetricPoint> Daily, IReadOnlyList<MetricPoint> TopAssets, IReadOnlyList<string> Notifications, double Utilization = 0, int UnfulfilledRequests = 0);
public sealed record MetricPoint(string Label, double Value);
public sealed record AssetInput(string Code, string Name, AssetKind Kind, long CategoryId, long LocationId, string Unit, int InitialQuantity = 0, int MinimumStock = 0, string Description = "", string Manufacturer = "", string Model = "", string? ImagePath = null, DateTime? PurchasedUtc = null, decimal? ReferencePrice = null);
public sealed record BorrowerInput(string Code, string Name, string ClassName, string Type = "Người mượn", string? Contact = null);
public sealed record IssueLine(long AssetId, long? UnitId, int Quantity);
public sealed record IssueRequest(long BorrowerId, DateTime DueUtc, string Purpose, IReadOnlyList<IssueLine> Lines, long? ReservationId = null);
public sealed record ReturnInput(long LoanLineId, int Quantity, ReturnCondition Condition);
public sealed record ReservationInput(long BorrowerId, DateTime StartUtc, DateTime EndUtc, IReadOnlyList<IssueLine> Lines, string Note = "");
public sealed record ImportPreview(IReadOnlyList<BorrowerInput> Rows, IReadOnlyList<string> Errors);
public sealed record AssetImportPreview(IReadOnlyList<AssetInput> Rows, IReadOnlyList<string> Errors);
public sealed record ModelInfo(string Path, string Sha256, bool Loaded, string Status, IReadOnlyList<string> Inputs, IReadOnlyList<string> Outputs);
public interface IExternalInsights
{
    Task<string> AnalyzeAsync(Analytics analytics,string endpoint,string model,string key,CancellationToken cancellation=default);
}
public interface ILabService
{
    Actor? CurrentActor { get; }
    string DataDirectory { get; }
    bool IsDemo {get;}
    Task InitializeAsync();
    Task<bool> NeedsSetupAsync();
    Task SetupAsync(string username, string password, string displayName, string room);
    Task<Actor> LoginAsync(string username, string password);
    void Logout();
    Task ChangePasswordAsync(string oldPassword, string newPassword);
    Task<IReadOnlyList<UserAccount>> UsersAsync();
    Task SaveUserAsync(long? id, string username, string name, Role role, string? password, bool active);
    Task<IReadOnlyList<Category>> CategoriesAsync();
    Task<IReadOnlyList<Location>> LocationsAsync();
    Task SaveLookupAsync(bool category, long? id, string name);
    Task<IReadOnlyList<AssetRow>> AssetsAsync(string search = "", bool includeArchived = false);
    Task<IReadOnlyList<AssetUnit>> UnitsAsync(long assetId);
    Task<Asset> AssetDetailsAsync(long assetId);
    Task<long> SaveAssetAsync(long? id, AssetInput input);
    Task ArchiveAssetAsync(long id);
    Task ImportAssetImageAsync(long id, string sourcePath);
    Task AdjustStockAsync(long assetId, int delta, string reason);
    Task MoveUnitAsync(long unitId, long locationId);
    Task SaveUnitDetailsAsync(long unitId, string? serial, string condition);
    Task RetireUnitAsync(long unitId, string reason);
    Task<IReadOnlyList<BorrowerRow>> BorrowersAsync(string search = "");
    Task<long> SaveBorrowerAsync(long? id, BorrowerInput input);
    Task DeactivateBorrowerAsync(long id);
    Task<IReadOnlyList<LoanRow>> LoansAsync(bool history = false);
    Task<long> IssueAsync(IssueRequest request);
    Task ReturnAsync(long loanId, IReadOnlyList<ReturnInput> lines, string note);
    Task ExtendAsync(long loanId, DateTime dueUtc, string reason);
    Task<IReadOnlyList<ReservationRow>> ReservationsAsync();
    Task<long> ReserveAsync(ReservationInput request);
    Task SetReservationStatusAsync(long id, ReservationStatus status, string reason = "");
    Task<IReadOnlyList<MaintenanceCase>> MaintenanceAsync();
    Task OpenMaintenanceAsync(long unitId, string reason);
    Task CloseMaintenanceAsync(long id, string resolution, decimal cost);
    Task<long> CreateInventoryAsync(long locationId, IReadOnlyDictionary<long, int> counts, string note);
    Task<long> CreateUnitInventoryAsync(long locationId, IReadOnlySet<string> scannedQrValues, string note);
    Task<IReadOnlyList<InventorySession>> InventoryAsync();
    Task ConfirmInventoryAsync(long id);
    Task<Analytics> AnalyticsAsync(DateTime startUtc, DateTime endUtc);
    Task<IReadOnlyList<AuditEvent>> AuditAsync();
    Task<IReadOnlyDictionary<string, string>> SettingsAsync();
    Task SaveSettingAsync(string key, string value);
    Task<string?> ReadLlmKeyAsync();
    Task<string> AnalyzeLlmAsync(DateTime startUtc,DateTime endUtc,CancellationToken cancellation=default);
    Task<string> BackupAsync(string? directory = null);
    Task RunScheduledBackupAsync();
    Task RestoreAsync(string path);
    Task<IReadOnlyList<string>> BackupsAsync();
    Task SeedDemoAsync();
    Task<ImportPreview> PreviewBorrowersAsync(string path, IReadOnlyDictionary<string, string>? mapping = null);
    Task ImportBorrowersAsync(ImportPreview preview);
    Task<AssetImportPreview> PreviewAssetsAsync(string path, IReadOnlyDictionary<string, string>? mapping = null);
    Task ImportAssetsAsync(AssetImportPreview preview);
    Task<string> ExportAsync(string report, string path, DateTime startUtc, DateTime endUtc);
    Task<string> ExportQrAsync(string text, string path);
    Task<long> GrantConsentAsync(long borrowerId, string version, string basis);
    Task EnrollFaceAsync(long consentId, float[] embedding, string modelVersion);
    Task<IReadOnlyList<(long BorrowerId, float[] Embedding)>> FaceProfilesAsync(string modelVersion);
    Task RevokeConsentAsync(long borrowerId);
}
