namespace Kindergarten.Core.DTOs;

public class SendNotificationDto
{
    public string DeviceToken { get; set; } = string.Empty;
    public string TitleAr     { get; set; } = string.Empty;
    public string TitleEn     { get; set; } = string.Empty;
    public string BodyAr      { get; set; } = string.Empty;
    public string BodyEn      { get; set; } = string.Empty;
    public Dictionary<string, string> Data { get; set; } = new();
}

public class NotificationEvent
{
    public const string TripStarted     = "trip_started";
    public const string DriverArriving  = "driver_arriving";
    public const string ChildPickedUp   = "child_picked_up";
    public const string ChildDroppedOff = "child_dropped_off";
    public const string TripCompleted   = "trip_completed";
    public const string SubExpiring     = "subscription_expiring";
}
