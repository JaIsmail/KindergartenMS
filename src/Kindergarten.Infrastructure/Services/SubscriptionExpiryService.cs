using System.Text.Json;
using Kindergarten.Core.Interfaces;
using Kindergarten.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kindergarten.Infrastructure.Services;

public class SubscriptionExpiryService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<SubscriptionExpiryService> _logger;

    public SubscriptionExpiryService(
        IServiceProvider services,
        ILogger<SubscriptionExpiryService> logger)
    {
        _services = services;
        _logger   = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await CheckExpiringSubscriptions();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking subscriptions");
            }
            await Task.Delay(TimeSpan.FromHours(24), ct);
        }
    }

    private async Task CheckExpiringSubscriptions()
    {
        using var scope = _services.CreateScope();
        var db     = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notify = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var today  = DateTime.UtcNow.Date;

        var tenants = await db.Tenants.Where(t => t.IsActive).ToListAsync();

        foreach (var tenant in tenants)
        {
            var alertDays = new List<int> { 3, 7 };
            if (!string.IsNullOrEmpty(tenant.Settings))
            {
                try
                {
                    var settings = JsonSerializer.Deserialize<JsonElement>(tenant.Settings);
                    if (settings.TryGetProperty("alertDays", out var days))
                        alertDays = days.EnumerateArray().Select(d => d.GetInt32()).ToList();
                }
                catch { }
            }

            var datesToCheck = alertDays.Select(d => today.AddDays(d)).ToList();
            datesToCheck.Add(today);

            var expiring = await db.Subscriptions
                .IgnoreQueryFilters()
                .Include(s => s.Child)
                .Include(s => s.Parent)
                .Where(s => s.TenantId == tenant.Id && datesToCheck.Contains(s.EndDate.Date))
                .ToListAsync();

            foreach (var sub in expiring)
            {
                var daysLeft = (sub.EndDate.Date - today).Days;
                string titleAr, titleEn, bodyAr, bodyEn;

                if (daysLeft == 0)
                {
                    titleAr = "انتهى اشتراكك اليوم ⚠️";
                    titleEn = "Subscription Expired Today ⚠️";
                    bodyAr  = $"اشتراك {sub.Child.Name} انتهى اليوم. يرجى التجديد.";
                    bodyEn  = $"{sub.Child.Name}'s subscription expired today. Please renew.";
                }
                else
                {
                    titleAr = $"اشتراكك ينتهي خلال {daysLeft} أيام ⏰";
                    titleEn = $"Subscription expires in {daysLeft} days ⏰";
                    bodyAr  = $"اشتراك {sub.Child.Name} ينتهي بتاريخ {sub.EndDate:dd/MM/yyyy}";
                    bodyEn  = $"{sub.Child.Name}'s subscription expires on {sub.EndDate:dd/MM/yyyy}";
                }

                await notify.SendToParentAsync(
                    sub.ParentId,
                    titleAr, titleEn,
                    bodyAr, bodyEn,
                    new Dictionary<string, string>
                    {
                        ["type"]           = "subscription_expiry",
                        ["subscriptionId"] = sub.Id.ToString(),
                        ["daysLeft"]       = daysLeft.ToString()
                    }
                );

                _logger.LogInformation(
                    "Expiry notification sent for subscription {Id}, days left: {Days}",
                    sub.Id, daysLeft);
            }

            _logger.LogInformation("Checked {Count} expiring subscriptions for tenant {TenantId}",
                expiring.Count, tenant.Id);
        }
    }
}
