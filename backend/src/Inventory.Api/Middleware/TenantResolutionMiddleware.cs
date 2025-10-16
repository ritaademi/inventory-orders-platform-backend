using System.Net;
using System.Text.Json;
using Inventory.Application.Multitenancy;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api.Middleware;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    public TenantResolutionMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext ctx, ITenantContext tenant, InventoryDbContext db)
    {
        if (!ctx.Request.Headers.TryGetValue("X-Tenant-ID", out var h) || string.IsNullOrWhiteSpace(h))
        {
            ctx.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Missing X-Tenant-ID" }));
            return;
        }

        if (!Guid.TryParse(h!, out var id))
        {
            ctx.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Invalid X-Tenant-ID" }));
            return;
        }

        var exists = await db.Tenants.AsNoTracking().AnyAsync(t => t.Id == id && t.IsActive);
        if (!exists)
        {
            ctx.Response.StatusCode = (int)HttpStatusCode.NotFound;
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Tenant not found" }));
            return;
        }

        tenant.Set(id);
        await _next(ctx);
    }
}
