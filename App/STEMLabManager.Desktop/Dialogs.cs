using Avalonia.Controls;
using Avalonia.Platform.Storage;
using STEMLabManager.Desktop.ViewModels;
using STEMLabManager.Desktop.Views;
namespace STEMLabManager.Desktop;

public interface IDialogs
{
    Task<bool> FormAsync(string title,string description,IEnumerable<FormField> fields,Func<FormViewModel,Task> save);
    Task<bool> ConfirmAsync(string title,string explanation);
    Task<bool> PreviewAsync(string title,string explanation,byte[] image);
    Task<string?> OpenAsync(string title,params string[] patterns);
    Task<string?> SaveAsync(string title,string filename,params string[] patterns);
    Task<string?> FolderAsync(string title);
    Task<bool> WizardAsync(LoanWizardViewModel vm);
}
public sealed class Dialogs : IDialogs
{
    public Window? Owner {get;set;}
    private Window Window=>Owner??throw new InvalidOperationException("Chưa có cửa sổ chính.");
    private Task<bool> ShowAsync(Window dialog)
    {
        dialog.Width=Math.Max(dialog.MinWidth,Math.Min(dialog.Width,Window.Bounds.Width-48));dialog.Height=Math.Max(dialog.MinHeight,Math.Min(dialog.Height,Window.Bounds.Height-48));
        return dialog.ShowDialog<bool>(Window);
    }
    public Task<bool> FormAsync(string title,string description,IEnumerable<FormField> fields,Func<FormViewModel,Task> save)=>ShowAsync(new FormWindow(new(title,description,fields,save)));
    public Task<bool> ConfirmAsync(string title,string explanation)=>FormAsync(title,explanation,[],_=>Task.CompletedTask);
    public async Task<bool> PreviewAsync(string title,string explanation,byte[] image)
    {
        using var stream=new MemoryStream(image);using var bitmap=new Avalonia.Media.Imaging.Bitmap(stream);
        var vm=new FormViewModel(title,explanation,[],_=>Task.CompletedTask){Preview=bitmap};return await ShowAsync(new FormWindow(vm));
    }
    public async Task<string?> OpenAsync(string title,params string[] patterns)
    {var files=await Window.StorageProvider.OpenFilePickerAsync(new(){Title=title,AllowMultiple=false,FileTypeFilter=[new(title){Patterns=patterns}]});return files.FirstOrDefault()?.TryGetLocalPath();}
    public async Task<string?> SaveAsync(string title,string filename,params string[] patterns)
    {var file=await Window.StorageProvider.SaveFilePickerAsync(new(){Title=title,SuggestedFileName=filename,FileTypeChoices=[new(title){Patterns=patterns}]});return file?.TryGetLocalPath();}
    public async Task<string?> FolderAsync(string title)
    {var folders=await Window.StorageProvider.OpenFolderPickerAsync(new(){Title=title,AllowMultiple=false});return folders.FirstOrDefault()?.TryGetLocalPath();}
    public Task<bool> WizardAsync(LoanWizardViewModel vm)=>ShowAsync(new LoanWizardWindow(vm));
}
