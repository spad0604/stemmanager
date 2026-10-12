using STEMLabManager.Domain;
namespace STEMLabManager.Desktop.ViewModels;
public partial class MainViewModel
{
    private DateTime nextBackupCheckUtc=DateTime.UtcNow;
    public async Task MaintainAsync()
    {
        if(!Authenticated || Busy || DateTime.UtcNow<nextBackupCheckUtc)return;
        nextBackupCheckUtc=DateTime.UtcNow.AddHours(1);
        await RunAsync(()=>service.RunScheduledBackupAsync());
    }
}
