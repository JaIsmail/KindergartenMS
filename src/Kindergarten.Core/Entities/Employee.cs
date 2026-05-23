namespace Kindergarten.Core.Entities;

public class Employee
{
    public int Id       { get; set; }
    public int? TenantId { get; set; } = 1;
    public Tenant? Tenant { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
}
