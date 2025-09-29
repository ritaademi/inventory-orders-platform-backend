using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Infrastructure.Tenancy
{
    public sealed class TenantResolverMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantResolverMiddleware(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext ctx, ITenantContext tenantCtx)
        {
            if (ctx.Request.Headers.TryGetValue("X-Tenant-Id", out var values))
            {
                if (Guid.TryParse(values.FirstOrDefault(), out var tid))
                    tenantCtx.Set(tid);
            }
            await _next(ctx);
        }
    }
}
