using System.Security.Claims;
using Kindergarten.Core.DTOs;
using Kindergarten.Core.Interfaces;
using Kindergarten.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kindergarten.Api.Controllers;

[ApiController]
[Route("api/leave-requests")]
[Authorize]
public class LeaveRequestsController : ControllerBase
{
    private readonly ILeaveRequestService _service;
    private readonly ApplicationDbContext _db;

    public LeaveRequestsController(ILeaveRequestService service, ApplicationDbContext db)
    {
        _service = service;
        _db      = db;
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    // Any staff member with an Employee record can submit
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateLeaveRequestDto dto)
    {
        var emp = await _db.Employees.FirstOrDefaultAsync(e => e.UserId == GetUserId());
        if (emp == null) return Forbid(); // Must have Employee record

        var result = await _service.CreateAsync(dto, emp.Id);
        return Ok(result);
    }
// Employee views own requests
    [HttpGet("my")]
    [Authorize]
    public async Task<IActionResult> GetMy()
    {
        var emp = await _db.Employees.FirstOrDefaultAsync(e => e.UserId == GetUserId());
        if (emp == null) return NotFound();
        var result = await _service.GetByEmployeeAsync(emp.Id);
        return Ok(result);
    }

    // Any staff with Employee record checks hours
    [HttpGet("my-hours")]
    [Authorize]
    public async Task<IActionResult> GetMyHours()
    {
        var emp = await _db.Employees.FirstOrDefaultAsync(e => e.UserId == GetUserId());
        if (emp == null) return NotFound();
        var hours = await _service.GetMonthlyHoursAsync(emp.Id);
        return Ok(new { usedHours = hours, freeHours = 4.0, remainingFree = Math.Max(0, 4.0 - hours) });
    }

    // Admin views all requests
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(result);
    }
// Admin approves/rejects
    [HttpPut("{id}/review")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Review(int id, [FromBody] ReviewLeaveRequestDto dto)
    {
        var result = await _service.ReviewAsync(id, dto, GetUserId());
        if (result == null) return NotFound();
        return Ok(result);
    }
}
