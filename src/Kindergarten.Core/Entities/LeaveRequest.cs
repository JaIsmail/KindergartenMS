namespace Kindergarten.Core.Entities;

public class LeaveRequest
{
    public int    Id         { get; set; }
    public int    EmployeeId { get; set; }
    public string Reason     { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime   { get; set; }
    public double   Hours     { get; set; }
    public string Status      { get; set; } = "Pending"; // Pending, Approved, Rejected
    public string? AdminNote  { get; set; }
    public bool   IsPaid      { get; set; } = true; // false = deducted from salary
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedBy   { get; set; }

    public Employee Employee { get; set; } = null!;
}
