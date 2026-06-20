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

    private async Task<List<string>> GetUserIdsWithPermissionAsync(string permissionName, int tenantId)
    {
        var viaGroups = await _db.UserPermissionGroups
            .IgnoreQueryFilters()
            .Where(ug => ug.TenantId == tenantId)
            .Join(_db.PermissionGroupPermissions, ug => ug.GroupId, pgp => pgp.GroupId, (ug, pgp) => new { ug.UserId, pgp.PermissionId })
            .Join(_db.Permissions, x => x.PermissionId, p => p.Id, (x, p) => new { x.UserId, p.Name })
            .Where(x => x.Name == permissionName)
            .Select(x => x.UserId)
            .ToListAsync();
 var viaDirect = await _db.UserPermissions
            .IgnoreQueryFilters()
            .Where(up => up.TenantId == tenantId)
            .Join(_db.Permissions, up => up.PermissionId, p => p.Id, (up, p) => new { up.UserId, p.Name })
            .Where(x => x.Name == permissionName)
            .Select(x => x.UserId)
            .ToListAsync();

        return viaGroups.Concat(viaDirect).Distinct().ToList();
    }

    public async Task<double> GetMonthlyHoursAsync(string userId)
    {
        var now   = DateTime.UtcNow;
        var start = new DateTime(now.Year, now.Month, 1);
        var end   = start.AddMonths(1);
        return await _db.LeaveRequests
            .IgnoreQueryFilters().Where(r => r.UserId == userId
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
        // Notify approvers (best effort)
        try
        {
            var user = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
            var tenantId = user?.TenantId ?? request.TenantId;

            var approverIds = await GetUserIdsWithPermissionAsync("Leave.Approve", tenantId);
            if (!approverIds.Any())
            {
                approverIds = await _db.Users
                    .IgnoreQueryFilters()
                    .Where(u => u.TenantId == tenantId && u.RoleType == "Admin")
                    .Select(u => u.Id)
                    .ToListAsync();
            }

            var data = new Dictionary<string, string> { { "type", "leave_request" }, { "id", request.Id.ToString() } };
            var replacements = new Dictionary<string, string>
            {
                { "employeeName", user?.FullName ?? "Employee" },
                { "hours", request.Hours.ToString("F1") }
            };
foreach (var approverId in approverIds)
            {
                await _notify.SendTemplatedAsync("leave_request_submitted", approverId, replacements, data);
            }
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

        // Notify the employee of the decision (best effort)
        try
        {
  var statusAr = dto.Status == "Approved" ? "قبول" : "رفض";
            var statusEn = dto.Status == "Approved" ? "Approved" : "Rejected";
            var replacements = new Dictionary<string, string>
            {
                { "statusAr", statusAr },
                { "statusEn", statusEn }
            };
            var data = new Dictionary<string, string> { { "type", "leave_request_reviewed" }, { "id", request.Id.ToString() } };
            await _notify.SendTemplatedAsync("leave_request_reviewed", request.UserId, replacements, data);
        }
        catch { /* notification failed */ }

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
