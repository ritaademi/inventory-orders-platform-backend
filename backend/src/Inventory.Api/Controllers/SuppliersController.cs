using Inventory.Api.Models.Requests;
using Inventory.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Inventory.Domain.Catalog; // ose Inventory.Domain.Parties; vendose namespace-in real

namespace Inventory.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "ManageInventory")]
public sealed class SuppliersController : ControllerBase
{
    private readonly InventoryDbContext _db;
    public SuppliersController(InventoryDbContext db) => _db = db;

    // GET /api/suppliers?search=&page=1&pageSize=10
    [HttpGet]
    public async Task<ActionResult<object>> List([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page <= 0) page = 1; if (pageSize <= 0 || pageSize > 200) pageSize = 10;

        IQueryable<Supplier> q = _db.Set<Supplier>().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(x => x.Name.Contains(s) || (x.Email != null && x.Email.Contains(s)) || (x.Phone != null && x.Phone.Contains(s)));
        }

        var total = await q.CountAsync();
        var items = await q.OrderBy(x => x.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Ok(new { page, pageSize, total, items });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Supplier>> Get(int id)
        => await _db.Set<Supplier>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id) is { } s ? Ok(s) : NotFound();

    [HttpPost]
    public async Task<ActionResult<Supplier>> Create([FromBody] SupplierUpsertRequest req)
    {
        var s = new Supplier { Name = req.Name, Email = req.Email, Phone = req.Phone };
        _db.Add(s); await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = s.Id }, s);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Supplier>> Update(int id, [FromBody] SupplierUpsertRequest req)
    {
        var s = await _db.Set<Supplier>().FirstOrDefaultAsync(x => x.Id == id);
        if (s is null) return NotFound();
        s.Name = req.Name; s.Email = req.Email; s.Phone = req.Phone;
        await _db.SaveChangesAsync();
        return Ok(s);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var s = await _db.Set<Supplier>().FirstOrDefaultAsync(x => x.Id == id);
        if (s is null) return NotFound();
        _db.Remove(s); await _db.SaveChangesAsync();
        return NoContent();
    }
}
