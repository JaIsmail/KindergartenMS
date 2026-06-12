using Kindergarten.Api.Authorization;
using Kindergarten.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Kindergarten.Api.Controllers;

[ApiController]
[Route("api/audit")]
[Authorize]
public class AuditController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AuditController(ApplicationDbContext db) => _db = db;

    private bool IsSuperAdmin() => User.FindFirstValue("TenantId") == "0";
    private int GetTenantId() => int.TryParse(User.FindFirstValue("TenantId"), out var id) ? id : 1;

    [HttpGet]
    [RequirePermission("AuditLog.View")]
    public async Task<IActionResult> GetLogs(
        [FromQuery] string? entityType = null,
        [FromQuery] string? action = null,
        [FromQuery] string? userId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _db.AuditLogs.IgnoreQueryFilters().AsQueryable();

        if (!IsSuperAdmin())
            query = query.Where(l => l.TenantId == GetTenantId());

        if (!string.IsNullOrEmpty(entityType))
            query = query.Where(l => l.EntityType == entityType);
        if (!string.IsNullOrEmpty(action))
            query = query.Where(l => l.Action == action);
        if (!string.IsNullOrEmpty(userId))
            query = query.Where(l => l.UserId == userId);

        var total = await query.CountAsync();
        var logs  = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { total, page, pageSize, logs });
    }
}
