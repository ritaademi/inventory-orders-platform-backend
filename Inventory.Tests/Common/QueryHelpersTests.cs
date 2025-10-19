using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Tests.Common
{
    public static class QueryHelpers
    {
        public static IQueryable<T> ApplySearch<T>(this IQueryable<T> q, string? term, Func<T, string> accessor)
            => string.IsNullOrWhiteSpace(term) ? q : q.Where(x => accessor(x).Contains(term, StringComparison.OrdinalIgnoreCase)).AsQueryable();
    }

    public class QueryHelpersTests
    {
        private record Item(string Name);

        [Fact]
        public void ApplySearch_filters_by_term()
        {
            var data = new[] { new Item("apple"), new Item("banana") }.AsQueryable();
            var filtered = QueryHelpers.ApplySearch(data, "app", x => x.Name).ToList();

            filtered.Should().HaveCount(1);
            filtered[0].Name.Should().Be("apple");
        }
    }
}
