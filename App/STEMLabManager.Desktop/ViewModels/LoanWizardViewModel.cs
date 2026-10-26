using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using STEMLabManager.Application;
using STEMLabManager.Domain;
namespace STEMLabManager.Desktop.ViewModels;

public sealed record CartLine(AssetRow Asset,AssetUnit? Unit,int Quantity)
{
    public string Label=>$"{Asset.Code} • {Asset.Name} • {Unit?.QrValue??"Tự chọn cá thể / số lượng"} • {Quantity} {Asset.Unit}";
}
public partial class LoanWizardViewModel : ObservableObject
{
    private readonly ILabService service;
    private readonly Func<Task<BorrowerRow?>>? faceBorrowerLookup;
    private readonly IReadOnlyList<AssetCartItem> initialCart;
    private readonly long? defaultBorrowerId;
    public bool IsReservation {get;}
    public string Title=>IsReservation?"Tạo yêu cầu đặt trước":"Tạo phiếu mượn";
    public ObservableCollection<BorrowerRow> Borrowers {get;}=[];
    public ObservableCollection<AssetRow> Assets {get;}=[];
    public ObservableCollection<AssetUnit> Units {get;}=[];
    public ObservableCollection<CartLine> Cart {get;}=[];
    [ObservableProperty] private BorrowerRow? borrower;
    [ObservableProperty] private AssetRow? asset;
    [ObservableProperty] private AssetUnit? unit;
    [ObservableProperty] private Avalonia.Media.Imaging.Bitmap? selectedAssetImage;
    [ObservableProperty] private CartLine? selectedLine;
    [ObservableProperty] private string quantity="1";
    [ObservableProperty] private string borrowerSearch="";
    [ObservableProperty] private string assetSearch="";
    [ObservableProperty] private string purpose="";
    public FormField StartSelection {get;}=new("start","Bắt đầu",DateTime.Now.AddHours(1).ToString("dd/MM/yyyy HH:mm"),dateTime:true);
    public FormField DueSelection {get;}=new("due","Hạn trả",DateTime.Now.AddDays(1).ToString("dd/MM/yyyy HH:mm"),dateTime:true);
    [ObservableProperty] private int step=1;
    [ObservableProperty] private string error="";
    [ObservableProperty] private bool busy;
    public bool StepOne=>Step==1;public bool StepTwo=>Step==2;public bool StepThree=>Step==3;
    public bool HasBorrowers=>Borrowers.Count>0;public bool HasAssets=>Assets.Count>0;public bool HasUnits=>Units.Count>0;public bool HasCart=>Cart.Count>0;
    public bool UsesUnits=>Asset?.Kind==AssetKind.Unit;public bool ShowNoUnits=>UsesUnits&&!HasUnits;public bool ShowQuantityHint=>Asset!=null&&!UsesUnits;public bool CanAddLine=>Asset!=null;
    public bool HasSelectedAssetImage=>SelectedAssetImage!=null;
    public string StepTitle=>$"Bước {Step}/3 • "+(Step==1?"Chọn người mượn":Step==2?"Chọn thiết bị":"Kiểm tra và xác nhận bàn giao");
    public string Review=>$"Người nhận: {Borrower?.Name} • {Borrower?.Code}\n"+$"Cảnh báo: {Borrower?.OverdueLoans??0} phiếu quá hạn; {Borrower?.OpenLoans??0} phiếu đang giữ.\n"+string.Join("\n",Cart.Select(x=>x.Label));
    public event Action<bool>? CloseRequested;
    public LoanWizardViewModel(ILabService service,bool reservation,Func<Task<BorrowerRow?>>? faceBorrowerLookup=null,IReadOnlyList<AssetCartItem>? initialCart=null,long? defaultBorrowerId=null){this.service=service;IsReservation=reservation;this.faceBorrowerLookup=faceBorrowerLookup;this.initialCart=initialCart??[];this.defaultBorrowerId=defaultBorrowerId;}
    public async Task LoadAsync()
    {
        foreach(var b in (await service.BorrowersAsync()).Where(x=>x.Active))Borrowers.Add(b);
        foreach(var a in await service.AssetsAsync())Assets.Add(a);
        Borrower=defaultBorrowerId.HasValue?Borrowers.FirstOrDefault(x=>x.Id==defaultBorrowerId.Value):null;
        foreach(var item in initialCart){var current=Assets.FirstOrDefault(x=>x.Id==item.Asset.Id);if(current!=null&&current.Available>=item.Quantity)Cart.Add(new(current,null,item.Quantity));}
        NotifyChoices();
    }
    [RelayCommand] private async Task SearchBorrowerAsync(){Borrowers.Clear();foreach(var b in (await service.BorrowersAsync(BorrowerSearch)).Where(x=>x.Active))Borrowers.Add(b);NotifyChoices();}
    public bool CanLookupFace=>!IsReservation&&faceBorrowerLookup!=null;
    [RelayCommand] private async Task LookupFaceAsync()
    {
        if(faceBorrowerLookup==null)return;
        try{Error="";var match=await faceBorrowerLookup();if(match==null)return;if(!Borrowers.Any(x=>x.Id==match.Id))Borrowers.Insert(0,match);Borrower=Borrowers.Single(x=>x.Id==match.Id);BorrowerSearch=match.Name;}
        catch(Exception e){Error=e is BusinessException?e.Message:"Không thể tra cứu khuôn mặt. Hãy tìm bằng mã/QR.";}
    }
    [RelayCommand] private async Task SearchAssetAsync(){Assets.Clear();foreach(var a in await service.AssetsAsync(AssetSearch))Assets.Add(a);NotifyChoices();}
    partial void OnAssetChanged(AssetRow? value){Unit=null;Units.Clear();SelectedAssetImage?.Dispose();SelectedAssetImage=null;NotifyChoices();_ = LoadUnitsAsync(value);_ = LoadAssetImageAsync(value);}
    partial void OnSelectedAssetImageChanged(Avalonia.Media.Imaging.Bitmap? value)=>OnPropertyChanged(nameof(HasSelectedAssetImage));
    private async Task LoadAssetImageAsync(AssetRow? value)
    {
        if(value?.ImagePath is not {Length:>0} relativePath)return;
        try{var path=Path.Combine(service.DataDirectory,relativePath);if(!File.Exists(path))return;var bitmap=await Task.Run(()=>new Avalonia.Media.Imaging.Bitmap(path));if(Asset?.Id==value.Id)SelectedAssetImage=bitmap;else bitmap.Dispose();}
        catch{ }
    }
    private async Task LoadUnitsAsync(AssetRow? value)
    {
        try{if(value is {Kind:AssetKind.Unit}){var units=await service.UnitsAsync(value.Id);if(Asset?.Id==value.Id)foreach(var u in units.Where(x=>IsReservation?x.Status is UnitStatus.Available or UnitStatus.CheckedOut:x.Status==UnitStatus.Available))Units.Add(u);}NotifyChoices();}
        catch(Exception e){Error=e.Message;}
    }
    private void NotifyChoices(){OnPropertyChanged(nameof(HasBorrowers));OnPropertyChanged(nameof(HasAssets));OnPropertyChanged(nameof(HasUnits));OnPropertyChanged(nameof(HasCart));OnPropertyChanged(nameof(UsesUnits));OnPropertyChanged(nameof(ShowNoUnits));OnPropertyChanged(nameof(ShowQuantityHint));OnPropertyChanged(nameof(CanAddLine));}
    [RelayCommand] private void AddLine()
    {
        try{Rules.Require(Asset!=null,"Chọn thiết bị.");Rules.Require(int.TryParse(Quantity,out var qty) && qty>0,"Số lượng phải là số nguyên dương.");if(Unit!=null)qty=1;Rules.Require(Unit==null || Cart.All(x=>x.Unit?.Id!=Unit.Id),"Cá thể đã được thêm.");Cart.Add(new(Asset!,Unit,qty));Error="";NotifyChoices();}
        catch(BusinessException e){Error=e.Message;}
    }
    [RelayCommand] private void RemoveLine(){if(SelectedLine!=null)Cart.Remove(SelectedLine);NotifyChoices();}
    [RelayCommand] private void Next()
    {
        try{if(Step==1)Rules.Require(Borrower!=null,"Chọn người mượn.");if(Step==2)Rules.Require(Cart.Count>0,"Thêm ít nhất một dòng thiết bị.");if(Step<3)Step++;Error="";NotifyStep();}
        catch(BusinessException e){Error=e.Message;}
    }
    [RelayCommand] private void Back(){if(Step>1)Step--;NotifyStep();}
    private void NotifyStep(){OnPropertyChanged(nameof(StepOne));OnPropertyChanged(nameof(StepTwo));OnPropertyChanged(nameof(StepThree));OnPropertyChanged(nameof(StepTitle));OnPropertyChanged(nameof(Review));}
    [RelayCommand] private async Task ConfirmAsync()
    {
        Busy=true;Error="";
        try
        {
            Rules.Require(Borrower!=null && Cart.Count>0,"Thiếu người mượn hoặc thiết bị.");
            var lines=Cart.Select(x=>new IssueLine(x.Asset.Id,x.Unit?.Id,x.Quantity)).ToList();
            if(IsReservation)await service.ReserveAsync(new(Borrower!.Id,StartSelection.UtcValue(),DueSelection.UtcValue(),lines,Purpose));
            else await service.IssueAsync(new(Borrower!.Id,DueSelection.UtcValue(),Purpose,lines));CloseRequested?.Invoke(true);
        }
        catch(Exception e){Error=e is BusinessException?e.Message:"Không thể ghi phiếu. Hãy tải lại và kiểm tra dữ liệu.";}
        finally{Busy=false;}
    }
    [RelayCommand] private void Cancel()=>CloseRequested?.Invoke(false);
}
