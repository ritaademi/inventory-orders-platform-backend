using Inventory.Domain.Common;

namespace Inventory.Domain.Catalog;

public class Product : AuditableEntity, ITenantScoped, ISoftDeletable
{
    public string Sku { get; set; } = default!;     // unique per tenant
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid UomId { get; set; }
    public bool IsActive { get; set; } = true;

    public Guid TenantId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}