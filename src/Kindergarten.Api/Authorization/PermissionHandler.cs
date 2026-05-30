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

        // Check permission claims in JWT (impersonation token)
        var permClaims = context.User.FindAll("Permission").ToList();
        // Direct claim check
        if (permClaims.Any(pc => pc.Value == requirement.Permission))
        {
            context.Succeed(requirement);
            return;
        }
        foreach (var claim in permClaims)
        {
            if (claim.Value == requirement.Permission)
            {
                context.Succeed(requirement);
                return;
            }
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

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            context.Fail();
            return;
        }

        // Check direct UserPermissions table
        var hasPermission = await _db.UserPermissions
            .Include(up => up.Permission)
            .AnyAsync(up => up.UserId == userId &&
                           up.Permission.Name == requirement.Permission);
        if (hasPermission)
        {
            context.Succeed(requirement);
            return;
        }

        // Check permissions via PermissionGroups - simplified query
        var userGroupIds = await _db.UserPermissionGroups
            .IgnoreQueryFilters()
            .Where(upg => upg.UserId == userId)
            .Select(upg => upg.GroupId)
            .ToListAsync();

        if (userGroupIds.Any())
        {
            var hasGroupPermission = await _db.PermissionGroupPermissions
                .Include(pgp => pgp.Permission)
                .AnyAsync(pgp => userGroupIds.Contains(pgp.GroupId) &&
                                 pgp.Permission.Name == requirement.Permission);
            if (hasGroupPermission)
            {
                context.Succeed(requirement);
                return;
            }
        }

        context.Fail();
    }
}
