using Inventory.Domain.Common;

namespace Inventory.Domain.Catalog;

public class UnitOfMeasure : AuditableEntity, ITenantScoped
{
    public string Code { get; set; } = default!;    // EA, KG, L
    public string Name { get; set; } = default!;
    public int Precision { get; set; } = 0;
    public Guid TenantId { get; set; }
}