namespace Kindergarten.Core.DTOs;

public class CreateTripDto
{
    public string   DriverId  { get; set; } = string.Empty;
    public string   Direction { get; set; } = string.Empty; // ToKindergarten, ToHome
    public DateTime Date      { get; set; } = DateTime.UtcNow;
    public List<int> ChildIds { get; set; } = new();
}

public class TripResponseDto
{
    public int      Id        { get; set; }
    public string   DriverId  { get; set; } = string.Empty;
    public string   DriverName { get; set; } = string.Empty;
    public DateTime Date      { get; set; }
    public string   Direction { get; set; } = string.Empty;
    public string   Status    { get; set; } = string.Empty;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime   { get; set; }
    public List<TripChildStatusDto> Children { get; set; } = new();
}

public class TripChildStatusDto
{
    public int    ChildId       { get; set; }
    public string ChildName     { get; set; } = string.Empty;
    public string PickupStatus  { get; set; } = string.Empty;
    public string DropoffStatus { get; set; } = string.Empty;
    public DateTime? PickupTime  { get; set; }
    public DateTime? DropoffTime { get; set; }
}

public class UpdateLocationDto
{
    public int    TripId    { get; set; }
    public double Latitude  { get; set; }
    public double Longitude { get; set; }
}

public class UpdateChildStatusDto
{
    public int    TripId  { get; set; }
    public int    ChildId { get; set; }
    public string Status  { get; set; } = string.Empty; // PickedUp, DroppedOff, Missed
    public string Type    { get; set; } = string.Empty; // Pickup, Dropoff
}
