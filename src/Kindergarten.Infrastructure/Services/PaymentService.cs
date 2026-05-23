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
    public PaymentService(ApplicationDbContext db, ITenantService tenantService)
    {
        _db = db;
        _tenantService = tenantService;
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

        return new PaymentResponseDto
        {
            Id             = payment.Id,
            SubscriptionId = payment.SubscriptionId,
            Amount         = payment.Amount,
            Method         = payment.Method,
            PaymentDate    = payment.PaymentDate
        };
    }
}
