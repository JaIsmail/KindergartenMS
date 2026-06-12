using Kindergarten.Core.DTOs;
using Kindergarten.Core.Entities;
using Kindergarten.Core.Interfaces;
using Kindergarten.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Kindergarten.Infrastructure.Services;

public class LeaveRequestService : ILeaveRequestService
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notify;
    private const double MonthlyFreeHours = 4.0;

    public LeaveRequestService(ApplicationDbContext db, INotificationService notify)
    {
        _db = db;
        _notify = notify;
    }

    public async Task<double> GetMonthlyHoursAsync(string userId)
    {
        var now   = DateTime.UtcNow;
        var start = new DateTime(now.Year, now.Month, 1);
        var end   = start.AddMonths(1);
        return await _db.LeaveRequests
            .Where(r => r.UserId == userId
                     && r.Status == "Approved"
                     && r.StartTime >= start
                     && r.StartTime < end)
            .SumAsync(r => r.Hours);
    }

    public async Task<LeaveRequestResponseDto> CreateAsync(CreateLeaveRequestDto dto, string userId)
    {
        var hours       = (dto.EndTime - dto.StartTime).TotalHours;
        var monthlyUsed = await GetMonthlyHoursAsync(userId);
        var isPaid      = (monthlyUsed + hours) <= MonthlyFreeHours;

        var request = new LeaveRequest
        {
            UserId    = userId,
            Reason    = dto.Reason,
            StartTime = dto.StartTime,
            EndTime   = dto.EndTime,
            Hours     = hours,
            IsPaid    = isPaid,
            Status    = "Pending"
        };
        _db.LeaveRequests.Add(request);
        await _db.SaveChangesAsync();
        // Notify admin (best effort)
        try
        {
            var user = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
            await _notify.SendToAllParentsAsync(
                titleAr: "طلب إذن جديد",
                titleEn: "New Leave Request",
                bodyAr:  $"{user?.FullName ?? "موظف"} طلب إذناً لمدة {request.Hours:F1} ساعة",
                bodyEn:  $"{user?.FullName ?? "Employee"} requested {request.Hours:F1}h leave",
                data: new Dictionary<string, string> { { "type", "leave_request" }, { "id", request.Id.ToString() } }
            );
        }
        catch { /* notification failed */ }
        return await MapAsync(request.Id);
    }

    public async Task<IEnumerable<LeaveRequestResponseDto>> GetByUserAsync(string userId)
    {
        var list = await _db.LeaveRequests
            .IgnoreQueryFilters()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        var result = new List<LeaveRequestResponseDto>();
        foreach (var r in list) result.Add(await MapAsync(r.Id));
        return result;
    }

    public async Task<IEnumerable<LeaveRequestResponseDto>> GetAllAsync()
    {
        var list = await _db.LeaveRequests
            .IgnoreQueryFilters()
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        var result = new List<LeaveRequestResponseDto>();
        foreach (var r in list) result.Add(await MapAsync(r.Id));
        return result;
    }

    public async Task<LeaveRequestResponseDto?> ReviewAsync(int id, ReviewLeaveRequestDto dto, string reviewerId)
    {
        var request = await _db.LeaveRequests.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == id);
        if (request == null) return null;

        request.Status     = dto.Status;
        request.AdminNote  = dto.AdminNote;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewedBy = reviewerId;
        await _db.SaveChangesAsync();
        return await MapAsync(id);
    }

    private async Task<LeaveRequestResponseDto> MapAsync(int id)
    {
        var r = await _db.LeaveRequests
            .IgnoreQueryFilters()
            .Include(x => x.User)
            .FirstAsync(x => x.Id == id);
        return new LeaveRequestResponseDto
        {
            Id         = r.Id,
            UserId     = r.UserId,
            UserName   = r.User?.FullName ?? r.UserId,
            Reason     = r.Reason,
            StartTime  = r.StartTime,
            EndTime    = r.EndTime,
            Hours      = r.Hours,
            Status     = r.Status,
            AdminNote  = r.AdminNote,
            IsPaid     = r.IsPaid,
            CreatedAt  = r.CreatedAt,
            ReviewedAt = r.ReviewedAt,
            ReviewedBy = r.ReviewedBy
        };
    }
}
