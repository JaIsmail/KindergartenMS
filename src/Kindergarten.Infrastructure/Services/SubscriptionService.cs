using Kindergarten.Core.DTOs;
using Kindergarten.Core.Entities;
using Kindergarten.Core.Interfaces;
using Kindergarten.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Kindergarten.Infrastructure.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ApplicationDbContext _db;
    public SubscriptionService(ApplicationDbContext db) => _db = db;

    public async Task<IEnumerable<SubscriptionResponseDto>> GetAllAsync(string parentId)
    {
        return await _db.Subscriptions
            .Include(s => s.Child)
            .Where(s => s.ParentId == parentId)
            .Select(s => new SubscriptionResponseDto
            {
                Id            = s.Id,
                ChildId       = s.ChildId,
                ChildName     = s.Child.Name,
                Type          = s.Type,
                Price         = s.Price,
                StartDate     = s.StartDate,
                EndDate       = s.EndDate,
                PaymentStatus = s.PaymentStatus
            })
            .ToListAsync();
    }

    public async Task<SubscriptionResponseDto?> GetByIdAsync(int id)
    {
        var s = await _db.Subscriptions
            .Include(s => s.Child)
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
            PaymentStatus = s.PaymentStatus
        };
    }

    public async Task<SubscriptionResponseDto> CreateAsync(CreateSubscriptionDto dto, string parentId)
    {
        var subscription = new Subscription
        {
            ParentId      = parentId,
            ChildId       = dto.ChildId,
            Type          = dto.Type,
            Price         = dto.Price,
            StartDate     = dto.StartDate,
            EndDate       = dto.EndDate,
            PaymentStatus = "Pending"
        };

        _db.Subscriptions.Add(subscription);
        await _db.SaveChangesAsync();

        var child = await _db.Children.FindAsync(dto.ChildId);

        return new SubscriptionResponseDto
        {
            Id            = subscription.Id,
            ChildId       = subscription.ChildId,
            ChildName     = child?.Name ?? string.Empty,
            Type          = subscription.Type,
            Price         = subscription.Price,
            StartDate     = subscription.StartDate,
            EndDate       = subscription.EndDate,
            PaymentStatus = subscription.PaymentStatus
        };
    }
}
