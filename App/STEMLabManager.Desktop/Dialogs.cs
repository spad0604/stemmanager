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
    Task<bool> PreviewLiveAsync(string title,string explanation,byte[] image,Func<CancellationToken,Task<byte[]>> readFrame);
    Task<string?> OpenAsync(string title,params string[] patterns);
    Task<string?> SaveAsync(string title,string filename,params string[] patterns);
    Task<string?> FolderAsync(string title);
    Task<bool> WizardAsync(LoanWizardViewModel vm);
    Task<bool> AnalyticsAsync(MainViewModel vm);
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
        var vm=PreviewModel(title,explanation,image);try{return await ShowAsync(new FormWindow(vm));}finally{vm.SetPreview(null);}
    }
    public async Task<bool> PreviewLiveAsync(string title,string explanation,byte[] image,Func<CancellationToken,Task<byte[]>> readFrame)
    {
        var vm=PreviewModel(title,explanation,image);vm.ActionLabel="Chụp ảnh";vm.Busy=true;using var stop=new CancellationTokenSource();var dialogTask=ShowAsync(new FormWindow(vm));
        var pump=PumpPreviewAsync(vm,readFrame,stop.Token);
        try{return await dialogTask;}
        finally{stop.Cancel();try{await pump;}catch(OperationCanceledException){}vm.SetPreview(null);}
    }
    private static FormViewModel PreviewModel(string title,string explanation,byte[] image)
    {
        var vm=new FormViewModel(title,explanation,[],_=>Task.CompletedTask);using var stream=new MemoryStream(image);vm.SetPreview(new Avalonia.Media.Imaging.Bitmap(stream));return vm;
    }
    private static async Task PumpPreviewAsync(FormViewModel vm,Func<CancellationToken,Task<byte[]>> readFrame,CancellationToken cancellation)
    {
        try
        {
            while(!cancellation.IsCancellationRequested)
            {
                var bytes=await readFrame(cancellation);
                try{await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(()=>{vm.SetPreviewBytes(bytes);vm.Error="";vm.Busy=false;});}
                finally{Array.Clear(bytes);}
                await Task.Delay(120,cancellation);
            }
        }
        catch(OperationCanceledException) when(cancellation.IsCancellationRequested){}
        catch(Exception){await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(()=>vm.Error="Không nhận được hình trực tiếp từ camera. Đóng cửa sổ và thử lại.");}
    }
    public async Task<string?> OpenAsync(string title,params string[] patterns)
    {var files=await Window.StorageProvider.OpenFilePickerAsync(new(){Title=title,AllowMultiple=false,FileTypeFilter=[new(title){Patterns=patterns}]});return files.FirstOrDefault()?.TryGetLocalPath();}
    public async Task<string?> SaveAsync(string title,string filename,params string[] patterns)
    {var file=await Window.StorageProvider.SaveFilePickerAsync(new(){Title=title,SuggestedFileName=filename,FileTypeChoices=[new(title){Patterns=patterns}]});return file?.TryGetLocalPath();}
    public async Task<string?> FolderAsync(string title)
    {var folders=await Window.StorageProvider.OpenFolderPickerAsync(new(){Title=title,AllowMultiple=false});return folders.FirstOrDefault()?.TryGetLocalPath();}
    public Task<bool> WizardAsync(LoanWizardViewModel vm)=>ShowAsync(new LoanWizardWindow(vm));
    public Task<bool> AnalyticsAsync(MainViewModel vm)=>ShowAsync(new AnalyticsWindow(vm));
}
