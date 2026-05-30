namespace Kindergarten.Core.DTOs;
public class CreateLeaveRequestDto
{
    public string   Reason    { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime   { get; set; }
}
public class ReviewLeaveRequestDto
{
    public string  Status    { get; set; } = string.Empty;
    public string? AdminNote { get; set; }
}
public class LeaveRequestResponseDto
{
    public int      Id           { get; set; }
    public string   UserId       { get; set; } = string.Empty;
    public string   UserName     { get; set; } = string.Empty;
    public string   Reason       { get; set; } = string.Empty;
    public DateTime StartTime    { get; set; }
    public DateTime EndTime      { get; set; }
    public double   Hours        { get; set; }
    public string   Status       { get; set; } = string.Empty;
    public string?  AdminNote    { get; set; }
    public bool     IsPaid       { get; set; }
    public DateTime CreatedAt    { get; set; }
    public DateTime? ReviewedAt  { get; set; }
    public string?  ReviewedBy   { get; set; }
}
