namespace Kindergarten.Api.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var tenantIdClaim = context.User?.FindFirst("TenantId")?.Value;
        if (!string.IsNullOrEmpty(tenantIdClaim) && int.TryParse(tenantIdClaim, out var tenantId))
        {
            context.Items["TenantId"] = tenantId;
        }
        else
        {
            context.Items["TenantId"] = 1;
        }
        await _next(context);
    }
}
