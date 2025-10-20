using Inventory.Api.Models.Requests;
using Inventory.Domain.Catalog;
using Inventory.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Inventory.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "ManageInventory")]
public sealed class UnitsController : ControllerBase
{
    private const string CacheKey = "lookups:uoms";
    private readonly InventoryDbContext _db;
    private readonly IMemoryCache _cache;

    public UnitsController(InventoryDbContext db, IMemoryCache cache)
    {
        _db = db; _cache = cache;
    }

    // GET /api/units?search=&page=1&pageSize=10
    [HttpGet]
    public async Task<ActionResult<PagedResult<UnitOfMeasure>>> List([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page <= 0) page = 1; if (pageSize <= 0 || pageSize > 200) pageSize = 10;

        IQueryable<UnitOfMeasure> q = _db.Uoms.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(u => u.Name.Contains(s) || (u.Symbol != null && u.Symbol.Contains(s)));
        }

        var total = await q.CountAsync();
        var items = await q.OrderBy(u => u.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Ok(new PagedResult<UnitOfMeasure> { Page = page, PageSize = pageSize, Total = total, Items = items });
    }

    // GET /api/units/lookups (cached)
    [HttpGet("lookups")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<LookupItem>>> Lookups()
    {
        var items = await _cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return await _db.Uoms.AsNoTracking()
                .OrderBy(u => u.Name)
                .Select(u => new LookupItem { Id = u.Id, Name = u.Name })
                .ToListAsync();
        });
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UnitOfMeasure>> Get(int id)
        => await _db.Uoms.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id) is { } u ? Ok(u) : NotFound();

    [HttpPost]
    public async Task<ActionResult<UnitOfMeasure>> Create([FromBody] UomUpsertRequest req)
    {
        var u = new UnitOfMeasure { Name = req.Name, Symbol = req.Symbol };
        _db.Add(u);
        await _db.SaveChangesAsync();
        _cache.Remove(CacheKey);
        return CreatedAtAction(nameof(Get), new { id = u.Id }, u);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UnitOfMeasure>> Update(int id, [FromBody] UomUpsertRequest req)
    {
        var u = await _db.Uoms.FirstOrDefaultAsync(x => x.Id == id);
        if (u is null) return NotFound();
        u.Name = req.Name; u.Symbol = req.Symbol;
        await _db.SaveChangesAsync();
        _cache.Remove(CacheKey);
        return Ok(u);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var u = await _db.Uoms.FirstOrDefaultAsync(x => x.Id == id);
        if (u is null) return NotFound();
        _db.Remove(u);
        await _db.SaveChangesAsync();
        _cache.Remove(CacheKey);
        return NoContent();
    }
}
