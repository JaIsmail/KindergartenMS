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
}
