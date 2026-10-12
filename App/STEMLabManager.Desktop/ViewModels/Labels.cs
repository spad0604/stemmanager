using STEMLabManager.Domain;
namespace STEMLabManager.Desktop.ViewModels;
public static class Labels
{
    public static string Role(Role r)=>r switch {Domain.Role.SuperAdmin=>"Quản trị",Domain.Role.LabManager=>"Quản lý",Domain.Role.Teacher=>"Giáo viên",_=>"Chỉ đọc"};
    public static string Unit(UnitStatus s)=>s switch{UnitStatus.Available=>"Sẵn sàng",UnitStatus.Reserved=>"Đã đặt",UnitStatus.CheckedOut=>"Đang mượn",UnitStatus.Maintenance=>"Bảo trì",UnitStatus.Lost=>"Mất",_=>"Ngừng sử dụng"};
    public static string Reservation(ReservationStatus s)=>s switch{ReservationStatus.Draft=>"Nháp",ReservationStatus.Pending=>"Chờ duyệt",ReservationStatus.Approved=>"Đã duyệt",ReservationStatus.Rejected=>"Từ chối",ReservationStatus.Cancelled=>"Đã hủy",ReservationStatus.Fulfilled=>"Đã giao",_=>"Hết hạn"};
    public static string Kind(AssetKind k)=>k switch{AssetKind.Unit=>"Cá thể",AssetKind.Bulk=>"Theo số lượng",_=>"Tiêu hao"};
}
