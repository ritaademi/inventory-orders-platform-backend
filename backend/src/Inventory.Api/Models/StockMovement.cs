using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Api.Models
{
    public class StockMovement
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StockItemId { get; set; }

        [ForeignKey("StockItemId")]
        public StockItem StockItem { get; set; }

        [Required]
        [EnumDataType(typeof(MovementType))]
        public string Type { get; set; } // IN, OUT, TRANSFER

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity cannot be negative")]
        public int Quantity { get; set; }

        public DateTime Date { get; set; } = DateTime.UtcNow;
    }

    public enum MovementType
    {
        IN,
        OUT,
        TRANSFER
    }
}
