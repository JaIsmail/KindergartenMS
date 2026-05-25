namespace Kindergarten.Core.Entities;

public class PermissionGroup
{
    public int    Id          { get; set; }
    public string NameAr      { get; set; } = string.Empty;
    public string NameEn      { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int    TenantId    { get; set; } = 1;
    public bool   IsActive    { get; set; } = true;
    public string RoleType    { get; set; } = string.Empty; // Admin, Parent, Driver, Employee, Accountant, Supervisor
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PermissionGroupPermission> GroupPermissions { get; set; } = new List<PermissionGroupPermission>();
    public ICollection<UserPermissionGroup>       UserGroups       { get; set; } = new List<UserPermissionGroup>();
}
