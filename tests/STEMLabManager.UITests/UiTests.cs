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
    [Fact] public void DateTimeFormFieldCombinesCalendarDateAndClockTime()
    {
        var original=new DateTime(2026,10,15,9,30,0,DateTimeKind.Local);var field=new FormField("due","Hạn trả",original.ToString("dd/MM/yyyy HH:mm"),dateTime:true);
        Assert.False(field.IsText);Assert.True(field.IsDateTime);Assert.Equal(original.ToString("dd/MM/yyyy HH:mm"),field.Result);Assert.Equal("15/10/2026  09:30",field.DateTimeDisplay);
        var selected=new DateTime(2026,10,18,0,0,0,DateTimeKind.Local);field.Date=new DateTimeOffset(selected);field.Time=new TimeSpan(16,25,0);
        Assert.Equal("18/10/2026 16:25",field.Result);Assert.Equal("18/10/2026  16:25",field.DateTimeDisplay);Assert.Equal(DateTime.SpecifyKind(new DateTime(2026,10,18,16,25,0),DateTimeKind.Local).ToUniversalTime(),field.UtcValue());
    }
    [AvaloniaFact] public async Task SetupLoginNavigationAndDashboardUseSqlite()
    {
        var directory=Path.Combine(Path.GetTempPath(),"stem-ui-"+Guid.NewGuid().ToString("N"));var service=new LabService(directory);
        var vm=Vm(service,new TestDialogs());var window=new MainWindow {DataContext=vm,Width=1366,Height=768};window.Show();
        try
        {
            await vm.InitializeAsync();Assert.True(vm.NeedsSetup);vm.Username="admin";vm.Password="UiPassword123!";vm.DisplayName="UI Admin";await vm.LoginCommand.ExecuteAsync(null);
            Assert.Equal("",vm.Error);Assert.True(vm.Authenticated);Assert.Equal("true",(await service.SettingsAsync())["face.enabled"]);Assert.Equal(17,vm.Navigation.Count);Assert.Equal(4,vm.Kpis.Count);Assert.Equal("0",vm.Kpis[0].Value);
            await service.SaveSettingAsync("face.enabled","false");await service.InitializeAsync();Assert.Equal("false",(await service.SettingsAsync())["face.enabled"]);
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
        var window=new LoanWizardWindow {DataContext=wizard,Width=820,Height=720};window.Show();window.UpdateLayout();Assert.NotNull(window.FindControl<DateTimeField>("DueDateTimeField"));Assert.NotNull(window.FindControl<DateTimeField>("StartDateTimeField"));
        try
        {
            await wizard.ConfirmCommand.ExecuteAsync(null);Assert.Equal("",wizard.Error);Assert.Single(await service.LoansAsync());Assert.Equal(0,(await service.AssetsAsync()).Single().Available);
            var loan=(await service.LoansAsync()).Single();var form=new FormViewModel("Trả", "",[new("quantity","Số lượng","1")],f=>service.ReturnAsync(loan.Id,[new(loan.Lines[0].Id,f.Integer("quantity"),ReturnCondition.Damaged)],"Hỏng"));
            await form.SaveCommand.ExecuteAsync(null);Assert.Equal("",form.Error);Assert.Single(await service.MaintenanceAsync());Assert.Equal(LoanStatus.Open,(await service.LoansAsync()).Single().Status);
            window.UpdateLayout();using var frame=window.CaptureRenderedFrame();Assert.NotNull(frame);
        }
        finally{window.Close();service.Logout();Directory.Delete(directory,true);}
    }
    [AvaloniaFact] public async Task FaceLookupSelectsBorrowerButDoesNotIssueLoan()
    {
        var directory=Path.Combine(Path.GetTempPath(),"stem-ui-face-lookup-"+Guid.NewGuid().ToString("N"));var service=new LabService(directory);
        try
        {
            await service.InitializeAsync();await service.SetupAsync("admin","UiPassword123!","Admin","Room");
            var borrowerId=await service.SaveBorrowerAsync(null,new("B-FACE","Face borrower","7A"));var borrower=(await service.BorrowersAsync()).Single(x=>x.Id==borrowerId);
            var wizard=new LoanWizardViewModel(service,false,()=>Task.FromResult<BorrowerRow?>(borrower));await wizard.LoadAsync();
            await wizard.LookupFaceCommand.ExecuteAsync(null);
            Assert.Equal(borrowerId,wizard.Borrower?.Id);Assert.Equal(1,wizard.Step);Assert.Empty(await service.LoansAsync());Assert.True(wizard.CanLookupFace);
        }
        finally{service.Logout();Directory.Delete(directory,true);}
    }
    [AvaloniaFact] public async Task AnalyticsOpensAsAChartPopup()
    {
        var directory=Path.Combine(Path.GetTempPath(),"stem-ui-analytics-popup-"+Guid.NewGuid().ToString("N"));var service=new LabService(directory,true);
        try
        {
            await service.InitializeAsync();await service.SetupAsync("admin","UiPassword123!","Admin","Room");await service.SeedDemoAsync();
            var vm=Vm(service,new TestDialogs());await vm.InitializeAsync();vm.Username="admin";vm.Password="UiPassword123!";await vm.LoginCommand.ExecuteAsync(null);vm.SelectedNav=vm.Navigation.Single(n=>n.Key=="analytics");await Wait(vm);
            var popup=new AnalyticsWindow(vm){Width=1120,Height=820};popup.Show();popup.UpdateLayout();
            try{Assert.NotNull(popup.FindControl<STEMLabManager.Desktop.Controls.TrendChart>("TrendChart"));Assert.NotNull(popup.FindControl<STEMLabManager.Desktop.Controls.ForecastChart>("ForecastChart"));using var frame=popup.CaptureRenderedFrame();Assert.NotNull(frame);}
            finally{popup.Close();}
        }
        finally{service.Logout();Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();Directory.Delete(directory,true);}
    }
    [AvaloniaFact] public async Task DashboardKpisOpenTheirFilteredDetailPages()
    {
        var directory=Path.Combine(Path.GetTempPath(),"stem-ui-kpi-"+Guid.NewGuid().ToString("N"));var service=new LabService(directory,true);
        try
        {
            await service.InitializeAsync();await service.SetupAsync("admin","UiPassword123!","Admin","Room");await service.SeedDemoAsync();
            var vm=Vm(service,new TestDialogs());await vm.InitializeAsync();vm.Username="admin";vm.Password="UiPassword123!";await vm.LoginCommand.ExecuteAsync(null);
            Assert.Equal(4,vm.Kpis.Count);vm.Kpis[0].Command!.Execute(null);await Task.Delay(100);await Wait(vm);
            Assert.Equal("assets",vm.SelectedNav?.Key);Assert.NotEmpty(vm.Rows);Assert.All(vm.Rows,row=>Assert.True(((AssetRow)row.Source).Available>0));
            vm.SelectedNav=vm.Navigation.Single(n=>n.Key=="dashboard");await Task.Delay(100);await Wait(vm);
            vm.Kpis[1].Command!.Execute(null);await Task.Delay(100);await Wait(vm);
            Assert.Equal("loans",vm.SelectedNav?.Key);Assert.NotEmpty(vm.Rows);Assert.All(vm.Rows,row=>Assert.IsType<LoanRow>(row.Source));
        }
        finally{service.Logout();Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();Directory.Delete(directory,true);}
    }
    [AvaloniaFact] public async Task WizardShowsEmptyAndPopulatedDeviceStatesAndLookupSetupWorks()
    {
        var directory=Path.Combine(Path.GetTempPath(),"stem-ui-options-"+Guid.NewGuid().ToString("N"));var service=new LabService(directory);
        try
        {
            await service.InitializeAsync();await service.SetupAsync("admin","UiPassword123!","Admin","Room");
            await service.SaveSettingAsync("room","Room 204");await service.SaveLookupAsync(true,null,"Robotics");await service.SaveLookupAsync(false,null,"Cabinet 2");
            Assert.Equal("Room 204",(await service.SettingsAsync())["room"]);Assert.Contains(await service.CategoriesAsync(),x=>x.Name=="Robotics");Assert.Contains(await service.LocationsAsync(),x=>x.Name=="Cabinet 2");
            var emptyBorrowers=new LoanWizardViewModel(service,true);await emptyBorrowers.LoadAsync();Assert.False(emptyBorrowers.HasBorrowers);Assert.False(emptyBorrowers.HasAssets);

            var category=(await service.CategoriesAsync()).Single(x=>x.Name=="Robotics").Id;var location=(await service.LocationsAsync()).Single(x=>x.Name=="Cabinet 2").Id;
            var borrowerId=await service.SaveBorrowerAsync(null,new("B001","Wizard borrower","7A"));var emptyAssets=new LoanWizardViewModel(service,true);await emptyAssets.LoadAsync();emptyAssets.Borrower=emptyAssets.Borrowers.Single(x=>x.Id==borrowerId);emptyAssets.NextCommand.Execute(null);Assert.True(emptyAssets.StepTwo);Assert.False(emptyAssets.HasAssets);
            var emptyWindow=new LoanWizardWindow {DataContext=emptyAssets,Width=820,Height=720};emptyWindow.Show();emptyWindow.UpdateLayout();using(var frame=emptyWindow.CaptureRenderedFrame()){Assert.NotNull(frame);var path=Path.Combine(AppContext.BaseDirectory,"Screenshots");Directory.CreateDirectory(path);frame!.Save(Path.Combine(path,"reservation-empty-assets.png"),Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);}emptyWindow.Close();

            await service.SaveAssetAsync(null,new("BULK1","Micro:bit kits",AssetKind.Bulk,category,location,"kit",5));var unitAsset=await service.SaveAssetAsync(null,new("UNIT1","Robot arm",AssetKind.Unit,category,location,"unit",1));
            var populated=new LoanWizardViewModel(service,true);await populated.LoadAsync();Assert.True(populated.HasBorrowers);Assert.True(populated.HasAssets);populated.Borrower=populated.Borrowers.Single(x=>x.Id==borrowerId);populated.NextCommand.Execute(null);populated.Asset=populated.Assets.Single(x=>x.Id==unitAsset);for(var i=0;i<100&&!populated.HasUnits;i++)await Task.Delay(10);Assert.True(populated.UsesUnits);Assert.True(populated.HasUnits);populated.Unit=populated.Units.Single();
            var populatedWindow=new LoanWizardWindow {DataContext=populated,Width=820,Height=720};populatedWindow.Show();populatedWindow.UpdateLayout();using(var frame=populatedWindow.CaptureRenderedFrame()){Assert.NotNull(frame);frame!.Save(Path.Combine(AppContext.BaseDirectory,"Screenshots","reservation-populated-assets.png"),Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);}populatedWindow.Close();
            populated.AddLineCommand.Execute(null);Assert.True(populated.HasCart);
            await service.SaveUnitDetailsAsync(populated.Unit.Id,"SN-01","Xước nhẹ ở mặt trên");Assert.Equal("Xước nhẹ ở mặt trên",(await service.UnitsAsync(unitAsset)).Single().Condition);
            Assert.Contains(await service.AssetsAsync(),x=>x.Code=="BULK1"&&x.Total==5);
        }
        finally{service.Logout();Directory.Delete(directory,true);}
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
            {vm.SelectedNav=nav;await Wait(vm);Assert.Equal("",vm.Error);Assert.NotEmpty(vm.Title);window.UpdateLayout();using var frame=window.CaptureRenderedFrame();Assert.NotNull(frame);if(nav.Key is "dashboard" or "analytics"){var screenshots=Path.Combine(AppContext.BaseDirectory,"Screenshots");Directory.CreateDirectory(screenshots);frame!.Save(Path.Combine(screenshots,$"{nav.Key}-{width}-{scaling*100:0}.png"),Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);}}
            Avalonia.Application.Current!.RequestedThemeVariant=Avalonia.Styling.ThemeVariant.Dark;vm.SelectedNav=vm.Navigation.Single(n=>n.Key=="dashboard");await Wait(vm);window.UpdateLayout();using var dark=window.CaptureRenderedFrame();Assert.NotNull(dark);dark!.Save(Path.Combine(AppContext.BaseDirectory,"Screenshots",$"dark-{width}-{scaling*100:0}.png"),Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);Avalonia.Application.Current.RequestedThemeVariant=Avalonia.Styling.ThemeVariant.Light;
        }
        finally{window.Close();service.Logout();Directory.Delete(directory,true);}
    }
    [AvaloniaTheory] [InlineData(Role.Viewer,false)] [InlineData(Role.Teacher,false)] [InlineData(Role.LabManager,true)]
    public async Task RoleNavigationOnlyShowsInventoryAndMaintenanceToManagers(Role role,bool canManageAssets)
    {
        var directory=Path.Combine(Path.GetTempPath(),"stem-ui-roles-"+Guid.NewGuid().ToString("N"));var service=new LabService(directory);
        try
        {
            await service.InitializeAsync();await service.SetupAsync("admin","UiPassword123!","Admin","Room");await service.SaveUserAsync(null,"roleuser","Role User",role,"UiPassword123!",true);
            var vm=Vm(service,new TestDialogs());await vm.InitializeAsync();vm.Username="roleuser";vm.Password="UiPassword123!";await vm.LoginCommand.ExecuteAsync(null);
            Assert.True(vm.Authenticated);Assert.Equal(canManageAssets,vm.Navigation.Any(n=>n.Key=="inventory"));Assert.Equal(canManageAssets,vm.Navigation.Any(n=>n.Key=="maintenance"));
            if(role==Role.Viewer)Assert.Equal("Học sinh",Labels.Role(role));
            if(!canManageAssets)Assert.DoesNotContain(vm.Navigation,n=>n.Key is "inventory" or "maintenance");
            else
            {
                vm.SelectedNav=vm.Navigation.Single(n=>n.Key=="inventory");await Wait(vm);Assert.Contains(vm.Actions,a=>a.Label is "Nhập kho" or "Kiểm kê số lượng" or "Kiểm kê QR cá thể");
                vm.SelectedNav=vm.Navigation.Single(n=>n.Key=="maintenance");await Wait(vm);Assert.Contains(vm.Actions,a=>a.Label is "Báo hỏng" or "Hoàn tất sửa");
            }
            if(role==Role.Viewer){Assert.DoesNotContain(vm.Navigation,n=>n.Key is "settings" or "users" or "face" or "backup" or "loans" or "analytics" or "borrowers");Assert.Contains(vm.Navigation,n=>n.Key=="reports");Assert.DoesNotContain(vm.Actions,a=>a.Label=="Tạo phiếu mượn");}
        }
        finally{service.Logout();Directory.Delete(directory,true);}
    }
    private static async Task Wait(MainViewModel vm){for(int i=0;i<100 && vm.Busy;i++)await Task.Delay(20);Assert.False(vm.Busy);}
    private sealed class TestDialogs : IDialogs
    {
        public Task<bool> FormAsync(string title,string description,IEnumerable<FormField> fields,Func<FormViewModel,Task> save)=>Task.FromResult(false);
        public Task<bool> ConfirmAsync(string title,string explanation)=>Task.FromResult(false);
        public Task<bool> PreviewAsync(string title,string explanation,byte[] image)=>Task.FromResult(false);
        public Task<bool> PreviewLiveAsync(string title,string explanation,byte[] image,Func<CancellationToken,Task<byte[]>> readFrame)=>Task.FromResult(false);
        public Task<string?> OpenAsync(string title,params string[] patterns)=>Task.FromResult<string?>(null);
        public Task<string?> SaveAsync(string title,string filename,params string[] patterns)=>Task.FromResult<string?>(null);
        public Task<string?> FolderAsync(string title)=>Task.FromResult<string?>(null);
        public Task<bool> WizardAsync(LoanWizardViewModel vm)=>Task.FromResult(false);
        public Task<bool> AnalyticsAsync(MainViewModel vm)=>Task.FromResult(false);
    }
}
