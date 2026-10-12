using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using STEMLabManager.Application;
using STEMLabManager.Domain;
namespace STEMLabManager.AI;

public sealed class Insights(HttpClient client) : IExternalInsights
{
    public static string Local(Analytics a)
    {
        var days=Math.Max(1,(a.EndUtc-a.StartUtc).TotalDays);
        var text=$"Nguồn: SQLite, {a.StartUtc.ToLocalTime():dd/MM/yyyy}–{a.EndUtc.ToLocalTime():dd/MM/yyyy}.\n"+
            $"{a.TotalLoans} phiếu; {a.UniqueBorrowers} người mượn; tỷ lệ quá hạn {a.OverdueRate:P1}; sử dụng {a.Utilization:P1}.\n"+
            $"Dự báo cơ sở 7 ngày: {a.TotalLoans/days*7:F1} phiếu (trung bình theo ngày trong kỳ).\n";
        if(a.TopAssets.Count>0)text+=$"Thiết bị được xuất nhiều nhất: {a.TopAssets[0].Label} ({a.TopAssets[0].Value} đơn vị).\n";
        text+=a.TotalLoans<30?"Dữ liệu ít: độ tin cậy thấp, chưa đủ kết luận về mùa vụ hoặc mua bổ sung.":"Độ tin cậy vừa: trung bình lịch sử không phản ánh thay đổi lịch học; rà soát tồn và các yêu cầu không đáp ứng trước khi mua.";
        return text;
    }
    public static object Aggregate(Analytics a) => new
    {
        startUtc=a.StartUtc,endUtc=a.EndUtc,loanCount=a.TotalLoans,distinctBorrowerCount=a.UniqueBorrowers,overdueRate=a.OverdueRate,utilization=a.Utilization,unfulfilledRequests=a.UnfulfilledRequests,
        topAssets=a.TopAssets.Take(5).Select((p,i)=>new {asset=$"asset-{i+1}",units=p.Value}),dailyWindowDays=Math.Min(a.Daily.Count,90),truncatedDaily=a.Daily.Count>90,daily=a.Daily.Select((p,i)=>new {day=i+1,loans=p.Value}).TakeLast(90)
    };
    public static string Preview(Analytics a)=>JsonSerializer.Serialize(Aggregate(a),new JsonSerializerOptions {WriteIndented=true});
    public async Task<string> AnalyzeAsync(Analytics a,string endpoint,string model,string key,CancellationToken cancellation=default)
    {
        Rules.Require(Uri.TryCreate(endpoint,UriKind.Absolute,out var uri) && uri.Scheme=="https","LLM endpoint phải là HTTPS.");Rules.Text(model,"Tên model");Rules.Text(key,"API key",4000);
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(cancellation);timeout.CancelAfter(TimeSpan.FromSeconds(25));
        using var request=new HttpRequestMessage(HttpMethod.Post,uri);
        request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",key);
        var messages=new[]{
            new {role="system",content="Viết tiếng Việt, ngắn gọn dưới 250 từ. Chỉ giải thích JSON tổng hợp được cung cấp. Không bịa KPI, danh tính hay giá mua. Không dùng công cụ, SQL, không sửa dữ liệu. Trả về tóm tắt, bằng chứng định lượng, khuyến nghị, độ tin cậy và giới hạn. Asset alias không phải tên thật."},
            new {role="user",content=Preview(a)} };
        // GPT-5 reasoning tokens share the completion budget; custom endpoints keep the legacy contract.
        object payload=model=="gpt-5" || model.StartsWith("gpt-5-2025-",StringComparison.Ordinal)
            ? new {model,max_completion_tokens=2048,reasoning_effort="minimal",verbosity="low",store=false,messages}
            : (object)new {model,max_tokens=600,temperature=0.1,messages};
        request.Content=JsonContent.Create(payload);
        try
        {
            using var response=await client.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,timeout.Token);
            if(!response.IsSuccessStatusCode)throw new BusinessException($"LLM trả lỗi HTTP {(int)response.StatusCode}. Kiểm tra cấu hình; thống kê local vẫn dùng được.");
            using var stream=await response.Content.ReadAsStreamAsync(timeout.Token);using var buffer=new MemoryStream();var chunk=new byte[8192];
            int read;while((read=await stream.ReadAsync(chunk,timeout.Token))>0){Rules.Require(buffer.Length+read<=256000,"Phản hồi LLM quá lớn.");await buffer.WriteAsync(chunk.AsMemory(0,read),timeout.Token);}var bytes=buffer.ToArray();
            using var json=JsonDocument.Parse(bytes);var content=json.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            Rules.Require(!string.IsNullOrWhiteSpace(content),"LLM trả nội dung trống.");return "Nhận xét LLM • cần đối chiếu các KPI local • "+DateTime.Now.ToString("dd/MM/yyyy HH:mm")+"\n"+content;
        }
        catch(OperationCanceledException) when(!cancellation.IsCancellationRequested){throw new BusinessException("LLM quá thời gian 25 giây. Thống kê local vẫn hoạt động.");}
        catch(HttpRequestException){throw new BusinessException("Không kết nối được LLM. Kiểm tra Internet; thống kê local vẫn hoạt động.");}
        catch(Exception e) when(e is JsonException or KeyNotFoundException or InvalidOperationException){throw new BusinessException("Phản hồi LLM không đúng định dạng chat completion.");}
    }
}
