using System.Security.Claims;
using Kindergarten.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Kindergarten.Api.Authorization;
using Kindergarten.Core.Entities;
using Kindergarten.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kindergarten.Api.Controllers;

[ApiController]
[Route("api/permission-groups")]
[Authorize(Roles = "Admin,SuperAdmin")]
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
        var isSuperAdmin = User.IsInRole("SuperAdmin");
        var query = isSuperAdmin
            ? _db.PermissionGroups.IgnoreQueryFilters()
            : _db.PermissionGroups;
        var groups = await query
            .Include(g => g.GroupPermissions)
                .ThenInclude(gp => gp.Permission)
            .OrderBy(g => g.NameAr)
            .Select(g => new {
                g.Id, g.NameAr, g.NameEn, g.Description, g.RoleType, g.IsActive, g.CreatedAt,
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
        var isSuperAdmin = User.IsInRole("SuperAdmin");
        var group = await _db.PermissionGroups
            .IgnoreQueryFilters()
            .Include(g => g.GroupPermissions)
                .ThenInclude(gp => gp.Permission)
            .FirstOrDefaultAsync(g => g.Id == id && (isSuperAdmin || g.TenantId == GetTenantId()));
        if (group == null) return NotFound();
        return Ok(new {
            group.Id, group.NameAr, group.NameEn, group.Description, group.RoleType, group.IsActive,
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
            RoleType  = dto.RoleType ?? string.Empty,
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
        group.RoleType  = dto.RoleType ?? string.Empty;

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

        // Set user's system role from group's RoleType
        if (!string.IsNullOrEmpty(group.RoleType))
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!await _userManager.IsInRoleAsync(user, group.RoleType))
                    await _userManager.AddToRoleAsync(user, group.RoleType);
                user.RoleType = group.RoleType;
                await _userManager.UpdateAsync(user);
            }
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
}


    // Seed default permission groups
    [HttpPost("seed")]
    public async Task<IActionResult> Seed()
    {
        var tenantId = GetTenantId();

        // Delete existing groups for this tenant
        var existing = await _db.PermissionGroups.IgnoreQueryFilters()
            .Where(g => g.TenantId == tenantId).ToListAsync();
        _db.PermissionGroups.RemoveRange(existing);
        await _db.SaveChangesAsync();

        var groups = new List<PermissionGroup>
        {
            new() { NameAr="مدير النظام",    NameEn="Admin",       RoleType="Admin",       Description="مدير الروضة",         TenantId=tenantId },
            new() { NameAr="سائق",           NameEn="Driver",      RoleType="Driver",      Description="سائق الحافلة",        TenantId=tenantId },
            new() { NameAr="ولي أمر",        NameEn="Parent",      RoleType="Parent",      Description="ولي أمر الطفل",       TenantId=tenantId },
            new() { NameAr="موظف",           NameEn="Employee",    RoleType="Employee",    Description="موظف الروضة",         TenantId=tenantId },
            new() { NameAr="محاسب",          NameEn="Accountant",  RoleType="Accountant",  Description="المحاسب",             TenantId=tenantId },
            new() { NameAr="مشرف",           NameEn="Supervisor",  RoleType="Supervisor",  Description="المشرف",              TenantId=tenantId },
        };

        _db.PermissionGroups.AddRange(groups);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Seeded", count = groups.Count });
    }

public class CreatePermissionGroupDto
{
    public string       NameAr        { get; set; } = string.Empty;
    public string       NameEn        { get; set; } = string.Empty;
    public string?      Description   { get; set; }
    public string?      RoleType    { get; set; }
    public List<int>?   PermissionIds { get; set; }
}
