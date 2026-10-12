using System.Text;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using STEMLabManager.Application;
using STEMLabManager.Domain;
namespace STEMLabManager.Infrastructure;

public sealed partial class LabService
{
    public async Task<Analytics> AnalyticsAsync(DateTime startUtc,DateTime endUtc)
    {
        Rules.Interval(startUtc,endUtc); await using var db=Db(); await VerifyActorAsync(db,Permission.Reports);
        var now=DateTime.UtcNow; var today=DateTime.Today.ToUniversalTime(); var tomorrow=DateTime.Today.AddDays(1).ToUniversalTime();
        var loans=await db.Loans.AsNoTracking().Include(x=>x.Lines).Where(x=>x.IssuedUtc>=startUtc && x.IssuedUtc<endUtc).ToListAsync();
        var open=await db.Loans.AsNoTracking().Where(x=>x.Status==LoanStatus.Open).ToListAsync();
        var reservations=await db.Reservations.AsNoTracking().Include(x=>x.Lines).ToListAsync();
        var allAssets=await AssetsAsync(); var assets=await db.Assets.ToDictionaryAsync(x=>x.Id,x=>x.Name);
        Rules.Require((endUtc-startUtc).TotalDays<=3660,"Kỳ thống kê tối đa 10 năm.");
        var counts=loans.GroupBy(x=>x.IssuedUtc.ToLocalTime().Date).ToDictionary(g=>g.Key,g=>g.Count());
        var daily=new List<MetricPoint>();for(var day=startUtc.ToLocalTime().Date;day<endUtc.ToLocalTime();day=day.AddDays(1))daily.Add(new(day.ToString("dd/MM"),counts.GetValueOrDefault(day)));
        var top=loans.SelectMany(x=>x.Lines).GroupBy(x=>x.AssetId).OrderByDescending(g=>g.Sum(x=>x.QuantityIssued)).Take(5).Select(g=>new MetricPoint(assets[g.Key],g.Sum(x=>x.QuantityIssued))).ToList();
        var notifications=new List<string>();
        notifications.AddRange(open.Where(x=>x.DueUtc<now).Select(x=>$"Quá hạn: {x.Code} • {x.DueUtc.ToLocalTime():dd/MM/yyyy HH:mm}"));
        notifications.AddRange(open.Where(x=>x.DueUtc>=now && x.DueUtc<=now.AddHours(24)).Select(x=>$"Sắp đến hạn: {x.Code}"));
        notifications.AddRange(reservations.Where(x=>x.Status==ReservationStatus.Pending).Select(x=>$"Chờ duyệt: {x.Code}"));
        notifications.AddRange(reservations.Where(x=>x.Status==ReservationStatus.Approved && x.EndUtc<now).Select(x=>$"Lịch đặt hết hạn/no-show: {x.Code}"));
        var minimums=await db.Assets.ToDictionaryAsync(x=>x.Id,x=>x.MinimumStock);
        notifications.AddRange(allAssets.Where(x=>x.Available<=minimums[x.Id]).Select(x=>$"Tồn thấp: {x.Name} • còn {x.Available} {x.Unit}"));
        var maintenance=await db.Units.Where(x=>x.Status==UnitStatus.Maintenance).ToListAsync();
        notifications.AddRange(maintenance.Select(x=>$"Cần bảo trì: {x.QrValue}"));
        var unhealthy=await db.Units.Where(x=>x.Status==UnitStatus.Maintenance || x.Status==UnitStatus.Lost || x.Status==UnitStatus.Retired).Select(x=>x.Id).ToListAsync();
        notifications.AddRange(reservations.Where(r=>r.Status==ReservationStatus.Approved && r.EndUtc>now && r.Lines.Any(l=>l.UnitId.HasValue && unhealthy.Contains(l.UnitId.Value))).Select(r=>$"Lịch đặt bị ảnh hưởng bởi thiết bị hỏng: {r.Code}. Hãy hủy/đặt thiết bị thay thế."));
        // Reconstruct overdue history using actual return timestamps; closed loans are included.
        var returns=await db.Returns.AsNoTracking().Include(x=>x.Lines).ToListAsync();
        int late=loans.Count(l=>l.Status==LoanStatus.Open?l.DueUtc<now:returns.Any(r=>r.LoanId==l.Id && r.CreatedUtc>l.DueUtc));
        double occupiedHours=0;
        var usageLoans=await db.Loans.AsNoTracking().Include(x=>x.Lines).Where(x=>x.IssuedUtc<endUtc).ToListAsync();
        foreach(var l in usageLoans)
        foreach(var line in l.Lines.Where(x=>!x.IsConsumable))
        {
            var beginning=l.IssuedUtc>startUtc?l.IssuedUtc:startUtc;
            var pending=line.QuantityIssued;
            foreach(var r in returns.Where(r=>r.LoanId==l.Id).OrderBy(r=>r.CreatedUtc))
            {
                var quantity=r.Lines.Where(x=>x.LoanLineId==line.Id).Sum(x=>x.Quantity);
                var finish=r.CreatedUtc<endUtc?r.CreatedUtc:endUtc;
                occupiedHours+=quantity*Math.Max(0,(finish-beginning).TotalHours); pending-=quantity;
            }
            occupiedHours+=pending*Math.Max(0,((endUtc<now?endUtc:now)-beginning).TotalHours);
        }
        var denominator=allAssets.Where(x=>x.Kind!=AssetKind.Consumable).Sum(x=>x.Total)*(endUtc-startUtc).TotalHours;
        return new(startUtc,endUtc,allAssets.Sum(x=>x.Available),open.Count,open.Count(x=>x.DueUtc<now),reservations.Count(x=>x.Status==ReservationStatus.Approved && x.StartUtc<tomorrow && x.EndUtc>today),loans.Count,loans.Select(x=>x.BorrowerId).Distinct().Count(),loans.Count==0?0:(double)late/loans.Count,daily,top,notifications,denominator==0?0:Math.Min(1,occupiedHours/denominator),reservations.Count(x=>x.StartUtc>=startUtc && x.StartUtc<endUtc && x.Status is ReservationStatus.Rejected or ReservationStatus.Expired));
    }
    public async Task<IReadOnlyList<AuditEvent>> AuditAsync() { await using var db=Db(); await VerifyActorAsync(db,Permission.Audit); return await db.Audit.AsNoTracking().OrderByDescending(x=>x.CreatedUtc).Take(1000).ToListAsync(); }
    public async Task<IReadOnlyDictionary<string,string>> SettingsAsync()
    {
        await using var db=Db(); await VerifyActorAsync(db,Permission.Read);
        var settings=await db.Settings.AsNoTracking().ToListAsync();
        return settings.Where(x=>!x.Key.Contains("secret",StringComparison.OrdinalIgnoreCase)).ToDictionary(x=>x.Key,x=>x.Value);
    }
    public Task SaveSettingAsync(string key,string value) => WriteAsync(Permission.Settings,async db=>
    {
        var allowed=new[]{"room","teacher.issue","face.enabled","llm.enabled","llm.endpoint","llm.model","llm.secret","face.model","face.embeddingModel","face.threshold","backup.directory","session.minutes"};
        Rules.Require(allowed.Contains(key),"Cấu hình không được hỗ trợ.");
        if(key.EndsWith(".enabled") || key=="teacher.issue") Rules.Require(value is "true" or "false","Giá trị boolean không hợp lệ.");
        if(key=="llm.endpoint") Rules.Require(Uri.TryCreate(value,UriKind.Absolute,out var u) && u.Scheme=="https" && !string.IsNullOrWhiteSpace(u.Host),"LLM endpoint phải dùng HTTPS.");
        if(key=="face.threshold") Rules.Require(double.TryParse(value,System.Globalization.CultureInfo.InvariantCulture,out var threshold) && threshold>=0.3 && threshold<=1,"Ngưỡng từ 0,3 đến 1.");
        if(key=="session.minutes")Rules.Require(int.TryParse(value,out var minutes) && minutes>=1 && minutes<=120,"Khóa phiên từ 1 đến 120 phút.");
        var s=await db.Settings.SingleOrDefaultAsync(x=>x.Key==key); if(s==null){s=new AppSetting { Key=key };db.Settings.Add(s);}
        s.Value=key.Contains("secret") && value!=""?secrets.EncryptText(value):value; Audit(db,"SaveSetting","AppSetting",s.Id,key);
    });
    public async Task<string?> ReadLlmKeyAsync()
    {
        await using var db=Db(); await VerifyActorAsync(db,Permission.Settings); var value=(await db.Settings.SingleOrDefaultAsync(x=>x.Key=="llm.secret"))?.Value;
        return string.IsNullOrEmpty(value)?null:secrets.DecryptText(value);
    }
    public async Task<string> AnalyzeLlmAsync(DateTime startUtc,DateTime endUtc,CancellationToken cancellation=default)
    {
        Require(Permission.Reports);Rules.Require(externalInsights!=null,"LLM adapter chưa được cấu hình.");
        var analytics=await AnalyticsAsync(startUtc,endUtc);await using var db=Db();await VerifyActorAsync(db,Permission.Reports);
        var settings=await db.Settings.ToDictionaryAsync(x=>x.Key,x=>x.Value,cancellation);Rules.Require(settings.GetValueOrDefault("llm.enabled")=="true","LLM đang tắt.");
        var encrypted=settings.GetValueOrDefault("llm.secret");Rules.Require(!string.IsNullOrEmpty(encrypted),"Chưa cấu hình API key.");
        await WriteAsync(Permission.Reports,async d=>
        {
            var today=DateTime.UtcNow.Date;Rules.Require(await d.Audit.CountAsync(x=>x.Action=="RequestExternalAnalysis" && x.CreatedUtc>=today,cancellation)<20,"Đã đạt giới hạn 20 yêu cầu LLM/ngày. Thống kê local vẫn hoạt động.");
            Audit(d,"RequestExternalAnalysis","Report",null,$"From={startUtc:O}; To={endUtc:O}; AggregateOnly=true; MaxReplyTokens=600");
        });
        return await externalInsights!.AnalyzeAsync(analytics,settings.GetValueOrDefault("llm.endpoint")??"",settings.GetValueOrDefault("llm.model")??"",secrets.DecryptText(encrypted!),cancellation);
    }
    public async Task<ImportPreview> PreviewBorrowersAsync(string path,IReadOnlyDictionary<string,string>? mapping=null)
    {
        await using var db=Db(); await VerifyActorAsync(db,Permission.ManageBorrowers);
        return await Task.Run(async ()=>
        {
            var errors=new List<string>(); var rows=new List<BorrowerInput>(); var existing=await db.Borrowers.Select(x=>x.Code).ToListAsync(); var seen=existing.ToHashSet(StringComparer.OrdinalIgnoreCase);
            IReadOnlyList<string[]> table;
            try
            {
                if(Path.GetExtension(path).Equals(".xlsx",StringComparison.OrdinalIgnoreCase))
                {
                    using var book=new XLWorkbook(path); var sheet=book.Worksheet(1); var range=sheet.RangeUsed();
                    table=range==null?[]:range.Rows().Select(r=>Enumerable.Range(1,range.ColumnCount()).Select(c=>r.Cell(c).GetString()).ToArray()).ToList();
                }
                else table=Csv.Parse(await File.ReadAllTextAsync(path));
            }
            catch(Exception e) when(e is IOException or ArgumentException or FormatException) { return new ImportPreview([],[$"Không thể đọc file: {e.Message}"]); }
            if(table.Count<2)return new ImportPreview([],["File phải có tiêu đề và ít nhất một dòng dữ liệu."]);
            var headers=table[0]; var fields=new[]{"Code","Name","Class","Type","Contact"};
            var indexes=fields.ToDictionary(f=>f,f=>Array.FindIndex(headers,h=>h.Equals(mapping?.GetValueOrDefault(f)??f,StringComparison.OrdinalIgnoreCase)));
            if(indexes["Code"]<0 || indexes["Name"]<0)return new ImportPreview([],["Thiếu cột Code hoặc Name. Hãy xác nhận ánh xạ cột."]);
            for(int i=1;i<table.Count;i++)
            {
                var cells=table[i]; string Get(string field)=>indexes[field]>=0 && indexes[field]<cells.Length?cells[indexes[field]].Trim():"";
                if(cells.All(string.IsNullOrWhiteSpace))continue;
                var code=Get("Code").ToUpperInvariant(); var name=Get("Name");
                if(code.Length==0 || code.Length>80 || name.Length==0 || name.Length>200) {errors.Add($"Dòng {i+1}: mã/tên thiếu hoặc quá dài.");continue;}
                if(!seen.Add(code)) {errors.Add($"Dòng {i+1}: trùng mã {code}.");continue;}
                rows.Add(new(code,name,Get("Class"),Get("Type")==""?"Học sinh":Get("Type"),Get("Contact")));
            }
            return new ImportPreview(rows,errors);
        });
    }
    public Task ImportBorrowersAsync(ImportPreview preview) => WriteAsync(Permission.ManageBorrowers,async db=>
    {
        Rules.Require(preview.Errors.Count==0 && preview.Rows.Count>0,"Import có lỗi hoặc không có dữ liệu. Không có dòng nào được nhập.");
        Rules.Require(preview.Rows.Select(x=>CleanCode(x.Code)).Distinct().Count()==preview.Rows.Count,"Trùng mã trong file.");
        foreach(var row in preview.Rows) { Rules.Text(row.Name,"Tên"); var code=CleanCode(row.Code); Rules.Require(!await db.Borrowers.AnyAsync(x=>x.Code==code),$"Mã {code} đã tồn tại. Xem trước lại file."); db.Borrowers.Add(new Borrower { Code=code,DisplayName=row.Name,ClassName=row.ClassName,Type=row.Type,Contact=row.Contact }); }
        Audit(db,"ImportBorrowers","Borrower",null,$"Rows={preview.Rows.Count}");
    });
    public async Task<string> ExportAsync(string report,string path,DateTime startUtc,DateTime endUtc)
    {
        Require(Permission.Reports); Rules.Interval(startUtc,endUtc);
        if(report=="borrowers")Require(Permission.PersonalData);
        List<string[]> rows;
        switch(report)
        {
            case "assets": rows=[new[]{"Code","Name","Kind","Category","Location","Total","Available","Unit"},..(await AssetsAsync()).Select(x=>new[]{x.Code,x.Name,x.Kind.ToString(),x.Category,x.Location,x.Total.ToString(),x.Available.ToString(),x.Unit})];break;
            case "borrowers": rows=[new[]{"Code","Name","Class","Type","Contact"},..(await BorrowersAsync()).Select(x=>new[]{x.Code,x.Name,x.ClassName,x.Type,x.Contact??""})];break;
            case "loans": rows=[new[]{"Code","Borrower","Staff","Issued","Due","Status","Lines"},..(await LoansAsync(true)).Where(x=>x.IssuedUtc>=startUtc && x.IssuedUtc<endUtc).Select(x=>new[]{x.Code,x.Borrower,x.Staff,x.IssuedUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),x.DueUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),x.Status.ToString(),x.Summary})];break;
            case "reservations": rows=[new[]{"Code","Borrower","Start","End","Status"},..(await ReservationsAsync()).Where(x=>x.StartUtc>=startUtc && x.StartUtc<endUtc).Select(x=>new[]{x.Code,x.Borrower,x.StartUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),x.EndUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),x.Status.ToString()})];break;
            case "inventory": rows=[new[]{"Session","Created","Asset","Expected","Actual","Delta","Confirmed"},..(await InventoryAsync()).SelectMany(s=>s.Counts.Select(c=>new[]{s.Id.ToString(),s.CreatedUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),c.AssetId.ToString(),c.Expected.ToString(),c.Actual.ToString(),(c.Actual-c.Expected).ToString(),s.ConfirmedUtc?.ToString("O")??""}))];break;
            case "analytics": var a=await AnalyticsAsync(startUtc,endUtc);rows=[new[]{"Metric","Value"},new[]{"Loans",a.TotalLoans.ToString()},new[]{"UniqueBorrowers",a.UniqueBorrowers.ToString()},new[]{"OverdueRate",a.OverdueRate.ToString("P1")},new[]{"Utilization",a.Utilization.ToString("P1")},..a.TopAssets.Select(x=>new[]{x.Label,x.Value.ToString()})];break;
            default: throw new BusinessException("Loại báo cáo không được hỗ trợ.");
        }
        await Task.Run(async ()=>
        {
            var extension=Path.GetExtension(path).ToLowerInvariant();
            if(extension==".xlsx")
            {
                using var book=new XLWorkbook(); var sheet=book.AddWorksheet("Báo cáo");
                for(int r=0;r<rows.Count;r++)for(int c=0;c<rows[r].Length;c++)sheet.Cell(r+1,c+1).Value=rows[r][c];
                sheet.Row(1).Style.Font.Bold=true;sheet.Columns().AdjustToContents();book.SaveAs(path);
            }
            else if(extension==".pdf") SimplePdf.Write(path,rows);
            else { Rules.Require(extension==".csv","Chọn định dạng CSV, XLSX hoặc PDF."); await File.WriteAllTextAsync(path,Csv.Write(rows),new UTF8Encoding(true)); }
        });
        await WriteAsync(Permission.Reports,db=> { Audit(db,"ExportReport","Report",null,$"Type={report}; Rows={rows.Count-1}"); return Task.CompletedTask; }); return path;
    }
    public async Task<string> ExportQrAsync(string text,string path)
    {
        Require(Permission.Read); Rules.Text(text,"Nội dung QR",1000);
        await Task.Run(()=> { using var generator=new QRCodeGenerator();using var data=generator.CreateQrCode(text,QRCodeGenerator.ECCLevel.Q);using var png=new PngByteQRCode(data);File.WriteAllBytes(path,png.GetGraphic(12)); }); return path;
    }
}
public static class Csv
{
    public static string Write(IEnumerable<string[]> rows)=>string.Join("\r\n",rows.Select(r=>string.Join(',',r.Select(Escape))));
    private static string Escape(string value)
    {
        // Prevent spreadsheet formula execution on exported user-controlled cells.
        if(value.Length>0 && "=+-@\t\r".Contains(value[0]))value="'"+value;
        return "\""+value.Replace("\"","\"\"")+"\"";
    }
    public static IReadOnlyList<string[]> Parse(string text)
    {
        var rows=new List<string[]>();var cells=new List<string>();var cell=new StringBuilder();bool quoted=false;
        for(int i=0;i<text.Length;i++)
        {
            char c=text[i];
            if(c=='"') { if(quoted && i+1<text.Length && text[i+1]=='"'){cell.Append('"');i++;}else quoted=!quoted; }
            else if(c==',' && !quoted){cells.Add(cell.ToString());cell.Clear();}
            else if((c=='\n' || c=='\r') && !quoted){if(c=='\r' && i+1<text.Length && text[i+1]=='\n')i++;cells.Add(cell.ToString());rows.Add(cells.ToArray());cells.Clear();cell.Clear();}
            else if(c!='\uFEFF')cell.Append(c);
        }
        if(quoted)throw new FormatException("CSV có dấu ngoặc kép chưa đóng.");
        if(cell.Length>0 || cells.Count>0){cells.Add(cell.ToString());rows.Add(cells.ToArray());} return rows;
    }
}
