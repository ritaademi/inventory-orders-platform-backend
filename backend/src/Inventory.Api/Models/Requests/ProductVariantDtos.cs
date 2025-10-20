namespace Inventory.Api.Models.Requests
{
    public sealed class VariantUpsertRequest
    {
        public required string Name { get; set; }
        public string? Sku { get; set; }
        public decimal UnitPrice { get; set; }
        public int? StockItemId { get; set; } // nëse lidhet me StockItem
    }

    public sealed class VariantResponse
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Name { get; set; } = default!;
        public string? Sku { get; set; }
        public decimal UnitPrice { get; set; }
        public int? StockItemId { get; set; }
    }
}
