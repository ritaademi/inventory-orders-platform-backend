using Inventory.Application.Multitenancy;

namespace Inventory.Infrastructure.Multitenancy;

public class TenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }
    public bool HasTenant => TenantId.HasValue;
    public void Set(Guid tenantId) => TenantId = tenantId;
}
