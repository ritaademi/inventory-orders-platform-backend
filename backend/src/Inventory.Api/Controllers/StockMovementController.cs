using Inventory.Api.Services;
using Inventory.Domain.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "ManageInventory")]
    public sealed class StockMovementController : ControllerBase
    {
        private readonly IStockMovementService _svc;

        public StockMovementController(IStockMovementService svc) => _svc = svc;

        // GET /api/stockmovement?stockItemId=1&page=1&pageSize=50
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<StockMovement>>> List([FromQuery] int stockItemId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var list = await _svc.ListAsync(stockItemId, page, pageSize, HttpContext.RequestAborted);
            return Ok(list);
        }

        // POST /api/stockmovement
        [HttpPost]
        public async Task<ActionResult<StockMovement>> Create([FromBody] CreateStockMovementRequest req)
        {
            var created = await _svc.CreateAsync(req.StockItemId, req.QuantityChange, req.Note, HttpContext.RequestAborted);
            return CreatedAtAction(nameof(List), new { stockItemId = created.StockItemId }, created);
        }

        public sealed class CreateStockMovementRequest
        {
            public int StockItemId { get; set; }
            public int QuantityChange { get; set; } // +in, -out
            public string? Note { get; set; }
        }
    }
}
