using Kindergarten.Core.DTOs;
using Kindergarten.Core.Entities;
using Kindergarten.Core.Interfaces;
using Kindergarten.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Kindergarten.Infrastructure.Services;

public class LeaveRequestService : ILeaveRequestService
{
    private readonly ApplicationDbContext  _db;
    private readonly INotificationService  _notify;
    private const double MonthlyFreeHours = 4.0;

    public LeaveRequestService(ApplicationDbContext db, INotificationService notify)
    {
        _db     = db;
        _notify = notify;
    }

    public async Task<double> GetMonthlyHoursAsync(int employeeId)
    {
        var now   = DateTime.UtcNow;
        var start = new DateTime(now.Year, now.Month, 1);
        var end   = start.AddMonths(1);

        return await _db.LeaveRequests
            .Where(r => r.EmployeeId == employeeId
                     && r.Status     == "Approved"
                     && r.StartTime  >= start
                     && r.StartTime  <  end)
            .SumAsync(r => r.Hours);
    }

 public async Task<LeaveRequestResponseDto> CreateAsync(CreateLeaveRequestDto dto, int employeeId)
    {
        var hours = (dto.EndTime - dto.StartTime).TotalHours;
        var monthlyUsed = await GetMonthlyHoursAsync(employeeId);
        var isPaid = (monthlyUsed + hours) <= MonthlyFreeHours;

        var request = new LeaveRequest
        {
            EmployeeId = employeeId,
            Reason     = dto.Reason,
            StartTime  = dto.StartTime,
            EndTime    = dto.EndTime,
            Hours      = hours,
            IsPaid     = isPaid,
            Status     = "Pending",
            CreatedAt  = DateTime.UtcNow
        };

        _db.LeaveRequests.Add(request);
        await _db.SaveChangesAsync();
// Notify admin
        await _notify.SendToAllParentsAsync(
            "طلب إذن جديد 📋", "New Leave Request 📋",
            $"موظف طلب إذن لمدة {hours:F1} ساعة", $"Employee requested {hours:F1} hours leave",
            new() { ["type"] = "leave_request", ["id"] = request.Id.ToString() }
        );

        // Warn employee if exceeding free hours
        if (!isPaid)
        {
            var emp = await _db.Employees.Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == employeeId);
            if (emp != null)
            {
                await _notify.SendToUserAsync(
                    emp.UserId,
                    "تنبيه: خصم من الراتب ⚠️", "Warning: Salary Deduction ⚠️",
                    $"لقد تجاوزت حد {MonthlyFreeHours} ساعات شهرياً. هذا الإذن سيُخصم من راتبك.",
                    $"You exceeded {MonthlyFreeHours} free hours/month. This leave will be deducted from salary.",
                    new() { ["type"] = "salary_deduction" }
                );
            }
        }

        return await MapAsync(request);
    }
  public async Task<IEnumerable<LeaveRequestResponseDto>> GetByEmployeeAsync(int employeeId)
    {
        var requests = await _db.LeaveRequests
            .Include(r => r.Employee).ThenInclude(e => e.User)
            .Where(r => r.EmployeeId == employeeId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return await Task.WhenAll(requests.Select(MapAsync));
    }

    public async Task<IEnumerable<LeaveRequestResponseDto>> GetAllAsync()
    {
        var requests = await _db.LeaveRequests
            .Include(r => r.Employee).ThenInclude(e => e.User)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return await Task.WhenAll(requests.Select(MapAsync));
    }

    public async Task<LeaveRequestResponseDto?> ReviewAsync(int id, ReviewLeaveRequestDto dto, string adminId)
    {
        var request = await _db.LeaveRequests
            .Include(r => r.Employee).ThenInclude(e => e.User)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null) return null;

        request.Status     = dto.Status;
        request.AdminNote  = dto.AdminNote;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewedBy = adminId;

        await _db.SaveChangesAsync();

        // Notify employee
        var approved = dto.Status == "Approved";
        await _notify.SendToUserAsync(
            request.Employee.UserId,
            approved ? "تمت الموافقة على طلبك ✅" : "تم رفض طلبك ❌",
            approved ? "Leave Request Approved ✅" : "Leave Request Rejected ❌",
            approved ? $"تمت الموافقة على إذنك. {(request.IsPaid ? "" : "سيتم خصمه من راتبك.")}"
                     : $"تم رفض طلب إذنك. {dto.AdminNote ?? ""}",
            approved ? $"Your leave was approved. {(request.IsPaid ? "" : "It will be deducted from salary.")}"
                     : $"Your leave request was rejected. {dto.AdminNote ?? ""}",
            new() { ["type"] = "leave_reviewed", ["status"] = dto.Status }
        );

        return await MapAsync(request);
    }


    private async Task<LeaveRequestResponseDto> MapAsync(LeaveRequest r)
    {
        var monthlyHours = await GetMonthlyHoursAsync(r.EmployeeId);
        return new LeaveRequestResponseDto
        {
            Id               = r.Id,
            EmployeeId       = r.EmployeeId,
            EmployeeName     = r.Employee?.User?.FullName ?? "",
            Reason           = r.Reason,
            StartTime        = r.StartTime,
            EndTime          = r.EndTime,
            Hours            = r.Hours,
            Status           = r.Status,
            AdminNote        = r.AdminNote,
            IsPaid           = r.IsPaid,
            CreatedAt        = r.CreatedAt,
            ReviewedBy       = r.ReviewedBy,
            MonthlyUsedHours = monthlyHours
        };
    }
}
