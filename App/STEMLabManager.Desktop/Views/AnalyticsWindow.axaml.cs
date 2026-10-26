using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using STEMLabManager.Desktop.ViewModels;
namespace STEMLabManager.Desktop.Views;
public partial class AnalyticsWindow : Window
{
    public AnalyticsWindow(){AvaloniaXamlLoader.Load(this);}
    public AnalyticsWindow(MainViewModel vm):this()=>DataContext=vm;
    private void CloseClick(object? sender,Avalonia.Interactivity.RoutedEventArgs e)=>Close(true);
}
