using Inventory.Api.Models.Requests;
using Inventory.Domain.Catalog;
using Inventory.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api.Controllers;

[ApiController]
[Route("api/products/{productId:int}/[controller]")]
[Authorize(Policy = "ManageInventory")]
public sealed class ProductVariantsController : ControllerBase
{
    private readonly InventoryDbContext _db;

    public ProductVariantsController(InventoryDbContext db) => _db = db;

    private async Task RecomputeProductPriceAsync(int productId, CancellationToken ct)
    {
        var prices = await _db.ProductVariants
            .Where(v => v.ProductId == productId)
            .Select(v => v.UnitPrice)
            .ToListAsync(ct);

        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId, ct);
        if (product is null) return;

        if (prices.Count > 0)
            product.UnitPrice = prices.Min();
        await _db.SaveChangesAsync(ct);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VariantResponse>>> List(int productId)
    {
        var exists = await _db.Products.AnyAsync(p => p.Id == productId);
        if (!exists) return NotFound();

        var items = await _db.ProductVariants.AsNoTracking()
            .Where(v => v.ProductId == productId)
            .OrderBy(v => v.Name)
            .Select(v => new VariantResponse
            {
                Id = v.Id, ProductId = v.ProductId, Name = v.Name, Sku = v.Sku, UnitPrice = v.UnitPrice, StockItemId = v.StockItemId
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VariantResponse>> Get(int productId, int id)
    {
        var v = await _db.ProductVariants.AsNoTracking().FirstOrDefaultAsync(x => x.ProductId == productId && x.Id == id);
        if (v is null) return NotFound();

        return Ok(new VariantResponse { Id = v.Id, ProductId = v.ProductId, Name = v.Name, Sku = v.Sku, UnitPrice = v.UnitPrice, StockItemId = v.StockItemId });
    }

    [HttpPost]
    public async Task<ActionResult<VariantResponse>> Create(int productId, [FromBody] VariantUpsertRequest req)
    {
        if (!await _db.Products.AnyAsync(p => p.Id == productId))
            return NotFound();

        var v = new ProductVariant
        {
            ProductId = productId,
            Name = req.Name,
            Sku = req.Sku,
            UnitPrice = req.UnitPrice,
            StockItemId = req.StockItemId
        };

        _db.Add(v);
        await _db.SaveChangesAsync();
        await RecomputeProductPriceAsync(productId, HttpContext.RequestAborted);

        return CreatedAtAction(nameof(Get), new { productId, id = v.Id }, new VariantResponse
        {
            Id = v.Id, ProductId = v.ProductId, Name = v.Name, Sku = v.Sku, UnitPrice = v.UnitPrice, StockItemId = v.StockItemId
        });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<VariantResponse>> Update(int productId, int id, [FromBody] VariantUpsertRequest req)
    {
        var v = await _db.ProductVariants.FirstOrDefaultAsync(x => x.ProductId == productId && x.Id == id);
        if (v is null) return NotFound();

        v.Name = req.Name; v.Sku = req.Sku; v.UnitPrice = req.UnitPrice; v.StockItemId = req.StockItemId;
        await _db.SaveChangesAsync();
        await RecomputeProductPriceAsync(productId, HttpContext.RequestAborted);

        return Ok(new VariantResponse
        {
            Id = v.Id, ProductId = v.ProductId, Name = v.Name, Sku = v.Sku, UnitPrice = v.UnitPrice, StockItemId = v.StockItemId
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int productId, int id)
    {
        var v = await _db.ProductVariants.FirstOrDefaultAsync(x => x.ProductId == productId && x.Id == id);
        if (v is null) return NotFound();

        _db.Remove(v);
        await _db.SaveChangesAsync();
        await RecomputeProductPriceAsync(productId, HttpContext.RequestAborted);

        return NoContent();
    }
}
