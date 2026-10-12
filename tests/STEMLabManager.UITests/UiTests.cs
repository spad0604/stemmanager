using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Logging.Abstractions;
using STEMLabManager.Application;
using STEMLabManager.Infrastructure;
using STEMLabManager.AI;
using STEMLabManager.Domain;
using STEMLabManager.Desktop;
using STEMLabManager.Desktop.ViewModels;
using STEMLabManager.Desktop.Views;
using Xunit;

[assembly: AvaloniaTestApplication(typeof(STEMLabManager.UITests.TestAppBuilder))]
namespace STEMLabManager.UITests;

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()=>AppBuilder.Configure<TestApplication>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions {UseHeadlessDrawing=false});
}
public sealed class TestApplication : Avalonia.Application
{
    public override void Initialize(){var app=new STEMLabManager.Desktop.App();app.Initialize();var styles=app.Styles.ToArray();app.Styles.Clear();foreach(var style in styles)Styles.Add(style);var resources=app.Resources;app.Resources=new Avalonia.Controls.ResourceDictionary();Resources=resources;RequestedThemeVariant=Avalonia.Styling.ThemeVariant.Light;}
}
public sealed class UiTests
{
    private static MainViewModel Vm(ILabService service,IDialogs dialogs)=>new(service,dialogs,new Insights(new HttpClient()),new FaceEngine(),new CameraCapture(),NullLogger<MainViewModel>.Instance);
    [AvaloniaFact] public async Task SetupLoginNavigationAndDashboardUseSqlite()
    {
        var directory=Path.Combine(Path.GetTempPath(),"stem-ui-"+Guid.NewGuid().ToString("N"));var service=new LabService(directory);
        var vm=Vm(service,new TestDialogs());var window=new MainWindow {DataContext=vm,Width=1366,Height=768};window.Show();
        try
        {
            await vm.InitializeAsync();Assert.True(vm.NeedsSetup);vm.Username="admin";vm.Password="UiPassword123!";vm.DisplayName="UI Admin";await vm.LoginCommand.ExecuteAsync(null);
            Assert.Equal("",vm.Error);Assert.True(vm.Authenticated);Assert.Equal(17,vm.Navigation.Count);Assert.Equal(4,vm.Kpis.Count);Assert.Equal("0",vm.Kpis[0].Value);
            vm.SelectedNav=vm.Navigation.Single(n=>n.Key=="assets");await Wait(vm);Assert.True(vm.IsEmpty);
            vm.Logout();Assert.False(vm.Authenticated);
            window.UpdateLayout();using var frame=window.CaptureRenderedFrame();Assert.NotNull(frame);
            var screenshots=Path.Combine(AppContext.BaseDirectory,"Screenshots");Directory.CreateDirectory(screenshots);frame!.Save(Path.Combine(screenshots,"setup-login-1366.png"),Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
        }
        finally{window.Close();service.Logout();Directory.Delete(directory,true);}
    }
    [AvaloniaFact] public async Task WizardIssuesAndReturnsThroughViewModelServiceAndDatabase()
    {
        var directory=Path.Combine(Path.GetTempPath(),"stem-ui-"+Guid.NewGuid().ToString("N"));var service=new LabService(directory);await service.InitializeAsync();await service.SetupAsync("admin","UiPassword123!","Admin","Room");
        var category=(await service.CategoriesAsync())[0].Id;var location=(await service.LocationsAsync())[0].Id;
        var borrower=await service.SaveBorrowerAsync(null,new("B001","Demo user","7A"));var asset=await service.SaveAssetAsync(null,new("A001","Arduino",AssetKind.Unit,category,location,"cái",2));
        var wizard=new LoanWizardViewModel(service,false);await wizard.LoadAsync();wizard.Borrower=wizard.Borrowers.Single();wizard.NextCommand.Execute(null);wizard.Asset=wizard.Assets.Single();wizard.Quantity="2";wizard.AddLineCommand.Execute(null);wizard.NextCommand.Execute(null);wizard.Purpose="Bài học STEM";
        var window=new LoanWizardWindow {DataContext=wizard,Width=820,Height=720};window.Show();
        try
        {
            await wizard.ConfirmCommand.ExecuteAsync(null);Assert.Equal("",wizard.Error);Assert.Single(await service.LoansAsync());Assert.Equal(0,(await service.AssetsAsync()).Single().Available);
            var loan=(await service.LoansAsync()).Single();var form=new FormViewModel("Trả", "",[new("quantity","Số lượng","1")],f=>service.ReturnAsync(loan.Id,[new(loan.Lines[0].Id,f.Integer("quantity"),ReturnCondition.Damaged)],"Hỏng"));
            await form.SaveCommand.ExecuteAsync(null);Assert.Equal("",form.Error);Assert.Single(await service.MaintenanceAsync());Assert.Equal(LoanStatus.Open,(await service.LoansAsync()).Single().Status);
            window.UpdateLayout();using var frame=window.CaptureRenderedFrame();Assert.NotNull(frame);
        }
        finally{window.Close();service.Logout();Directory.Delete(directory,true);}
    }
    [AvaloniaTheory] [InlineData(1366,768,1.0)] [InlineData(1366,768,1.5)] [InlineData(1600,900,1.25)] [InlineData(1920,1080,1.5)]
    public async Task DemoScreensRenderAndAllPagesLoad(int width,int height,double scaling)
    {
        var directory=Path.Combine(Path.GetTempPath(),"stem-ui-"+Guid.NewGuid().ToString("N"));var service=new LabService(directory,true);await service.InitializeAsync();await service.SetupAsync("admin","UiPassword123!","Admin","Demo room");await service.SeedDemoAsync();
        var vm=Vm(service,new TestDialogs());await vm.InitializeAsync();vm.Username="admin";vm.Password="UiPassword123!";await vm.LoginCommand.ExecuteAsync(null);
        var window=new MainWindow {DataContext=vm,Width=width/scaling,Height=height/scaling};window.Show();window.SetRenderScaling(scaling);
        try
        {
            Assert.Equal("",vm.Error);Assert.Equal(9,(await service.LoansAsync()).Count);Assert.True((await service.AnalyticsAsync(DateTime.UtcNow.AddYears(-1),DateTime.UtcNow.AddDays(1))).TotalLoans>=200);
            foreach(var nav in vm.Navigation.ToList())
            {vm.SelectedNav=nav;await Wait(vm);Assert.Equal("",vm.Error);Assert.NotEmpty(vm.Title);window.UpdateLayout();using var frame=window.CaptureRenderedFrame();Assert.NotNull(frame);if(nav.Key=="dashboard"){var screenshots=Path.Combine(AppContext.BaseDirectory,"Screenshots");Directory.CreateDirectory(screenshots);frame!.Save(Path.Combine(screenshots,$"dashboard-{width}-{scaling*100:0}.png"),Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);}}
            Avalonia.Application.Current!.RequestedThemeVariant=Avalonia.Styling.ThemeVariant.Dark;vm.SelectedNav=vm.Navigation.Single(n=>n.Key=="dashboard");await Wait(vm);window.UpdateLayout();using var dark=window.CaptureRenderedFrame();Assert.NotNull(dark);dark!.Save(Path.Combine(AppContext.BaseDirectory,"Screenshots",$"dark-{width}-{scaling*100:0}.png"),Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);Avalonia.Application.Current.RequestedThemeVariant=Avalonia.Styling.ThemeVariant.Light;
        }
        finally{window.Close();service.Logout();Directory.Delete(directory,true);}
    }
    [AvaloniaFact] public async Task ViewerNavigationDoesNotExposeWritePages()
    {
        var directory=Path.Combine(Path.GetTempPath(),"stem-ui-"+Guid.NewGuid().ToString("N"));var service=new LabService(directory);await service.InitializeAsync();await service.SetupAsync("admin","UiPassword123!","Admin","Room");await service.SaveUserAsync(null,"viewer","Viewer",Role.Viewer,"UiPassword123!",true);
        var vm=Vm(service,new TestDialogs());await vm.InitializeAsync();vm.Username="viewer";vm.Password="UiPassword123!";await vm.LoginCommand.ExecuteAsync(null);
        try{Assert.True(vm.Authenticated);Assert.DoesNotContain(vm.Navigation,n=>n.Key is "settings" or "users" or "face" or "backup" or "loans");Assert.DoesNotContain(vm.Actions,a=>a.Label=="Tạo phiếu mượn");}
        finally{service.Logout();Directory.Delete(directory,true);}
    }
    private static async Task Wait(MainViewModel vm){for(int i=0;i<100 && vm.Busy;i++)await Task.Delay(20);Assert.False(vm.Busy);}
    private sealed class TestDialogs : IDialogs
    {
        public Task<bool> FormAsync(string title,string description,IEnumerable<FormField> fields,Func<FormViewModel,Task> save)=>Task.FromResult(false);
        public Task<bool> ConfirmAsync(string title,string explanation)=>Task.FromResult(false);
        public Task<bool> PreviewAsync(string title,string explanation,byte[] image)=>Task.FromResult(false);
        public Task<string?> OpenAsync(string title,params string[] patterns)=>Task.FromResult<string?>(null);
        public Task<string?> SaveAsync(string title,string filename,params string[] patterns)=>Task.FromResult<string?>(null);
        public Task<string?> FolderAsync(string title)=>Task.FromResult<string?>(null);
        public Task<bool> WizardAsync(LoanWizardViewModel vm)=>Task.FromResult(false);
    }
}
