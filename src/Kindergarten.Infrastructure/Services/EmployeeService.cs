using System.Security.Claims;
using System.Text.Json;
using Kindergarten.Core.DTOs;
using Kindergarten.Core.Entities;
using Kindergarten.Core.Interfaces;
using Kindergarten.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Kindergarten.Infrastructure.Services;

public class EmployeeService : IEmployeeService
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantService       _tenantService;
    private readonly UserManager<ApplicationUser> _userManager;

    public EmployeeService(
        ApplicationDbContext db,
        ITenantService tenantService,
        UserManager<ApplicationUser> userManager)
    {
        _db            = db;
        _tenantService = tenantService;
        _userManager   = userManager;
    }
// ── Geo distance (Haversine) ──────────────────────────────────────
    private static double DistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    // ── Get or create today's Attendance record ───────────────────────
    private async Task<Attendance> GetOrCreateAttendanceAsync(string userId, int tenantId)
    {
        var emp = await _db.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
        if (emp == null) throw new Exception("Employee not found");

        var today = DateTime.UtcNow.Date;
        var att = await _db.Attendance
            .Include(a => a.Periods)
            .FirstOrDefaultAsync(a => a.EmployeeId == emp.Id && a.Date == today);

        if (att == null)
        {
            att = new Attendance
            {
                EmployeeId = emp.Id,
                Date       = today,
                TenantId   = tenantId,
                Status     = "Present"
            };
            _db.Attendance.Add(att);
            await _db.SaveChangesAsync();
        }
        return att;
    }

    // ── Check In ──────────────────────────────────────────────────────
    public async Task<AttendanceResponseDto?> CheckInAsync(string userId, double? lat = null, double? lng = null)
    {
        var tenantId = _tenantService.GetTenantId();

        // Geo restriction check
        var tenant = await _db.Tenants.FindAsync(tenantId);
        if (tenant?.Settings != null)
        {
            try
            {
                var settings = JsonSerializer.Deserialize<JsonElement>(tenant.Settings);
                if (settings.TryGetProperty("location", out var loc))
                {
                    var tLat    = loc.GetProperty("lat").GetDouble();
                    var tLng    = loc.GetProperty("lng").GetDouble();
                    var radius  = loc.GetProperty("radius").GetDouble();
                    var allowOutside = false;
                    if (settings.TryGetProperty("attendance", out var att) &&
                        att.TryGetProperty("allowOutside", out var ao))
                        allowOutside = ao.GetBoolean();

                    if (!allowOutside && lat.HasValue && lng.HasValue && tLat != 0 && tLng != 0)
                    {
                        var dist = DistanceMeters(lat.Value, lng.Value, tLat, tLng);
                        if (dist > radius)
                            throw new Exception($"outside_range:{dist:F0}:{radius}");
                    }
                }
            }
            catch (Exception ex) when (ex.Message.StartsWith("outside_range"))
            {
                throw;
            }
            catch { }
        }

        var attendance = await GetOrCreateAttendanceAsync(userId, tenantId);

// Check if there's an open period (checked in but not out)
        var openPeriod = attendance.Periods.FirstOrDefault(p => !p.CheckOut.HasValue);
        if (openPeriod != null)
            throw new Exception("already_checked_in");

        // Add new period
        var period = new AttendancePeriod
        {
            AttendanceId = attendance.Id,
            CheckIn      = DateTime.UtcNow
        };
        _db.AttendancePeriods.Add(period);

        // Update legacy fields
        attendance.CheckInTime ??= period.CheckIn;
        await _db.SaveChangesAsync();

        return MapToDto(attendance);
    }

 // ── Check Out ─────────────────────────────────────────────────────
    public async Task<AttendanceResponseDto?> CheckOutAsync(string userId)
    {
        var tenantId = _tenantService.GetTenantId();
        var emp = await _db.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
        if (emp == null) return null;

        var today = DateTime.UtcNow.Date;
        var attendance = await _db.Attendance
            .Include(a => a.Periods)
            .FirstOrDefaultAsync(a => a.EmployeeId == emp.Id && a.Date == today);

        if (attendance == null) return null;

        var openPeriod = attendance.Periods.FirstOrDefault(p => !p.CheckOut.HasValue);
        if (openPeriod == null) return null;

        openPeriod.CheckOut = DateTime.UtcNow;
        openPeriod.Hours    = (openPeriod.CheckOut.Value - openPeriod.CheckIn).TotalHours;

        // Update legacy fields
        attendance.CheckOutTime = openPeriod.CheckOut;

        await _db.SaveChangesAsync();
        return MapToDto(attendance);
    }

