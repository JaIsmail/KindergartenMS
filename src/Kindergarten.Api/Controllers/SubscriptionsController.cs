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
        var role = User.FindFirstValue(ClaimTypes.Role) ?? "";
        var subs = role == "Admin"
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

    [HttpPost]
    [RequirePermission("ManageSubscriptions")]
    public async Task<IActionResult> Create([FromBody] CreateSubscriptionDto dto)
    {
        var sub = await _subscriptionService.CreateAsync(dto, GetUserId());
        return CreatedAtAction(nameof(GetById), new { id = sub.Id }, sub);
    }
}
