// backend/src/Inventory.Domain/Common/TenantEntity.cs
namespace Inventory.Domain.Common;
public abstract class TenantEntity
{
    public Guid TenantId { get; set; }
}
