namespace Kindergarten.Core.Entities;

public class PermissionGroupPermission
{
    public int GroupId      { get; set; }
    public int PermissionId { get; set; }

    public PermissionGroup Group      { get; set; } = null!;
    public Permission      Permission { get; set; } = null!;
}
