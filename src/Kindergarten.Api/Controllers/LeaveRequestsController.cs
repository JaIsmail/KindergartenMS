using System.Security.Claims;
using Kindergarten.Api.Authorization;
using Kindergarten.Core.DTOs;
using Kindergarten.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kindergarten.Api.Controllers;

[ApiController]
[Route("api/leave-requests")]
[Authorize]
public class LeaveRequestsController : ControllerBase
{
    private readonly ILeaveRequestService _service;

    public LeaveRequestsController(ILeaveRequestService service) => _service = service;

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    [HttpPost]
    [RequirePermission("Leave.Submit")]
    public async Task<IActionResult> Create([FromBody] CreateLeaveRequestDto dto)
    {
        var result = await _service.CreateAsync(dto, GetUserId());
        return Ok(result);
    }

    [HttpGet("my")]
    [RequirePermission("Leave.Submit")]
    public async Task<IActionResult> GetMy()
    {
        var result = await _service.GetByUserAsync(GetUserId());
        return Ok(result);
    }

    [HttpGet("my-hours")]
    [RequirePermission("Leave.Submit")]
    public async Task<IActionResult> GetMyHours()
    {
        var hours = await _service.GetMonthlyHoursAsync(GetUserId());
        return Ok(new { usedHours = hours, freeHours = 4.0, remainingFree = Math.Max(0, 4.0 - hours) });
    }

    [HttpGet]
    [RequirePermission("Leave.ViewAll")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(result);
    }

    [HttpPut("{id}/review")]
    [RequirePermission("Leave.Approve")]
    public async Task<IActionResult> Review(int id, [FromBody] ReviewLeaveRequestDto dto)
    {
        var result = await _service.ReviewAsync(id, dto, GetUserId());
        if (result == null) return NotFound();
        return Ok(result);
    }
}
