namespace Kindergarten.Core.DTOs;

public class CreateSubscriptionDto
{
    public int      ChildId   { get; set; }
    public string   Type      { get; set; } = string.Empty; // Daily, Monthly, Yearly
    public decimal  Price     { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate   { get; set; }
    public string   Period    { get; set; } = string.Empty;
    public string?  ParentId  { get; set; }
}

public class SubscriptionResponseDto
{
    public int      Id            { get; set; }
    public int      ChildId       { get; set; }
    public string   ChildName     { get; set; } = string.Empty;
    public string   Type          { get; set; } = string.Empty;
    public decimal  Price         { get; set; }
    public DateTime StartDate     { get; set; }
    public DateTime EndDate       { get; set; }
    public string   PaymentStatus { get; set; } = string.Empty;
    public string   Period        { get; set; } = string.Empty;
    public string   ParentId      { get; set; } = string.Empty;
    public string   ParentName    { get; set; } = string.Empty;
}
