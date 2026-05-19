using Microsoft.Maui.Storage;

namespace Kindergarten.Maui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var shell = new AppShell();
        var window = new Window(shell);

        // Check if already logged in
        Task.Run(async () =>
        {
            await Task.Delay(500); // Wait for shell to initialize
            var token = await SecureStorage.GetAsync(Constants.TokenKey);
            var role  = await SecureStorage.GetAsync(Constants.UserRoleKey);

            if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(role))
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        await Shell.Current.GoToAsync(role switch
                        {
                            "Parent"   => "//parent/dashboard",
                            "Driver"   => "//driver/trips",
                            "Employee" => "//employee/attendance",
                            _          => "//parent/dashboard"
                        });
                    }
                    catch { /* Stay on login if navigation fails */ }
                });
            }
        });

        return window;
    }
}
