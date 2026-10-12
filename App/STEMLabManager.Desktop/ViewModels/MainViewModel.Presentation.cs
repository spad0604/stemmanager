using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using STEMLabManager.Application;
namespace STEMLabManager.Desktop.ViewModels;

public partial class MainViewModel
{
    [ObservableProperty] private IReadOnlyList<MetricPoint> trendPoints=[];
    [ObservableProperty] private IReadOnlyList<MetricPoint> rankingPoints=[];
    [ObservableProperty] private double overdueFraction;
    [ObservableProperty] private string periodLoanCount="0";
    [ObservableProperty] private string periodBorrowerCount="0";
    [ObservableProperty] private string utilizationLabel="0%";
    public string TodayLabel=>DateTime.Today.ToString("dddd, dd MMMM yyyy",CultureInfo.GetCultureInfo("vi-VN"));
    public string UserInitial=>string.IsNullOrWhiteSpace(service.CurrentActor?.DisplayName)?"S":service.CurrentActor.DisplayName[..1].ToUpper(CultureInfo.GetCultureInfo("vi-VN"));
    public string PageIcon=>Visuals.PageIcon(currentPage);
    public string TableTitle=>IsDashboard?"Giao dịch gần đây":"Danh sách "+Title.ToLower(CultureInfo.GetCultureInfo("vi-VN"));
    public bool ShowPeriod=>IsDashboard || currentPage=="reports";
    public bool HasNotifications=>Notifications.Count>0;
    public string ModeLabel=>IsDemo?"Dữ liệu minh họa":"Hoạt động offline";
    private void UpdatePresentation()
    {
        OnPropertyChanged(nameof(UserInitial));OnPropertyChanged(nameof(PageIcon));OnPropertyChanged(nameof(TableTitle));OnPropertyChanged(nameof(ShowPeriod));OnPropertyChanged(nameof(TodayLabel));OnPropertyChanged(nameof(HasNotifications));
    }
}
