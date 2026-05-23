namespace Kindergarten.Core.DTOs;

public class CreateLeaveRequestDto
{
    public string   Reason    { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime   { get; set; }
}

public class ReviewLeaveRequestDto
{
    public string  Status    { get; set; } = string.Empty; // Approved, Rejected
    public string? AdminNote { get; set; }
}

public class LeaveRequestResponseDto
{
    public int      Id           { get; set; }
    public int      EmployeeId   { get; set; }
    public string   EmployeeName { get; set; } = string.Empty;
    public string   Reason       { get; set; } = string.Empty;
    public DateTime StartTime    { get; set; }
    public DateTime EndTime      { get; set; }
    public double   Hours        { get; set; }
    public string   Status       { get; set; } = string.Empty;
    public string?  AdminNote    { get; set; }
    public bool     IsPaid       { get; set; }
    public DateTime CreatedAt    { get; set; }
    public string?  ReviewedBy   { get; set; }
}
