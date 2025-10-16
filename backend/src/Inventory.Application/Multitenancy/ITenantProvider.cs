// backend/src/Inventory.Application/Multitenancy/ITenantProvider.cs
namespace Inventory.Application.Multitenancy;
public interface ITenantProvider
{
    Guid Id { get; }
    bool HasTenant { get; }
    void Set(Guid tenantId, string? name = null);
}
