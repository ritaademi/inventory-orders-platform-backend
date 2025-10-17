using Inventory.Api.Common;
using Inventory.Domain.Catalog;
using Inventory.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;

namespace Inventory.Api.Controllers.Catalog
{
    [ApiController]
    [Route("api/catalog/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly InventoryDbContext _db;
        public CategoriesController(InventoryDbContext db) => _db = db;

        [HttpGet]
        public async Task<ActionResult<PagedResult<Category>>> List([FromQuery] ListQuery q, CancellationToken ct)
        {
            var sort = new Dictionary<string, Expression<Func<Category, object>>>
            {
                ["name"] = x => x.Name,
                ["createdAt"] = x => x.CreatedAt
            };

            var qry = _db.Categories
                .ApplySearch(q.Search, x => x.Name)
                .ApplySort(q.SortBy, q.SortDir, sort);

            return Ok(await qry.ToPagedAsync(q.Page, q.PageSize, ct));
        }

        public record UpsertCategory(string Name, Guid? ParentId);

        [HttpPost]
        public async Task<ActionResult<Category>> Create([FromBody] UpsertCategory req, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { message = "Name is required." });
            var e = new Category { Name = req.Name.Trim(), ParentId = req.ParentId };
            _db.Categories.Add(e);
            await _db.SaveChangesAsync(ct);
            return Ok(e);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<Category>> Update(Guid id, [FromBody] UpsertCategory req, CancellationToken ct)
        {
            var e = await _db.Categories.FindAsync([id], ct);
            if (e is null) return NotFound();
            e.Name = req.Name.Trim();
            e.ParentId = req.ParentId;
            await _db.SaveChangesAsync(ct);
            return Ok(e);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var e = await _db.Categories.FindAsync([id], ct);
            if (e is null) return NoContent();
            _db.Categories.Remove(e);
            await _db.SaveChangesAsync(ct);
            return NoContent();
        }
    }
}
