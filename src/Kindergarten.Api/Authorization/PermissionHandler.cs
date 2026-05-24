using System.Security.Claims;
using Kindergarten.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Kindergarten.Api.Authorization;

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ApplicationDbContext _db;

    public PermissionHandler(ApplicationDbContext db)
    {
        _db = db;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.IsInRole("SuperAdmin"))
        {
            context.Succeed(requirement);
            return;
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            context.Fail();
            return;
        }

        var hasPermission = await _db.UserPermissions
            .Include(up => up.Permission)
            .AnyAsync(up => up.UserId == userId &&
                           up.Permission.Name == requirement.Permission);

        if (hasPermission)
            context.Succeed(requirement);
        else
            context.Fail();
    }
}
