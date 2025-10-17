using Inventory.Api.Common;
using Inventory.Domain.Catalog;
using Inventory.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Linq.Expressions;

namespace Inventory.Api.Controllers.Catalog
{
    [ApiController]
    [Route("api/catalog/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly InventoryDbContext _db;
        public ProductsController(InventoryDbContext db) => _db = db;

        [HttpGet]
        public async Task<ActionResult<PagedResult<Product>>> List([FromQuery] ListQuery q, CancellationToken ct)
        {
            var sort = new Dictionary<string, Expression<Func<Product, object>>>
            {
                ["name"] = x => x.Name,
                ["sku"] = x => x.Sku,
                ["createdAt"] = x => x.CreatedAt
            };

            var qry = _db.Products
                .ApplySearch(q.Search, x => x.Name, x => x.Sku)
                .ApplySort(q.SortBy, q.SortDir, sort);

            return Ok(await qry.ToPagedAsync(q.Page, q.PageSize, ct));
        }

        public record UpsertProduct(string Name, string Sku, Guid? CategoryId, Guid UomId);

        [HttpPost]
        public async Task<ActionResult<Product>> Create([FromBody] UpsertProduct req, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Sku))
                return BadRequest(new { message = "Name and SKU are required." });

            var e = new Product
            {
                Name = req.Name.Trim(),
                Sku = req.Sku.Trim(),
                CategoryId = req.CategoryId,
                UomId = req.UomId
            };

            _db.Products.Add(e);
            try
            {
                await _db.SaveChangesAsync(ct);
                return Ok(e);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                return Conflict(new { message = "A product or variant with this SKU already exists." });
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<Product>> Update(Guid id, [FromBody] UpsertProduct req, CancellationToken ct)
        {
            var e = await _db.Products.FindAsync([id], ct);
            if (e is null) return NotFound();

            e.Name = req.Name.Trim();
            e.Sku = req.Sku.Trim();
            e.CategoryId = req.CategoryId;
            e.UomId = req.UomId;

            try
            {
                await _db.SaveChangesAsync(ct);
                return Ok(e);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                return Conflict(new { message = "A product or variant with this SKU already exists." });
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var e = await _db.Products.FindAsync([id], ct);
            if (e is null) return NoContent();
            _db.Products.Remove(e);
            await _db.SaveChangesAsync(ct);
            return NoContent();
        }
    }
}
