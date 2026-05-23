namespace Kindergarten.Core.Entities;

public class Permission
{
    public int    Id             { get; set; }
    public string Name           { get; set; } = string.Empty;
    public string DisplayNameAr  { get; set; } = string.Empty;
    public string DisplayNameEn  { get; set; } = string.Empty;
    public string DescriptionAr  { get; set; } = string.Empty;
    public string DescriptionEn  { get; set; } = string.Empty;
    public string Category       { get; set; } = string.Empty;

    public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}
