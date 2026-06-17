namespace Kindergarten.Core.Entities;

public class Payment
{
    public int Id { get; set; }
    public int SubscriptionId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public int TenantId { get; set; } = 1;
    public string Method { get; set; } = string.Empty;
    public string Notes  { get; set; } = string.Empty;
    public string Status { get; set; } = "Completed";
    public Subscription Subscription { get; set; } = null!;
}
