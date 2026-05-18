using Kindergarten.Maui.Services;
using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;

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
        await LoadAttendanceAsync();
    }

    async Task LoadAttendanceAsync()
    {
        var records = await _api.GetMyAttendanceAsync();
        if (records == null) return;

        AttendanceList.ItemsSource = records;

        var today = records.FirstOrDefault(r => r.Date.Date == DateTime.UtcNow.Date);
        if (today != null)
        {
            CheckInTimeLabel.Text  = today.CheckInTime?.ToLocalTime().ToString("HH:mm") ?? "--:--";
            CheckOutTimeLabel.Text = today.CheckOutTime?.ToLocalTime().ToString("HH:mm") ?? "--:--";
            StatusLabel.Text       = today.Status;
        }
    }

    async void OnCheckInClicked(object sender, EventArgs e)
    {
        // Biometric verification
        var isAvailable = await CrossFingerprint.Current.IsAvailableAsync();
        if (isAvailable)
        {
            var request = new AuthenticationRequestConfiguration(
                "تسجيل الحضور",
                "استخدم بصمتك لتسجيل الحضور");
            var result = await CrossFingerprint.Current.AuthenticateAsync(request);

            if (!result.Authenticated)
            {
                await DisplayAlert("❌", "فشل التحقق من البصمة", "موافق");
                return;
            }
        }

        CheckInButton.IsEnabled = false;
        var attendance = await _api.CheckInAsync();
        if (attendance != null)
        {
            CheckInTimeLabel.Text = attendance.CheckInTime?.ToLocalTime().ToString("HH:mm") ?? "--:--";
            StatusLabel.Text      = attendance.Status;
            await DisplayAlert("✅", "تم تسجيل حضورك بنجاح", "موافق");
            await LoadAttendanceAsync();
        }
        else
        {
            await DisplayAlert("ℹ️", "تم تسجيل الحضور مسبقاً اليوم", "موافق");
        }
        CheckInButton.IsEnabled = true;
    }

    async void OnCheckOutClicked(object sender, EventArgs e)
    {
        CheckOutButton.IsEnabled = false;
        var attendance = await _api.CheckOutAsync();
        if (attendance != null)
        {
            CheckOutTimeLabel.Text = attendance.CheckOutTime?.ToLocalTime().ToString("HH:mm") ?? "--:--";
            await DisplayAlert("✅", $"تم تسجيل انصرافك — ساعات العمل: {attendance.WorkingHours}", "موافق");
            await LoadAttendanceAsync();
        }
        else
        {
            await DisplayAlert("❌", "لم يتم تسجيل الحضور اليوم", "موافق");
        }
        CheckOutButton.IsEnabled = true;
    }
}