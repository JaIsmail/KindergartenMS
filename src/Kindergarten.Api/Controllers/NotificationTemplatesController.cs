using Kindergarten.Api.Authorization;
using Kindergarten.Core.Entities;
using Kindergarten.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kindergarten.Api.Controllers;

[ApiController]
[Route("api/notification-templates")]
[Authorize]
public class NotificationTemplatesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public NotificationTemplatesController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    [RequirePermission("Notifications.Send")]
    public async Task<IActionResult> GetAll()
    {
        var templates = await _db.NotificationTemplates.IgnoreQueryFilters()
            .Where(t => t.TenantId == GetTenantId())
            .ToListAsync();
        return Ok(templates);
    }

    [HttpGet("registry")]
    [RequirePermission("Notifications.Send")]
    public async Task<IActionResult> GetRegistry()
    {
        var tenantId = GetTenantId();
        var customKeys = await _db.NotificationTemplates.IgnoreQueryFilters()
            .Where(t => t.TenantId == tenantId && t.IsActive)
            .Select(t => t.Key)
            .ToListAsync();

 var result = NotificationRegistry.All.Select(k => new
        {
            key            = k.Key,
            category       = k.Category,
            descriptionAr  = k.DescriptionAr,
            descriptionEn  = k.DescriptionEn,
            placeholders   = k.Placeholders,
            defaultTitleAr = k.DefaultTitleAr,
            defaultTitleEn = k.DefaultTitleEn,
            defaultBodyAr  = k.DefaultBodyAr,
            defaultBodyEn  = k.DefaultBodyEn,
            status         = k.Status.ToString(),
            hasCustomTemplate = customKeys.Contains(k.Key)
        });

   return Ok(result);
    }


 [HttpGet("{key}")]
    [RequirePermission("Notifications.Send")]
    public async Task<IActionResult> GetByKey(string key)
    {
        var template = await _db.NotificationTemplates.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Key == key && t.TenantId == GetTenantId());
        if (template == null) return NotFound();
        return Ok(template);
    }

    [HttpPut("{key}")]
    [RequirePermission("Notifications.Send")]
    public async Task<IActionResult> Upsert(string key, [FromBody] NotificationTemplate dto)
    {
        var tenantId = GetTenantId();
        var existing = await _db.NotificationTemplates.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Key == key && t.TenantId == tenantId);

        if (existing == null)
        {
            existing = new NotificationTemplate { Key = key, TenantId = tenantId };
            _db.NotificationTemplates.Add(existing);
        }

        existing.TitleAr = dto.TitleAr;
        existing.TitleEn = dto.TitleEn;
        existing.BodyAr  = dto.BodyAr;
        existing.BodyEn  = dto.BodyEn;
        existing.IsActive = dto.IsActive;

 await _db.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{key}")]
    [RequirePermission("Notifications.Send")]
    public async Task<IActionResult> ResetToDefault(string key)
    {
        var tenantId = GetTenantId();
        var existing = await _db.NotificationTemplates.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Key == key && t.TenantId == tenantId);
        if (existing == null) return NotFound();

 _db.NotificationTemplates.Remove(existing);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Reset to default" });
    }

    private int GetTenantId() =>
        int.TryParse(User.FindFirst("TenantId")?.Value, out var id) ? id : 1;
}
