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
