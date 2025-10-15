using Inventory.Domain.Common;

namespace Inventory.Domain.Catalog;

public class Category : AuditableEntity, ITenantScoped
{
    public string Name { get; set; } = default!;
    public Guid? ParentId { get; set; }
    public Guid TenantId { get; set; }
}