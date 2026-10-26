using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using STEMLabManager.Application;
using STEMLabManager.Domain;
namespace STEMLabManager.Desktop.ViewModels;

public partial class NavItem(string key,string title,string group,Permission permission) : ObservableObject
{
    public string Key {get;}=key;
    public string Title {get;}=title;
    public string Group {get;}=group;
    public Permission Permission {get;}=permission;
    public string Icon=>Visuals.PageIcon(Key);
    public bool ShowGroup {get;set;}
    [ObservableProperty] private bool active;
}
public sealed record DisplayRow(long Id,string Code,string Name,string Details,string Status,string Metric,object Source)
{
    public Avalonia.Media.Imaging.Bitmap? Thumbnail {get;set;}
    public bool HasThumbnail=>Thumbnail!=null;
    public bool IsAssetRow {get;set;}
    public string Icon=>Visuals.RowIcon(Source);
    public string StatusBackground=>Visuals.StatusColors(Status).Background;
    public string StatusForeground=>Visuals.StatusColors(Status).Foreground;
}
public sealed record Choice(long Id,string Label) { public override string ToString()=>Label; }
public sealed record AssetCartItem(AssetRow Asset,int Quantity);
public sealed record ChartBar(string Label,double Value,double Width);
public sealed record Kpi(string Label,string Value,string Hint,string Icon="box",string Accent="#315BDB",string Tint="#EAF0FF",System.Windows.Input.ICommand? Command=null);
public sealed class ToolbarAction(string label,IAsyncRelayCommand command,bool primary=false)
{
    public string Label {get;}=label;
    public IAsyncRelayCommand Command {get;}=command;
    public bool Primary {get;}=primary;
    public string Icon=>Visuals.ActionIcon(Label);
    public string IconBrush=>Primary?"#FFFFFF":"#61728F";
}
public partial class FormField : ObservableObject
{
    public string Key {get;}
    public string Label {get;}
    public string Hint {get;}
    public bool Required {get;}
    public bool IsSecret {get;}
    private readonly bool hasChoices;
    private readonly bool hasDateTime;
    private readonly Func<Task<string?>>? browse;
    public bool IsImage=>browse!=null;
    public IAsyncRelayCommand BrowseCommand {get;}
    public bool IsChoice=>hasChoices;
    public bool IsDateTime=>hasDateTime;
    public string DateTimeDisplay=>Date.HasValue&&Time.HasValue?Date.Value.LocalDateTime.Date.Add(Time.Value).ToString("dd/MM/yyyy  HH:mm"):"Chọn ngày và giờ";
    public bool IsText=>!IsChoice&&!IsDateTime;
    public bool Multiline=>Key is "qr" or "description" or "note" or "reason" or "basis" or "condition";
    public int InputHeight=>Multiline?90:36;
    public char PasswordChar=>IsSecret?'●':'\0';
    public ObservableCollection<Choice> Choices {get;}=[];
    [ObservableProperty] private string value="";
    [ObservableProperty] private Choice? selected;
    [ObservableProperty] private DateTimeOffset? date;
    [ObservableProperty] private TimeSpan? time;
    [ObservableProperty,NotifyPropertyChangedFor(nameof(HasError))] private string error="";
    public bool HasError=>!string.IsNullOrWhiteSpace(Error);
    public bool HasHint=>!string.IsNullOrWhiteSpace(Hint);
    public FormField(string key,string label,string value="",bool required=true,string hint="",bool secret=false,IEnumerable<Choice>? choices=null,Func<Task<string?>>? browse=null,bool dateTime=false)
    {
        Key=key;Label=label;Value=value;Required=required;Hint=hint;IsSecret=secret;hasChoices=choices!=null;hasDateTime=dateTime;this.browse=browse;BrowseCommand=new AsyncRelayCommand(BrowseAsync);
        if(dateTime&&DateTime.TryParseExact(value,"dd/MM/yyyy HH:mm",System.Globalization.CultureInfo.GetCultureInfo("vi-VN"),System.Globalization.DateTimeStyles.None,out var parsed)){date=new DateTimeOffset(DateTime.SpecifyKind(parsed,DateTimeKind.Local));time=parsed.TimeOfDay;}
        if(choices!=null){foreach(var c in choices)Choices.Add(c);Selected=Choices.FirstOrDefault(c=>c.Id.ToString()==value)??Choices.FirstOrDefault();}
    }
    private async Task BrowseAsync(){if(browse!=null){var path=await browse();if(path!=null)Value=path;}}
    public string Result=>IsChoice?Selected?.Id.ToString()??"":IsDateTime?DateTimeValue:IsSecret?Value:Value.Trim();
    private string DateTimeValue=>Date.HasValue&&Time.HasValue?Date.Value.LocalDateTime.Date.Add(Time.Value).ToString("dd/MM/yyyy HH:mm"):"";
    public DateTime UtcValue()=>DateTime.TryParseExact(Result,"dd/MM/yyyy HH:mm",System.Globalization.CultureInfo.GetCultureInfo("vi-VN"),System.Globalization.DateTimeStyles.None,out var value)?DateTime.SpecifyKind(value,DateTimeKind.Local).ToUniversalTime():throw new BusinessException("Hãy chọn ngày và giờ.");
    public bool Validate(){Error=Required && string.IsNullOrWhiteSpace(Result)?Label+" bắt buộc.":Result.Length>4000?"Tối đa 4.000 ký tự.":"";return Error=="";}
    partial void OnValueChanged(string value){if(Error!="")Validate();}
    partial void OnSelectedChanged(Choice? value){if(Error!="")Validate();}
    partial void OnDateChanged(DateTimeOffset? value){if(value.HasValue&&Time is null)Time=TimeSpan.FromHours(8);SyncDateTime();}
    partial void OnTimeChanged(TimeSpan? value)=>SyncDateTime();
    private void SyncDateTime(){if(IsDateTime){Value=DateTimeValue;OnPropertyChanged(nameof(Result));OnPropertyChanged(nameof(DateTimeDisplay));if(Error!="")Validate();}}
}
public partial class FormViewModel : ObservableObject
{
    public string Title {get;}
    public string Description {get;}
    public string Icon=>Visuals.ActionIcon(Title);
    public ObservableCollection<FormField> Fields {get;}
    public Avalonia.Media.Imaging.Bitmap? Preview {get;private set;}
    public bool HasPreview=>Preview!=null;
    [ObservableProperty] private string error="";
    [ObservableProperty] private string actionLabel="Xác nhận";
    [ObservableProperty,NotifyPropertyChangedFor(nameof(CanSave))] private bool busy;
    public bool CanSave=>!Busy;
    private readonly Func<FormViewModel,Task> save;
    public event Action<bool>? CloseRequested;
    public FormViewModel(string title,string description,IEnumerable<FormField> fields,Func<FormViewModel,Task> save)
    {Title=title;Description=description;Fields=new(fields);this.save=save;}
    public void SetPreview(Avalonia.Media.Imaging.Bitmap? bitmap){var previous=Preview;Preview=bitmap;OnPropertyChanged(nameof(Preview));OnPropertyChanged(nameof(HasPreview));if(!ReferenceEquals(previous,bitmap))previous?.Dispose();}
    public void SetPreviewBytes(byte[] bytes){using var stream=new MemoryStream(bytes);SetPreview(new Avalonia.Media.Imaging.Bitmap(stream));}
    public string this[string key]=>Fields.Single(f=>f.Key==key).Result;
    public long Id(string key)=>long.TryParse(this[key],out var value)?value:throw new BusinessException("Chọn "+Fields.Single(f=>f.Key==key).Label);
    public int Integer(string key)
    {if(int.TryParse(this[key],out var value))return value;var field=Fields.Single(f=>f.Key==key);field.Error=field.Label+" phải là số nguyên.";throw new BusinessException(field.Error);}
    public DateTime Utc(string key)=>Fields.Single(f=>f.Key==key).UtcValue();
    [RelayCommand] private async Task SaveAsync()
    {
        Error="";Busy=true;
        try {var valid=true;foreach(var f in Fields)valid=f.Validate() && valid;Rules.Require(valid,"Kiểm tra các trường được đánh dấu.");await save(this);CloseRequested?.Invoke(true);}
        catch(Exception e){Error=e is BusinessException?e.Message:"Không thể hoàn tất. Kiểm tra dữ liệu và thử lại.";}
        finally{Busy=false;OnPropertyChanged(nameof(CanSave));}
    }
    [RelayCommand] private void Cancel()=>CloseRequested?.Invoke(false);
}
