using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Microsoft.Extensions.Logging.Abstractions;
using STEMLabManager.Desktop;
using STEMLabManager.Desktop.ViewModels;
using STEMLabManager.Desktop.Views;
using STEMLabManager.Infrastructure;
using STEMLabManager.AI;
using STEMLabManager.Domain;
using Xunit;
namespace STEMLabManager.UITests;

public sealed class DemoRecordingTests
{
    [AvaloniaFact] public async Task RecordRealUiStatesForSyntheticDataWalkthrough()
    {
        var directory=Path.Combine(Path.GetTempPath(),"stem-record-"+Guid.NewGuid().ToString("N"));var service=new LabService(directory,true);await service.InitializeAsync();await service.SetupAsync("demo-admin","DemoPassword123!","Quản lý demo","Phòng STEM minh họa");await service.SeedDemoAsync();
        var vm=new MainViewModel(service,new RecordingDialogs(),new Insights(new HttpClient()),new FaceEngine(),new CameraCapture(),NullLogger<MainViewModel>.Instance);await vm.InitializeAsync();vm.Username="demo-admin";vm.Password="DemoPassword123!";await vm.LoginCommand.ExecuteAsync(null);Assert.Equal("",vm.Error);
        var window=new MainWindow {DataContext=vm,Width=1366,Height=768};window.Show();
        var frames=Path.Combine(AppContext.BaseDirectory,"DemoFrames");Directory.CreateDirectory(frames);int frame=0;
        void Capture(Window target)
        {
            target.UpdateLayout();using var bitmap=target.CaptureRenderedFrame();Assert.NotNull(bitmap);bitmap!.Save(Path.Combine(frames,$"frame-{frame++:000}.png"),Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
        }
        async Task Page(string name)
        {vm.SelectedNav=vm.Navigation.Single(n=>n.Key==name);for(int i=0;i<100 && vm.Busy;i++)await Task.Delay(20);Assert.False(vm.Busy);Assert.Equal("",vm.Error);Capture(window);}
        try
        {
            Capture(window);await Page("assets");vm.Search="Arduino";Capture(window);vm.Search="";await Page("reservations");
            var wizard=new LoanWizardViewModel(service,false);await wizard.LoadAsync();wizard.Borrower=wizard.Borrowers.First();var dialog=new LoanWizardWindow {DataContext=wizard,Width=1366,Height=768};dialog.Show();
            try
            {
                Capture(dialog);wizard.NextCommand.Execute(null);wizard.Asset=wizard.Assets.First(a=>a.Kind==AssetKind.Unit && a.Available>=2);wizard.Quantity="2";wizard.AddLineCommand.Execute(null);Capture(dialog);wizard.NextCommand.Execute(null);wizard.Purpose="Thực hành STEM minh họa";Capture(dialog);await wizard.ConfirmCommand.ExecuteAsync(null);Assert.Equal("",wizard.Error);
            }
            finally{dialog.Close();}
            await Page("loans");var issued=(await service.LoansAsync()).First();await service.ReturnAsync(issued.Id,[new(issued.Lines[0].Id,1,ReturnCondition.Damaged)],"Hỏng trong demo");await vm.RefreshCommand.ExecuteAsync(null);Capture(window);await Page("maintenance");await Page("analytics");await Page("reports");await Page("backup");Assert.Equal(13,frame);
        }
        finally{window.Close();service.Logout();Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();Directory.Delete(directory,true);}
    }
    private sealed class RecordingDialogs : IDialogs
    {
        public Task<bool> FormAsync(string t,string d,IEnumerable<FormField> f,Func<FormViewModel,Task> s)=>Task.FromResult(false);
        public Task<bool> ConfirmAsync(string t,string d)=>Task.FromResult(false);
        public Task<bool> PreviewAsync(string t,string d,byte[] image)=>Task.FromResult(false);
        public Task<bool> PreviewLiveAsync(string t,string d,byte[] image,Func<CancellationToken,Task<byte[]>> readFrame)=>Task.FromResult(false);
        public Task<string?> OpenAsync(string t,params string[] patterns)=>Task.FromResult<string?>(null);
        public Task<string?> SaveAsync(string t,string filename,params string[] patterns)=>Task.FromResult<string?>(null);
        public Task<string?> FolderAsync(string t)=>Task.FromResult<string?>(null);
        public Task<bool> WizardAsync(LoanWizardViewModel vm)=>Task.FromResult(false);
        public Task<bool> AnalyticsAsync(MainViewModel vm)=>Task.FromResult(false);
    }
}
