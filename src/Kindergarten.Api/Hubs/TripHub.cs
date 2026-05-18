using Microsoft.AspNetCore.SignalR;

namespace Kindergarten.Api.Hubs;

public class TripHub : Hub
{
    public async Task SendLocation(int tripId, double latitude, double longitude)
        => await Clients.All.SendAsync("LocationUpdated", tripId, latitude, longitude);

    public async Task SendChildStatus(int tripId, int childId, string status)
        => await Clients.All.SendAsync("ChildStatusUpdated", tripId, childId, status);

    public async Task SendTripStatus(int tripId, string status)
        => await Clients.All.SendAsync("TripStatusUpdated", tripId, status);
}
