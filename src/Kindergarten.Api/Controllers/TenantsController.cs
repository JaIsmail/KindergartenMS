using Kindergarten.Core.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Kindergarten.Core.Entities;
using Kindergarten.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
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

    public TenantsController(ApplicationDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    private async Task SeedDefaultGroupsForTenantAsync(int tenantId)
    {
        var allPerms = await _db.Permissions.ToListAsync();
        var p = allPerms.ToDictionary(x => x.Name, x => x.Id);

        var groups = new List<(Core.Entities.PermissionGroup Group, List<string> Perms)>
        {
            (new() { NameAr="مدير النظام", NameEn="Admin",      Description="مدير الروضة",   TenantId=tenantId },
             allPerms.Select(x => x.Name).ToList()),
            (new() { NameAr="سائق",        NameEn="Driver",     Description="سائق الحافلة",  TenantId=tenantId },
             new List<string> { "ManageTrips","TrackTrips","ViewChildren","SubmitLeaveRequest","ViewOwnAttendance" }),
            (new() { NameAr="ولي أمر",     NameEn="Parent",     Description="ولي أمر الطفل", TenantId=tenantId },
             new List<string> { "ViewChildren","ViewSubscriptions","TrackTrips" }),
            (new() { NameAr="معلم",        NameEn="Teacher",    Description="معلم الروضة",   TenantId=tenantId },
             new List<string> { "ManageAttendance","ViewOwnAttendance","SubmitLeaveRequest","ViewChildren" }),
            (new() { NameAr="محاسب",       NameEn="Accountant", Description="المحاسب",       TenantId=tenantId },
             new List<string> { "ViewFinancials","ManagePayments","ViewSubscriptions","ManageSubscriptions","ViewReports" }),
            (new() { NameAr="مشرف",        NameEn="Supervisor", Description="المشرف",        TenantId=tenantId },
             new List<string> { "ViewUsers","ViewChildren","ManageAttendance","ViewOwnAttendance","ManageLeaveRequests","ViewReports" }),
        };

        foreach (var (group, permNames) in groups)
        {
            _db.PermissionGroups.Add(group);
            await _db.SaveChangesAsync();
            var permIds = permNames
                .Where(n => p.ContainsKey(n))
                .Select(n => new Core.Entities.PermissionGroupPermission { GroupId = group.Id, PermissionId = p[n] });
            _db.PermissionGroupPermissions.AddRange(permIds);
        }
        await _db.SaveChangesAsync();
    }

    private bool IsSuperAdmin() =>
        int.TryParse(User.FindFirstValue("TenantId"), out var tid) && tid == 0;

    private IActionResult ForbidIfNotSuperAdmin() =>
        IsSuperAdmin() ? null! : Forbid();

    // SuperAdmin only
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll()
    {
        var tenantId = int.TryParse(User.FindFirstValue("TenantId"), out var tid) ? tid : 0;
        var isSuperAdmin = tenantId == 0;
        var tenantsQuery = isSuperAdmin
            ? _db.Tenants
            : _db.Tenants.Where(t => t.Id == tenantId);
        var tenants = await tenantsQuery.IgnoreQueryFilters()
            .Select(t => new {
                t.Id, t.NameAr, t.NameEn, t.City, t.Phone,
                t.Email, t.Plan, t.IsActive, t.CreatedAt, t.Settings,
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
    [Authorize]
    public async Task<IActionResult> Create([FromBody] Tenant dto)
    {
        if (!IsSuperAdmin()) return Forbid();
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

        // Auto-seed default permission groups for new tenant
        await SeedDefaultGroupsForTenantAsync(tenant.Id);

        return Ok(tenant);
    }

    [HttpPut("{id}")]
    [Authorize]
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
        tenant.Settings = dto.Settings;
        await _db.SaveChangesAsync();
        return Ok(tenant);
    }

    [HttpPut("{id}/toggle")]
    [Authorize]
    public async Task<IActionResult> Toggle(int id)
    {
        var tenant = await _db.Tenants.FindAsync(id);
        if (tenant == null) return NotFound();
        tenant.IsActive = !tenant.IsActive;
        await _db.SaveChangesAsync();
        return Ok(new { tenant.Id, tenant.IsActive });
    }
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        if (!IsSuperAdmin()) return Forbid();
        var tenant = await _db.Tenants.FindAsync(id);
        if (tenant == null) return NotFound();
        _db.Tenants.Remove(tenant);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Tenant deleted" });
    }

 // Seed default tenant
    [HttpPost("seed")]
    [Authorize]
    public async Task<IActionResult> Seed()
    {
        if (!IsSuperAdmin()) return Forbid();
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
    [Authorize]
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
    [Authorize]
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
    [Authorize]
    public async Task<IActionResult> CreateTenantAdmin(int id,
        [FromBody] CreateTenantAdminDto dto)
    {
        var tenant = await _db.Tenants.FindAsync(id);
        if (tenant == null) return NotFound("Tenant not found");

        // Check if email already exists
        var existing = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (existing != null) return BadRequest("Email already exists");

        var user = new Kindergarten.Core.Entities.ApplicationUser
        {
            UserName       = dto.Email,
            Email          = dto.Email,
            FullName       = dto.FullName,
            RoleType       = "TenantAdmin",
            TenantId       = id
        };

        var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<Kindergarten.Core.Entities.ApplicationUser>();
        user.PasswordHash = hasher.HashPassword(user, dto.Password);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

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
    [Authorize]
    public async Task<IActionResult> Impersonate(int id,
        [FromServices] IConfiguration config)
    {
        if (!IsSuperAdmin()) return Forbid();
        var tenant = await _db.Tenants.FindAsync(id);
        if (tenant == null) return NotFound("Tenant not found");

        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var currentUser = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == (currentUserId ?? ""));
        if (currentUser == null) return Unauthorized();

        // Generate impersonation token with tenant context
        // Get all permissions to include in impersonation token
        var allPerms = await _db.Permissions.Select(p => p.Name).ToListAsync();

        var claims = new System.Collections.Generic.List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.NameIdentifier, currentUser.Id),
            new(System.Security.Claims.ClaimTypes.Email, currentUser.Email!),
            new(System.Security.Claims.ClaimTypes.Name, currentUser.FullName),
                        new("TenantId", id.ToString()),
            new("ImpersonatingTenant", id.ToString()),
            new("ImpersonatingTenantName", tenant.NameAr),
            new("OriginalRole", currentUser.RoleType),
            new("jti", Guid.NewGuid().ToString())
        };

        // Add all permissions as claims for full tenant access
        claims.AddRange(allPerms.Select(p => new System.Security.Claims.Claim("Permission", p)));

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

    // !! DANGER — SuperAdmin only — resets all data except SuperAdmin user
    [HttpPost("reset-platform")]
    public async Task<IActionResult> ResetPlatform()
    {
        try
        {
            var tenantId = int.TryParse(User.FindFirstValue("TenantId"), out var tid) ? tid : -1;
            if (tenantId != 0)
                return Forbid();

            var saId = await _db.Users
                .IgnoreQueryFilters()
                .Where(u => u.Email == "superadmin@kms-platform.com")
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            if (saId == null)
                return BadRequest(new { error = "SuperAdmin not found" });

            var deleted = new List<string>();
            var errors  = new List<string>();

            // Clear tables
            var tables = new[] {
                "UserPermissionGroups","UserPermissions",
                "PermissionGroupPermissions","PermissionGroups","Permissions",
                "Payments","Subscriptions",
                "TripChildren","TripLocations","Trips",
                "AttendancePeriods","Attendance",
                "LeaveRequests","UserDevices","Children","Employees",
                "DynamicLists",
                "AspNetUserRoles","AspNetUserClaims",
                "AspNetUserLogins","AspNetUserTokens",
                "AspNetRoleClaims","AspNetRoles"
            };
            foreach (var t in tables)
            {
                try {
                    await _db.Database.ExecuteSqlRawAsync($"DELETE FROM [{t}]");
                    deleted.Add(t);
                } catch (Exception ex) { errors.Add($"{t}: {ex.Message.Split('\n')[0]}"); }
            }

            // Delete non-SuperAdmin users
            try {
                await _db.Database.ExecuteSqlRawAsync("DELETE FROM [AspNetUsers] WHERE [Id] <> {0}", saId);
                deleted.Add("AspNetUsers (non-SA)");
            } catch (Exception ex) { errors.Add($"AspNetUsers: {ex.Message.Split('\n')[0]}"); }

            // Delete tenants
            try {
                await _db.Database.ExecuteSqlRawAsync("DELETE FROM [Tenants]");
                deleted.Add("Tenants");
            } catch (Exception ex) { errors.Add($"Tenants: {ex.Message.Split('\n')[0]}"); }

            return Ok(new { message = "Platform reset completed", keptUser = saId, deleted, errors });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace?.Split('\n')[0] });
        }
    }

}
