namespace STEMLabManager.Domain;

public sealed class BusinessException(string message) : Exception(message);
public sealed record Actor(long Id, string Username, string DisplayName, Role Role);
public static class Access
{
    public static bool Allows(Role role, Permission p) => role == Role.SuperAdmin || p switch
    {
        Permission.Read or Permission.Reports => true,
        Permission.ManageAssets or Permission.ManageBorrowers or Permission.ApproveReservation or Permission.PersonalData or Permission.Audit or Permission.Biometrics => role == Role.LabManager,
        Permission.RequestReservation or Permission.IssueLoan or Permission.ReturnLoan => role is Role.LabManager or Role.Teacher,
        _ => false
    };
    public static void Require(Actor? actor, Permission permission)
    {
        if (actor is null || !Allows(actor.Role, permission)) throw new BusinessException("Bạn không có quyền thực hiện thao tác này.");
    }
}
public static class Rules
{
    public static void Require(bool condition, string message) { if (!condition) throw new BusinessException(message); }
    public static void Text(string value, string label, int max = 200) => Require(!string.IsNullOrWhiteSpace(value) && value.Trim().Length <= max, $"{label} bắt buộc, tối đa {max} ký tự.");
    public static void Interval(DateTime start, DateTime end) => Require(start.Kind == DateTimeKind.Utc && end.Kind == DateTimeKind.Utc && start < end, "Khoảng thời gian không hợp lệ (UTC, bắt đầu trước kết thúc).");
    public static bool Overlaps(DateTime a, DateTime b, DateTime c, DateTime d) => a < d && c < b;
}
