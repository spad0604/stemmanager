using STEMLabManager.Application;
using STEMLabManager.Domain;
namespace STEMLabManager.Desktop.ViewModels;

public static class Visuals
{
    public static string PageIcon(string key)=>key switch
    {
        "dashboard"=>"dashboard","assets" or "units"=>"box","catalog"=>"layers","inventory"=>"clipboard","maintenance"=>"tool",
        "loans" or "history"=>"arrows","reservations"=>"calendar","overdue"=>"clock","borrowers"=>"users","users"=>"shield",
        "analytics"=>"chart","face"=>"scan","reports"=>"report","settings"=>"settings","backup"=>"database","audit"=>"history",_=>"help"
    };
    public static string ActionIcon(string label)=>label switch
    {
        var s when s.Contains("QR")=>"qr",
        var s when s.Contains("Import")=>"upload",
        var s when s.Contains("Xuất")=>"download",
        var s when s.Contains("Thêm") || s.Contains("Tạo") || s.Contains("Đặt trước")=>"plus",
        var s when s.Contains("mật khẩu")=>"lock",
        var s when s.Contains("Sửa") || s.Contains("Serial")=>"edit",
        var s when s.Contains("Duyệt") || s.Contains("Xác nhận") || s.Contains("Hoàn tất")=>"check",
        var s when s.Contains("Hủy") || s.Contains("Từ chối") || s.Contains("xóa") || s.Contains("Ngừng")=>"close",
        var s when s.Contains("Phân tích") || s.Contains("AI")=>"sparkles",
        var s when s.Contains("Sao lưu") || s.Contains("sao lưu")=>"database",
        var s when s.Contains("Báo hỏng")=>"tool",
        var s when s.Contains("camera")=>"camera",
        var s when s.Contains("model") || s.Contains("ONNX")=>"scan",
        var s when s.Contains("Ảnh")=>"image",
        var s when s.Contains("vị trí")=>"location",
        var s when s.Contains("Cấu hình")=>"settings",
        var s when s.Contains("Giao diện")=>"moon",
        var s when s.Contains("kiểm kê") || s.Contains("Kiểm kê")=>"clipboard",
        var s when s.Contains("Gia hạn")=>"clock",
        var s when s.Contains("Lịch sử")=>"history",
        var s when s.Contains("Về")=>"arrow-left",
        _=>"arrows"
    };
    public static string RowIcon(object source)=>source switch
    {
        AssetRow=>"box",BorrowerRow=>"user",LoanRow=>"arrows",ReservationRow=>"calendar",UserAccount=>"shield",
        AssetUnit=>"qr",Category=>"layers",Location=>"layers",InventorySession=>"clipboard",MaintenanceCase=>"tool",AuditEvent=>"history",_=>"report"
    };
    public static (string Background,string Foreground) StatusColors(string status)=>status switch
    {
        "Quá hạn" or "Mất" or "Từ chối"=>("#FCECEF","#A32C47"),
        "Chờ duyệt" or "Chờ xác nhận" or "Bảo trì" or "Đang sửa"=>("#FFF3DC","#895B0C"),
        "Hoạt động" or "Sẵn sàng" or "Hoàn tất" or "Đã duyệt" or "Đã sửa" or "Đã xác nhận"=>("#E4F4EC","#246C4D"),
        "Vô hiệu" or "Ngừng sử dụng" or "Đã hủy" or "Hết hạn"=>("#E9EDF3","#586579"),
        _=>("#E7EEFF","#3153AE")
    };
}
