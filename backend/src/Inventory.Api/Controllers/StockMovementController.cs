using Inventory.Api.Models;
using Inventory.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Inventory.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StockMovementController : ControllerBase
    {
        private readonly IStockMovementService _service;

        public StockMovementController(IStockMovementService service)
        {
            _service = service;
        }

        // POST: api/stockmovement
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StockMovement movement)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var created = await _service.CreateStockMovementAsync(movement);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET: api/stockmovement
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _service.GetAllAsync();
            return Ok(list);
        }

        // GET: api/stockmovement/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var movement = await _service.GetByIdAsync(id);
            if (movement == null)
                return NotFound();
            return Ok(movement);
        }
    }
}
