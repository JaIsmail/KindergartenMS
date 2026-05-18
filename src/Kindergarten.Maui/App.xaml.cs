using Microsoft.Maui.Storage;

namespace Kindergarten.Maui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new AppShell();
    }

    protected override async void OnStart()
    {
        base.OnStart();
        // Check if already logged in
        var token = await SecureStorage.GetAsync(Constants.TokenKey);
        var role  = await SecureStorage.GetAsync(Constants.UserRoleKey);

        if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(role))
        {
            await Shell.Current.GoToAsync(role switch
            {
                "Parent"   => "//parent/dashboard",
                "Driver"   => "//driver/trips",
                "Employee" => "//employee/attendance",
                _          => "//parent/dashboard"
            });
        }
    }
}