// ── Get All Attendance ────────────────────────────────────────────
    public async Task<IEnumerable<AttendanceResponseDto>> GetAllAttendanceAsync(DateTime? date)
    {
        var query = _db.Attendance
            .Include(a => a.Employee)
            .Include(a => a.Periods)
            .AsQueryable();

        if (date.HasValue)
            query = query.Where(a => a.Date == date.Value.Date);

        var list = await query.OrderByDescending(a => a.Date).ToListAsync();
        return list.Select(MapToDto);
    }

 // ── Map to DTO ────────────────────────────────────────────────────
    private static AttendanceResponseDto MapToDto(Attendance a)
    {
        var totalHours = a.Periods.Where(p => p.CheckOut.HasValue)
                          .Sum(p => p.Hours);
        var totalMinutes = (int)(totalHours * 60);

        return new AttendanceResponseDto
        {
            Id           = a.Id,
            EmployeeId   = a.EmployeeId,
            EmployeeName = a.Employee?.UserId ?? "",
            Date         = a.Date,
            CheckInTime  = a.CheckInTime,
            CheckOutTime = a.CheckOutTime,
            WorkingHours = $"{totalMinutes / 60:D2}:{totalMinutes % 60:D2}",
            Status       = a.Status,
            Periods      = a.Periods.Select(p => new AttendancePeriodDto
            {
                Id       = p.Id,
                CheckIn  = p.CheckIn,
                CheckOut = p.CheckOut,
                Hours    = Math.Round(p.Hours, 2)
            }).ToList()
        };
    }

 // ── Other methods ─────────────────────────────────────────────────
    public async Task<IEnumerable<AttendanceResponseDto>> GetMyAttendanceAsync(string userId)
    {
        var emp = await _db.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
        if (emp == null) return [];

        var list = await _db.Attendance
            .Include(a => a.Employee)
            .Include(a => a.Periods)
            .Where(a => a.EmployeeId == emp.Id)
            .OrderByDescending(a => a.Date)
            .ToListAsync();

        return list.Select(MapToDto);
    }

    public async Task EnsureDriverExistsAsync(string userId)
    {
        var exists = await _db.Employees.AnyAsync(e => e.UserId == userId);
        if (!exists)
        {
            _db.Employees.Add(new Employee
            {
                UserId   = userId,
                Position = "",
                TenantId = _tenantService.GetTenantId()
            });
            await _db.SaveChangesAsync();
        }
    }


    // ── Legacy Employee Methods ──────────────────────────────────────
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
            Phone    = dto.Phone,
            TenantId = _tenantService.GetTenantId()
        };
        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();
        return await GetByIdAsync(employee.Id) ?? new EmployeeResponseDto();
    }

    public async Task<IEnumerable<AttendanceResponseDto>> GetAttendanceAsync(string userId, DateTime? from = null, DateTime? to = null)
    {
        var employee = await _db.Employees.Include(e => e.User).FirstOrDefaultAsync(e => e.UserId == userId);
        if (employee == null) return Enumerable.Empty<AttendanceResponseDto>();

        var query = _db.Attendance.Where(a => a.EmployeeId == employee.Id);
        if (from.HasValue) query = query.Where(a => a.Date >= from.Value.Date);
        if (to.HasValue)   query = query.Where(a => a.Date <= to.Value.Date);

        var records = await query.OrderByDescending(a => a.Date).ToListAsync();
        return records.Select(a => MapToDto(a));
    }
}
