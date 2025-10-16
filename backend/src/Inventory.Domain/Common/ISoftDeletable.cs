namespace Inventory.Domain.Common;

public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
}
