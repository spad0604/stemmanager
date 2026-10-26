using ClosedXML.Excel;
using STEMLabManager.Infrastructure;

if(args.Length!=1 || args[0]!="prepare-docs")
{Console.Error.WriteLine("Usage: dotnet run --project App/STEMLabManager.Tools -- prepare-docs (from repository root)");return 1;}
var root=Directory.GetCurrentDirectory();
if(!File.Exists(Path.Combine(root,"STEMManager.sln")))throw new InvalidOperationException("Run from repository root.");
var output=Path.Combine(root,"artifacts","docs");Directory.CreateDirectory(output);
var lines=await File.ReadAllLinesAsync(Path.Combine(root,"docs","USER_GUIDE.md"));
SimplePdf.Write(Path.Combine(output,"STEMManager-UserGuide.pdf"),lines.Where(x=>!string.IsNullOrWhiteSpace(x)).Select(x=>new[]{x.Replace("**","").Replace("`","").TrimStart('#',' ')}).ToList());
foreach(var name in new[]{"borrowers","assets"})
{
    var csv=Path.Combine(root,"samples",name+".csv");var rows=Csv.Parse(await File.ReadAllTextAsync(csv));using var book=new XLWorkbook();var sheet=book.AddWorksheet("Import");
    for(int row=0;row<rows.Count;row++)for(int col=0;col<rows[row].Length;col++)sheet.Cell(row+1,col+1).Value=rows[row][col];
    sheet.Row(1).Style.Font.Bold=true;sheet.Columns().AdjustToContents();book.SaveAs(Path.Combine(output,name+".xlsx"));
}
Console.WriteLine("Generated user guide PDF and Excel import samples in artifacts/docs.");return 0;
