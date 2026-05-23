using Microsoft.AspNetCore.Identity;

namespace Kindergarten.Core.Entities;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string RoleType { get; set; } = string.Empty;
    public int    TenantId  { get; set; } = 1;
    public Tenant? Tenant   { get; set; }
}
