using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Domain.Products
{
    public class StockMovement
    {
        [Key]
        public int Id { get; set; }

        // FK te StockItem
        [ForeignKey(nameof(StockItem))]
        public int StockItemId { get; set; }
        public StockItem StockItem { get; set; } = default!;

        // Sasia që shtohet (+) ose zbritet (-)
        public int QuantityChange { get; set; }

        [MaxLength(256)]
        public string? Note { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
