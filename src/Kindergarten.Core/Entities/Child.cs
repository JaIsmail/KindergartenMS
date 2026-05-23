namespace Kindergarten.Core.Entities;

public class Child
{
    public int Id       { get; set; }
    public int? TenantId { get; set; } = 1;
    public Tenant? Tenant { get; set; }
    public string ParentId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public string Class { get; set; } = string.Empty;
    public string HealthNotes { get; set; } = string.Empty;
    public ApplicationUser Parent { get; set; } = null!;
    public ICollection<TripChild> TripChildren { get; set; } = new List<TripChild>();
}
