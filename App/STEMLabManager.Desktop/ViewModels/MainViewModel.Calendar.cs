using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using STEMLabManager.Application;
namespace STEMLabManager.Desktop.ViewModels;

public sealed record CalendarDay(string Date,IReadOnlyList<string> Bookings);
public partial class MainViewModel
{
    private DateTime weekStart=DateTime.Today.AddDays(-((int)DateTime.Today.DayOfWeek+6)%7);
    public ObservableCollection<CalendarDay> WeekDays {get;}=[];
    public bool IsCalendar=>currentPage=="reservations";
    public string WeekTitle=>$"{weekStart:dd/MM/yyyy} – {weekStart.AddDays(6):dd/MM/yyyy}";
    public ObservableCollection<ChartBar> DailyBars {get;}=[];
    [RelayCommand] private void PreviousWeek(){weekStart=weekStart.AddDays(-7);UpdateCalendar();}
    [RelayCommand] private void NextWeek(){weekStart=weekStart.AddDays(7);UpdateCalendar();}
    private void UpdateCalendar()
    {
        WeekDays.Clear();var reservations=allRows.Select(x=>x.Source).OfType<ReservationRow>().ToList();
        for(int i=0;i<7;i++)
        {
            var date=weekStart.AddDays(i);var from=date.ToUniversalTime();var to=date.AddDays(1).ToUniversalTime();
            WeekDays.Add(new(date.ToString("ddd dd/MM",System.Globalization.CultureInfo.GetCultureInfo("vi-VN")),reservations.Where(r=>r.StartUtc<to && r.EndUtc>from).OrderBy(r=>r.StartUtc).Select(r=>$"{r.StartUtc.ToLocalTime():HH:mm}–{r.EndUtc.ToLocalTime():HH:mm}\n{r.Borrower}\n{Labels.Reservation(r.Status)}").ToList()));
        }
        OnPropertyChanged(nameof(WeekTitle));OnPropertyChanged(nameof(IsCalendar));
    }
    private async Task QrInventoryFormAsync()
    {
        var locations=await service.LocationsAsync();await dialogs.FormAsync("Kiểm kê cá thể bằng QR","Nhập hoặc scan từng QR, mỗi dòng một mã. Xác nhận sẽ đánh dấu cá thể sẵn sàng nhưng không tìm thấy là Mất; QR tìm lại trở về Sẵn sàng.",[Choose("location","Vị trí",locations.Select(x=>new Choice(x.Id,x.Name))),Field("qr","Các QR hiện có trên kệ (mỗi dòng một mã)","",false),Field("note","Lý do kiểm kê")],async f=>{var scanned=f["qr"].Split(['\r','\n'],StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries).ToHashSet(StringComparer.Ordinal);await service.CreateUnitInventoryAsync(f.Id("location"),scanned,f["note"]);});
    }
}
