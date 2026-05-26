using Kindergarten.Core.DTOs;
using Kindergarten.Core.Entities;
using Kindergarten.Core.Interfaces;
using Kindergarten.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Kindergarten.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantService _tenantService;
    private readonly INotificationService _notify;

    public PaymentService(ApplicationDbContext db, ITenantService tenantService, INotificationService notify)
    {
        _db = db;
        _tenantService = tenantService;
        _notify = notify;
    }

    public async Task<IEnumerable<PaymentResponseDto>> GetBySubscriptionAsync(int subscriptionId)
    {
        return await _db.Payments
            .Where(p => p.SubscriptionId == subscriptionId)
            .Select(p => new PaymentResponseDto
            {
                Id             = p.Id,
                SubscriptionId = p.SubscriptionId,
                Amount         = p.Amount,
                Method         = p.Method,
                PaymentDate    = p.PaymentDate
            })
            .ToListAsync();
    }

    public async Task<PaymentResponseDto> CreateAsync(CreatePaymentDto dto)
    {
        var payment = new Payment
        {
            SubscriptionId = dto.SubscriptionId,
            Amount         = dto.Amount,
            Method         = dto.Method,
            PaymentDate    = DateTime.UtcNow,
            TenantId       = _tenantService.GetTenantId()
        };

        _db.Payments.Add(payment);

        // Update subscription status to Paid
        var subscription = await _db.Subscriptions.FindAsync(dto.SubscriptionId);
        if (subscription != null)
            subscription.PaymentStatus = "Paid";

        await _db.SaveChangesAsync();

        // Notify parent
        if (subscription != null)
        {
            var parent = await _db.Users.FindAsync(subscription.ParentId);
            var child  = await _db.Children.FindAsync(subscription.ChildId);
            if (parent != null && child != null)
            {
                await _notify.SendToUserAsync(
                    parent.Id,
                    "تم تأكيد الدفع ✅", "Payment Confirmed ✅",
                    $"تم استلام دفعة بمبلغ {dto.Amount} ريال لاشتراك {child.Name}",
                    $"Payment of {dto.Amount} SAR received for {child.Name}'s subscription",
                    new Dictionary<string, string>
                    {
                        ["type"]      = "payment_confirmed",
                        ["paymentId"] = payment.Id.ToString()
                    }
                );
            }
        }

        return new PaymentResponseDto
        {
            Id             = payment.Id,
            SubscriptionId = payment.SubscriptionId,
            Amount         = payment.Amount,
            Method         = payment.Method,
            PaymentDate    = payment.PaymentDate
        };
    }

    public async Task<IEnumerable<PaymentResponseDto>> GetAllAsync()
    {
        return await _db.Payments
            .OrderByDescending(p => p.PaymentDate)
            .Select(p => new PaymentResponseDto
            {
                Id             = p.Id,
                SubscriptionId = p.SubscriptionId,
                Amount         = p.Amount,
                Method         = p.Method,
                PaymentDate    = p.PaymentDate
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<OverdueSubscriptionDto>> GetOverdueAsync()
    {
        var today = DateTime.UtcNow.Date;
        return await _db.Subscriptions
            .Include(s => s.Child)
            .Include(s => s.Parent)
            .Where(s => s.EndDate.Date < today && s.PaymentStatus != "Paid")
            .OrderBy(s => s.EndDate)
            .Select(s => new OverdueSubscriptionDto
            {
                Id            = s.Id,
                ChildName     = s.Child.Name,
                ParentName    = s.Parent.FullName,
                ParentEmail   = s.Parent.Email ?? string.Empty,
                Type          = s.Type,
                Price         = s.Price,
                EndDate       = s.EndDate,
                DaysOverdue   = (int)(today - s.EndDate.Date).TotalDays,
                PaymentStatus = s.PaymentStatus
            })
            .ToListAsync();
    }
}
