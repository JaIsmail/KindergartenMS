using System.Security.Claims;
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
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        var employees = await _employeeService.GetAllAsync();
        return Ok(employees);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeDto dto)
    {
        var employee = await _employeeService.CreateAsync(dto);
        return Ok(employee);
    }

    [HttpGet("attendance/all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllAttendance()
    {
        var attendance = await _employeeService.GetAllAttendanceAsync(null);
        return Ok(attendance);
    }

    [HttpGet("attendance/today")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetTodayAttendance()
    {
        var attendance = await _employeeService.GetAllAttendanceAsync(DateTime.UtcNow);
        return Ok(attendance);
    }

    [HttpGet("attendance/date/{date}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAttendanceByDate(DateTime date)
    {
        var attendance = await _employeeService.GetAllAttendanceAsync(date);
        return Ok(attendance);
    }

    // ── Employee endpoints (Biometric) ───────────────
    [HttpPost("checkin")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> CheckIn([FromBody] CheckInDto dto)
    {
        if (!dto.BiometricVerified)
            return BadRequest(new { message = "Biometric verification failed" });

        var result = await _employeeService.CheckInAsync(GetUserId());
        if (result == null)
            return BadRequest(new { message = "Employee not found or already checked in" });

        // 🔔 Notify admin
        await _notify.SendToAllParentsAsync(
            titleAr: "تسجيل حضور",
            titleEn: "Check-in Recorded",
            bodyAr:  "تم تسجيل حضور موظف",
            bodyEn:  "Employee check-in recorded",
            data: new Dictionary<string, string> {
                { "type", "employee_checkin" },
                { "employeeId", result.EmployeeId.ToString() }
            }
        );

        return Ok(result);
    }

    [HttpPost("checkout")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> CheckOut()
    {
        var result = await _employeeService.CheckOutAsync(GetUserId());
        if (result == null)
            return BadRequest(new { message = "No check-in found for today" });

        return Ok(result);
    }

    [HttpGet("my-attendance")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> GetMyAttendance(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var records = await _employeeService.GetAttendanceAsync(GetUserId(), from, to);
        return Ok(records);
    }

    [HttpGet("profile")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> GetMyProfile()
    {
        var profile = await _employeeService.GetByUserIdAsync(GetUserId());
        if (profile == null) return NotFound();
        return Ok(profile);
    }
}
