using System.Security.Claims;
using Kindergarten.Api.Authorization;
using Kindergarten.Core.DTOs;
using Kindergarten.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kindergarten.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    public SubscriptionsController(ISubscriptionService subscriptionService) =>
        _subscriptionService = subscriptionService;

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    [HttpGet]
    [RequirePermission("ViewSubscriptions")]
    public async Task<IActionResult> GetAll()
    {
        var canViewAll = User.HasClaim("Permission", "ViewAllSubscriptions");
        var subs = canViewAll
            ? await _subscriptionService.GetAllAsync(null)
            : await _subscriptionService.GetAllAsync(GetUserId());
        return Ok(subs);
    }

    [HttpGet("{id}")]
    [RequirePermission("ViewSubscriptions")]
    public async Task<IActionResult> GetById(int id)
    {
        var sub = await _subscriptionService.GetByIdAsync(id);
        if (sub == null) return NotFound();
        return Ok(sub);
    }


    [HttpPatch("{id}/status")]
    [RequirePermission("ManageSubscriptions")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
    {
        var sub = await _subscriptionService.UpdateStatusAsync(id, dto.Status);
        if (sub == null) return NotFound();
        return Ok(sub);
    }

    [HttpPost]
    [RequirePermission("ManageSubscriptions")]
    public async Task<IActionResult> Create([FromBody] CreateSubscriptionDto dto)
    {
        var sub = await _subscriptionService.CreateAsync(dto, GetUserId());
        return CreatedAtAction(nameof(GetById), new { id = sub.Id }, sub);
    }
}

public class UpdateStatusDto
{
    public string Status { get; set; } = string.Empty;
}
