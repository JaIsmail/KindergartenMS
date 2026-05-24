namespace Kindergarten.Core.Entities;

public class UserPermissionGroup
{
    public int    Id        { get; set; }
    public string UserId    { get; set; } = string.Empty;
    public int    GroupId   { get; set; }
    public int    TenantId  { get; set; } = 1;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public string AssignedBy   { get; set; } = string.Empty;

    public ApplicationUser  User  { get; set; } = null!;
    public PermissionGroup  Group { get; set; } = null!;
}
