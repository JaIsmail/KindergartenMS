namespace Kindergarten.Core.Entities;

public class UserRoleGroup
{
    public int    Id          { get; set; }
    public string UserId      { get; set; } = string.Empty;
    public int    RoleGroupId { get; set; }
    public string AssignedBy  { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser User      { get; set; } = null!;
    public RoleGroup       RoleGroup { get; set; } = null!;
}
