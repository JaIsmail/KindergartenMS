namespace Kindergarten.Core.Entities;

public class DynamicList
{
    public int    Id       { get; set; }
    public string Category { get; set; } = string.Empty; // Classes, SubscriptionTypes, TripStatuses
    public string NameAr   { get; set; } = string.Empty;
    public string NameEn   { get; set; } = string.Empty;
    public string Value    { get; set; } = string.Empty; // stored value (e.g. KG1)
    public int    Order    { get; set; } = 0;
    public bool   IsActive { get; set; } = true;
    public int    TenantId { get; set; } = 1;
    public DateTime? StartDate { get; set; } // used for SubscriptionPeriods
    public DateTime? EndDate   { get; set; } // used for SubscriptionPeriods
}
