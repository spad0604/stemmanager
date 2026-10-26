namespace STEMLabManager.Desktop.ViewModels;
public partial class MainViewModel
{
    private async Task ImportAssetsAsync()
    {
        var path=await dialogs.OpenAsync("Import thiết bị","*.csv","*.xlsx");if(path==null)return;Dictionary<string,string>? mapping=null;
        string[] fields=["Code","Name","Kind","Category","Location","Unit","Quantity","MinimumStock"];
        if(!await dialogs.FormAsync("Ánh xạ cột thiết bị","Kind: Unit/Bulk/Consumable. Category và Location phải khớp tên đã có. Import chỉ tạo mới, không ghi đè.",fields.Select(k=>Field(k,"Tên cột "+k,k,k!="MinimumStock")),f=>{mapping=fields.ToDictionary(k=>k,k=>f[k]);return Task.CompletedTask;}))return;
        var preview=await service.PreviewAssetsAsync(path,mapping);Domain.Rules.Require(preview.Errors.Count==0,string.Join("\n",preview.Errors.Take(30)));
        if(await dialogs.ConfirmAsync("Xem trước import thiết bị",$"{preview.Rows.Count} dòng hợp lệ:\n"+string.Join("\n",preview.Rows.Take(15).Select(x=>$"{x.Code} • {x.Name} • {x.InitialQuantity} {x.Unit}"))))await service.ImportAssetsAsync(preview);
    }
}
