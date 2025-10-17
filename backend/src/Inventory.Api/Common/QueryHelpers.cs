using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Inventory.Api.Common
{
    public record ListQuery(string? Search, int Page = 1, int PageSize = 10, string? SortBy = null, string? SortDir = "asc");
    public record PagedResult<T>(IEnumerable<T> Items, int Total);

    public static class QueryHelpers
    {
        public static IQueryable<T> ApplySearch<T>(this IQueryable<T> q, string? term, params Expression<Func<T, string>>[] fields)
        {
            if (string.IsNullOrWhiteSpace(term)) return q;
            var like = $"%{term.Trim()}%";
            var param = Expression.Parameter(typeof(T), "x");
            Expression? body = null;

            foreach (var f in fields)
            {
                var call = Expression.Call(
                    typeof(NpgsqlDbFunctionsExtensions),
                    nameof(NpgsqlDbFunctionsExtensions.ILike),
                    Type.EmptyTypes,
                    Expression.Constant(EF.Functions),
                    Expression.Invoke(f, param),
                    Expression.Constant(like)
                );
                body = body == null ? call : Expression.OrElse(body, call);
            }

            return body == null ? q : q.Where(Expression.Lambda<Func<T, bool>>(body, param));
        }

        public static IQueryable<T> ApplySort<T>(this IQueryable<T> q, string? by, string? dir, Dictionary<string, Expression<Func<T, object>>> map)
        {
            if (string.IsNullOrWhiteSpace(by) || !map.TryGetValue(by, out var expr)) return q;
            var asc = string.Equals(dir, "asc", StringComparison.OrdinalIgnoreCase);
            return asc ? q.OrderBy(expr) : q.OrderByDescending(expr);
        }

        public static async Task<PagedResult<T>> ToPagedAsync<T>(this IQueryable<T> q, int page, int size, CancellationToken ct)
        {
            var total = await q.CountAsync(ct);
            var items = await q.Skip((page - 1) * size).Take(size).ToListAsync(ct);
            return new(items, total);
        }
    }
}
