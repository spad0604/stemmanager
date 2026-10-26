using STEMLabManager.Domain;
namespace STEMLabManager.Desktop.ViewModels;
public partial class MainViewModel
{
    private async Task UnitDetailsAsync()
    {
        var unit=(AssetUnit)Selected().Source;await dialogs.FormAsync("Serial và tình trạng",unit.QrValue+". Ghi tình trạng ngoại quan tại đây (ví dụ: xước nhẹ ở mặt trên). Trạng thái kho chỉ đổi qua nghiệp vụ mượn/trả/bảo trì/kiểm kê.",[Field("serial","Serial (tùy chọn)",unit.Serial??"",false),Field("condition","Tình trạng / vết xước",unit.Condition,hint:"Ví dụ: Tốt; xước nhẹ ở mặt trên; móp góc trái. Tối đa 200 ký tự.")],f=>service.SaveUnitDetailsAsync(unit.Id,f["serial"],f["condition"]));
    }
}
