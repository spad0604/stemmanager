using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using STEMLabManager.Desktop.ViewModels;

namespace STEMLabManager.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var timer=new DispatcherTimer {Interval=TimeSpan.FromSeconds(15)};
        timer.Tick+=async (_,_)=>{if(DataContext is MainViewModel vm && vm.Authenticated && !vm.Busy && OwnedWindows.Count==0){if(DateTime.UtcNow-vm.LastActivityUtc>TimeSpan.FromMinutes(vm.SessionMinutes))vm.Logout();else await vm.MaintainAsync();}};
        Opened+=(_,_)=>timer.Start();Closed+=(_,_)=>timer.Stop();
        AddHandler(KeyDownEvent,(_,e)=>{Activity();if(e.Key==Key.K && e.KeyModifiers.HasFlag(KeyModifiers.Control) && DataContext is MainViewModel vm && vm.Authenticated){vm.GlobalSearchCommand.Execute(null);e.Handled=true;}},Avalonia.Interactivity.RoutingStrategies.Tunnel);
        AddHandler(PointerPressedEvent,(_,_)=>Activity(),Avalonia.Interactivity.RoutingStrategies.Tunnel);
    }
    private void Activity(){if(DataContext is MainViewModel vm)vm.LastActivityUtc=DateTime.UtcNow;}
}
