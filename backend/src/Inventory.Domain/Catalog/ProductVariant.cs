using Inventory.Domain.Common;

namespace Inventory.Domain.Catalog;

public class ProductVariant : AuditableEntity, ITenantScoped, ISoftDeletable
{
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = default!;       // optional override; unique per tenant if used
    public string? Barcode { get; set; }              // unique per tenant if set
    public string? AttributesJson { get; set; }       // e.g., {"size":"M","color":"red"}
    public bool IsActive { get; set; } = true;

    public Guid TenantId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}