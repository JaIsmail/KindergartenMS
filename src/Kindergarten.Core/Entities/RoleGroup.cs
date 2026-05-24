namespace Kindergarten.Core.Entities;

public class RoleGroup
{
    public int     Id          { get; set; }
    public int     TenantId    { get; set; }
    public string  NameAr      { get; set; } = string.Empty;
    public string  NameEn      { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool    IsActive    { get; set; } = true;
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;

    public ICollection<RoleGroupPermission> RoleGroupPermissions { get; set; } = new List<RoleGroupPermission>();
    public ICollection<UserRoleGroup>       UserRoleGroups       { get; set; } = new List<UserRoleGroup>();
}
