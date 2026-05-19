using Kindergarten.Maui.Services;
using Microsoft.Maui.Storage;

namespace Kindergarten.Maui.Views.Auth;

public partial class LoginPage : ContentPage
{
    private readonly ApiService _api = new();
    private string _lang = "ar";

    public LoginPage()
    {
        InitializeComponent();
        _lang = Preferences.Get(Constants.LangKey, "ar");
        ApplyLang();
    }

    void ApplyLang()
    {
        bool ar = _lang == "ar";
        ArBtn.BackgroundColor = ar ? Color.FromArgb("#f97316") : Color.FromArgb("#1e293b");
        ArBtn.TextColor       = ar ? Colors.White : Color.FromArgb("#94a3b8");
        EnBtn.BackgroundColor = ar ? Color.FromArgb("#1e293b") : Color.FromArgb("#f97316");
        EnBtn.TextColor       = ar ? Color.FromArgb("#94a3b8") : Colors.White;

        SubtitleLabel.Text  = ar ? "نظام إدارة الروضة" : "Kindergarten Management System";
        WelcomeLabel.Text   = ar ? "مرحباً بعودتك"     : "Welcome back";
        SignInLabel.Text    = ar ? "سجّل دخولك للمتابعة" : "Sign in to continue";
        EmailLabel.Text     = ar ? "البريد الإلكتروني"  : "Email Address";
        PasswordLabel.Text  = ar ? "كلمة المرور"        : "Password";
        LoginButton.Text    = ar ? "تسجيل الدخول"       : "Sign In";

        FlowDirection = ar ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    }

    void OnArClicked(object sender, EventArgs e) { _lang = "ar"; Preferences.Set(Constants.LangKey, "ar"); ApplyLang(); }
    void OnEnClicked(object sender, EventArgs e) { _lang = "en"; Preferences.Set(Constants.LangKey, "en"); ApplyLang(); }

    async void OnLoginClicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;
        LoginButton.IsEnabled = false;
        LoginButton.Text = _lang == "ar" ? "جاري التحقق..." : "Signing in...";

        try
        {
            var result = await _api.LoginAsync(EmailEntry.Text?.Trim() ?? "", PasswordEntry.Text ?? "");
            if (result?.Token != null)
            {
                await SecureStorage.SetAsync(Constants.TokenKey,    result.Token);
                await SecureStorage.SetAsync(Constants.UserIdKey,   result.UserId);
                await SecureStorage.SetAsync(Constants.UserRoleKey, result.Role);
                await SecureStorage.SetAsync(Constants.UserNameKey, result.FullName);

                // Navigate based on role
                switch (result.Role)
                {
                    case "Parent":
                        await Shell.Current.GoToAsync("//parent/dashboard");
                        break;
                    case "Driver":
                        await Shell.Current.GoToAsync("//driver/trips");
                        break;
                    case "Employee":
                        await Shell.Current.GoToAsync("//employee/attendance");
                        break;
                    default:
                        await Shell.Current.GoToAsync("//parent/dashboard");
                        break;
                }
            }
            else
            {
                ErrorLabel.Text = _lang == "ar" ? "بيانات الدخول غير صحيحة" : "Invalid email or password";
                ErrorLabel.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = $"Error: {ex.Message}";
            ErrorLabel.IsVisible = true;
        }

        LoginButton.IsEnabled = true;
        LoginButton.Text = _lang == "ar" ? "تسجيل الدخول" : "Sign In";
    }
}