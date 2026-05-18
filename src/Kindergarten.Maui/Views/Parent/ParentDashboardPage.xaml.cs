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

    async void OnRefreshing(object sender, EventArgs e)
    {
        await LoadDataAsync();
        RefreshView.IsRefreshing = false;
    }

    async Task LoadDataAsync()
    {
        var lang     = Preferences.Get(Constants.LangKey, "ar");
        var userName = await SecureStorage.GetAsync(Constants.UserNameKey) ?? "";
        bool ar      = lang == "ar";

        WelcomeLabel.Text  = ar ? "مرحباً،" : "Welcome,";
        UserNameLabel.Text = userName;
        ChildrenLabel.Text = ar ? "الأطفال"    : "Children";
        SubsLabel.Text     = ar ? "الاشتراكات" : "Subscriptions";
        MyChildrenLabel.Text = ar ? "أطفالي"      : "My Children";
        MySubsLabel.Text     = ar ? "اشتراكاتي"   : "My Subscriptions";

        FlowDirection = ar ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

        // Load children
        var children = await _api.GetChildrenAsync();
        if (children != null)
        {
            ChildrenList.ItemsSource   = children;
            ChildrenCountLabel.Text    = children.Count.ToString();
        }

        // Load subscriptions
        var subs = await _api.GetSubscriptionsAsync();
        if (subs != null)
        {
            SubsList.ItemsSource = subs;
            SubsCountLabel.Text  = subs.Count.ToString();
        }
    }
}