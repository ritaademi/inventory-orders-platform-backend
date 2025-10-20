namespace Inventory.Api.Models.Requests
{
    public sealed class ProductCreateRequest
    {
        public required string Name { get; set; }
        public int Quantity { get; set; }
        public string? Sku { get; set; }
        public string? Category { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public sealed class ProductUpdateRequest
    {
        public required string Name { get; set; }
        public int Quantity { get; set; }
        public string? Sku { get; set; }
        public string? Category { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public sealed class ProductResponse
    {
        public int Id { get; set; }                 // <-- INT
        public string Name { get; set; } = default!;
        public int Quantity { get; set; }
        public string? Sku { get; set; }
        public string? Category { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public sealed class PagedResult<T>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    }
}
