using System.Security.Claims;
using Kindergarten.Core.DTOs;
using Kindergarten.Core.Entities;
using Kindergarten.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kindergarten.Api.Controllers;

[ApiController]
[Route("api/permissions")]
[Authorize(Roles = "Admin")]
public class PermissionsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public PermissionsController(ApplicationDbContext db) => _db = db;

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    // Get all permissions
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var perms = await _db.Permissions.OrderBy(p => p.Category).ThenBy(p => p.Id).ToListAsync();
        return Ok(perms);
    }

    // Get user permissions
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserPermissions(string userId)
    {
        var allPerms = await _db.Permissions.ToListAsync();
        var userPerms = await _db.UserPermissions
            .Where(up => up.UserId == userId)
            .Select(up => up.PermissionId)
            .ToListAsync();

        var result = allPerms.Select(p => new PermissionResponseDto
        {
            Id            = p.Id,
            Name          = p.Name,
            DisplayNameAr = p.DisplayNameAr,
            DisplayNameEn = p.DisplayNameEn,
            DescriptionAr = p.DescriptionAr,
            DescriptionEn = p.DescriptionEn,
            Category      = p.Category,
            IsGranted     = userPerms.Contains(p.Id)
        });

        return Ok(result);
    }
 // Update user permissions
    [HttpPut("user/{userId}")]
    public async Task<IActionResult> UpdateUserPermissions(string userId, [FromBody] UpdateUserPermissionsDto dto)
    {
        // Remove existing
        var existing = await _db.UserPermissions.Where(up => up.UserId == userId).ToListAsync();
        _db.UserPermissions.RemoveRange(existing);

        // Add new
        var newPerms = dto.PermissionIds.Select(pid => new UserPermission
        {
            UserId       = userId,
            PermissionId = pid,
            GrantedBy    = GetUserId(),
            GrantedAt    = DateTime.UtcNow
        });
        _db.UserPermissions.AddRange(newPerms);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Permissions updated", count = dto.PermissionIds.Count });
    }

// Seed default permissions
    [HttpPost("seed")]
    public async Task<IActionResult> Seed()
    {
        if (await _db.Permissions.AnyAsync()) return Ok(new { message = "Already seeded" });

        var permissions = new List<Permission>
        {
            // Trips
            new() { Name="ManageTrips",        Category="Trips",    DisplayNameAr="إدارة الرحلات",          DisplayNameEn="Manage Trips",          DescriptionAr="إنشاء وتعديل وإنهاء الرحلات",         DescriptionEn="Create, edit and end trips" },
            new() { Name="TrackTrips",          Category="Trips",    DisplayNameAr="تتبع الرحلات",           DisplayNameEn="Track Trips",           DescriptionAr="مشاهدة الرحلات الجارية",               DescriptionEn="View ongoing trips" },
            // HR
            new() { Name="SubmitLeaveRequest",  Category="HR",       DisplayNameAr="طلب الإذن",              DisplayNameEn="Submit Leave",          DescriptionAr="تقديم طلبات الإذن والغياب",            DescriptionEn="Submit leave and absence requests" },
            new() { Name="ManageAttendance",    Category="HR",       DisplayNameAr="إدارة الحضور",           DisplayNameEn="Manage Attendance",     DescriptionAr="تسجيل ومراجعة الحضور",                 DescriptionEn="Record and review attendance" },
            new() { Name="ViewOwnAttendance",   Category="HR",       DisplayNameAr="عرض الحضور الشخصي",     DisplayNameEn="View Own Attendance",   DescriptionAr="مشاهدة سجل الحضور الخاص",              DescriptionEn="View personal attendance record" },
            // Finance
            new() { Name="ViewFinancials",      Category="Finance",  DisplayNameAr="عرض المالية",            DisplayNameEn="View Financials",       DescriptionAr="الاطلاع على التقارير المالية",          DescriptionEn="Access financial reports" },
            new() { Name="ManagePayments",      Category="Finance",  DisplayNameAr="إدارة المدفوعات",        DisplayNameEn="Manage Payments",       DescriptionAr="تسجيل ومتابعة المدفوعات",              DescriptionEn="Record and track payments" },
            new() { Name="ViewReports",         Category="Finance",  DisplayNameAr="التقارير",               DisplayNameEn="View Reports",          DescriptionAr="عرض جميع تقارير النظام",               DescriptionEn="View all system reports" },
            // Users
            new() { Name="ManageUsers",         Category="Users",    DisplayNameAr="إدارة المستخدمين",      DisplayNameEn="Manage Users",          DescriptionAr="إضافة وتعديل وحذف المستخدمين",         DescriptionEn="Add, edit and delete users" },
            new() { Name="ManageChildren",      Category="Users",    DisplayNameAr="إدارة الأطفال",          DisplayNameEn="Manage Children",       DescriptionAr="إضافة وتعديل بيانات الأطفال",          DescriptionEn="Add and edit children data" },
            new() { Name="ViewChildren",        Category="Users",    DisplayNameAr="عرض الأطفال",            DisplayNameEn="View Children",         DescriptionAr="مشاهدة بيانات الأطفال",                DescriptionEn="View children data" },
            // Notifications
            new() { Name="SendNotifications",   Category="Notifications", DisplayNameAr="إرسال الإشعارات", DisplayNameEn="Send Notifications",    DescriptionAr="إرسال إشعارات للمستخدمين",             DescriptionEn="Send notifications to users" },
        };

        _db.Permissions.AddRange(permissions);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Seeded", count = permissions.Count });
    }
}
