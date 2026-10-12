using STEMLabManager.Infrastructure;
using STEMLabManager.Application;
using STEMLabManager.Domain;
using Xunit;
namespace STEMLabManager.Tests;

public sealed class SecurityAndSchedulingTests
{
    [Fact] public async Task ScheduledBackupRunsForManagerButManualBackupAndSecretsStayAdminOnly()
    {
        var directory=Path.Combine(Path.GetTempPath(),"stem-scheduled-"+Guid.NewGuid().ToString("N"));var service=new LabService(directory);
        try
        {
            await service.InitializeAsync();await service.SetupAsync("admin","SchedulingPassword123!","Admin","Room");await service.SaveSettingAsync("llm.secret","test-secret");await service.SaveUserAsync(null,"manager","Manager",Role.LabManager,"SchedulingPassword123!",true);await service.LoginAsync("manager","SchedulingPassword123!");
            await service.RunScheduledBackupAsync();Assert.Single(Directory.GetFiles(Path.Combine(directory,"Backups"),"*.zip"));await service.RunScheduledBackupAsync();Assert.Single(Directory.GetFiles(Path.Combine(directory,"Backups"),"*.zip"));
            await Assert.ThrowsAsync<BusinessException>(()=>service.BackupAsync());await Assert.ThrowsAsync<BusinessException>(()=>service.ReadLlmKeyAsync());Assert.Contains(await service.AuditAsync(),x=>x.Action=="ScheduledBackup" && x.ActorId==null);
        }
        finally{service.Logout();Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();Directory.Delete(directory,true);}
    }
    [Fact] public async Task ServiceDerivesLlmMetricsInternallyAndNeverExposesKeyToViewer()
    {
        var directory=Path.Combine(Path.GetTempPath(),"stem-llm-"+Guid.NewGuid().ToString("N"));var gateway=new CapturingGateway();var service=new LabService(directory,false,gateway);
        try
        {
            await service.InitializeAsync();await service.SetupAsync("admin","LlmPassword123!","Admin","Room");await service.SaveSettingAsync("llm.secret","stored-key");await service.SaveSettingAsync("llm.endpoint","https://example.invalid/chat");await service.SaveSettingAsync("llm.model","configured-model");await service.SaveSettingAsync("llm.enabled","true");await service.SaveUserAsync(null,"viewer","Viewer",Role.Viewer,"LlmPassword123!",true);await service.LoginAsync("viewer","LlmPassword123!");
            var result=await service.AnalyzeLlmAsync(DateTime.UtcNow.AddDays(-1),DateTime.UtcNow.AddDays(1));Assert.Equal("Test insight",result);Assert.NotNull(gateway.Analytics);Assert.Equal(0,gateway.Analytics!.TotalLoans);Assert.Equal("stored-key",gateway.Key);await Assert.ThrowsAsync<BusinessException>(()=>service.ReadLlmKeyAsync());
            for(int i=1;i<20;i++)await service.AnalyzeLlmAsync(DateTime.UtcNow.AddDays(-1),DateTime.UtcNow.AddDays(1));await Assert.ThrowsAsync<BusinessException>(()=>service.AnalyzeLlmAsync(DateTime.UtcNow.AddDays(-1),DateTime.UtcNow.AddDays(1)));
        }
        finally{service.Logout();Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();Directory.Delete(directory,true);}
    }
    private sealed class CapturingGateway : IExternalInsights
    {
        public Analytics? Analytics {get;private set;}public string? Key {get;private set;}
        public Task<string> AnalyzeAsync(Analytics analytics,string endpoint,string model,string key,CancellationToken cancellation=default){Analytics=analytics;Key=key;return Task.FromResult("Test insight");}
    }
}
