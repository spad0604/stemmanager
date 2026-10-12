using System.Net;
using System.Text.Json;
using STEMLabManager.AI;
using STEMLabManager.Application;
using Xunit;

namespace STEMLabManager.Tests;

public sealed class OpenAiInsightsTests
{
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
