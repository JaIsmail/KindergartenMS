namespace Kindergarten.Core.Entities;

public class NotificationTemplate
{
    public int    Id       { get; set; }
    public string Key      { get; set; } = string.Empty; // e.g. "payment_confirmed", "subscription_created", "attendance_marked"
    public string TitleAr  { get; set; } = string.Empty;
    public string TitleEn  { get; set; } = string.Empty;
    public string BodyAr   { get; set; } = string.Empty; // supports {placeholders}
    public string BodyEn   { get; set; } = string.Empty;
    public int    TenantId { get; set; } = 1;
    public bool   IsActive { get; set; } = true;
}
