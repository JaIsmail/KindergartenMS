using System.Security.Claims;
using Kindergarten.Core.Entities;
using Kindergarten.Core.Interfaces;
using Kindergarten.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kindergarten.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly IServiceProvider _services;
    private readonly IHttpContextAccessor _http;

    public AuditService(IServiceProvider services, IHttpContextAccessor http)
    {
        _services = services;
        _http     = http;
    }

    public async Task LogAsync(string action, string entityType, string entityId, string details = "")
    {
        try
        {
            var user      = _http.HttpContext?.User;
            var userId    = user?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var userEmail = user?.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            var userName  = user?.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
            var tenantId  = int.TryParse(user?.FindFirstValue("TenantId"), out var tid) ? tid : 0;
            var ip        = _http.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

            // Use a new scope to avoid DbContext conflicts
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            db.AuditLogs.Add(new AuditLog
            {
                TenantId   = tenantId,
                UserId     = userId,
                UserEmail  = userEmail,
                UserName   = userName,
                Action     = action,
                EntityType = entityType,
                EntityId   = entityId,
                Details    = details,
                IpAddress  = ip,
                CreatedAt  = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Audit log error: {ex.Message}");
        }
    }
}
