using System.Net;
using System.Text.Json;
using STEMLabManager.AI;
using STEMLabManager.Application;
using Xunit;

namespace STEMLabManager.Tests;

public sealed class OpenAiInsightsTests
{
    [Fact]
    public void LocalSsaForecastRequiresHistoryAndReturnsSevenLocallyTrainedPointsWithBacktest()
    {
        var start=DateTime.Today.AddDays(-35).ToUniversalTime();var end=start.AddDays(35);
        var tooShort=Insights.Forecast(Enumerable.Range(0,20).Select(i=>new MetricPoint(i.ToString(),i%4)).ToArray(),start,end);
        Assert.False(tooShort.Ready);Assert.Empty(tooShort.Points);Assert.Contains("21",tooShort.Status);

        var daily=Enumerable.Range(0,35).Select(i=>new MetricPoint(i.ToString(),(i%7==0?3:1)+(i%5==0?1:0))).ToArray();
        var forecast=Insights.Forecast(daily,start,end);
        Assert.True(forecast.Ready,forecast.Status);Assert.Equal(7,forecast.Points.Count);
        Assert.Equal(start.ToLocalTime().Date.AddDays(daily.Length).ToString("dd/MM"),forecast.Points[0].Label);
        Assert.All(forecast.Points,p=>Assert.True(double.IsFinite(p.Value)&&p.Value>=0));
        Assert.NotNull(forecast.BacktestMae);Assert.True(double.IsFinite(forecast.BacktestMae!.Value));
        Assert.True(forecast.BacktestMae.HasValue);Assert.Contains("dự báo lệch",forecast.Status);Assert.DoesNotContain("SSA",forecast.Status);Assert.DoesNotContain("MAE",forecast.Status);
    }
    [Theory]
    [InlineData("gpt-5",true)]
    [InlineData("gpt-5-2025-08-07",true)]
    [InlineData("custom-model",false)]
    public async Task PayloadMatchesModelAndNeverIncludesKeyOrPersonalLabels(string model,bool reasoning)
    {
        var handler=new CaptureHandler();using var client=new HttpClient(handler);
        var analytics=new Analytics(DateTime.UtcNow.AddDays(-1),DateTime.UtcNow,1,0,0,0,1,1,0,[],[new("PRIVATE student name",1)],[]);
        var result=await new Insights(client).AnalyzeAsync(analytics,"https://api.openai.com/v1/chat/completions",model,"fake-api-key");
        Assert.Contains("Test analysis",result);Assert.Equal("Bearer",handler.Scheme);
        Assert.DoesNotContain("fake-api-key",handler.Body);Assert.DoesNotContain("PRIVATE",handler.Body);
        using var json=JsonDocument.Parse(handler.Body);var root=json.RootElement;
        Assert.Equal(model,root.GetProperty("model").GetString());
        if(reasoning)
        {
            Assert.Equal(2048,root.GetProperty("max_completion_tokens").GetInt32());
            Assert.Equal("minimal",root.GetProperty("reasoning_effort").GetString());
            Assert.False(root.GetProperty("store").GetBoolean());
            Assert.False(root.TryGetProperty("temperature",out _));Assert.False(root.TryGetProperty("max_tokens",out _));
        }
        else{Assert.Equal(600,root.GetProperty("max_tokens").GetInt32());Assert.True(root.TryGetProperty("temperature",out _));}
    }
    private sealed class CaptureHandler:HttpMessageHandler
    {
        public string Body="";public string? Scheme;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
        {
            Body=await request.Content!.ReadAsStringAsync(cancellationToken);Scheme=request.Headers.Authorization?.Scheme;
            return new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("{\"choices\":[{\"message\":{\"content\":\"Test analysis\"}}]}")};
        }
    }
}
