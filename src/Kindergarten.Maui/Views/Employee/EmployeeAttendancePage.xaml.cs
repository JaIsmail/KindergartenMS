using Kindergarten.Maui.Services;
using Microsoft.Maui.Storage;

namespace Kindergarten.Maui.Views.Employee;

public partial class EmployeeAttendancePage : ContentPage
{
    private readonly ApiService _api = new();

    public EmployeeAttendancePage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        DateLabel.Text = DateTime.Now.ToString("dddd، dd MMMM yyyy");
        TimeLabel.Text = DateTime.Now.ToString("HH:mm");
        await LoadAttendanceAsync();
    }

    async Task LoadAttendanceAsync()
    {
        try
        {
            var records = await _api.GetMyAttendanceAsync();
            if (records == null) return;

            AttendanceList.ItemsSource = records;

            var today = records.FirstOrDefault(r =>
                r.Date.Date == DateTime.UtcNow.Date);

            if (today != null)
            {
                CheckInTimeLabel.Text  = today.CheckInTime?.ToLocalTime().ToString("HH:mm") ?? "--:--";
                CheckOutTimeLabel.Text = today.CheckOutTime?.ToLocalTime().ToString("HH:mm") ?? "--:--";
                StatusLabel.Text       = today.Status;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("خطأ", ex.Message, "موافق");
        }
    }

    async void OnCheckInClicked(object sender, EventArgs e)
    {
        CheckInButton.IsEnabled = false;
        try
        {
            var attendance = await _api.CheckInAsync();
            if (attendance?.CheckInTime != null)
            {
                CheckInTimeLabel.Text = attendance.CheckInTime.Value.ToLocalTime().ToString("HH:mm");
                StatusLabel.Text = attendance.Status;
                await DisplayAlert("✅", "تم تسجيل حضورك بنجاح", "موافق");
                await LoadAttendanceAsync();
            }
            else
            {
                await DisplayAlert("ℹ️", "تم تسجيل الحضور مسبقاً اليوم", "موافق");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("خطأ", ex.Message, "موافق");
        }
        CheckInButton.IsEnabled = true;
    }

    async void OnCheckOutClicked(object sender, EventArgs e)
    {
        CheckOutButton.IsEnabled = false;
        try
        {
            var attendance = await _api.CheckOutAsync();
            if (attendance?.CheckOutTime != null)
            {
                CheckOutTimeLabel.Text = attendance.CheckOutTime.Value.ToLocalTime().ToString("HH:mm");
                await DisplayAlert("✅", $"تم تسجيل انصرافك\nساعات العمل: {attendance.WorkingHours}", "موافق");
                await LoadAttendanceAsync();
            }
            else
            {
                await DisplayAlert("❌", "لم يتم تسجيل الحضور اليوم", "موافق");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("خطأ", ex.Message, "موافق");
        }
        CheckOutButton.IsEnabled = true;
    }

    async void OnLogoutClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("تسجيل الخروج", "هل تريد تسجيل الخروج؟", "نعم", "لا");
        if (!confirm) return;
        SecureStorage.RemoveAll();
        await Shell.Current.GoToAsync("//login");
    }
}