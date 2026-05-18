namespace Kindergarten.Core.Entities;

public class UserDevice
{
    public int    Id          { get; set; }
    public string UserId      { get; set; } = string.Empty;
    public string DeviceToken { get; set; } = string.Empty;
    public string Platform    { get; set; } = string.Empty; // Android, iOS
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ApplicationUser User { get; set; } = null!;
}
