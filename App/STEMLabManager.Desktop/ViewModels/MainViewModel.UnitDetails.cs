using STEMLabManager.Domain;
namespace STEMLabManager.Desktop.ViewModels;
public partial class MainViewModel
{
    private async Task UnitDetailsAsync()
    {
        var unit=(AssetUnit)Selected().Source;await dialogs.FormAsync("Serial và tình trạng",unit.QrValue+". Trạng thái kho chỉ đổi qua nghiệp vụ mượn/trả/bảo trì/kiểm kê.",[Field("serial","Serial (tùy chọn)",unit.Serial??"",false),Field("condition","Mô tả tình trạng",unit.Condition)],f=>service.SaveUnitDetailsAsync(unit.Id,f["serial"],f["condition"]));
    }
}
