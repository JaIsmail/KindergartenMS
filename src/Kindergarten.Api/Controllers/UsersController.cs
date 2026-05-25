using System.Security.Claims;
using Kindergarten.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Kindergarten.Api.Authorization;
using Kindergarten.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kindergarten.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;

    public UsersController(UserManager<ApplicationUser> userManager, ApplicationDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    private int GetTenantId() =>
        int.TryParse(User.FindFirstValue("TenantId"), out var id) ? id : 1;
    private bool IsSuperAdmin() => User.IsInRole("SuperAdmin");

    // GET all users
    [HttpGet]
    [RequirePermission("ViewUsers")]
    public async Task<IActionResult> GetAll()
    {
        var query = IsSuperAdmin()
            ? _userManager.Users
            : _userManager.Users.Where(u => u.TenantId == GetTenantId());

        var users = await query
            .OrderByDescending(u => u.Id)
            .Select(u => new {
                u.Id, u.FullName, u.Email,
                u.PhoneNumber, u.RoleType, u.Address, u.TenantId,
                GroupName = _db.UserPermissionGroups
                    .IgnoreQueryFilters()
                    .Where(g => g.UserId == u.Id)
                    .Select(g => g.Group.NameEn)
                    .FirstOrDefault()
            })
            .ToListAsync();
        return Ok(users);
    }

    // GET single user
    [HttpGet("{id}")]
    [RequirePermission("ViewUsers")]
    public async Task<IActionResult> GetById(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();
        return Ok(new {
            user.Id, user.FullName, user.Email,
            user.PhoneNumber, user.RoleType,
            user.Address, Role = user.RoleType
        });
    }

    // PUT update user
    [HttpPut("{id}")]
    [RequirePermission("ManageUsers")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateUserDto dto)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        user.FullName    = dto.FullName    ?? user.FullName;
        user.PhoneNumber = dto.PhoneNumber ?? user.PhoneNumber;
        user.Address     = dto.Address     ?? user.Address;

        // Update roleType directly — no Identity role assignment
        if (!string.IsNullOrEmpty(dto.RoleType) && dto.RoleType != user.RoleType)
            user.RoleType = dto.RoleType;

        // Update password if provided
        if (!string.IsNullOrEmpty(dto.NewPassword))
        {
            var token  = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);
            if (!result.Succeeded)
                return BadRequest(new { message = "Password update failed", errors = result.Errors });
        }

        await _userManager.UpdateAsync(user);
        return Ok(new { message = "User updated successfully", user.Id, user.FullName });
    }

    // DELETE user
    [HttpDelete("{id}")]
    [RequirePermission("ManageUsers")]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { message = "Failed to delete user" });

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
