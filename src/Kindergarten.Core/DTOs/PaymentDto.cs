namespace Kindergarten.Core.DTOs;

public class CreatePaymentDto
{
    public int     SubscriptionId { get; set; }
    public decimal Amount         { get; set; }
    public string  Method         { get; set; } = string.Empty;
    public string  Notes          { get; set; } = string.Empty;
}

public class PaymentResponseDto
{
    public int      Id             { get; set; }
    public int      SubscriptionId { get; set; }
    public decimal  Amount         { get; set; }
    public string   Method         { get; set; } = string.Empty;
    public DateTime PaymentDate    { get; set; }
    public string   Notes         { get; set; } = string.Empty;
    public string   ChildName     { get; set; } = string.Empty;
    public string   ParentName    { get; set; } = string.Empty;
}

public class OverdueSubscriptionDto
{
    public int      Id            { get; set; }
    public string   ChildName     { get; set; } = string.Empty;
    public string   ParentName    { get; set; } = string.Empty;
    public string   ParentEmail   { get; set; } = string.Empty;
    public string   Type          { get; set; } = string.Empty;
    public decimal  Price         { get; set; }
    public DateTime EndDate       { get; set; }
    public int      DaysOverdue   { get; set; }
    public string   PaymentStatus { get; set; } = string.Empty;
}

public class ChildPaymentHistoryDto
{
    public int     ChildId       { get; set; }
    public string  ChildName     { get; set; } = string.Empty;
    public string  ParentName    { get; set; } = string.Empty;
    public string  ParentEmail   { get; set; } = string.Empty;
    public decimal TotalPrice    { get; set; }
    public decimal TotalPaid     { get; set; }
    public decimal Balance       { get; set; }
    public string  PaymentStatus { get; set; } = string.Empty;
    public List<SubscriptionSummaryDto> Subscriptions { get; set; } = new();
    public List<PaymentResponseDto>     Payments      { get; set; } = new();
}

public class SubscriptionSummaryDto
{
    public int      Id            { get; set; }
    public string   Type          { get; set; } = string.Empty;
    public string   Period        { get; set; } = string.Empty;
    public decimal  Price         { get; set; }
    public DateTime StartDate     { get; set; }
    public DateTime EndDate       { get; set; }
    public string   PaymentStatus { get; set; } = string.Empty;
}
