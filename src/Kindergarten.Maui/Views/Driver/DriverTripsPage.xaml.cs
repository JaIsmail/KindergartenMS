using Kindergarten.Maui.Services;

namespace Kindergarten.Maui.Views.Driver;

public partial class DriverTripsPage : ContentPage
{
    private readonly ApiService _api = new();
    private int _activeTripId = 0;

    public DriverTripsPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadTripsAsync();
    }

    async void OnRefreshing(object sender, EventArgs e)
    {
        await LoadTripsAsync();
        RefreshView.IsRefreshing = false;
    }

    async Task LoadTripsAsync()
    {
        var trips = await _api.GetMyTripsAsync();
        if (trips == null) return;

        TripsList.ItemsSource = trips;

        // Check for active trip
        var activeTrip = trips.FirstOrDefault(t => t.Status == "InProgress");
        if (activeTrip != null)
        {
            _activeTripId              = activeTrip.Id;
            ActiveTripFrame.IsVisible  = true;
            ActiveTripIdLabel.Text     = $"رحلة #{activeTrip.Id} — {activeTrip.Direction}";
        }
        else
        {
            ActiveTripFrame.IsVisible = false;
        }
    }

    async void OnStartTripClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is int tripId)
        {
            var result = await _api.StartTripAsync(tripId);
            if (result != null)
            {
                _activeTripId = tripId;
                await DisplayAlert("✅", $"بدأت الرحلة #{tripId}", "موافق");
                await LoadTripsAsync();
            }
        }
    }

    async void OnEndTripClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("إنهاء الرحلة", "هل تريد إنهاء الرحلة؟", "نعم", "لا");
        if (!confirm) return;

        var result = await _api.EndTripAsync(_activeTripId);
        if (result != null)
        {
            await DisplayAlert("✅", "تم إنهاء الرحلة بنجاح", "موافق");
            await LoadTripsAsync();
        }
    }
}