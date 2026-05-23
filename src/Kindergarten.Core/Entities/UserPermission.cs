namespace Kindergarten.Core.Entities;

public class UserPermission
{
    public int      Id          { get; set; }
    public string   UserId      { get; set; } = string.Empty;
    public int      PermissionId { get; set; }
    public string   GrantedBy   { get; set; } = string.Empty;
    public int TenantId { get; set; } = 1;
    public DateTime GrantedAt   { get; set; } = DateTime.UtcNow;

    public ApplicationUser User       { get; set; } = null!;
    public Permission      Permission { get; set; } = null!;
}
