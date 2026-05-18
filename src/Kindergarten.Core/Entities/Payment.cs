namespace Kindergarten.Core.Entities;

public class Payment
{
    public int Id { get; set; }
    public int SubscriptionId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public string Method { get; set; } = string.Empty;
    public Subscription Subscription { get; set; } = null!;
}
