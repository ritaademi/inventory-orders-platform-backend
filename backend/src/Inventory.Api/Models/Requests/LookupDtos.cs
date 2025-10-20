namespace Inventory.Api.Models.Requests
{
    public sealed class CategoryUpsertRequest
    {
        public required string Name { get; set; }
        public string? Code { get; set; }
    }

    public sealed class UomUpsertRequest
    {
        public required string Name { get; set; }
        public string? Symbol { get; set; }
    }

    public sealed class LookupItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
    }

    public sealed class PagedResult<T>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    }
}
