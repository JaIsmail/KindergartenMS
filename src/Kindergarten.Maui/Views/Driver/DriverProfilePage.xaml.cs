using Microsoft.Maui.Storage;

namespace Kindergarten.Maui.Views.Driver;

public partial class DriverProfilePage : ContentPage
{{
    public DriverProfilePage()
    {{
        InitializeComponent();
    }}

    protected override async void OnAppearing()
    {{
        base.OnAppearing();
        var name  = await SecureStorage.GetAsync(Constants.UserNameKey) ?? "";
        var email = await SecureStorage.GetAsync(Constants.UserRoleKey) ?? "";
        var role  = await SecureStorage.GetAsync(Constants.UserRoleKey) ?? "";
        var lang  = Preferences.Get(Constants.LangKey, "ar");

        AvatarLabel.Text = name.Length > 0 ? name[0].ToString().ToUpper() : "?";
        NameLabel.Text   = name;
        EmailLabel.Text  = await SecureStorage.GetAsync("kms_email") ?? "";
        RoleLabel.Text   = role;

        BtnAr.BackgroundColor = lang == "ar" ? Color.FromArgb("#f97316") : Color.FromArgb("#f1f5f9");
        BtnAr.TextColor       = lang == "ar" ? Colors.White : Color.FromArgb("#475569");
        BtnEn.BackgroundColor = lang == "en" ? Color.FromArgb("#f97316") : Color.FromArgb("#f1f5f9");
        BtnEn.TextColor       = lang == "en" ? Colors.White : Color.FromArgb("#475569");

        Title = lang == "ar" ? "الملف الشخصي" : "Profile";
        FlowDirection = lang == "ar" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    }}

    void OnArabicClicked(object sender, EventArgs e)
    {{
        Preferences.Set(Constants.LangKey, "ar");
        BtnAr.BackgroundColor = Color.FromArgb("#f97316"); BtnAr.TextColor = Colors.White;
        BtnEn.BackgroundColor = Color.FromArgb("#f1f5f9"); BtnEn.TextColor = Color.FromArgb("#475569");
        FlowDirection = FlowDirection.RightToLeft;
        Title = "الملف الشخصي";
    }}

    void OnEnglishClicked(object sender, EventArgs e)
    {{
        Preferences.Set(Constants.LangKey, "en");
        BtnEn.BackgroundColor = Color.FromArgb("#f97316"); BtnEn.TextColor = Colors.White;
        BtnAr.BackgroundColor = Color.FromArgb("#f1f5f9"); BtnAr.TextColor = Color.FromArgb("#475569");
        FlowDirection = FlowDirection.LeftToRight;
        Title = "Profile";
    }}

    async void OnLogoutClicked(object sender, EventArgs e)
    {{
        bool confirm = await DisplayAlert(
            "تسجيل الخروج", "هل تريد تسجيل الخروج؟", "نعم", "لا");
        if (!confirm) return;
        SecureStorage.RemoveAll();
        await Shell.Current.GoToAsync("//login");
    }}
}}