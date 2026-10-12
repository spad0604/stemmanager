using Avalonia;
using System;

namespace STEMLabManager.Desktop;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        var demo=args.Contains("--demo");
        var identity=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))))[..16];
        using var mutex=new System.Threading.Mutex(true,$"STEMManager-{identity}-{(demo?"Demo":"Data")}",out var created);
        if(!created){Console.Error.WriteLine("STEM Lab Manager đã mở. Hãy dùng cửa sổ hiện tại.");return;}
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
