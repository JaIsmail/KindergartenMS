namespace Kindergarten.Core.Entities;

public class ApplicationUser
{
    public string Id           { get; set; } = Guid.NewGuid().ToString();
    public string UserName     { get; set; } = string.Empty;
    public string Email        { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public string? PhoneNumber  { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string Address  { get; set; } = string.Empty;
    public string RoleType { get; set; } = string.Empty;
    public int    TenantId { get; set; } = 1;
    public Tenant? Tenant  { get; set; }
}
