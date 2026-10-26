using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using STEMLabManager.Application;
using STEMLabManager.Domain;
namespace STEMLabManager.Infrastructure;

public sealed partial class LabService
{
    public async Task<AssetImportPreview> PreviewAssetsAsync(string path,IReadOnlyDictionary<string,string>? mapping=null)
    {
        await using var db=Db();await VerifyActorAsync(db,Permission.ManageAssets);Rules.Require(new FileInfo(path).Length<=20*1024*1024,"File import tối đa 20 MB.");
        var errors=new List<string>();var rows=new List<AssetInput>();var existing=(await db.Assets.Select(x=>x.Code).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var categories=await db.Categories.ToDictionaryAsync(x=>x.Name,x=>x.Id);var locations=await db.Locations.ToDictionaryAsync(x=>x.Name,x=>x.Id);
        return await Task.Run(async ()=>
        {
            IReadOnlyList<string[]> table;
            if(Path.GetExtension(path).Equals(".xlsx",StringComparison.OrdinalIgnoreCase))
            {using var workbook=new XLWorkbook(path);var range=workbook.Worksheet(1).RangeUsed();table=range is null?[]:range.Rows().Select(r=>Enumerable.Range(1,range.ColumnCount()).Select(c=>r.Cell(c).GetString()).ToArray()).ToList();}
            else table=Csv.Parse(await File.ReadAllTextAsync(path));
            if(table.Count<2)return new AssetImportPreview([],["File cần hàng tiêu đề và dữ liệu."]);
            var fields=new[]{"Code","Name","Kind","Category","Location","Unit","Quantity","MinimumStock","Description","Manufacturer","Model"};
            var headers=table[0];var indexes=fields.ToDictionary(f=>f,f=>Array.FindIndex(headers,h=>h.Equals(mapping?.GetValueOrDefault(f)??f,StringComparison.OrdinalIgnoreCase)));
            var required=new[]{"Code","Name","Kind","Category","Location","Unit","Quantity"};if(required.Any(f=>indexes[f]<0))return new AssetImportPreview([],["Thiếu cột: "+string.Join(", ",required.Where(f=>indexes[f]<0))]);
            Rules.Require(table.Count<=10001,"Tối đa 10.000 dòng mỗi lần import.");
            for(int i=1;i<table.Count;i++)
            {
                var cells=table[i];if(cells.All(string.IsNullOrWhiteSpace))continue;string Get(string field)=>indexes[field]>=0 && indexes[field]<cells.Length?cells[indexes[field]].Trim():"";
                try
                {
                    var code=CleanCode(Get("Code"));Rules.Text(Get("Name"),"Tên");Rules.Text(Get("Unit"),"Đơn vị",30);Rules.Require(existing.Add(code),"Trùng mã "+code);
                    Rules.Require(Enum.TryParse<AssetKind>(Get("Kind"),true,out var kind)&&Enum.IsDefined(kind),"Kind phải là Unit, Bulk hoặc Consumable.");
                    Rules.Require(categories.TryGetValue(Get("Category"),out var category),"Danh mục chưa có: "+Get("Category"));Rules.Require(locations.TryGetValue(Get("Location"),out var location),"Vị trí chưa có: "+Get("Location"));
                    Rules.Require(int.TryParse(Get("Quantity"),out var qty) && qty>=0 && qty<=10000,"Quantity phải từ 0 đến 10.000.");
                    int minimum=0;if(Get("MinimumStock")!="")Rules.Require(int.TryParse(Get("MinimumStock"),out minimum)&&minimum>=0,"MinimumStock không hợp lệ.");
                    rows.Add(new(code,Get("Name"),kind,category,location,Get("Unit"),qty,minimum,Get("Description"),Get("Manufacturer"),Get("Model")));
                }
                catch(BusinessException e){errors.Add($"Dòng {i+1}: {e.Message}");}
            }
            return new AssetImportPreview(rows,errors);
        });
    }
    public Task ImportAssetsAsync(AssetImportPreview preview)=>WriteAsync(Permission.ManageAssets,async db=>
    {
        Rules.Require(preview.Errors.Count==0 && preview.Rows.Count>0,"Import có lỗi hoặc không có dữ liệu; không nhập dòng nào.");
        foreach(var input in preview.Rows)await SaveAssetCoreAsync(db,null,input);
        Audit(db,"ImportAssets","Asset",null,$"Rows={preview.Rows.Count}");
    });
}
