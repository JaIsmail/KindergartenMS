using Kindergarten.Core.DTOs;
using Kindergarten.Core.Entities;
using Kindergarten.Core.Interfaces;
using Kindergarten.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Kindergarten.Infrastructure.Services;

public class EmployeeService : IEmployeeService
{
    private readonly ApplicationDbContext _db;
    public EmployeeService(ApplicationDbContext db) => _db = db;

    public async Task<IEnumerable<EmployeeResponseDto>> GetAllAsync()
    {
        return await _db.Employees
            .Include(e => e.User)
            .Select(e => new EmployeeResponseDto
            {
                Id       = e.Id,
                UserId   = e.UserId,
                FullName = e.User.FullName,
                Email    = e.User.Email ?? string.Empty,
                Position = e.Position,
                Phone    = e.Phone
            })
            .ToListAsync();
    }

    public async Task<EmployeeResponseDto?> GetByIdAsync(int id)
    {
        var e = await _db.Employees.Include(e => e.User).FirstOrDefaultAsync(e => e.Id == id);
        if (e == null) return null;
        return new EmployeeResponseDto { Id=e.Id, UserId=e.UserId, FullName=e.User.FullName, Email=e.User.Email??string.Empty, Position=e.Position, Phone=e.Phone };
    }

    public async Task<EmployeeResponseDto?> GetByUserIdAsync(string userId)
    {
        var e = await _db.Employees.Include(e => e.User).FirstOrDefaultAsync(e => e.UserId == userId);
        if (e == null) return null;
        return new EmployeeResponseDto { Id=e.Id, UserId=e.UserId, FullName=e.User.FullName, Email=e.User.Email??string.Empty, Position=e.Position, Phone=e.Phone };
    }

    public async Task<EmployeeResponseDto> CreateAsync(CreateEmployeeDto dto)
    {
        var employee = new Employee
        {
            UserId   = dto.UserId,
            Position = dto.Position,
            Phone    = dto.Phone
        };
        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();
        return await GetByIdAsync(employee.Id) ?? new EmployeeResponseDto();
    }

    public async Task<AttendanceResponseDto?> CheckInAsync(string userId)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employee == null) return null;

        var today = DateTime.UtcNow.Date;

        // Check if already checked in today
        var existing = await _db.Attendance
            .FirstOrDefaultAsync(a => a.EmployeeId == employee.Id && a.Date == today);

        if (existing != null && existing.CheckInTime.HasValue)
            return MapAttendance(existing, employee.Id);

        var attendance = existing ?? new Attendance { EmployeeId = employee.Id, Date = today };

        attendance.CheckInTime = DateTime.UtcNow;
        attendance.Status      = DateTime.UtcNow.Hour >= 9 ? "Late" : "Present";

        if (existing == null) _db.Attendance.Add(attendance);
        await _db.SaveChangesAsync();

        return MapAttendance(attendance, employee.Id);
    }

    public async Task<AttendanceResponseDto?> CheckOutAsync(string userId)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employee == null) return null;

        var today = DateTime.UtcNow.Date;
        var attendance = await _db.Attendance
            .FirstOrDefaultAsync(a => a.EmployeeId == employee.Id && a.Date == today);

        if (attendance == null || !attendance.CheckInTime.HasValue) return null;

        attendance.CheckOutTime = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return MapAttendance(attendance, employee.Id);
    }

    public async Task<IEnumerable<AttendanceResponseDto>> GetAttendanceAsync(string userId, DateTime? from = null, DateTime? to = null)
    {
        var employee = await _db.Employees.Include(e => e.User).FirstOrDefaultAsync(e => e.UserId == userId);
        if (employee == null) return Enumerable.Empty<AttendanceResponseDto>();

        var query = _db.Attendance.Where(a => a.EmployeeId == employee.Id);
        if (from.HasValue) query = query.Where(a => a.Date >= from.Value.Date);
        if (to.HasValue)   query = query.Where(a => a.Date <= to.Value.Date);

        var records = await query.OrderByDescending(a => a.Date).ToListAsync();
        return records.Select(a => MapAttendance(a, employee.Id, employee.User.FullName));
    }

    public async Task<IEnumerable<AttendanceResponseDto>> GetAllAttendanceAsync(DateTime? date = null)
    {
        var targetDate = date?.Date ?? DateTime.UtcNow.Date;
        return await _db.Attendance
            .Include(a => a.Employee).ThenInclude(e => e.User)
            .Where(a => a.Date == targetDate)
            .Select(a => new AttendanceResponseDto
            {
                Id           = a.Id,
                EmployeeId   = a.EmployeeId,
                EmployeeName = a.Employee.User.FullName,
                Date         = a.Date,
                CheckInTime  = a.CheckInTime,
                CheckOutTime = a.CheckOutTime,
                Status       = a.Status,
                WorkingHours = a.CheckInTime.HasValue && a.CheckOutTime.HasValue
                    ? (a.CheckOutTime.Value - a.CheckInTime.Value).ToString(@"hh\:mm")
                    : null
            })
            .ToListAsync();
    }

    private static AttendanceResponseDto MapAttendance(Attendance a, int empId, string empName = "")
    => new()
    {
        Id           = a.Id,
        EmployeeId   = empId,
        EmployeeName = empName,
        Date         = a.Date,
        CheckInTime  = a.CheckInTime,
        CheckOutTime = a.CheckOutTime,
        Status       = a.Status,
        WorkingHours = a.CheckInTime.HasValue && a.CheckOutTime.HasValue
            ? (a.CheckOutTime.Value - a.CheckInTime.Value).ToString(@"hh\:mm")
            : null
    };
}
