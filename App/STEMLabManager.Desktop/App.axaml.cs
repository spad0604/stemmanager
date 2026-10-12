using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using STEMLabManager.Application;
using STEMLabManager.Infrastructure;
using STEMLabManager.AI;
using STEMLabManager.Desktop.ViewModels;
using STEMLabManager.Desktop.Views;
namespace STEMLabManager.Desktop;
public partial class App : Avalonia.Application
{
    private IHost? host;
    public override void Initialize()=>AvaloniaXamlLoader.Load(this);
    public override void OnFrameworkInitializationCompleted()
    {
        if(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            bool demo=Environment.GetCommandLineArgs().Contains("--demo");
            string directory=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"STEMManager",demo?"Demo":"Data");
            host=Host.CreateDefaultBuilder().ConfigureLogging(l=>{l.ClearProviders();l.AddProvider(new FileLogProvider(Path.Combine(directory,"Logs")));})
                .ConfigureServices(s=>{
                    s.AddSingleton<ILabService>(p=>new LabService(directory,demo,p.GetRequiredService<Insights>()));
                    s.AddSingleton<Dialogs>();s.AddSingleton<IDialogs>(p=>p.GetRequiredService<Dialogs>());
                    s.AddSingleton(new HttpClient(new HttpClientHandler {AllowAutoRedirect=false}) {Timeout=TimeSpan.FromSeconds(30)});
                    s.AddSingleton<Insights>();s.AddSingleton<FaceEngine>();s.AddSingleton<CameraCapture>();s.AddSingleton<MainViewModel>();
                }).Build();
            var vm=host.Services.GetRequiredService<MainViewModel>();
            var window=new MainWindow {DataContext=vm};host.Services.GetRequiredService<Dialogs>().Owner=window;desktop.MainWindow=window;
            window.Opened+=async (_,_)=>
            {
                var screen=window.Screens.ScreenFromWindow(window)??window.Screens.Primary;
                if(screen!=null)
                {
                    window.Width=Math.Min(window.Width,Math.Max(window.MinWidth,screen.WorkingArea.Width/screen.Scaling-32));
                    window.Height=Math.Min(window.Height,Math.Max(window.MinHeight,screen.WorkingArea.Height/screen.Scaling-48));
                    window.Position=new Avalonia.PixelPoint(screen.WorkingArea.X+(int)((screen.WorkingArea.Width-window.Width*screen.Scaling)/2),screen.WorkingArea.Y+(int)((screen.WorkingArea.Height-window.Height*screen.Scaling)/2));
                }
                await vm.InitializeAsync();
            };
            desktop.Exit+=(_,_)=>host.Dispose();
        }
        base.OnFrameworkInitializationCompleted();
    }
}
