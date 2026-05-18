namespace Kindergarten.Core.Entities;

public class Employee
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
}
