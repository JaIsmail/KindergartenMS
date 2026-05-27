using System.Security.Claims;
using Kindergarten.Api.Authorization;
using Kindergarten.Core.Entities;
using Kindergarten.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kindergarten.Api.Controllers;

[ApiController]
[Route("api/lists")]
[Authorize]
public class DynamicListsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public DynamicListsController(ApplicationDbContext db) => _db = db;

    private int GetTenantId() =>
        int.TryParse(User.FindFirstValue("TenantId"), out var id) ? id : 1;

    // Get all items by category
    [HttpGet("{category}")]
    public async Task<IActionResult> GetByCategory(string category)
    {
        var items = await _db.DynamicLists
            .Where(x => x.Category == category && x.IsActive)
            .OrderBy(x => x.Order)
            .ToListAsync();
        return Ok(items);
    }

    // Get all categories
    [HttpGet]
    [RequirePermission("ManageUsers")]
    public async Task<IActionResult> GetAll()
    {
        var items = await _db.DynamicLists
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Order)
            .ToListAsync();
        return Ok(items);
    }

    // Create item
    [HttpPost]
    [RequirePermission("ManageUsers")]
    public async Task<IActionResult> Create([FromBody] DynamicList dto)
    {
        var item = new DynamicList
        {
            Category = dto.Category,
            NameAr   = dto.NameAr,
            NameEn   = dto.NameEn,
            Value    = dto.Value,
            Order    = dto.Order,
            IsActive = true,
            TenantId = GetTenantId()
        };
        _db.DynamicLists.Add(item);
        await _db.SaveChangesAsync();
        return Ok(item);
    }

    // Update item
    [HttpPut("{id}")]
    [RequirePermission("ManageUsers")]
    public async Task<IActionResult> Update(int id, [FromBody] DynamicList dto)
    {
        var item = await _db.DynamicLists.FindAsync(id);
        if (item == null) return NotFound();
        item.NameAr   = dto.NameAr;
        item.NameEn   = dto.NameEn;
        item.Value    = dto.Value;
        item.Order    = dto.Order;
        item.IsActive = dto.IsActive;
        await _db.SaveChangesAsync();
        return Ok(item);
    }

    // Delete item
    [HttpDelete("{id}")]
    [RequirePermission("ManageUsers")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.DynamicLists.FindAsync(id);
        if (item == null) return NotFound();
        _db.DynamicLists.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Deleted" });
    }

    // Seed default lists
    [HttpPost("seed")]
    [RequirePermission("ManageUsers")]
    public async Task<IActionResult> Seed()
    {
        var tenantId = GetTenantId();
        var existing = await _db.DynamicLists.ToListAsync();
        if (existing.Any()) return Ok(new { message = "Already seeded" });

        var items = new List<DynamicList>
        {
            // Class types
            new() { Category="Classes", NameAr="KG 1", NameEn="KG 1", Value="KG1", Order=1, TenantId=tenantId },
            new() { Category="Classes", NameAr="KG 2", NameEn="KG 2", Value="KG2", Order=2, TenantId=tenantId },
            new() { Category="Classes", NameAr="KG 3", NameEn="KG 3", Value="KG3", Order=3, TenantId=tenantId },
            // Subscription types
            new() { Category="SubscriptionTypes", NameAr="يومي",  NameEn="Daily",   Value="Daily",   Order=1, TenantId=tenantId },
            new() { Category="SubscriptionTypes", NameAr="أسبوعي", NameEn="Weekly",  Value="Weekly",  Order=2, TenantId=tenantId },
            new() { Category="SubscriptionTypes", NameAr="شهري",  NameEn="Monthly", Value="Monthly", Order=3, TenantId=tenantId },
            new() { Category="SubscriptionTypes", NameAr="سنوي",  NameEn="Yearly",  Value="Yearly",  Order=4, TenantId=tenantId },
            // Trip statuses
            new() { Category="TripStatuses", NameAr="تم الإنشاء",  NameEn="Created",    Value="Created",    Order=1, TenantId=tenantId },
            new() { Category="TripStatuses", NameAr="جاري",        NameEn="InProgress", Value="InProgress", Order=2, TenantId=tenantId },
            new() { Category="TripStatuses", NameAr="مكتملة",      NameEn="Completed",  Value="Completed",  Order=3, TenantId=tenantId },
        };

        _db.DynamicLists.AddRange(items);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Seeded", count = items.Count });
    }
}
