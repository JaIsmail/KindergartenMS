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
            .IgnoreQueryFilters().Where(p => p.SubscriptionId == subscriptionId)
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
            Notes          = dto.Notes,
            PaymentDate    = DateTime.UtcNow,
            TenantId       = _tenantService.GetTenantId()
        };

        _db.Payments.Add(payment);

        // Update subscription status to Paid
        var subscription = await _db.Subscriptions.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == dto.SubscriptionId);
        if (subscription != null)
            subscription.PaymentStatus = "Paid";

        await _db.SaveChangesAsync();

        // Notify parent
        if (subscription != null)
        {
            var parent = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == subscription.ParentId);
            var child  = await _db.Children.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == subscription.ChildId);
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
            Notes          = payment.Notes,
            PaymentDate    = payment.PaymentDate
        };
    }

    public async Task<ChildPaymentHistoryDto?> GetByChildAsync(int childId)
    {
        var child = await _db.Children.IgnoreQueryFilters().Include(c => c.Parent).IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == childId);
        if (child == null) return null;

        var subscriptions = await _db.Subscriptions
            .IgnoreQueryFilters().Where(s => s.ChildId == childId)
            .OrderByDescending(s => s.StartDate)
            .ToListAsync();

        var subIds = subscriptions.Select(s => s.Id).ToList();
        var payments = await _db.Payments
            .IgnoreQueryFilters().Where(p => subIds.Contains(p.SubscriptionId))
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();

        var totalPrice = subscriptions.Sum(s => s.Price);
        var totalPaid  = payments.Sum(p => p.Amount);

        return new ChildPaymentHistoryDto
        {
            ChildId       = child.Id,
            ChildName     = child.Name,
            ParentName    = child.Parent?.FullName ?? string.Empty,
            ParentEmail   = child.Parent?.Email ?? string.Empty,
            TotalPrice    = totalPrice,
            TotalPaid     = totalPaid,
            Balance       = totalPrice - totalPaid,
            PaymentStatus = subscriptions.Any() ? subscriptions.First().PaymentStatus : "None",
            Subscriptions = subscriptions.Select(s => new SubscriptionSummaryDto
            {
                Id            = s.Id,
                Type          = s.Type,
                Period        = s.Period,
                Price         = s.Price,
                StartDate     = s.StartDate,
                EndDate       = s.EndDate,
                PaymentStatus = s.PaymentStatus
            }).ToList(),
            Payments = payments.Select(p => new PaymentResponseDto
            {
                Id             = p.Id,
                SubscriptionId = p.SubscriptionId,
                Amount         = p.Amount,
                Method         = p.Method,
                Notes          = p.Notes,
                PaymentDate    = p.PaymentDate
            }).ToList()
        };
    }

    public async Task<IEnumerable<PaymentResponseDto>> GetAllAsync()
    {
        return await _db.Payments
            .IgnoreQueryFilters().OrderByDescending(p => p.PaymentDate)
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
            .IgnoreQueryFilters().Include(s => s.Child)
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
