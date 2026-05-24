namespace Kindergarten.Core.Entities;

public class RoleGroupPermission
{
    public int RoleGroupId  { get; set; }
    public int PermissionId { get; set; }

    public RoleGroup  RoleGroup  { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}
