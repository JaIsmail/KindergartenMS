namespace Kindergarten.Core.Entities;
public class Attendance
{
    public int       Id           { get; set; }
    public string    UserId       { get; set; } = string.Empty;
    public int       TenantId     { get; set; } = 1;
    public DateTime  Date         { get; set; }
    public DateTime? CheckInTime  { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string    Status       { get; set; } = "Present";
    public ApplicationUser User   { get; set; } = null!;
    public ICollection<AttendancePeriod> Periods { get; set; } = new List<AttendancePeriod>();
}
