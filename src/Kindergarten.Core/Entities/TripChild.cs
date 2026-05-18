namespace Kindergarten.Core.Entities;

public class TripChild
{
    public int TripId { get; set; }
    public int ChildId { get; set; }
    public string PickupStatus { get; set; } = "Pending";
    public string DropoffStatus { get; set; } = "Pending";
    public DateTime? PickupTime { get; set; }
    public DateTime? DropoffTime { get; set; }
    public Trip Trip { get; set; } = null!;
    public Child Child { get; set; } = null!;
}
