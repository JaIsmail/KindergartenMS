namespace Kindergarten.Core.Entities;

public class TripLocation
{
    public int Id { get; set; }
    public int TenantId { get; set; } = 1;
    public int TripId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public Trip Trip { get; set; } = null!;
}
