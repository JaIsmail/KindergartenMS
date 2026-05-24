using Kindergarten.Core.Entities;
using Kindergarten.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kindergarten.Api.Controllers;

[ApiController]
[Route("api/tenants")]
public class TenantsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public TenantsController(ApplicationDbContext db) => _db = db;

    // SuperAdmin only
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        var tenants = await _db.Tenants
            .Select(t => new {
                t.Id, t.NameAr, t.NameEn, t.City, t.Phone,
                t.Email, t.Plan, t.IsActive, t.CreatedAt,
                usersCount    = _db.Users.Count(u => u.TenantId == t.Id),
                childrenCount = _db.Children.Count(c => c.TenantId == t.Id),
                totalRevenue  = _db.Payments
                    .Where(p => _db.Subscriptions
                        .Where(s => s.TenantId == t.Id)
                        .Select(s => s.Id)
                        .Contains(p.SubscriptionId))
                    .Sum(p => (decimal?)p.Amount) ?? 0
            })
            .ToListAsync();
        return Ok(tenants);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetById(int id)
    {
        var tenant = await _db.Tenants.FindAsync(id);
        if (tenant == null) return NotFound();
        return Ok(tenant);
    }
 [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] Tenant dto)
    {
        var tenant = new Tenant
        {
            NameAr    = dto.NameAr,
            NameEn    = dto.NameEn,
            City      = dto.City,
            Phone     = dto.Phone,
            Email     = dto.Email,
            Plan      = dto.Plan ?? "Basic",
            IsActive  = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync();
        return Ok(tenant);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] Tenant dto)
    {
        var tenant = await _db.Tenants.FindAsync(id);
        if (tenant == null) return NotFound();
        tenant.NameAr   = dto.NameAr;
        tenant.NameEn   = dto.NameEn;
        tenant.City     = dto.City;
        tenant.Phone    = dto.Phone;
        tenant.Email    = dto.Email;
        tenant.Plan     = dto.Plan;
        tenant.IsActive = dto.IsActive;
        await _db.SaveChangesAsync();
        return Ok(tenant);
    }

    [HttpPut("{id}/toggle")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Toggle(int id)
    {
        var tenant = await _db.Tenants.FindAsync(id);
        if (tenant == null) return NotFound();
        tenant.IsActive = !tenant.IsActive;
        await _db.SaveChangesAsync();
        return Ok(new { tenant.Id, tenant.IsActive });
    }
 // Seed default tenant
    [HttpPost("seed")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Seed()
    {
        if (await _db.Tenants.AnyAsync())
            return Ok(new { message = "Already seeded" });

        var tenant = new Tenant
        {
            NameAr    = "الروضة الافتراضية",
            NameEn    = "Default Kindergarten",
            City      = "الرياض",
            Plan      = "Pro",
            IsActive  = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync();

        // Update all existing data to belong to this tenant
        await _db.Database.ExecuteSqlRawAsync($"UPDATE AspNetUsers SET TenantId = {tenant.Id}");
        await _db.Database.ExecuteSqlRawAsync($"UPDATE Children SET TenantId = {tenant.Id}");
        await _db.Database.ExecuteSqlRawAsync($"UPDATE Trips SET TenantId = {tenant.Id}");
        await _db.Database.ExecuteSqlRawAsync($"UPDATE Employees SET TenantId = {tenant.Id}");
        await _db.Database.ExecuteSqlRawAsync($"UPDATE Subscriptions SET TenantId = {tenant.Id}");

        return Ok(new { message = "Default tenant created", tenantId = tenant.Id });
    }

    // Fix all data with TenantId = 0
    [HttpPost("fix-tenant-data")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> FixTenantData()
    {
        await _db.Database.ExecuteSqlRawAsync("UPDATE LeaveRequests SET TenantId = 1 WHERE TenantId = 0");
        await _db.Database.ExecuteSqlRawAsync("UPDATE AspNetUsers SET TenantId = 1 WHERE TenantId = 0");
        await _db.Database.ExecuteSqlRawAsync("UPDATE Children SET TenantId = 1 WHERE TenantId = 0");
        await _db.Database.ExecuteSqlRawAsync("UPDATE Trips SET TenantId = 1 WHERE TenantId = 0");
        await _db.Database.ExecuteSqlRawAsync("UPDATE Employees SET TenantId = 1 WHERE TenantId = 0");
        await _db.Database.ExecuteSqlRawAsync("UPDATE Subscriptions SET TenantId = 1 WHERE TenantId = 0");
        return Ok(new { message = "All TenantId=0 data fixed to TenantId=1" });
    }


    // Platform-wide stats for SuperAdmin
    [HttpGet("platform-stats")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetPlatformStats()
    {
        var tenants = await _db.Tenants.ToListAsync();
        var stats = new
        {
            totalTenants    = tenants.Count,
            activeTenants   = tenants.Count(t => t.IsActive),
            inactiveTenants = tenants.Count(t => !t.IsActive),
            totalUsers      = await _db.Users.CountAsync(),
            totalChildren   = await _db.Children.CountAsync(),
            totalTrips      = await _db.Trips.CountAsync(),
            totalRevenue    = await _db.Payments.SumAsync(p => (decimal?)p.Amount) ?? 0,
            tenants         = tenants.Select(t => new {
                t.Id, t.NameAr, t.NameEn, t.City, t.Plan, t.IsActive, t.CreatedAt,
                usersCount    = _db.Users.Count(u => u.TenantId == t.Id),
                childrenCount = _db.Children.Count(c => c.TenantId == t.Id),
                tripsCount    = _db.Trips.Count(tr => tr.TenantId == t.Id),
                revenue       = _db.Payments
                    .Where(p => _db.Subscriptions
                        .Where(s => s.TenantId == t.Id)
                        .Select(s => s.Id)
                        .Contains(p.SubscriptionId))
                    .Sum(p => (decimal?)p.Amount) ?? 0
            })
        };
        return Ok(stats);
    }

}
