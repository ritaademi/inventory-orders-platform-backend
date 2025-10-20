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
public sealed class CustomersController : ControllerBase
{
    private readonly InventoryDbContext _db;
    public CustomersController(InventoryDbContext db) => _db = db;

    // GET /api/customers?search=&page=1&pageSize=10
    [HttpGet]
    public async Task<ActionResult<object>> List([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page <= 0) page = 1; if (pageSize <= 0 || pageSize > 200) pageSize = 10;

        IQueryable<Customer> q = _db.Set<Customer>().AsNoTracking();
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
    public async Task<ActionResult<Customer>> Get(int id)
        => await _db.Set<Customer>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id) is { } c ? Ok(c) : NotFound();

    [HttpPost]
    public async Task<ActionResult<Customer>> Create([FromBody] CustomerUpsertRequest req)
    {
        var c = new Customer { Name = req.Name, Email = req.Email, Phone = req.Phone };
        _db.Add(c); await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = c.Id }, c);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Customer>> Update(int id, [FromBody] CustomerUpsertRequest req)
    {
        var c = await _db.Set<Customer>().FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();
        c.Name = req.Name; c.Email = req.Email; c.Phone = req.Phone;
        await _db.SaveChangesAsync();
        return Ok(c);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await _db.Set<Customer>().FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();
        _db.Remove(c); await _db.SaveChangesAsync();
        return NoContent();
    }
}
