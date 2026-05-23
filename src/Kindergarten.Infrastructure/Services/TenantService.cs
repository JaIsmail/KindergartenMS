using Kindergarten.Core.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Kindergarten.Infrastructure.Services
{
    public class TenantService : ITenantService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TenantService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int GetTenantId()
        {
            var claim = _httpContextAccessor.HttpContext?.User?
                .FindFirst("TenantId")?.Value;

            return int.TryParse(claim, out var tenantId) ? tenantId : 1;
        }
    }
}
