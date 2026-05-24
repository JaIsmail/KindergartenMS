using Kindergarten.Core.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Kindergarten.Core.Entities;
using Kindergarten.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Kindergarten.Api.Controllers;

[ApiController]
[Route("api/tenants")]
public class TenantsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    private readonly UserManager<ApplicationUser> _userManager;

    public TenantsController(ApplicationDbContext db, IConfiguration config, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _config = config;
        _userManager = userManager;
    }

    // SuperAdmin only
    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetAll()
    {
        var isSuperAdmin = User.IsInRole("SuperAdmin");
        var tenantId = int.TryParse(User.FindFirstValue("TenantId"), out var tid) ? tid : 0;
        var tenantsQuery = isSuperAdmin
            ? _db.Tenants
            : _db.Tenants.Where(t => t.Id == tenantId);
        var tenants = await tenantsQuery.IgnoreQueryFilters()
            .Select(t => new {
                t.Id, t.NameAr, t.NameEn, t.City, t.Phone,
                t.Email, t.Plan, t.IsActive, t.CreatedAt,
                usersCount    = _db.Users.Count(u => u.TenantId == t.Id),
                childrenCount = _db.Children.Count(c => c.TenantId == t.Id),
                tripsCount    = _db.Trips.Count(tr => tr.TenantId == t.Id),
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
    [Authorize(Roles = "Admin,SuperAdmin")]
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


    // Create TenantAdmin for a specific tenant - SuperAdmin only
    [HttpPost("{id}/create-admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> CreateTenantAdmin(int id,
        [FromBody] CreateTenantAdminDto dto,
        [FromServices] Microsoft.AspNetCore.Identity.UserManager<Kindergarten.Core.Entities.ApplicationUser> userManager,
        [FromServices] Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole> roleManager)
    {
        var tenant = await _db.Tenants.FindAsync(id);
        if (tenant == null) return NotFound("Tenant not found");

        // Check if email already exists
        var existing = await userManager.FindByEmailAsync(dto.Email);
        if (existing != null) return BadRequest("Email already exists");

        var user = new Kindergarten.Core.Entities.ApplicationUser
        {
            UserName       = dto.Email,
            Email          = dto.Email,
            FullName       = dto.FullName,
            RoleType       = "TenantAdmin",
            TenantId       = id,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        if (!await roleManager.RoleExistsAsync("TenantAdmin"))
            await roleManager.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole("TenantAdmin"));

        await userManager.AddToRoleAsync(user, "TenantAdmin");

        return Ok(new { 
            message = "TenantAdmin created successfully",
            userId  = user.Id,
            email   = user.Email,
            tenantId = id,
            tenantName = tenant.NameAr
        });
    }


    // SuperAdmin impersonates a tenant - generates token with tenant context
    [HttpPost("{id}/impersonate")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Impersonate(int id,
        [FromServices] IConfiguration config,
        [FromServices] Microsoft.AspNetCore.Identity.UserManager<Kindergarten.Core.Entities.ApplicationUser> userManager)
    {
        var tenant = await _db.Tenants.FindAsync(id);
        if (tenant == null) return NotFound("Tenant not found");

        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var currentUser = await userManager.FindByIdAsync(currentUserId ?? "");
        if (currentUser == null) return Unauthorized();

        // Generate impersonation token with tenant context
        var claims = new System.Collections.Generic.List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.NameIdentifier, currentUser.Id),
            new(System.Security.Claims.ClaimTypes.Email, currentUser.Email!),
            new(System.Security.Claims.ClaimTypes.Name, currentUser.FullName),
            new(System.Security.Claims.ClaimTypes.Role, "Admin"), // Act as Admin in this tenant
            new("TenantId", id.ToString()),
            new("ImpersonatingTenant", id.ToString()),
            new("ImpersonatingTenantName", tenant.NameAr),
            new("OriginalRole", currentUser.RoleType),
            new("jti", Guid.NewGuid().ToString())
        };

        var key    = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes((config["Jwt__Key"] ?? config["Jwt:Key"])!));
        var creds  = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
        var token  = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer:             config["Jwt__Issuer"] ?? config["Jwt:Issuer"],
            audience:           config["Jwt__Audience"] ?? config["Jwt:Audience"],
            claims:             claims,
            expires:            DateTime.UtcNow.AddHours(2),
            signingCredentials: creds
        );

        return Ok(new {
            token          = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token),
            tenantId       = id,
            tenantNameAr   = tenant.NameAr,
            tenantNameEn   = tenant.NameEn,
            isImpersonating = true
        });
    }

}
