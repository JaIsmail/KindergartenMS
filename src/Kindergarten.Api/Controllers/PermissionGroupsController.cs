using System.Security.Claims;
using Kindergarten.Core.Entities;
using Kindergarten.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kindergarten.Api.Controllers;

[ApiController]
[Route("api/permission-groups")]
[Authorize(Roles = "Admin")]
public class PermissionGroupsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public PermissionGroupsController(ApplicationDbContext db) => _db = db;

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private int GetTenantId() =>
        int.TryParse(User.FindFirstValue("TenantId"), out var id) ? id : 1;

    // Get all groups with their permissions
    [HttpGet]
 public async Task<IActionResult> GetAll()
    {
        var groups = await _db.PermissionGroups
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
        var group = await _db.PermissionGroups
            .Include(g => g.GroupPermissions)
                .ThenInclude(gp => gp.Permission)
            .FirstOrDefaultAsync(g => g.Id == id);
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
    public async Task<IActionResult> Update(int id, [FromBody] CreatePermissionGroupDto dto)
    {
        var group = await _db.PermissionGroups.FindAsync(id);
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
    public async Task<IActionResult> Delete(int id)
    {
        var group = await _db.PermissionGroups.FindAsync(id);
        if (group == null) return NotFound();
        _db.PermissionGroups.Remove(group);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Group deleted" });
    }

    // Assign group to user
    [HttpPost("{id}/assign/{userId}")]
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

        await _db.SaveChangesAsync();
        return Ok(new { message = "Group assigned to user" });
    }

    // Remove group from user
    [HttpDelete("{id}/unassign/{userId}")]
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


public class CreatePermissionGroupDto
{
    public string       NameAr        { get; set; } = string.Empty;
    public string       NameEn        { get; set; } = string.Empty;
    public string?      Description   { get; set; }
    public List<int>?   PermissionIds { get; set; }
}
