namespace Kindergarten.Core.DTOs;

public class PermissionResponseDto
{
    public int    Id            { get; set; }
    public string Name          { get; set; } = string.Empty;
    public string DisplayNameAr { get; set; } = string.Empty;
    public string DisplayNameEn { get; set; } = string.Empty;
    public string DescriptionAr { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
    public string Category      { get; set; } = string.Empty;
    public bool   IsGranted     { get; set; }
}

public class UpdateUserPermissionsDto
{
    public List<int> PermissionIds { get; set; } = new();
}
