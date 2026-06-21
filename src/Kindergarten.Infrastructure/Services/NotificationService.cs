using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Kindergarten.Core.DTOs;
using Kindergarten.Core.Interfaces;
using Kindergarten.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Kindergarten.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<NotificationService> _logger;
    private static bool _firebaseInitialized = false;
    private static readonly object _lock = new();

    public NotificationService(
        ApplicationDbContext db,
        IConfiguration config,
        ILogger<NotificationService> logger)
    {
        _db     = db;
        _logger = logger;
        InitializeFirebase(config);
    }

    private void InitializeFirebase(IConfiguration config)
    {
        lock (_lock)
        {
            if (_firebaseInitialized) return;

            try
            {
                var json = config["FirebaseAdminSdk"];
                if (string.IsNullOrEmpty(json))
                {
                    _logger.LogWarning("FirebaseAdminSdk config not found");
                    return;
                }

                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromJson(json)
                });

                _firebaseInitialized = true;
                _logger.LogInformation("Firebase initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Firebase");
            }
        }
    }

    public async Task<bool> SendToDeviceAsync(SendNotificationDto dto)
    {
        if (!_firebaseInitialized) return false;
        if (string.IsNullOrEmpty(dto.DeviceToken)) return false;

        try
        {
            var message = new Message
            {
                Token = dto.DeviceToken,
                Notification = new Notification
                {
                    Title = dto.TitleAr,
                    Body  = dto.BodyAr
                },
                Data = new Dictionary<string, string>(dto.Data),
                Android = new AndroidConfig
                {
                    Notification = new AndroidNotification
                    {
                        Title = dto.TitleAr,
                        Body  = dto.BodyAr,
                        Sound = "default"
                    }
                },
                Apns = new ApnsConfig
                {
                    Aps = new Aps
                    {
                        Alert = new ApsAlert
                        {
                            Title = dto.TitleAr,
                            Body  = dto.BodyAr
                        },
                        Sound = "default"
                    }
                }
            };

            // Also add English translations in data
            var msgData = new Dictionary<string, string>(dto.Data)
            {
                ["titleEn"] = dto.TitleEn,
                ["bodyEn"]  = dto.BodyEn,
                ["titleAr"] = dto.TitleAr,
                ["bodyAr"]  = dto.BodyAr
            };
            message.Data = msgData;

            var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
            _logger.LogInformation("FCM sent: {Response}", response);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FCM send failed for token: {Token}", dto.DeviceToken[..10]);
            return false;
        }
    }

    public async Task<bool> SendToParentAsync(
        string parentId,
        string titleAr, string titleEn,
        string bodyAr,  string bodyEn,
        Dictionary<string, string>? data = null)
    {
        var devices = await _db.UserDevices
            .IgnoreQueryFilters().Where(d => d.UserId == parentId)
            .ToListAsync();

        if (!devices.Any()) return false;

        var results = await Task.WhenAll(devices.Select(device =>
            SendToDeviceAsync(new SendNotificationDto
            {
                DeviceToken = device.DeviceToken,
                TitleAr     = titleAr,
                TitleEn     = titleEn,
                BodyAr      = bodyAr,
                BodyEn      = bodyEn,
                Data        = data ?? new()
            })
        ));

        return results.Any(r => r);
    }

    public async Task<bool> SendToUserAsync(
        string userId,
        string titleAr, string titleEn,
        string bodyAr,  string bodyEn,
        Dictionary<string, string>? data = null)
    {
        var devices = await _db.UserDevices
            .IgnoreQueryFilters().Where(d => d.UserId == userId)
            .ToListAsync();

        if (!devices.Any()) return false;

        var results = await Task.WhenAll(devices.Select(device =>
            SendToDeviceAsync(new SendNotificationDto
            {
                DeviceToken = device.DeviceToken,
                TitleAr     = titleAr,
                TitleEn     = titleEn,
                BodyAr      = bodyAr,
                BodyEn      = bodyEn,
                Data        = data ?? new()
            })
        ));

        return results.Any(r => r);
    }

 public async Task<bool> SendToAllParentsAsync(
        string titleAr, string titleEn,
        string bodyAr,  string bodyEn,
        Dictionary<string, string>? data = null)
    {
        var devices = await _db.UserDevices
            .IgnoreQueryFilters().Include(d => d.User)
            .Where(d => d.User.RoleType == "Parent")
            .ToListAsync();

        if (!devices.Any()) return false;

        var results = await Task.WhenAll(devices.Select(device =>
            SendToDeviceAsync(new SendNotificationDto
            {
                DeviceToken = device.DeviceToken,
                TitleAr     = titleAr,
                TitleEn     = titleEn,
                BodyAr      = bodyAr,
                BodyEn      = bodyEn,
                Data        = data ?? new()
            })
        ));

        return results.Any(r => r);
    }

    public async Task<bool> SendTemplatedAsync(string key, string userId, Dictionary<string, string> replacements, Dictionary<string, string>? data = null)
    {
        var template = await _db.NotificationTemplates
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Key == key && t.IsActive);

        string titleAr, titleEn, bodyAr, bodyEn;
        if (template != null)
        {
            titleAr = template.TitleAr;
            titleEn = template.TitleEn;
            bodyAr  = template.BodyAr;
            bodyEn  = template.BodyEn;
        }
        else
        {
 // Fallback defaults if no custom template configured for this tenant
            var defaults = DefaultTemplates.Get(key);
            titleAr = defaults.titleAr;
            titleEn = defaults.titleEn;
            bodyAr  = defaults.bodyAr;
            bodyEn  = defaults.bodyEn;
        }

        foreach (var kv in replacements)
        {
            bodyAr = bodyAr.Replace("{" + kv.Key + "}", kv.Value);
            bodyEn = bodyEn.Replace("{" + kv.Key + "}", kv.Value);
        }

        return await SendToUserAsync(userId, titleAr, titleEn, bodyAr, bodyEn, data);
    }
}

