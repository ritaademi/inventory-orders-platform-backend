using Inventory.Application.Multitenancy;
using Inventory.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Inventory.Infrastructure.Persistence.Interceptors;

public class TenantSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ITenantContext _tenant;
    public TenantSaveChangesInterceptor(ITenantContext tenant) => _tenant = tenant;

    private void SetTenantIds(DbContext? ctx)
    {
        if (ctx is null || !_tenant.HasTenant) return;

        foreach (var e in ctx.ChangeTracker.Entries()
                 .Where(e => e.Entity is TenantEntity &&
                             (e.State == EntityState.Added || e.State == EntityState.Modified)))
        {
            ((TenantEntity)e.Entity).TenantId = _tenant.TenantId!.Value;
        }
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    { SetTenantIds(eventData.Context); return base.SavingChanges(eventData, result); }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken ct = default)
    { SetTenantIds(eventData.Context); return base.SavingChangesAsync(eventData, result, ct); }
}
