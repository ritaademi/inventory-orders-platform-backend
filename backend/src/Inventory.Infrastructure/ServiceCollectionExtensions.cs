using Inventory.Application.Multitenancy;
using Inventory.Infrastructure.Multitenancy;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<TenantSaveChangesInterceptor>();

        services.AddDbContext<InventoryDbContext>((sp, opts) =>
        {
            var cs = cfg.GetConnectionString("DefaultConnection")
                     ?? "Host=localhost;Port=5432;Database=inventory;Username=postgres;Password=ritaademi";
            opts.UseNpgsql(cs);
            opts.AddInterceptors(sp.GetRequiredService<TenantSaveChangesInterceptor>());
        });

        // regjistro shërbime të tjera (p.sh. IStockMovementService, etj.)

        return services;
    }
}
