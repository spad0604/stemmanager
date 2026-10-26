using CommunityToolkit.Mvvm.Input;
using STEMLabManager.Application;
using STEMLabManager.Domain;
namespace STEMLabManager.Desktop.ViewModels;
public partial class MainViewModel
{
    public string Breadcrumb=>SelectedRow==null?Title:$"{Title} / {SelectedRow.Code}";
    [RelayCommand] private Task GlobalSearchAsync()=>RunAsync(async ()=>
    {
        string query="";
        if(!await dialogs.FormAsync("Tìm nhanh • Ctrl+K","Tìm thiết bị hoặc người mượn theo tên/mã, hỗ trợ tiếng Việt không dấu.",[Field("query","Tên hoặc mã")],f=>{query=f["query"];return Task.CompletedTask;}))return;
        var assets=await service.AssetsAsync(query);var borrowers=await service.BorrowersAsync(query);
        var choices=assets.Select(a=>new Choice(a.Id,$"Thiết bị • {a.Code} • {a.Name}")).Concat(borrowers.Select(b=>new Choice(-b.Id,$"Người mượn • {b.Code} • {b.Name}"))).ToList();Rules.Require(choices.Count>0,"Không có kết quả. Thử mã/tên khác.");long chosen=0;
        if(!await dialogs.FormAsync("Kết quả tìm nhanh",$"{choices.Count} kết quả.",[Choose("item","Mở kết quả",choices)],f=>{chosen=f.Id("item");return Task.CompletedTask;}))return;
        SelectedNav=Navigation.Single(n=>n.Key==(chosen>0?"assets":"borrowers"));currentPage=chosen>0?"assets":"borrowers";Title=chosen>0?"Thiết bị":"Người mượn";Search=query;await LoadPageAsync();SelectedRow=allRows.SingleOrDefault(x=>x.Id==Math.Abs(chosen));
    });
}
