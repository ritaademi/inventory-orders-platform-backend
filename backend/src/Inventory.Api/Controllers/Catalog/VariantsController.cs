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
    public class VariantsController : ControllerBase
    {
        private readonly InventoryDbContext _db;
        public VariantsController(InventoryDbContext db) => _db = db;

        [HttpGet]
        public async Task<ActionResult<PagedResult<ProductVariant>>> List([FromQuery] ListQuery q, CancellationToken ct)
        {
            var sort = new Dictionary<string, Expression<Func<ProductVariant, object>>>
            {
                ["sku"] = x => x.Sku,
                ["createdAt"] = x => x.CreatedAt
            };

            var qry = _db.ProductVariants
                .ApplySearch(q.Search, x => x.Sku)
                .ApplySort(q.SortBy, q.SortDir, sort);

            return Ok(await qry.ToPagedAsync(q.Page, q.PageSize, ct));
        }

        public record UpsertVariant(Guid ProductId, string Sku, string? Barcode);

        [HttpPost]
        public async Task<ActionResult<ProductVariant>> Create([FromBody] UpsertVariant req, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(req.Sku))
                return BadRequest(new { message = "SKU is required." });

            var e = new ProductVariant
            {
                ProductId = req.ProductId,
                Sku = req.Sku.Trim(),
                Barcode = string.IsNullOrWhiteSpace(req.Barcode) ? null : req.Barcode.Trim()
            };

            _db.ProductVariants.Add(e);
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
        public async Task<ActionResult<ProductVariant>> Update(Guid id, [FromBody] UpsertVariant req, CancellationToken ct)
        {
            var e = await _db.ProductVariants.FindAsync([id], ct);
            if (e is null) return NotFound();
            e.ProductId = req.ProductId;
            e.Sku = req.Sku.Trim();
            e.Barcode = string.IsNullOrWhiteSpace(req.Barcode) ? null : req.Barcode.Trim();

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
            var e = await _db.ProductVariants.FindAsync([id], ct);
            if (e is null) return NoContent();
            _db.ProductVariants.Remove(e);
            await _db.SaveChangesAsync(ct);
            return NoContent();
        }
    }
}
