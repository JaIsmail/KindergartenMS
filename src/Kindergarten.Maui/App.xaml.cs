using Microsoft.Maui.Storage;

namespace Kindergarten.Maui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Global crash handler
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await Shell.Current.DisplayAlert(
                        "App Error",
                        ex?.Message + "\n\n" + ex?.StackTrace?.Substring(0, Math.Min(300, ex.StackTrace?.Length ?? 0)),
                        "OK");
                }
                catch { }
            });
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            e.SetObserved();
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await Shell.Current.DisplayAlert(
                        "Task Error",
                        e.Exception?.Message + "\n\n" + e.Exception?.InnerException?.Message,
                        "OK");
                }
                catch { }
            });
        };
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}
