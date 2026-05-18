using Kindergarten.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kindergarten.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    // GET all users
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userManager.Users
            .OrderByDescending(u => u.Id)
            .Select(u => new {
                u.Id, u.FullName, u.Email,
                u.PhoneNumber, u.RoleType, u.Address
            })
            .ToListAsync();
        return Ok(users);
    }

    // GET single user
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();
        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new {
            user.Id, user.FullName, user.Email,
            user.PhoneNumber, user.RoleType,
            user.Address, Role = roles.FirstOrDefault()
        });
    }

    // PUT update user
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateUserDto dto)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        user.FullName    = dto.FullName    ?? user.FullName;
        user.PhoneNumber = dto.PhoneNumber ?? user.PhoneNumber;
        user.Address     = dto.Address     ?? user.Address;

        // Update role if changed
        if (!string.IsNullOrEmpty(dto.RoleType) && dto.RoleType != user.RoleType)
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, dto.RoleType);
            user.RoleType = dto.RoleType;
        }

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
