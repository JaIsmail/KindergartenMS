using System.Security.Claims;
using Kindergarten.Core.Entities;
using Kindergarten.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kindergarten.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DevicesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public DevicesController(ApplicationDbContext db) => _db = db;

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    // Register or update device token
    [HttpPost("register")]
    public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceDto dto)
    {
        var userId = GetUserId();
        var existing = await _db.UserDevices
            .FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceToken == dto.DeviceToken);

        if (existing != null)
        {
            existing.UpdatedAt = DateTime.UtcNow;
            existing.Platform  = dto.Platform;
        }
        else
        {
            _db.UserDevices.Add(new UserDevice
            {
                UserId      = userId,
                DeviceToken = dto.DeviceToken,
                Platform    = dto.Platform,
                UpdatedAt   = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Device registered successfully" });
    }
}

public class RegisterDeviceDto
{
    public string DeviceToken { get; set; } = string.Empty;
    public string Platform    { get; set; } = "Android";
}
