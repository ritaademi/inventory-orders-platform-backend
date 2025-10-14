namespace Inventory.Infrastructure.Queries;

public sealed class PagedResult<T>
{
    public int Page { get; init; }
    public int Size { get; init; }
    public int Total { get; init; }
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
}