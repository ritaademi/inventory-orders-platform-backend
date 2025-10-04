using Inventory.Api.Tenants;
using Inventory.Domain.Tenants;
using Inventory.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers
{
    [ApiController]
    [Route("api/tenants")]
    public class TenantsController : ControllerBase
    {
        private readonly InventoryDbContext _db;
        public TenantsController(InventoryDbContext db) => _db = db;

        [HttpPost]
        public async Task<ActionResult<Tenant>> Create([FromBody] CreateTenantRequest req, CancellationToken ct)
        {
            var t = new Tenant { Name = req.Name, Domain = req.Domain };
            _db.Tenants.Add(t);
            await _db.SaveChangesAsync(ct);
            return CreatedAtAction(nameof(GetById), new { id = t.Id }, t);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<Tenant>> GetById(Guid id, CancellationToken ct)
        {
            var t = await _db.Tenants.FindAsync([id], ct);
            return t is null ? NotFound() : Ok(t);
        }

        [HttpGet]
        public ActionResult<IEnumerable<Tenant>> List() => Ok(_db.Tenants.OrderBy(x => x.CreatedAt).ToList());
    }
}
