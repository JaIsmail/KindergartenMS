namespace Kindergarten.Core.DTOs;

public class CreatePaymentDto
{
    public int     SubscriptionId { get; set; }
    public decimal Amount         { get; set; }
    public string  Method         { get; set; } = string.Empty; // Cash, Transfer, Online
}

public class PaymentResponseDto
{
    public int      Id             { get; set; }
    public int      SubscriptionId { get; set; }
    public decimal  Amount         { get; set; }
    public string   Method         { get; set; } = string.Empty;
    public DateTime PaymentDate    { get; set; }
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
