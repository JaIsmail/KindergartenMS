using Kindergarten.Maui.Services;
using Microsoft.Maui.Storage;

namespace Kindergarten.Maui.Views.Parent;

public partial class ParentDashboardPage : ContentPage
{
    private readonly ApiService _api = new();

    public ParentDashboardPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDataAsync();
    }

    async Task LoadDataAsync()
    {
        try
        {
            var lang     = Preferences.Get(Constants.LangKey, "ar");
            var userName = await SecureStorage.GetAsync(Constants.UserNameKey) ?? "";
            bool ar      = lang == "ar";

            WelcomeLabel.Text    = ar ? "مرحباً،" : "Welcome,";
            UserNameLabel.Text   = userName;
            ChildrenLabel.Text   = ar ? "الأطفال"     : "Children";
            SubsLabel.Text       = ar ? "الاشتراكات"  : "Subscriptions";
            MyChildrenLabel.Text = ar ? "أطفالي"      : "My Children";
            MySubsLabel.Text     = ar ? "اشتراكاتي"   : "My Subscriptions";

            // Load children
            var children = await _api.GetChildrenAsync();
            if (children != null && children.Count > 0)
            {
                ChildrenCountLabel.Text = children.Count.ToString();
                ChildrenStack.Children.Clear();
                foreach (var child in children)
                {
                    ChildrenStack.Children.Add(new Grid
                    {
                        Padding = new Thickness(16, 12),
                        ColumnDefinitions =
                        {
                            new ColumnDefinition { Width = GridLength.Star },
                            new ColumnDefinition { Width = GridLength.Auto }
                        },
                        Children =
                        {
                            new VerticalStackLayout
                            {
                                Children =
                                {
                                    new Label { Text = child.Name, FontSize = 15, FontAttributes = FontAttributes.Bold },
                                    new Label { Text = child.Class, FontSize = 13, TextColor = Color.FromArgb("#64748b") }
                                }
                            },
                            new Label
                            {
                                Text = child.Class,
                                TextColor = Color.FromArgb("#2563eb"),
                                FontSize = 12,
                                FontAttributes = FontAttributes.Bold,
                                VerticalOptions = LayoutOptions.Center
                            }
                        }
                    });
                }
            }

            // Load subscriptions
            var subs = await _api.GetSubscriptionsAsync();
            if (subs != null && subs.Count > 0)
            {
                SubsCountLabel.Text = subs.Count.ToString();
                SubsStack.Children.Clear();
                foreach (var sub in subs)
                {
                    SubsStack.Children.Add(new Grid
                    {
                        Padding = new Thickness(16, 12),
                        ColumnDefinitions =
                        {
                            new ColumnDefinition { Width = GridLength.Star },
                            new ColumnDefinition { Width = GridLength.Auto }
                        },
                        Children =
                        {
                            new VerticalStackLayout
                            {
                                Children =
                                {
                                    new Label { Text = sub.ChildName, FontSize = 15, FontAttributes = FontAttributes.Bold },
                                    new Label { Text = $"SAR {sub.Price:F2}", FontSize = 13, TextColor = Color.FromArgb("#f97316") }
                                }
                            },
                            new Label
                            {
                                Text = sub.PaymentStatus,
                                TextColor = sub.PaymentStatus == "Paid" ? Color.FromArgb("#16a34a") : Color.FromArgb("#ea580c"),
                                FontSize = 12,
                                FontAttributes = FontAttributes.Bold,
                                VerticalOptions = LayoutOptions.Center
                            }
                        }
                    });
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    async void OnLogoutClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("تسجيل الخروج", "هل تريد تسجيل الخروج؟", "نعم", "لا");
        if (!confirm) return;
        SecureStorage.RemoveAll();
        await Shell.Current.GoToAsync("//login");
    }
}
