using System.ComponentModel.DataAnnotations;

namespace Inventory.Domain.Products
{
    public class StockItem
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = default!;

        [Range(0, int.MaxValue, ErrorMessage = "Quantity cannot be negative")]
        public int Quantity { get; set; }

        // Optional product fields
        [MaxLength(64)]
        public string? Sku { get; set; }

        [MaxLength(64)]
        public string? Category { get; set; }

        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        // Soft delete
        public bool IsDeleted { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
    }
}
