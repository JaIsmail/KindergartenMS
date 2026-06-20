using Kindergarten.Core.DTOs;
using Kindergarten.Core.Entities;
using Kindergarten.Core.Interfaces;
using Kindergarten.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Kindergarten.Infrastructure.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantService _tenantService;
    private readonly INotificationService _notify;
    public SubscriptionService(ApplicationDbContext db, ITenantService tenantService, INotificationService notify)
    {
        _db = db;
        _tenantService = tenantService;
        _notify = notify;
    }

    public async Task<IEnumerable<SubscriptionResponseDto>> GetAllAsync(string? parentId)
    {
        return await _db.Subscriptions
            .IgnoreQueryFilters().Include(s => s.Child)
            .Include(s => s.Parent)
            .Where(s => parentId == null || s.ParentId == parentId)
            .Select(s => new SubscriptionResponseDto
            {
                Id            = s.Id,
                ChildId       = s.ChildId,
                ChildName     = s.Child.Name,
                Type          = s.Type,
                Price         = s.Price,
                StartDate     = s.StartDate,
                EndDate       = s.EndDate,
                PaymentStatus = s.PaymentStatus,
                Period        = s.Period,
                ParentId      = s.ParentId,
                ParentName    = s.Parent != null ? s.Parent.FullName : string.Empty
            })
            .ToListAsync();
    }

    public async Task<SubscriptionResponseDto?> GetByIdAsync(int id)
    {
        var s = await _db.Subscriptions
            .IgnoreQueryFilters().Include(s => s.Child)
            .Include(s => s.Parent)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (s == null) return null;

        return new SubscriptionResponseDto
        {
            Id            = s.Id,
            ChildId       = s.ChildId,
            ChildName     = s.Child.Name,
            Type          = s.Type,
            Price         = s.Price,
            StartDate     = s.StartDate,
            EndDate       = s.EndDate,
            PaymentStatus = s.PaymentStatus,
            Period        = s.Period,
            ParentId      = s.ParentId,
            ParentName    = s.Parent?.FullName ?? string.Empty
        };
    }

    public async Task<SubscriptionResponseDto> CreateAsync(CreateSubscriptionDto dto, string parentId)
    {
        // Auto-set ParentId from child if not provided
        var child = await _db.Children
            .IgnoreQueryFilters().Include(c => c.Parent)
            .FirstOrDefaultAsync(c => c.Id == dto.ChildId);
        var resolvedParentId = !string.IsNullOrEmpty(dto.ParentId)
            ? dto.ParentId
            : child?.ParentId ?? parentId;

        var subscription = new Subscription
        {
            ParentId      = resolvedParentId,
            ChildId       = dto.ChildId,
            Type          = dto.Type,
            Price         = dto.Price,
            StartDate     = dto.StartDate,
            EndDate       = dto.EndDate,
            Period        = dto.Period,
            PaymentStatus = "Pending",
            TenantId      = _tenantService.GetTenantId()
        };

        _db.Subscriptions.Add(subscription);
        await _db.SaveChangesAsync();


        if (child != null)
        {
            await _notify.SendToUserAsync(
                resolvedParentId,
                "تم تسجيل اشتراك جديد", "New Subscription Registered",
                $"تم تسجيل اشتراك {dto.Type} لـ {child.Name} بقيمة {dto.Price} ريال",
                $"A {dto.Type} subscription has been registered for {child.Name} for {dto.Price} SAR",
                new Dictionary<string, string>
                {
                    ["type"] = "subscription_created",
                    ["subscriptionId"] = subscription.Id.ToString()
                }
            );
        }

        return new SubscriptionResponseDto
        {
            Id            = subscription.Id,
            ChildId       = subscription.ChildId,
            ChildName     = child?.Name ?? string.Empty,
            Type          = subscription.Type,
            Price         = subscription.Price,
            StartDate     = subscription.StartDate,
            EndDate       = subscription.EndDate,
            Period        = subscription.Period,
            ParentId      = subscription.ParentId,
            ParentName    = child?.Parent?.FullName ?? string.Empty,
            PaymentStatus = subscription.PaymentStatus
        };
    }

    public async Task<SubscriptionResponseDto?> UpdateStatusAsync(int id, string status)
    {
        var sub = await _db.Subscriptions
            .IgnoreQueryFilters().Include(s => s.Child)
            .Include(s => s.Parent)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (sub == null) return null;

        sub.PaymentStatus = status;
        await _db.SaveChangesAsync();

        return new SubscriptionResponseDto
        {
            Id            = sub.Id,
            ChildId       = sub.ChildId,
            ChildName     = sub.Child.Name,
            Type          = sub.Type,
            Price         = sub.Price,
            StartDate     = sub.StartDate,
            EndDate       = sub.EndDate,
            PaymentStatus = sub.PaymentStatus,
            Period        = sub.Period,
            ParentId      = sub.ParentId,
            ParentName    = sub.Parent?.FullName ?? string.Empty
        };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var sub = await _db.Subscriptions.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == id);
        if (sub == null) return false;
        _db.Subscriptions.Remove(sub);
        await _db.SaveChangesAsync();
        return true;
    }

}
