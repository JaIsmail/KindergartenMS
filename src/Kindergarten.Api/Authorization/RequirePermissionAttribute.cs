using Microsoft.AspNetCore.Authorization;

namespace Kindergarten.Api.Authorization;

public class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission)
        : base(permission)
    {
    }
}
