using Kindergarten.Core.DTOs;

namespace Kindergarten.Core.Interfaces;

public interface INotificationService
{
    Task<bool> SendToDeviceAsync(SendNotificationDto dto);
    Task<bool> SendToParentAsync(string parentId, string titleAr, string titleEn, string bodyAr, string bodyEn, Dictionary<string, string>? data = null);
    Task<bool> SendToUserAsync(string userId, string titleAr, string titleEn, string bodyAr, string bodyEn, Dictionary<string, string>? data = null);
    Task<bool> SendToAllParentsAsync(string titleAr, string titleEn, string bodyAr, string bodyEn, Dictionary<string, string>? data = null);
}
