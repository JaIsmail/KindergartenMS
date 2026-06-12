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

    // Haversine distance
    private static double DistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat/2)*Math.Sin(dLat/2)
              + Math.Cos(lat1*Math.PI/180)*Math.Cos(lat2*Math.PI/180)
              * Math.Sin(dLon/2)*Math.Sin(dLon/2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1-a));
    }

    // Get or create today attendance record for user
    private async Task<Attendance> GetOrCreateTodayAsync(string userId, int tenantId)
    {
        var today = DateTime.UtcNow.Date;
        var att = await _db.Attendance
            .IgnoreQueryFilters().Include(a => a.Periods)
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.UserId == userId && a.Date == today);

        if (att == null)
        {
            att = new Attendance
            {
                UserId   = userId,
                Date     = today,
                TenantId = tenantId,
                Status   = "Present"
            };
            _db.Attendance.Add(att);
            await _db.SaveChangesAsync();
        }
        return att;
    }

    // Reload attendance with all includes
    private async Task<Attendance> ReloadAsync(int id) =>
        await _db.Attendance
            .IgnoreQueryFilters().Include(a => a.User)
            .Include(a => a.Periods)
            .FirstAsync(a => a.Id == id);

    public async Task<AttendanceResponseDto?> CheckInAsync(string userId, double? lat = null, double? lng = null)
    {
        var tenantId = _tenantService.GetTenantId();

        // Geo restriction
        var tenant = await _db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == tenantId);
        if (tenant?.Settings != null)
        {
            try
            {
                var settings = JsonSerializer.Deserialize<JsonElement>(tenant.Settings);
                if (settings.TryGetProperty("location", out var loc))
                {
                    var tLat   = loc.GetProperty("lat").GetDouble();
                    var tLng   = loc.GetProperty("lng").GetDouble();
                    var radius = loc.GetProperty("radius").GetDouble();
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
            catch (Exception ex) when (ex.Message.StartsWith("outside_range")) { throw; }
            catch { }
        }

        var attendance = await GetOrCreateTodayAsync(userId, tenantId);

        // Check if open period exists
        var openPeriod = attendance.Periods.FirstOrDefault(p => !p.CheckOut.HasValue);
        if (openPeriod != null) throw new Exception("already_checked_in");

        // Create new period
        var period = new AttendancePeriod { AttendanceId = attendance.Id, CheckIn = DateTime.UtcNow };
        _db.AttendancePeriods.Add(period);
        attendance.CheckInTime ??= period.CheckIn;
        await _db.SaveChangesAsync();

        return MapToDto(await ReloadAsync(attendance.Id));
    }

    public async Task<AttendanceResponseDto?> CheckOutAsync(string userId)
    {
        var today = DateTime.UtcNow.Date;
        var attendance = await _db.Attendance
            .IgnoreQueryFilters().Include(a => a.Periods)
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.UserId == userId && a.Date == today);

        if (attendance == null) return null;

        var openPeriod = attendance.Periods.FirstOrDefault(p => !p.CheckOut.HasValue);
        if (openPeriod == null) return null;

        openPeriod.CheckOut = DateTime.UtcNow;
        openPeriod.Hours    = (openPeriod.CheckOut.Value - openPeriod.CheckIn).TotalHours;
        attendance.CheckOutTime = openPeriod.CheckOut;
        await _db.SaveChangesAsync();

        return MapToDto(await ReloadAsync(attendance.Id));
    }

    public async Task<IEnumerable<AttendanceResponseDto>> GetAllAttendanceAsync(DateTime? date)
    {
        var query = _db.Attendance
            .IgnoreQueryFilters()
            .Include(a => a.User)
            .Include(a => a.Periods)
            .AsQueryable();

        if (date.HasValue)
            query = query.Where(a => a.Date == date.Value.Date);

        return (await query.OrderByDescending(a => a.Date).ToListAsync()).Select(MapToDto);
    }

    public async Task<IEnumerable<AttendanceResponseDto>> GetMyAttendanceAsync(string userId)
    {
        var list = await _db.Attendance
            .IgnoreQueryFilters().Include(a => a.User)
            .Include(a => a.Periods)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.Date)
            .ToListAsync();
        return list.Select(MapToDto);
    }

    private static AttendanceResponseDto MapToDto(Attendance a)
    {
        var totalHours   = a.Periods.Where(p => p.CheckOut.HasValue).Sum(p => p.Hours);
        var totalMinutes = (int)Math.Round(totalHours * 60);
        return new AttendanceResponseDto
        {
            Id           = a.Id,
            UserId       = a.UserId,
            UserName     = a.User?.FullName ?? a.UserId,
            Date         = a.Date,
            CheckInTime  = a.CheckInTime,
            CheckOutTime = a.CheckOutTime,
            WorkingHours = $"{totalMinutes/60:D2}:{totalMinutes%60:D2}",
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

    // Legacy employee methods
    public async Task<IEnumerable<EmployeeResponseDto>> GetAllAsync()
    {
        var emps = await _db.Employees.IgnoreQueryFilters().Include(e => e.User).IgnoreQueryFilters().ToListAsync();
        return emps.Select(MapEmployee);
    }

    public async Task<EmployeeResponseDto?> GetByIdAsync(int id)
    {
        var e = await _db.Employees.IgnoreQueryFilters().Include(e => e.User).IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id);
        return e == null ? null : MapEmployee(e);
    }

    public async Task<EmployeeResponseDto?> GetByUserIdAsync(string userId)
    {
        var e = await _db.Employees.IgnoreQueryFilters().Include(e => e.User).IgnoreQueryFilters().FirstOrDefaultAsync(e => e.UserId == userId);
        return e == null ? null : MapEmployee(e);
    }

    public async Task<EmployeeResponseDto> CreateAsync(CreateEmployeeDto dto)
    {
        var emp = new Employee { UserId = dto.UserId, Position = dto.Position, Phone = dto.Phone, TenantId = _tenantService.GetTenantId() };
        _db.Employees.Add(emp);
        await _db.SaveChangesAsync();
        return MapEmployee(emp);
    }

    public async Task<IEnumerable<AttendanceResponseDto>> GetAttendanceAsync(string userId, DateTime? from = null, DateTime? to = null)
    {
        var query = _db.Attendance
            .Include(a => a.User)
            .Include(a => a.Periods)
            .Where(a => a.UserId == userId)
            .AsQueryable();
        if (from.HasValue) query = query.Where(a => a.Date >= from.Value.Date);
        if (to.HasValue)   query = query.Where(a => a.Date <= to.Value.Date);
        return (await query.OrderByDescending(a => a.Date).ToListAsync()).Select(MapToDto);
    }

    public async Task EnsureDriverExistsAsync(string userId)
    {
        var exists = await _db.Employees.IgnoreQueryFilters().AnyAsync(e => e.UserId == userId);
        if (!exists)
        {
            _db.Employees.Add(new Employee { UserId = userId, Position = "", TenantId = _tenantService.GetTenantId() });
            await _db.SaveChangesAsync();
        }
    }

    private static EmployeeResponseDto MapEmployee(Employee e) => new()
    {
        Id       = e.Id,
        UserId   = e.UserId,
        FullName = e.User?.FullName ?? "",
        Email    = e.User?.Email ?? "",
        Position = e.Position,
        Phone    = e.Phone
    };
}