public static class DefaultTemplates
{
    public static (string titleAr, string titleEn, string bodyAr, string bodyEn) Get(string key)
    {
 return key switch
        {
            "payment_confirmed" => ("تم تأكيد الدفع \u2705", "Payment Confirmed \u2705",
                "تم استلام دفعة بمبلغ {amount} ريال لاشتراك {childName}",
                "Payment of {amount} SAR received for {childName}'s subscription"),
            "subscription_created" => ("تم تسجيل اشتراك جديد", "New Subscription Registered",
                "تم تسجيل اشتراك {type} لـ {childName} بقيمة {price} ريال",
                "A {type} subscription has been registered for {childName} for {price} SAR"),
            "attendance_marked" => ("تحديث حالة الحضور", "Attendance Status Update",
                "تم تسجيل حالة طفلك {childName}: {status}",
                "Your child {childName}'s attendance status: {status}"),
            "leave_request_submitted" => ("طلب إذن جديد", "New Leave Request",
                "{employeeName} طلب إذناً لمدة {hours} ساعة",
                "{employeeName} requested {hours}h leave"),
            "leave_request_reviewed" => ("تحديث حالة طلب الإذن", "Leave Request Update",
                "تم {statusAr} طلب الإذن الخاص بك",
                "Your leave request has been {statusEn}"),
            "subscription_cancelled" => ("تم إلغاء الاشتراك", "Subscription Cancelled",
                "تم إلغاء اشتراك {type} الخاص بـ {childName}",
                "The {type} subscription for {childName} has been cancelled"),
            "trip_started" => ("بدأت الرحلة", "Trip Started",
                "السائق في الطريق إليكم",
                "The driver is on the way"),
            "trip_ended" => ("انتهت الرحلة", "Trip Completed",
                "تمت الرحلة بنجاح",
                "The trip has been completed successfully"),
            "child_registered" => ("تم تسجيل طفلك", "Child Registered",
                "تم تسجيل {childName} بنجاح في الروضة",
                "{childName} has been successfully registered at the kindergarten"),
            _ => ("إشعار", "Notification", "{message}", "{message}")
        };
    }
}
