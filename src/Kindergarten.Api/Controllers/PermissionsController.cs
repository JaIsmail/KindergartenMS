using System.Security.Claims;
using Kindergarten.Api.Authorization;
using Kindergarten.Core.DTOs;
using Kindergarten.Core.Entities;
using Kindergarten.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kindergarten.Api.Controllers;

[ApiController]
[Route("api/permissions")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public PermissionsController(ApplicationDbContext db) => _db = db;

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    // Get all permissions
    [HttpGet]
    [RequirePermission("Permissions.View")]
    public async Task<IActionResult> GetAll()
    {
        var perms = await _db.Permissions.OrderBy(p => p.Category).ThenBy(p => p.Id).ToListAsync();
        return Ok(perms);
    }

    // Get user permissions
    [HttpGet("user/{userId}")]
    [RequirePermission("Permissions.View")]
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
    [RequirePermission("Permissions.Edit")]
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
        // Delete existing and re-seed with new granular permissions
        var existing = await _db.Permissions.ToListAsync();
        _db.Permissions.RemoveRange(existing);
        await _db.SaveChangesAsync();

        var permissions = new List<Permission>
        {
            // ── Children ──────────────────────────────────────────
            new() { Name="Children.View",         Category="Children",     DisplayNameAr="عرض الأطفال",              DisplayNameEn="View Children",           DescriptionAr="مشاهدة بيانات الأطفال",              DescriptionEn="View children data" },
            new() { Name="Children.Add",          Category="Children",     DisplayNameAr="إضافة طفل",                DisplayNameEn="Add Child",               DescriptionAr="إضافة طفل جديد",                     DescriptionEn="Add a new child" },
            new() { Name="Children.Edit",         Category="Children",     DisplayNameAr="تعديل بيانات الطفل",       DisplayNameEn="Edit Child",              DescriptionAr="تعديل بيانات طفل موجود",             DescriptionEn="Edit existing child data" },
            new() { Name="Children.Delete",       Category="Children",     DisplayNameAr="حذف طفل",                  DisplayNameEn="Delete Child",            DescriptionAr="حذف طفل من النظام",                  DescriptionEn="Delete a child from the system" },
            // ── Subscriptions ─────────────────────────────────────
            new() { Name="Subscriptions.View",    Category="Subscriptions",DisplayNameAr="عرض الاشتراكات",           DisplayNameEn="View Subscriptions",      DescriptionAr="مشاهدة الاشتراكات",                  DescriptionEn="View subscriptions" },
            new() { Name="Subscriptions.Add",     Category="Subscriptions",DisplayNameAr="إضافة اشتراك",             DisplayNameEn="Add Subscription",        DescriptionAr="إنشاء اشتراك جديد",                  DescriptionEn="Create a new subscription" },
            new() { Name="Subscriptions.Edit",    Category="Subscriptions",DisplayNameAr="تعديل الاشتراك",           DisplayNameEn="Edit Subscription",       DescriptionAr="تعديل حالة وتفاصيل الاشتراك",        DescriptionEn="Edit subscription status and details" },
            new() { Name="Subscriptions.Delete",  Category="Subscriptions",DisplayNameAr="حذف الاشتراك",             DisplayNameEn="Delete Subscription",     DescriptionAr="حذف اشتراك من النظام",               DescriptionEn="Delete a subscription" },
            // ── Payments ──────────────────────────────────────────
            new() { Name="Payments.View",         Category="Payments",     DisplayNameAr="عرض المدفوعات",            DisplayNameEn="View Payments",           DescriptionAr="مشاهدة سجلات المدفوعات",             DescriptionEn="View payment records" },
            new() { Name="Payments.Add",          Category="Payments",     DisplayNameAr="تسجيل دفعة",               DisplayNameEn="Add Payment",             DescriptionAr="تسجيل دفعة جديدة",                   DescriptionEn="Record a new payment" },
            new() { Name="Payments.Delete",       Category="Payments",     DisplayNameAr="حذف دفعة",                 DisplayNameEn="Delete Payment",          DescriptionAr="حذف سجل دفعة",                       DescriptionEn="Delete a payment record" },
            // ── Users ─────────────────────────────────────────────
            new() { Name="Users.View",            Category="Users",        DisplayNameAr="عرض المستخدمين",           DisplayNameEn="View Users",              DescriptionAr="مشاهدة قائمة المستخدمين",            DescriptionEn="View users list" },
            new() { Name="Users.Add",             Category="Users",        DisplayNameAr="إضافة مستخدم",             DisplayNameEn="Add User",                DescriptionAr="إضافة مستخدم جديد",                  DescriptionEn="Add a new user" },
            new() { Name="Users.Edit",            Category="Users",        DisplayNameAr="تعديل مستخدم",             DisplayNameEn="Edit User",               DescriptionAr="تعديل بيانات مستخدم",                DescriptionEn="Edit user data" },
            new() { Name="Users.Delete",          Category="Users",        DisplayNameAr="حذف مستخدم",               DisplayNameEn="Delete User",             DescriptionAr="حذف مستخدم من النظام",               DescriptionEn="Delete a user" },
            // ── Attendance ────────────────────────────────────────
            new() { Name="Attendance.CheckIn",    Category="Attendance",   DisplayNameAr="تسجيل الحضور",             DisplayNameEn="Check In/Out",            DescriptionAr="تسجيل حضور وانصراف",                 DescriptionEn="Record own check-in and check-out" },
            new() { Name="Attendance.ViewOwn",    Category="Attendance",   DisplayNameAr="عرض الحضور الشخصي",        DisplayNameEn="View Own Attendance",     DescriptionAr="مشاهدة سجل الحضور الخاص",            DescriptionEn="View personal attendance record" },
            new() { Name="Attendance.ViewAll",    Category="Attendance",   DisplayNameAr="عرض جميع الحضور",          DisplayNameEn="View All Attendance",     DescriptionAr="مشاهدة حضور جميع الموظفين",          DescriptionEn="View all employees attendance" },
            // ── Leave ─────────────────────────────────────────────
            new() { Name="Leave.Submit",          Category="Leave",        DisplayNameAr="طلب إذن",                  DisplayNameEn="Submit Leave",            DescriptionAr="تقديم طلب إذن",                      DescriptionEn="Submit a leave request" },
            new() { Name="Leave.ViewAll",         Category="Leave",        DisplayNameAr="عرض طلبات الإذن",          DisplayNameEn="View All Leave",          DescriptionAr="مشاهدة جميع طلبات الإذن",            DescriptionEn="View all leave requests" },
            new() { Name="Leave.Approve",         Category="Leave",        DisplayNameAr="الموافقة على الإذن",        DisplayNameEn="Approve Leave",           DescriptionAr="مراجعة والموافقة على طلبات الإذن",    DescriptionEn="Review and approve leave requests" },
            // ── Trips ─────────────────────────────────────────────
            new() { Name="Trips.View",            Category="Trips",        DisplayNameAr="عرض الرحلات",              DisplayNameEn="View Trips",              DescriptionAr="مشاهدة الرحلات (ولي الأمر)",         DescriptionEn="View trips (parent tracking)" },
            new() { Name="Trips.Manage",          Category="Trips",        DisplayNameAr="إدارة الرحلات",             DisplayNameEn="Manage Trips",            DescriptionAr="إنشاء وتعديل وإنهاء الرحلات",        DescriptionEn="Create, edit and end trips" },
            new() { Name="Trips.Track",           Category="Trips",        DisplayNameAr="تتبع GPS",                  DisplayNameEn="Track GPS",               DescriptionAr="إرسال الموقع أثناء الرحلة (سائق)",   DescriptionEn="Send GPS location during trip (driver)" },
            // ── Dynamic Lists ─────────────────────────────────────
            new() { Name="Lists.View",            Category="Lists",        DisplayNameAr="عرض القوائم",               DisplayNameEn="View Lists",              DescriptionAr="مشاهدة القوائم الديناميكية",          DescriptionEn="View dynamic lists" },
            new() { Name="Lists.Manage",          Category="Lists",        DisplayNameAr="إدارة القوائم",             DisplayNameEn="Manage Lists",            DescriptionAr="إضافة وتعديل وحذف عناصر القوائم",    DescriptionEn="Add, edit and delete list items" },
            // ── Permissions ───────────────────────────────────────
            new() { Name="Permissions.View",      Category="Permissions",  DisplayNameAr="عرض الصلاحيات",            DisplayNameEn="View Permissions",        DescriptionAr="مشاهدة صلاحيات المستخدمين",          DescriptionEn="View user permissions" },
            new() { Name="Permissions.Edit",      Category="Permissions",  DisplayNameAr="تعديل الصلاحيات",          DisplayNameEn="Edit Permissions",        DescriptionAr="منح وإلغاء صلاحيات المستخدمين",      DescriptionEn="Grant and revoke user permissions" },
            new() { Name="Groups.View",           Category="Permissions",  DisplayNameAr="عرض المجموعات",             DisplayNameEn="View Groups",             DescriptionAr="مشاهدة مجموعات الصلاحيات",           DescriptionEn="View permission groups" },
            new() { Name="Groups.Add",            Category="Permissions",  DisplayNameAr="إضافة مجموعة",             DisplayNameEn="Add Group",               DescriptionAr="إنشاء مجموعة صلاحيات جديدة",         DescriptionEn="Create new permission group" },
            new() { Name="Groups.Edit",           Category="Permissions",  DisplayNameAr="تعديل مجموعة",             DisplayNameEn="Edit Group",              DescriptionAr="تعديل مجموعة صلاحيات",               DescriptionEn="Edit permission group" },
            new() { Name="Groups.Delete",         Category="Permissions",  DisplayNameAr="حذف مجموعة",               DisplayNameEn="Delete Group",            DescriptionAr="حذف مجموعة صلاحيات",                 DescriptionEn="Delete permission group" },
            new() { Name="Groups.Assign",         Category="Permissions",  DisplayNameAr="تعيين المجموعة",            DisplayNameEn="Assign Group",            DescriptionAr="تعيين مستخدم لمجموعة صلاحيات",       DescriptionEn="Assign user to permission group" },
            // ── Tenants ───────────────────────────────────────────
            new() { Name="Tenants.View",          Category="Tenants",      DisplayNameAr="عرض الروضات",               DisplayNameEn="View Tenants",            DescriptionAr="مشاهدة قائمة الروضات",               DescriptionEn="View kindergartens list" },
            new() { Name="Tenants.Add",           Category="Tenants",      DisplayNameAr="إضافة روضة",                DisplayNameEn="Add Tenant",              DescriptionAr="إضافة روضة جديدة",                   DescriptionEn="Add new kindergarten" },
            new() { Name="Tenants.Edit",          Category="Tenants",      DisplayNameAr="تعديل روضة",                DisplayNameEn="Edit Tenant",             DescriptionAr="تعديل بيانات الروضة",                 DescriptionEn="Edit kindergarten details" },
            new() { Name="Tenants.Update",        Category="Tenants",      DisplayNameAr="إعدادات الروضة",            DisplayNameEn="Update Settings",         DescriptionAr="تعديل إعدادات الروضة الخاصة",         DescriptionEn="Update own kindergarten settings" },
            // ── Reports ───────────────────────────────────────────
            new() { Name="Reports.View",          Category="Reports",      DisplayNameAr="عرض التقارير",              DisplayNameEn="View Reports",            DescriptionAr="مشاهدة تقارير النظام",                DescriptionEn="View system reports" },
            new() { Name="Reports.Export",        Category="Reports",      DisplayNameAr="تصدير التقارير",            DisplayNameEn="Export Reports",          DescriptionAr="تصدير وتنزيل التقارير",               DescriptionEn="Export and download reports" },
            // ── Settings ──────────────────────────────────────────
            new() { Name="Settings.View",         Category="Settings",     DisplayNameAr="عرض الإعدادات",             DisplayNameEn="View Settings",           DescriptionAr="مشاهدة إعدادات الروضة",               DescriptionEn="View kindergarten settings" },
            new() { Name="Settings.Edit",         Category="Settings",     DisplayNameAr="تعديل الإعدادات",           DisplayNameEn="Edit Settings",           DescriptionAr="تعديل إعدادات الروضة",                DescriptionEn="Edit kindergarten settings" },
            // ── Discounts ─────────────────────────────────────────
            new() { Name="Discounts.View",        Category="Discounts",    DisplayNameAr="عرض الخصومات",              DisplayNameEn="View Discounts",          DescriptionAr="مشاهدة إعدادات الخصومات",             DescriptionEn="View discount settings" },
            new() { Name="Discounts.Manage",      Category="Discounts",    DisplayNameAr="إدارة الخصومات",            DisplayNameEn="Manage Discounts",        DescriptionAr="إضافة وتعديل أنواع الخصومات",         DescriptionEn="Add and edit discount types" },
            new() { Name="Discounts.Apply",       Category="Discounts",    DisplayNameAr="تطبيق الخصم",               DisplayNameEn="Apply Discount",          DescriptionAr="تطبيق خصم على اشتراك طفل",            DescriptionEn="Apply discount to child subscription" },
            // ── Notifications ─────────────────────────────────────
            new() { Name="Notifications.Send",    Category="Notifications",DisplayNameAr="إرسال الإشعارات",           DisplayNameEn="Send Notifications",      DescriptionAr="إرسال إشعارات للمستخدمين",            DescriptionEn="Send notifications to users" },
            // ── Audit ────────────────────────────────────────────────
            new() { Name="AuditLog.View", Category="Audit", DisplayNameAr="عرض سجل النشاط", DisplayNameEn="View Audit Log", DescriptionAr="مشاهدة سجل جميع العمليات", DescriptionEn="View system audit log" },
            // ── Portal Access ────────────────────────────────────
            new() { Name="Portal.Web", Category="Portal", DisplayNameAr="الوصول للبوابة الإدارية", DisplayNameEn="Web Portal Access", DescriptionAr="وصول لبوابة الإدارة", DescriptionEn="Access the admin web portal" },
            new() { Name="Portal.Mobile", Category="Portal", DisplayNameAr="الوصول للتطبيق", DisplayNameEn="Mobile App Access", DescriptionAr="وصول لتطبيق الجوال", DescriptionEn="Access the mobile PWA app" },
            // ── Finance ───────────────────────────────────────────
            new() { Name="Finance.ViewAll",       Category="Finance",      DisplayNameAr="عرض كامل المالية",          DisplayNameEn="View All Finance",        DescriptionAr="مشاهدة جميع البيانات المالية للروضة", DescriptionEn="View all financial data across tenant" },
        };

        _db.Permissions.AddRange(permissions);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Seeded", count = permissions.Count });
    }
}