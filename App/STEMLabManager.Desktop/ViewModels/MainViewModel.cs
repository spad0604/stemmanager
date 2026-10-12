using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using STEMLabManager.Application;
using STEMLabManager.Domain;
using STEMLabManager.AI;
namespace STEMLabManager.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ILabService service;
    private readonly IDialogs dialogs;
    private readonly Insights insights;
    private readonly FaceEngine face;
    private readonly CameraCapture camera;
    private readonly ILogger<MainViewModel> logger;
    private List<DisplayRow> allRows=[];
    private int page=1;
    private string currentPage="dashboard";
    private long unitAssetId;
    private Analytics? analytics;
    public ObservableCollection<NavItem> Navigation {get;}=[];
    public ObservableCollection<DisplayRow> Rows {get;}=[];
    public ObservableCollection<ToolbarAction> Actions {get;}=[];
    public ObservableCollection<Kpi> Kpis {get;}=[];
    public ObservableCollection<ChartBar> Bars {get;}=[];
    public ObservableCollection<string> Notifications {get;}=[];
    public ObservableCollection<string> Filters {get;}=["Tất cả"];
    public string[] SortOptions {get;}=["Mã tăng dần","Tên tăng dần","Tên giảm dần"];
    [ObservableProperty] private NavItem? selectedNav;
    [ObservableProperty] private DisplayRow? selectedRow;
    [ObservableProperty,NotifyPropertyChangedFor(nameof(Breadcrumb))] private string title="Tổng quan";
    [ObservableProperty] private string description="Dữ liệu hoạt động của phòng STEM";
    [ObservableProperty] private string search="";
    [ObservableProperty] private string statusFilter="Tất cả";
    [ObservableProperty] private string sort="Mã tăng dần";
    [ObservableProperty,NotifyPropertyChangedFor(nameof(HasError))] private string error="";
    [ObservableProperty,NotifyPropertyChangedFor(nameof(HasMessage))] private string message="";
    [ObservableProperty] private bool busy;
    [ObservableProperty] private bool authenticated;
    [ObservableProperty] private bool needsSetup;
    [ObservableProperty] private string username="";
    [ObservableProperty] private string password="";
    [ObservableProperty] private string displayName="";
    [ObservableProperty] private string room="Phòng STEM";
    [ObservableProperty] private string actorLabel="";
    [ObservableProperty] private string dateFrom=DateTime.Today.AddDays(-30).ToString("dd/MM/yyyy HH:mm");
    [ObservableProperty] private string dateTo=DateTime.Today.AddDays(1).ToString("dd/MM/yyyy HH:mm");
    [ObservableProperty] private string insightText="";
    [ObservableProperty] private Avalonia.Media.Imaging.Bitmap? selectedImage;
    public bool IsDashboard=>currentPage is "dashboard" or "analytics";
    public bool IsEmpty=>Rows.Count==0 && !Busy;
    public bool HasError=>!string.IsNullOrEmpty(Error);
    public bool HasMessage=>!string.IsNullOrEmpty(Message);
    public bool HasSelection=>SelectedRow!=null;
    public string SelectionDetails=>SelectedRow==null?"Chọn một dòng để xem chi tiết và thực hiện thao tác.":$"{SelectedRow.Name}\n{SelectedRow.Code}\n{SelectedRow.Details}\n{SelectedRow.Status}\n{SelectedRow.Metric}";
    public string PageLabel=>$"Trang {page} • {allRows.Count} mục • tối đa 20 mục/trang";
    public string AuthTitle=>NeedsSetup?"Thiết lập phòng STEM":"Đăng nhập";
    public bool IsDemo=>service.IsDemo;
    public string EnvironmentLabel=>IsDemo?"CHẾ ĐỘ MINH HỌA • DB riêng":"OFFLINE • Dữ liệu trên máy";
    public string VersionLabel=>"STEM Lab Manager 1.0 • .NET 10 • "+service.DataDirectory;
    public DateTime LastActivityUtc {get;set;}=DateTime.UtcNow;
    public int SessionMinutes {get;private set;}=10;
    public MainViewModel(ILabService service,IDialogs dialogs,Insights insights,FaceEngine face,CameraCapture camera,ILogger<MainViewModel> logger)
    {this.service=service;this.dialogs=dialogs;this.insights=insights;this.face=face;this.camera=camera;this.logger=logger;}
    public async Task InitializeAsync()=>await RunAsync(async ()=>{await service.InitializeAsync();NeedsSetup=await service.NeedsSetupAsync();OnPropertyChanged(nameof(AuthTitle));});
    private async Task RunAsync(Func<Task> action,string success="")
    {
        if(Busy)return;Busy=true;Error="";Message="";
        try{await action();if(success!="")Message=success;}
        catch(Exception e){Error=e is BusinessException?e.Message:"Không thể hoàn tất thao tác. Hãy thử lại hoặc kiểm tra thư mục dữ liệu.";logger.LogError("Operation failed: {Type}",e.GetType().Name);}
        finally{Busy=false;OnPropertyChanged(nameof(IsEmpty));LastActivityUtc=DateTime.UtcNow;}
    }
    [RelayCommand] private Task LoginAsync()=>RunAsync(async ()=>
    {
        if(NeedsSetup){await service.SetupAsync(Username,Password,DisplayName,Room);NeedsSetup=false;OnPropertyChanged(nameof(AuthTitle));}else await service.LoginAsync(Username,Password);
        Password="";Authenticated=true;ActorLabel=$"{service.CurrentActor!.DisplayName} • {Labels.Role(service.CurrentActor.Role)}";Navigation.Clear();
        string? previousGroup=null;
        foreach(var item in AllNavigation.Where(x=>Access.Allows(service.CurrentActor.Role,x.Permission)))
        {Navigation.Add(new(item.Key,item.Title,item.Group,item.Permission){ShowGroup=previousGroup!=item.Group});previousGroup=item.Group;}
        var settings=await service.SettingsAsync();Room=settings.GetValueOrDefault("room")??Room;SessionMinutes=int.TryParse(settings.GetValueOrDefault("session.minutes"),out var min)?min:10;
        SelectedNav=Navigation[0];currentPage="dashboard";await LoadPageAsync();
        await service.RunScheduledBackupAsync();
    });
    [RelayCommand] public void Logout(){service.Logout();Authenticated=false;Rows.Clear();allRows.Clear();Actions.Clear();Navigation.Clear();SelectedRow=null;InsightText="";Password="";Message="Phiên đã khóa. Đăng nhập để tiếp tục.";}
    partial void OnSelectedNavChanged(NavItem? value){if(value==null || !Authenticated)return;foreach(var item in Navigation)item.Active=item==value;currentPage=value.Key;Title=value.Title;Description=Descriptions.GetValueOrDefault(value.Key)??"";Search="";SelectedRow=null;page=1;_ = RunAsync(LoadPageAsync);}
    partial void OnSelectedRowChanged(DisplayRow? value){OnPropertyChanged(nameof(HasSelection));OnPropertyChanged(nameof(SelectionDetails));OnPropertyChanged(nameof(Breadcrumb));SelectedImage?.Dispose();SelectedImage=null;if(value?.Source is AssetRow)_ = LoadImageAsync(value);}
    private async Task LoadImageAsync(DisplayRow row)
    {
        try{var asset=await service.AssetDetailsAsync(row.Id);if(SelectedRow!=row || string.IsNullOrWhiteSpace(asset.ImagePath))return;var path=Path.Combine(service.DataDirectory,asset.ImagePath);if(File.Exists(path))SelectedImage=new Avalonia.Media.Imaging.Bitmap(path);}
        catch(Exception e){logger.LogWarning("Image load failed: {Type}",e.GetType().Name);}
    }
    partial void OnSearchChanged(string value){page=1;ApplyRows();}
    partial void OnStatusFilterChanged(string value){page=1;ApplyRows();}
    partial void OnSortChanged(string value){page=1;ApplyRows();}
    [RelayCommand] private Task RefreshAsync()=>RunAsync(LoadPageAsync);
    [RelayCommand] private void NextPage(){if(page*20<Filtered().Count()){page++;ApplyRows();}}
    [RelayCommand] private void PreviousPage(){if(page>1){page--;ApplyRows();}}
    private IEnumerable<DisplayRow> Filtered()
    {
        var rows=allRows.Where(x=>(StatusFilter=="Tất cả" || x.Status==StatusFilter) && (string.IsNullOrWhiteSpace(Search) || SearchMatch(x.Code+" "+x.Name+" "+x.Details,Search)));
        return Sort switch {"Tên tăng dần"=>rows.OrderBy(x=>x.Name),"Tên giảm dần"=>rows.OrderByDescending(x=>x.Name),_=>rows.OrderBy(x=>x.Code)};
    }
    private static bool SearchMatch(string text,string search)
    {
        static string N(string s)=>string.Concat(s.Normalize(System.Text.NormalizationForm.FormD).Where(c=>System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)!=System.Globalization.UnicodeCategory.NonSpacingMark)).Replace('đ','d').Replace('Đ','D').ToLowerInvariant();
        return N(text).Contains(N(search.Trim()));
    }
    private void ApplyRows(){Rows.Clear();foreach(var r in Filtered().Skip((page-1)*20).Take(20))Rows.Add(r);OnPropertyChanged(nameof(PageLabel));OnPropertyChanged(nameof(IsEmpty));}
    private void AddAction(string label,Permission permission,Func<Task> action)
    {if(service.CurrentActor!=null && Access.Allows(service.CurrentActor.Role,permission))Actions.Add(new(label,new AsyncRelayCommand(()=>RunAsync(async ()=>{await action();if(Authenticated)await LoadPageAsync();})),Actions.Count==0));}
    private DisplayRow Selected()=>SelectedRow??throw new BusinessException("Chọn một dòng trước khi thực hiện thao tác.");
    private (DateTime Start,DateTime End) Dates(){var f=new FormViewModel("","",[new("from","Từ",DateFrom),new("to","Đến",DateTo)],_=>Task.CompletedTask);var start=f.Utc("from");var end=f.Utc("to");Rules.Interval(start,end);return(start,end);}
    private static readonly NavItem[] AllNavigation=[
        new("dashboard","Tổng quan","TỔNG QUAN",Permission.Read),new("assets","Thiết bị","TÀI SẢN",Permission.Read),new("catalog","Danh mục & vị trí","TÀI SẢN",Permission.Read),new("inventory","Nhập kho & kiểm kê","TÀI SẢN",Permission.Read),new("maintenance","Bảo trì","TÀI SẢN",Permission.Read),
        new("loans","Mượn – Trả","GIAO DỊCH",Permission.IssueLoan),new("reservations","Đặt trước","GIAO DỊCH",Permission.RequestReservation),new("overdue","Quá hạn","GIAO DỊCH",Permission.IssueLoan),new("borrowers","Người mượn","CON NGƯỜI",Permission.Read),new("users","Giáo viên & tài khoản","CON NGƯỜI",Permission.ManageUsers),
        new("analytics","Phân tích AI","THÔNG MINH",Permission.Reports),new("face","Nhận diện khuôn mặt","THÔNG MINH",Permission.Biometrics),new("reports","Báo cáo & xuất dữ liệu","BÁO CÁO",Permission.Reports),new("settings","Cài đặt","HỆ THỐNG",Permission.Settings),new("backup","Sao lưu & phục hồi","HỆ THỐNG",Permission.Backup),new("audit","Nhật ký","HỆ THỐNG",Permission.Audit),new("help","Trợ giúp","HỆ THỐNG",Permission.Read)];
    private static readonly Dictionary<string,string> Descriptions=new()
    {
        ["dashboard"]="Theo dõi thiết bị, lịch đặt và hoạt động của phòng STEM trong một góc nhìn.",["assets"]="Quản lý thiết bị cá thể, dụng cụ theo số lượng và vật tư tiêu hao.",["catalog"]="Phân loại thiết bị và vị trí lưu trữ.",["inventory"]="Nhập bổ sung, kiểm kê đối chiếu; xác nhận điều chỉnh có nhật ký.",["maintenance"]="Theo dõi hỏng hóc và xác nhận đưa thiết bị trở lại sử dụng.",["loans"]="Chọn người → chọn thiết bị → xác nhận bàn giao. Có trả một phần và lịch sử.",["reservations"]="Sắp lịch sử dụng thiết bị; yêu cầu được giữ chỗ sau khi được duyệt.",["overdue"]="Phiếu chưa trả quá hạn theo giờ địa phương.",["borrowers"]="Người nhận thiết bị tách biệt với tài khoản đăng nhập.",["users"]="Quản lý vai trò và trạng thái; mật khẩu không hiển thị.",["analytics"]="Khám phá nhu cầu sử dụng và những gợi ý từ dữ liệu của phòng STEM.",["face"]="Tự nguyện; nhân viên xác nhận danh tính, luôn có QR/mã thay thế.",["reports"]="Xuất CSV, Excel hoặc PDF theo quyền và kỳ báo cáo.",["settings"]="Cấu hình phòng, bảo mật, AI và lưu khóa API mã hóa.",["backup"]="Backup bao gồm dữ liệu và khóa giải mã. Cất giữ ở nơi có quyền truy cập hạn chế.",["audit"]="Nhật ký chỉ đọc; không có chức năng xóa.",["help"]="Hướng dẫn vận hành và thông tin ứng dụng."
    };
}
