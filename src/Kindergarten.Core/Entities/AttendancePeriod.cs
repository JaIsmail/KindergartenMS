namespace Kindergarten.Core.Entities;

public class AttendancePeriod
{
    public int       Id           { get; set; }
    public int       AttendanceId { get; set; }
    public DateTime  CheckIn      { get; set; }
    public DateTime? CheckOut     { get; set; }
    public double    Hours        { get; set; } = 0;
    public Attendance Attendance  { get; set; } = null!;
}
