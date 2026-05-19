using Kindergarten.Maui.Services;

namespace Kindergarten.Maui.Views.Driver;

public partial class DriverTripsPage : ContentPage
{
    private readonly ApiService _api = new();

    public DriverTripsPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadTripsAsync();
    }

    async Task LoadTripsAsync()
    {
        try
        {
            var trips = await _api.GetMyTripsAsync();
            TripsStack.Children.Clear();

            if (trips == null || trips.Count == 0)
            {
                TripsStack.Children.Add(new Label
                {
                    Text = "لا توجد رحلات",
                    HorizontalOptions = LayoutOptions.Center,
                    TextColor = Color.FromArgb("#94a3b8"),
                    Margin = new Thickness(0, 20)
                });
                return;
            }

            foreach (var trip in trips)
            {
                var card = new Border
                {
                    BackgroundColor = Colors.White,
                    StrokeCornerRadius = 14,
                    Stroke = Color.FromArgb("#e2e8f0"),
                    Padding = new Thickness(16)
                };

                var stack = new VerticalStackLayout { Spacing = 8 };

                stack.Children.Add(new Label
                {
                    Text = $"رحلة #{trip.Id} — {(trip.Direction == "ToKindergarten" ? "🏫 إلى الروضة" : "🏠 إلى المنزل")}",
                    FontSize = 15,
                    FontAttributes = FontAttributes.Bold
                });

                stack.Children.Add(new Label
                {
                    Text = $"الحالة: {trip.Status}",
                    FontSize = 13,
                    TextColor = Color.FromArgb("#64748b")
                });

                if (trip.Status == "Created")
                {
                    var btn = new Button
                    {
                        Text = "▶️ بدء الرحلة",
                        BackgroundColor = Color.FromArgb("#f97316"),
                        TextColor = Colors.White,
                        CornerRadius = 10,
                        FontAttributes = FontAttributes.Bold
                    };
                    var tripId = trip.Id;
                    btn.Clicked += async (s, e) =>
                    {
                        var result = await _api.StartTripAsync(tripId);
                        if (result != null) { await DisplayAlert("✅", "بدأت الرحلة", "موافق"); await LoadTripsAsync(); }
                    };
                    stack.Children.Add(btn);
                }

                if (trip.Status == "InProgress")
                {
                    var btn = new Button
                    {
                        Text = "⏹️ إنهاء الرحلة",
                        BackgroundColor = Color.FromArgb("#3b82f6"),
                        TextColor = Colors.White,
                        CornerRadius = 10,
                        FontAttributes = FontAttributes.Bold
                    };
                    var tripId = trip.Id;
                    btn.Clicked += async (s, e) =>
                    {
                        bool confirm = await DisplayAlert("إنهاء الرحلة", "هل تريد إنهاء الرحلة؟", "نعم", "لا");
                        if (!confirm) return;
                        var result = await _api.EndTripAsync(tripId);
                        if (result != null) { await DisplayAlert("✅", "انتهت الرحلة", "موافق"); await LoadTripsAsync(); }
                    };
                    stack.Children.Add(btn);
                }

                card.Content = stack;
                TripsStack.Children.Add(card);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }
}