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
public sealed class CategoriesController : ControllerBase
{
    private const string CacheKey = "lookups:categories";
    private readonly InventoryDbContext _db;
    private readonly IMemoryCache _cache;

    public CategoriesController(InventoryDbContext db, IMemoryCache cache)
    {
        _db = db; _cache = cache;
    }

    // GET /api/categories?search=&page=1&pageSize=10
    [HttpGet]
    public async Task<ActionResult<PagedResult<Category>>> List([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page <= 0) page = 1; if (pageSize <= 0 || pageSize > 200) pageSize = 10;

        IQueryable<Category> q = _db.Categories.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(c => c.Name.Contains(s) || (c.Code != null && c.Code.Contains(s)));
        }

        var total = await q.CountAsync();
        var items = await q.OrderBy(c => c.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Ok(new PagedResult<Category> { Page = page, PageSize = pageSize, Total = total, Items = items });
    }

    // GET /api/categories/lookups (cached)
    [HttpGet("lookups")]
    [AllowAnonymous] // nëse do ta përdorë front-i pa auth; ndryshe hiqe
    public async Task<ActionResult<IReadOnlyList<LookupItem>>> Lookups()
    {
        var items = await _cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return await _db.Categories.AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new LookupItem { Id = c.Id, Name = c.Name })
                .ToListAsync();
        });
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Category>> Get(int id)
        => await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id) is { } cat ? Ok(cat) : NotFound();

    [HttpPost]
    public async Task<ActionResult<Category>> Create([FromBody] CategoryUpsertRequest req)
    {
        var c = new Category { Name = req.Name, Code = req.Code };
        _db.Add(c);
        await _db.SaveChangesAsync();
        _cache.Remove(CacheKey);
        return CreatedAtAction(nameof(Get), new { id = c.Id }, c);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Category>> Update(int id, [FromBody] CategoryUpsertRequest req)
    {
        var c = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();
        c.Name = req.Name; c.Code = req.Code;
        await _db.SaveChangesAsync();
        _cache.Remove(CacheKey);
        return Ok(c);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();
        _db.Remove(c);
        await _db.SaveChangesAsync();
        _cache.Remove(CacheKey);
        return NoContent();
    }
}
