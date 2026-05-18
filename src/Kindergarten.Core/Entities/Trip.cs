namespace Kindergarten.Core.Entities;

public class Trip
{
    public int Id { get; set; }
    public string DriverId { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Direction { get; set; } = string.Empty;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Status { get; set; } = "Created";
    public ApplicationUser Driver { get; set; } = null!;
    public ICollection<TripChild> TripChildren { get; set; } = new List<TripChild>();
    public ICollection<TripLocation> TripLocations { get; set; } = new List<TripLocation>();
}
