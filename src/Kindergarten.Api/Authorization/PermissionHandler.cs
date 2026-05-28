using System.Security.Claims;
using System.Text.Json;
using Kindergarten.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Kindergarten.Api.Authorization;

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ApplicationDbContext _db;
    public PermissionHandler(ApplicationDbContext db) => _db = db;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // SuperAdmin bypasses all checks
        if (context.User.IsInRole("SuperAdmin"))
        {
            context.Succeed(requirement);
            return;
        }

// Check permission claims in JWT (impersonation token or direct grants)
        var permClaims = context.User.FindAll("Permission").ToList();
        foreach (var claim in permClaims)
        {
            // Single value claim
            if (claim.Value == requirement.Permission)
            {
                context.Succeed(requirement);
                return;
            }
            // Array value claim (JSON array)
            if (claim.Value.StartsWith("["))
            {
                try
                {
                    var perms = JsonSerializer.Deserialize<List<string>>(claim.Value);
                    if (perms != null && perms.Contains(requirement.Permission))
                    {
                        context.Succeed(requirement);
                        return;
                    }
                }
                catch { }
            }
        }

        // Admin role bypasses all permission checks within their tenant
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return;
        }

      // Check UserPermissions table
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
