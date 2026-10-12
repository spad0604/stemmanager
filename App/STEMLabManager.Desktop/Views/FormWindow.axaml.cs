using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using STEMLabManager.Desktop.ViewModels;
namespace STEMLabManager.Desktop.Views;
public partial class FormWindow : Window
{
    public FormWindow(){AvaloniaXamlLoader.Load(this);}
    public FormWindow(FormViewModel vm):this(){DataContext=vm;vm.CloseRequested+=result=>Close(result);}
}
