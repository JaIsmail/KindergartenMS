using System.Security.Claims;
using Kindergarten.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Kindergarten.Api.Authorization;
using Kindergarten.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kindergarten.Api.Controllers;

[ApiController]
[Route("api/permission-groups")]
[Authorize]
public class PermissionGroupsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public PermissionGroupsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private int GetTenantId() =>
        int.TryParse(User.FindFirstValue("TenantId"), out var id) ? id : 1;

    // Get all groups with their permissions
    [HttpGet]
 public async Task<IActionResult> GetAll()
    {
        var isSuperAdmin = User.FindFirstValue("TenantId") == "0";
        var query = isSuperAdmin
            ? _db.PermissionGroups.IgnoreQueryFilters()
            : _db.PermissionGroups;
        var groups = await query
            .Include(g => g.GroupPermissions)
                .ThenInclude(gp => gp.Permission)
            .OrderBy(g => g.NameAr)
            .Select(g => new {
                g.Id, g.NameAr, g.NameEn, g.Description, g.IsActive, g.CreatedAt,
                permissions = g.GroupPermissions.Select(gp => new {
                    gp.Permission.Id,
                    gp.Permission.Name,
                    gp.Permission.DisplayNameAr,
                    gp.Permission.DisplayNameEn,
                    gp.Permission.Category
                })
            })
            .ToListAsync();
        return Ok(groups);
    }

    // Get single group
    [HttpGet("{id}")]
public async Task<IActionResult> GetById(int id)
    {
        var isSuperAdmin = User.FindFirstValue("TenantId") == "0";
        var group = await _db.PermissionGroups
            .IgnoreQueryFilters()
            .Include(g => g.GroupPermissions)
                .ThenInclude(gp => gp.Permission)
            .FirstOrDefaultAsync(g => g.Id == id && (isSuperAdmin || g.TenantId == GetTenantId()));
        if (group == null) return NotFound();
        return Ok(new {
            group.Id, group.NameAr, group.NameEn, group.Description, group.IsActive,
            permissions = group.GroupPermissions.Select(gp => new {
                gp.Permission.Id,
                gp.Permission.Name,
                gp.Permission.DisplayNameAr,
                gp.Permission.DisplayNameEn,
                gp.Permission.Category
            })
        });
    }

// Create group
    [HttpPost]
    [RequirePermission("ManageRoleGroups")]
    public async Task<IActionResult> Create([FromBody] CreatePermissionGroupDto dto)
    {
        var group = new PermissionGroup
        {
            NameAr      = dto.NameAr,
            NameEn      = dto.NameEn,
            Description = dto.Description ?? string.Empty,
            TenantId    = GetTenantId(),
            IsActive    = true,
            CreatedAt   = DateTime.UtcNow
        };
        _db.PermissionGroups.Add(group);
        await _db.SaveChangesAsync();

        // Assign permissions
        if (dto.PermissionIds?.Any() == true)
        {
            var perms = dto.PermissionIds.Select(pid => new PermissionGroupPermission
            {
                GroupId      = group.Id,
                PermissionId = pid
            });
            _db.PermissionGroupPermissions.AddRange(perms);
            await _db.SaveChangesAsync();
        }

        return Ok(new { group.Id, group.NameAr, group.NameEn, message = "Group created" });
    }

