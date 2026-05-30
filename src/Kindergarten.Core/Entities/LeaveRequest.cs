namespace Kindergarten.Core.Entities;
public class LeaveRequest
{
    public int      Id         { get; set; }
    public string   UserId     { get; set; } = string.Empty;
    public string   Reason     { get; set; } = string.Empty;
    public DateTime StartTime  { get; set; }
    public DateTime EndTime    { get; set; }
    public double   Hours      { get; set; }
    public string   Status     { get; set; } = "Pending";
    public string?  AdminNote  { get; set; }
    public bool     IsPaid     { get; set; } = true;
    public int      TenantId   { get; set; } = 1;
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public string?  ReviewedBy { get; set; }
    public ApplicationUser User { get; set; } = null!;
}
