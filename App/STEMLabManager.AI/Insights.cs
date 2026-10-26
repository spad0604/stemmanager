using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.TimeSeries;
using STEMLabManager.Application;
using STEMLabManager.Domain;
namespace STEMLabManager.AI;

public sealed record LocalForecast(bool Ready,IReadOnlyList<MetricPoint> Points,double? BacktestMae,string Status);
internal sealed class LoanCount { public float Value {get;set;} }
internal sealed class ForecastOutput
{
    [ColumnName("Forecast"),VectorType(7)] public float[] Forecast {get;set;}=[];
    [ColumnName("Lower"),VectorType(7)] public float[] Lower {get;set;}=[];
    [ColumnName("Upper"),VectorType(7)] public float[] Upper {get;set;}=[];
}

public sealed class Insights(HttpClient client) : IExternalInsights
{
    public static LocalForecast Forecast(IReadOnlyList<MetricPoint> daily,DateTime startUtc,DateTime endUtc)
    {
        const int window=7,horizon=7;
        var values=daily.Select(x=>(float)Math.Max(0,x.Value)).ToArray();
        if(values.Length<21)return new(false,[],null,$"Cần ít nhất 21 ngày dữ liệu để ước tính; hiện có {values.Length} ngày.");
        try
        {
            var ml=new MLContext(seed:17);
            float[]? Backtest(int trainSize,int testSize)
            {
                var rows=values.Take(trainSize).Select(x=>new LoanCount{Value=x});
                var data=ml.Data.LoadFromEnumerable(rows);
                var estimator=ml.Forecasting.ForecastBySsa("Forecast","Value",window,trainSize,trainSize,testSize,
                    confidenceLowerBoundColumn:"Lower",confidenceUpperBoundColumn:"Upper",confidenceLevel:0.95f);
                var model=estimator.Fit(data);
                return model.CreateTimeSeriesEngine<LoanCount,ForecastOutput>(ml).Predict().Forecast;
            }
            double? mae=null;
            if(values.Length>=28)
            {
                var testSize=Math.Min(horizon,values.Length-21);
                var heldOut=Backtest(values.Length-testSize,testSize);
                if(heldOut?.Length>=testSize)mae=Enumerable.Range(0,testSize).Average(i=>Math.Abs(heldOut[i]-values[values.Length-testSize+i]));
            }
            var forecast=Backtest(values.Length,horizon);
            if(forecast==null || forecast.Length!=horizon || forecast.Any(x=>!float.IsFinite(x)))
                return new(false,[],mae,"Chưa thể ước tính nhu cầu từ dữ liệu hiện có. Biểu đồ số liệu thực tế vẫn dùng được.");
            var first=startUtc.ToLocalTime().Date.AddDays(daily.Count);
            var points=forecast.Select((value,i)=>new MetricPoint(first.AddDays(i).ToString("dd/MM"),Math.Max(0,Math.Round(value,1)))).ToArray();
            var status=mae is { } error?$"Khi thử trên dữ liệu cũ, dự báo lệch trung bình khoảng {error:F1} phiếu mỗi ngày.":"Dữ liệu còn ít để kiểm tra độ chính xác của dự báo.";
            return new(true,points,mae,status);
        }
        catch(Exception e) when(e is not OutOfMemoryException and not StackOverflowException)
        {return new(false,[],null,"Chưa thể ước tính nhu cầu từ dữ liệu hiện có. Biểu đồ số liệu thực tế vẫn dùng được.");}
    }
    public static string Local(Analytics a,LocalForecast? forecast=null)
    {
        var text=$"Trong kỳ {a.StartUtc.ToLocalTime():dd/MM/yyyy}–{a.EndUtc.ToLocalTime():dd/MM/yyyy}, phòng ghi nhận {a.TotalLoans} phiếu mượn từ {a.UniqueBorrowers} người.\n"+
            $"Có {a.Overdue} phiếu đang quá hạn ({a.OverdueRate:P1}); mức sử dụng thiết bị là {a.Utilization:P1}.\n"+
            (forecast?.Ready==true?$"Tuần tới có thể có khoảng {forecast.Points.Sum(x=>x.Value):F0} lượt mượn.\n":"Chưa có đủ dữ liệu để ước tính nhu cầu tuần tới.\n");
        if(a.TopAssets.Count>0)text+=$"Thiết bị được mượn nhiều nhất là {a.TopAssets[0].Label}, với {a.TopAssets[0].Value} lượt.\n";
        text+=a.TotalLoans<30?"Dữ liệu hiện còn ít; hãy xem dự báo như gợi ý và kiểm tra lịch học, tồn kho trước khi quyết định.":"Dự báo dựa trên lịch sử nên có thể thay đổi theo lịch học; hãy đối chiếu với lịch đặt và tồn kho.";
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
