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
    public class UomsController : ControllerBase
    {
        private readonly InventoryDbContext _db;
        public UomsController(InventoryDbContext db) => _db = db;

        [HttpGet]
        public async Task<ActionResult<PagedResult<UnitOfMeasure>>> List([FromQuery] ListQuery q, CancellationToken ct)
        {
            var sort = new Dictionary<string, Expression<Func<UnitOfMeasure, object>>>
            {
                ["name"] = x => x.Name,
                ["code"] = x => x.Code,
                ["createdAt"] = x => x.CreatedAt
            };

            var qry = _db.Uoms
                .ApplySearch(q.Search, x => x.Name, x => x.Code)
                .ApplySort(q.SortBy, q.SortDir, sort);

            return Ok(await qry.ToPagedAsync(q.Page, q.PageSize, ct));
        }

        public record UpsertUom(string Name, string Code, int Precision = 0);

        [HttpPost]
        public async Task<ActionResult<UnitOfMeasure>> Create([FromBody] UpsertUom req, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Code))
                return BadRequest(new { message = "Name and Code are required." });

            var e = new UnitOfMeasure { Name = req.Name.Trim(), Code = req.Code.Trim(), Precision = req.Precision };
            _db.Uoms.Add(e);
            await _db.SaveChangesAsync(ct);
            return Ok(e);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<UnitOfMeasure>> Update(Guid id, [FromBody] UpsertUom req, CancellationToken ct)
        {
            var e = await _db.Uoms.FindAsync([id], ct);
            if (e is null) return NotFound();
            e.Name = req.Name.Trim();
            e.Code = req.Code.Trim();
            e.Precision = req.Precision;
            await _db.SaveChangesAsync(ct);
            return Ok(e);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var e = await _db.Uoms.FindAsync([id], ct);
            if (e is null) return NoContent();
            _db.Uoms.Remove(e);
            await _db.SaveChangesAsync(ct);
            return NoContent();
        }
    }
}
