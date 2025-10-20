using Inventory.Api.Models.Requests;
using Inventory.Domain.Products;                // entiteti është në Domain
using Inventory.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class ProductsController : ControllerBase
    {
        private readonly InventoryDbContext _db;

        public ProductsController(InventoryDbContext db) => _db = db;

        // GET /api/products?search=&category=&page=1&pageSize=10&sortBy=Name&desc=false
        [HttpGet]
        [Authorize(Policy = "ManageInventory")]
        public async Task<ActionResult<PagedResult<ProductResponse>>> Get(
            [FromQuery] string? search,
            [FromQuery] string? category,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string sortBy = "Name",
            [FromQuery] bool desc = false)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 200) pageSize = 10;

            IQueryable<StockItem> q = _db.Set<StockItem>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                q = q.Where(x =>
                    x.Name.Contains(s) ||
                    (x.Sku != null && x.Sku.Contains(s)) ||
                    (x.Category != null && x.Category.Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                var c = category.Trim();
                q = q.Where(x => x.Category != null && x.Category == c);
            }

            q = (sortBy?.ToLowerInvariant()) switch
            {
                "price" or "unitprice" => desc ? q.OrderByDescending(x => x.UnitPrice) : q.OrderBy(x => x.UnitPrice),
                "quantity"             => desc ? q.OrderByDescending(x => x.Quantity)  : q.OrderBy(x => x.Quantity),
                "sku"                  => desc ? q.OrderByDescending(x => x.Sku)       : q.OrderBy(x => x.Sku),
                "category"             => desc ? q.OrderByDescending(x => x.Category)  : q.OrderBy(x => x.Category),
                _                      => desc ? q.OrderByDescending(x => x.Name)      : q.OrderBy(x => x.Name)
            };

            var total = await q.CountAsync();
            var items = await q.Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new ProductResponse
                {
                    Id = x.Id,                     // <-- INT
                    Name = x.Name,
                    Quantity = x.Quantity,
                    Sku = x.Sku,
                    Category = x.Category,
                    UnitPrice = x.UnitPrice
                }).ToListAsync();

            return Ok(new PagedResult<ProductResponse>
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items
            });
        }

        // GET /api/products/{id}
        [HttpGet("{id:int}")]                        // <-- INT route
        [Authorize(Policy = "ManageInventory")]
        public async Task<ActionResult<ProductResponse>> GetById(int id)
        {
            var e = await _db.Set<StockItem>().AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (e is null) return NotFound();
            return Ok(ToResponse(e));
        }

        // POST /api/products
        [HttpPost]
        [Authorize(Policy = "ManageInventory")]
        public async Task<ActionResult<ProductResponse>> Create([FromBody] ProductCreateRequest req)
        {
            var e = new StockItem
            {
                Name = req.Name,
                Quantity = req.Quantity,
                Sku = req.Sku,
                Category = req.Category,
                UnitPrice = req.UnitPrice,
                IsDeleted = false
            };

            _db.Add(e);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = e.Id }, ToResponse(e)); // e.Id është int
        }

        // PUT /api/products/{id}
        [HttpPut("{id:int}")]                        // <-- INT route
        [Authorize(Policy = "ManageInventory")]
        public async Task<ActionResult<ProductResponse>> Update(int id, [FromBody] ProductUpdateRequest req)
        {
            var e = await _db.Set<StockItem>().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (e is null) return NotFound();

            e.Name = req.Name;
            e.Quantity = req.Quantity;
            e.Sku = req.Sku;
            e.Category = req.Category;
            e.UnitPrice = req.UnitPrice;

            await _db.SaveChangesAsync();
            return Ok(ToResponse(e));
        }

        // DELETE (soft) /api/products/{id}
        [HttpDelete("{id:int}")]                     // <-- INT route
        [Authorize(Policy = "ManageInventory")]
        public async Task<IActionResult> SoftDelete(int id)
        {
            var e = await _db.Set<StockItem>().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (e is null) return NotFound();

            e.IsDeleted = true;
            e.DeletedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // POST /api/products/{id}/restore
        [HttpPost("{id:int}/restore")]               // <-- INT route
        [Authorize(Policy = "ManageInventory")]
        public async Task<IActionResult> Restore(int id)
        {
            var e = await _db.Set<StockItem>().FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted);
            if (e is null) return NotFound();

            e.IsDeleted = false;
            e.DeletedAt = null;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        private static ProductResponse ToResponse(StockItem x) => new()
        {
            Id = x.Id,                                 // <-- INT
            Name = x.Name,
            Quantity = x.Quantity,
            Sku = x.Sku,
            Category = x.Category,
            UnitPrice = x.UnitPrice
        };
    }
}
