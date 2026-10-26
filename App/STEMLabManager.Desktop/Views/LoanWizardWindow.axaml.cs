using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using STEMLabManager.Desktop.ViewModels;
namespace STEMLabManager.Desktop.Views;
public partial class LoanWizardWindow : Window
{
    public LoanWizardWindow(){AvaloniaXamlLoader.Load(this);}
    public LoanWizardWindow(LoanWizardViewModel vm):this(){DataContext=vm;vm.CloseRequested+=result=>Close(result);}
}
