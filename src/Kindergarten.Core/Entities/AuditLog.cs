namespace Kindergarten.Core.Entities;

public class AuditLog
{
    public int      Id          { get; set; }
    public int      TenantId    { get; set; }
    public string   UserId      { get; set; } = string.Empty;
    public string   UserEmail   { get; set; } = string.Empty;
    public string   UserName    { get; set; } = string.Empty;
    public string   Action      { get; set; } = string.Empty; // Create, Update, Delete, Login, Approve, Reject
    public string   EntityType  { get; set; } = string.Empty; // Child, Subscription, Payment, User, LeaveRequest, Trip
    public string   EntityId    { get; set; } = string.Empty;
    public string   Details     { get; set; } = string.Empty; // JSON summary
    public string   IpAddress   { get; set; } = string.Empty;
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;
}