// Update group permissions
    [HttpPut("{id}")]
    [RequirePermission("ManageRoleGroups")]
    public async Task<IActionResult> Update(int id, [FromBody] CreatePermissionGroupDto dto)
    {
        var group = await _db.PermissionGroups.IgnoreQueryFilters().FirstOrDefaultAsync(g => g.Id == id);
        if (group == null) return NotFound();

        group.NameAr      = dto.NameAr;
        group.NameEn      = dto.NameEn;
        group.Description = dto.Description ?? string.Empty;

        // Replace permissions
        var existing = await _db.PermissionGroupPermissions
            .Where(x => x.GroupId == id).ToListAsync();
        _db.PermissionGroupPermissions.RemoveRange(existing);

        if (dto.PermissionIds?.Any() == true)
        {
            var perms = dto.PermissionIds.Select(pid => new PermissionGroupPermission
            {
                GroupId      = id,
                PermissionId = pid
            });
            _db.PermissionGroupPermissions.AddRange(perms);
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Group updated" });
    }
// Delete group
    [HttpDelete("{id}")]
    [RequirePermission("ManageRoleGroups")]
    public async Task<IActionResult> Delete(int id)
    {
        var group = await _db.PermissionGroups.IgnoreQueryFilters().FirstOrDefaultAsync(g => g.Id == id);
        if (group == null) return NotFound();
        _db.PermissionGroups.Remove(group);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Group deleted" });
    }

    // Assign group to user
    [HttpPost("{id}/assign/{userId}")]
    [RequirePermission("ManageRoleGroups")]
    public async Task<IActionResult> AssignToUser(int id, string userId)
    {
        // Check if already assigned
        var exists = await _db.UserPermissionGroups
            .AnyAsync(x => x.GroupId == id && x.UserId == userId);
        if (exists) return BadRequest(new { message = "Group already assigned to user" });

        var assignment = new UserPermissionGroup
        {
            GroupId    = id,
            UserId     = userId,
            TenantId   = GetTenantId(),
            AssignedAt = DateTime.UtcNow,
            AssignedBy = GetUserId()
        };
        _db.UserPermissionGroups.Add(assignment);


        // Also grant all group permissions to user
        var group = await _db.PermissionGroups
            .IgnoreQueryFilters()
            .Include(g => g.GroupPermissions)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group != null)
        {
            foreach (var gp in group.GroupPermissions)
            {
                var alreadyGranted = await _db.UserPermissions
                    .AnyAsync(up => up.UserId == userId && up.PermissionId == gp.PermissionId);
                if (!alreadyGranted)
                {
                    _db.UserPermissions.Add(new UserPermission
                    {
                        UserId       = userId,
                        PermissionId = gp.PermissionId,
                        GrantedBy    = GetUserId(),
                        GrantedAt    = DateTime.UtcNow,
                        TenantId     = GetTenantId()
                    });
                }
            }
        }

        // Set user's role from group's NameEn
        var assignUser = await _userManager.FindByIdAsync(userId);
        if (assignUser != null)
        {
            assignUser.RoleType = group.NameEn;
            await _userManager.UpdateAsync(assignUser);
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Group assigned to user" });
    }

    // Remove group from user
    [HttpDelete("{id}/unassign/{userId}")]
    [RequirePermission("ManageRoleGroups")]
    public async Task<IActionResult> UnassignFromUser(int id, string userId)
    {
        var assignment = await _db.UserPermissionGroups
            .FirstOrDefaultAsync(x => x.GroupId == id && x.UserId == userId);
        if (assignment == null) return NotFound();
        _db.UserPermissionGroups.Remove(assignment);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Group removed from user" });
    }

    // Get user's groups
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserGroups(string userId)
    {
        var groups = await _db.UserPermissionGroups
            .Include(x => x.Group)
                .ThenInclude(g => g.GroupPermissions)
                    .ThenInclude(gp => gp.Permission)
            .Where(x => x.UserId == userId)
            .Select(x => new {
                x.Group.Id,
                x.Group.NameAr,
                x.Group.NameEn,
                x.AssignedAt,
                permissions = x.Group.GroupPermissions.Select(gp => new {
                    gp.Permission.Id,
                    gp.Permission.Name,
                    gp.Permission.DisplayNameAr,
                    gp.Permission.DisplayNameEn
                })
            })
            .ToListAsync();
        return Ok(groups);
    }


    // Seed default permission groups
    [HttpPost("seed")]
    public async Task<IActionResult> Seed()
    {
        var tenantId = GetTenantId();

        // Only add missing groups — never delete existing ones
        var existing = await _db.PermissionGroups.IgnoreQueryFilters()
            .Where(g => g.TenantId == tenantId)
            .Select(g => g.NameEn)
            .ToListAsync();

        var defaults = new List<(string NameAr, string NameEn, string Desc)>
        {
            ("مدير النظام", "Admin",      "مدير الروضة"),
            ("سائق",        "Driver",     "سائق الحافلة"),
            ("ولي أمر",     "Parent",     "ولي أمر الطفل"),
            ("موظف",        "Employee",   "موظف الروضة"),
            ("محاسب",       "Accountant", "المحاسب"),
            ("مشرف",        "Supervisor", "المشرف"),
        };

        var toAdd = defaults
            .Where(d => !existing.Contains(d.NameEn))
            .Select(d => new PermissionGroup { NameAr=d.NameAr, NameEn=d.NameEn, Description=d.Desc, TenantId=tenantId })
            .ToList();

        if (toAdd.Any())
        {
            _db.PermissionGroups.AddRange(toAdd);
            await _db.SaveChangesAsync();
        }

        return Ok(new { message = "Seeded", added = toAdd.Count, existing = existing.Count });
    }

    // Internal seed helper — called when new tenant is created
    public async Task SeedDefaultGroupsAsync(int tenantId)
    {
        var allPerms = await _db.Permissions.ToListAsync();
        var p = allPerms.ToDictionary(x => x.Name, x => x.Id);

        var groups = new List<(PermissionGroup Group, List<string> Perms)>
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
                .Select(n => new PermissionGroupPermission { GroupId = group.Id, PermissionId = p[n] });
            _db.PermissionGroupPermissions.AddRange(permIds);
        }
        await _db.SaveChangesAsync();
    }
}

public class CreatePermissionGroupDto
{
    public string       NameAr        { get; set; } = string.Empty;
    public string       NameEn        { get; set; } = string.Empty;
    public string?      Description   { get; set; }
    public List<int>?   PermissionIds { get; set; }
}
