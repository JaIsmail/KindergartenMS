using Microsoft.EntityFrameworkCore;
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
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService     _employeeService;
    private readonly INotificationService _notify;

    public EmployeesController(IEmployeeService employeeService, INotificationService notify)
    {
        _employeeService = employeeService;
        _notify          = notify;
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    // ── Admin endpoints ──────────────────────────────
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll()
    {
        var employees = await _employeeService.GetAllAsync();
        return Ok(employees);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeDto dto)
    {
        var employee = await _employeeService.CreateAsync(dto);
        return Ok(employee);
    }

    [HttpGet("attendance/all")]
    [Authorize]
    public async Task<IActionResult> GetAllAttendance()
    {
        var attendance = await _employeeService.GetAllAttendanceAsync(null);
        return Ok(attendance);
    }

    [HttpGet("attendance/today")]
    [Authorize]
    public async Task<IActionResult> GetTodayAttendance()
    {
        var attendance = await _employeeService.GetAllAttendanceAsync(DateTime.UtcNow);
        return Ok(attendance);
    }

    [HttpGet("attendance/date/{date}")]
    [Authorize]
    public async Task<IActionResult> GetAttendanceByDate(DateTime date)
    {
        var attendance = await _employeeService.GetAllAttendanceAsync(date);
        return Ok(attendance);
    }

    // ── Employee endpoints (Biometric) ───────────────
    [HttpPost("checkin")]
    [RequirePermission("ViewOwnAttendance")]
    [Authorize]
    public async Task<IActionResult> CheckIn([FromBody] CheckInDto dto)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        try
        {
            var result = await _employeeService.CheckInAsync(userId, dto.Latitude, dto.Longitude);
            if (result == null)
                return BadRequest(new { message = "Employee not found" });
            await _notify.SendToAllParentsAsync(
                titleAr: "تسجيل حضور",
                titleEn: "Check-in Recorded",
                bodyAr:  "تم تسجيل حضور موظف",
                bodyEn:  "Employee check-in recorded",
                data: new Dictionary<string, string> {
                    { "type", "employee_checkin" },
                    { "employeeId", result.UserId }
                }
            );
            return Ok(result);
        }
        catch (Exception ex) when (ex.Message.StartsWith("outside_range"))
        {
            var parts = ex.Message.Split(':');
            return BadRequest(new { message = "outside_range", distance = parts[1], radius = parts[2] });
        }
        catch (Exception ex) when (ex.Message == "already_checked_in")
        {
            return BadRequest(new { message = "Already checked in" });
        }
    }
    public async Task<IActionResult> CheckOut()
    {
        var result = await _employeeService.CheckOutAsync(GetUserId());
        if (result == null)
            return BadRequest(new { message = "No check-in found for today" });

        return Ok(result);
    }

    [HttpGet("my-attendance")]
    [Authorize]
    public async Task<IActionResult> GetMyAttendance(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var records = await _employeeService.GetAttendanceAsync(GetUserId(), from, to);
        return Ok(records);
    }

    [HttpPost("ensure-driver/{userId}")]
    [Authorize]
    public async Task<IActionResult> EnsureDriverEmployee(string userId,
        [FromServices] Kindergarten.Infrastructure.Data.ApplicationDbContext db)
    {
        var existing = await db.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
        if (existing != null) return Ok(new { message = "Already exists", id = existing.Id });

        var emp = new Kindergarten.Core.Entities.Employee
        {
            UserId   = userId,
            Position = "Driver"
        };
        db.Employees.Add(emp);
        await db.SaveChangesAsync();
        return Ok(new { message = "Employee record created", id = emp.Id });
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetMyProfile()
    {
        var profile = await _employeeService.GetByUserIdAsync(GetUserId());
        if (profile == null) return NotFound();
        return Ok(profile);
    }
}
