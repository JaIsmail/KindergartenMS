using System.Security.Claims;
using Kindergarten.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Kindergarten.Api.Authorization;
using Kindergarten.Core.Interfaces;
using Kindergarten.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Kindergarten.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kindergarten.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;
    private readonly IPasswordHasher<ApplicationUser> _hasher = new PasswordHasher<ApplicationUser>();

    public UsersController(ApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    private int GetTenantId() =>
        int.TryParse(User.FindFirstValue("TenantId"), out var id) ? id : 1;
    private bool IsSuperAdmin() => User.FindFirstValue("TenantId") == "0";

    // GET all users
    [HttpGet]
    [RequirePermission("Users.View")]
    public async Task<IActionResult> GetAll()
    {
        var query = IsSuperAdmin()
            ? _db.Users.IgnoreQueryFilters()
            : _db.Users.IgnoreQueryFilters().Where(u => u.TenantId == GetTenantId());

        var users = await query
            .OrderByDescending(u => u.Id)
            .Select(u => new {
                u.Id, u.FullName, u.Email,
                u.PhoneNumber, u.RoleType, u.Address, u.TenantId,
                GroupName = _db.UserPermissionGroups
                    .IgnoreQueryFilters()
                    .Where(g => g.UserId == u.Id)
                    .Select(g => g.Group.NameEn)
                    .FirstOrDefault(),
                GroupNameAr = _db.UserPermissionGroups
                    .IgnoreQueryFilters()
                    .Where(g => g.UserId == u.Id)
                    .Select(g => g.Group.NameAr)
                    .FirstOrDefault()
            })
            .ToListAsync();
        return Ok(users);
    }

    // GET single user
    [HttpGet("{id}")]
    [RequirePermission("Users.View")]
    public async Task<IActionResult> GetById(string id)
    {
        var user = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();
        return Ok(user);
    }

    // PUT update user
    [HttpPut("{id}")]
    [RequirePermission("Users.Edit")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateUserDto dto)
    {
        var user = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        user.FullName    = dto.FullName    ?? user.FullName;
        user.PhoneNumber = dto.PhoneNumber ?? user.PhoneNumber;
        user.Address     = dto.Address     ?? user.Address;
        user.RoleType    = dto.RoleType    ?? user.RoleType;

        if (!string.IsNullOrEmpty(dto.NewPassword))
            user.PasswordHash = _hasher.HashPassword(user, dto.NewPassword);

        await _db.SaveChangesAsync();
        
        await _audit.LogAsync("Update", "User", id.ToString(), "Updated user");
        return Ok(new { message = "User updated successfully", id = user.Id, user.FullName });
    }

    // DELETE user
    [HttpDelete("{id}")]
    [RequirePermission("Users.Delete")]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        
        await _audit.LogAsync("Delete", "User", id.ToString(), "Deleted user");
        return Ok(new { message = "User deleted successfully" });
    }
}

public class UpdateUserDto
{
    public string? FullName    { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address     { get; set; }
    public string? RoleType    { get; set; }
    public string? NewPassword { get; set; }
}
