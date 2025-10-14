using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Queries;

public static class QueryableExtensions
{
    public static async Task<PagedResult<T>> ToPagedAsync<T>(this IQueryable<T> query, int page, int size, CancellationToken ct = default)
    {
        page = page <= 0 ? 1 : page;
        size = size <= 0 ? 10 : size;
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return new PagedResult<T> { Page = page, Size = size, Total = total, Items = items };
    }
}