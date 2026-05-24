using System.Security.Claims;
using Kindergarten.Core.Entities;
using Kindergarten.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kindergarten.Api.Controllers;

[ApiController]
[Route("api/role-groups")]
[Authorize(Roles = "Admin")]
public class RoleGroupsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public RoleGroupsController(ApplicationDbContext db) => _db = db;

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private int GetTenantId() =>
        int.TryParse(User.FindFirstValue("TenantId"), out var t) ? t : 1;

    // Get all role groups for tenant
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var groups = await _db.RoleGroups
            .Include(g => g.RoleGroupPermissions)
                .ThenInclude(rp => rp.Permission)
            .Include(g => g.UserRoleGroups)
            .Where(g => g.TenantId == GetTenantId())
            .Select(g => new
            {
                g.Id, g.NameAr, g.NameEn, g.Description, g.IsActive, g.CreatedAt,
                permissionsCount = g.RoleGroupPermissions.Count,
                usersCount       = g.UserRoleGroups.Count,
                permissions      = g.RoleGroupPermissions.Select(rp => new
                {
                    rp.Permission.Id,
                    rp.Permission.Name,
                    rp.Permission.DisplayNameAr,
                    rp.Permission.DisplayNameEn,
                    rp.Permission.Category
                })
            })
            .ToListAsync();

        return Ok(groups);
    }

    // Get single role group
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var group = await _db.RoleGroups
            .Include(g => g.RoleGroupPermissions)
                .ThenInclude(rp => rp.Permission)
            .Include(g => g.UserRoleGroups)
            .FirstOrDefaultAsync(g => g.Id == id && g.TenantId == GetTenantId());

        if (group == null) return NotFound();
        return Ok(group);
    }

    // Create role group
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoleGroupDto dto)
    {
        var group = new RoleGroup
        {
            TenantId    = GetTenantId(),
            NameAr      = dto.NameAr,
            NameEn      = dto.NameEn,
            Description = dto.Description,
            IsActive    = true,
            CreatedAt   = DateTime.UtcNow
        };

        _db.RoleGroups.Add(group);
        await _db.SaveChangesAsync();

        // Assign permissions
        if (dto.PermissionIds?.Any() == true)
        {
            var perms = dto.PermissionIds.Select(pid => new RoleGroupPermission
            {
                RoleGroupId  = group.Id,
                PermissionId = pid
            });
            _db.RoleGroupPermissions.AddRange(perms);
            await _db.SaveChangesAsync();
        }

        return Ok(new { group.Id, group.NameAr, group.NameEn, message = "Role group created" });
    }

    // Update role group
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateRoleGroupDto dto)
    {
        var group = await _db.RoleGroups
            .Include(g => g.RoleGroupPermissions)
            .FirstOrDefaultAsync(g => g.Id == id && g.TenantId == GetTenantId());

        if (group == null) return NotFound();

        group.NameAr      = dto.NameAr;
        group.NameEn      = dto.NameEn;
        group.Description = dto.Description;

        // Update permissions
        _db.RoleGroupPermissions.RemoveRange(group.RoleGroupPermissions);
        if (dto.PermissionIds?.Any() == true)
        {
            var perms = dto.PermissionIds.Select(pid => new RoleGroupPermission
            {
                RoleGroupId  = group.Id,
                PermissionId = pid
            });
            _db.RoleGroupPermissions.AddRange(perms);
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Role group updated" });
    }

    // Delete role group
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var group = await _db.RoleGroups
            .FirstOrDefaultAsync(g => g.Id == id && g.TenantId == GetTenantId());

        if (group == null) return NotFound();

        _db.RoleGroups.Remove(group);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Role group deleted" });
    }

    // Assign user to role group
    [HttpPost("{id}/assign/{userId}")]
    public async Task<IActionResult> AssignUser(int id, string userId)
    {
        var existing = await _db.UserRoleGroups
            .FirstOrDefaultAsync(urg => urg.UserId == userId && urg.RoleGroupId == id);

        if (existing != null)
            return Ok(new { message = "Already assigned" });

        _db.UserRoleGroups.Add(new UserRoleGroup
        {
            UserId      = userId,
            RoleGroupId = id,
            AssignedBy  = GetUserId(),
            AssignedAt  = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return Ok(new { message = "User assigned to role group" });
    }

    // Remove user from role group
    [HttpDelete("{id}/unassign/{userId}")]
    public async Task<IActionResult> UnassignUser(int id, string userId)
    {
        var existing = await _db.UserRoleGroups
            .FirstOrDefaultAsync(urg => urg.UserId == userId && urg.RoleGroupId == id);

        if (existing == null) return NotFound();

        _db.UserRoleGroups.Remove(existing);
        await _db.SaveChangesAsync();
        return Ok(new { message = "User removed from role group" });
    }

    // Get user's role groups
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserGroups(string userId)
    {
        var groups = await _db.UserRoleGroups
            .Include(urg => urg.RoleGroup)
                .ThenInclude(g => g.RoleGroupPermissions)
                    .ThenInclude(rp => rp.Permission)
            .Where(urg => urg.UserId == userId)
            .Select(urg => new
            {
                urg.RoleGroup.Id,
                urg.RoleGroup.NameAr,
                urg.RoleGroup.NameEn,
                urg.AssignedAt,
                permissions = urg.RoleGroup.RoleGroupPermissions.Select(rp => rp.Permission.Name)
            })
            .ToListAsync();

        return Ok(groups);
    }
}

public class CreateRoleGroupDto
{
    public string  NameAr      { get; set; } = string.Empty;
    public string  NameEn      { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<int>? PermissionIds { get; set; }
}
