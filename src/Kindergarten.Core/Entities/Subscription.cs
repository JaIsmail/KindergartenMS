namespace Kindergarten.Core.Entities;

public class Subscription
{
    public int Id       { get; set; }
    public int? TenantId { get; set; } = 1;
    public Tenant? Tenant { get; set; }
    public string ParentId { get; set; } = string.Empty;
    public int ChildId { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string PaymentStatus { get; set; } = "Pending";
    public string Period       { get; set; } = string.Empty;
    public ApplicationUser Parent { get; set; } = null!;
    public Child Child { get; set; } = null!;
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
