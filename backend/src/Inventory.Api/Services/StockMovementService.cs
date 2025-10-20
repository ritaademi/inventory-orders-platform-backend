using Inventory.Domain.Products;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api.Services
{
    public sealed class StockMovementService : IStockMovementService
    {
        private readonly InventoryDbContext _db;

        public StockMovementService(InventoryDbContext db) => _db = db;

        public async Task<StockMovement> CreateAsync(int stockItemId, int quantityChange, string? note, CancellationToken ct = default)
        {
            // verifiko që StockItem ekziston dhe s’është soft-deleted
            var item = await _db.Set<StockItem>().FirstOrDefaultAsync(x => x.Id == stockItemId && !x.IsDeleted, ct);
            if (item is null)
                throw new KeyNotFoundException($"StockItem {stockItemId} not found.");

            var movement = new StockMovement
            {
                StockItemId = stockItemId,
                QuantityChange = quantityChange,
                Note = note,
                CreatedAt = DateTimeOffset.UtcNow
            };

            // përditëso edhe sasinë (nëse kështu e do logjika jote)
            item.Quantity += quantityChange;
            if (item.Quantity < 0) item.Quantity = 0;

            _db.Add(movement);
            await _db.SaveChangesAsync(ct);

            return movement;
        }

        public async Task<IReadOnlyList<StockMovement>> ListAsync(int stockItemId, int page = 1, int pageSize = 50, CancellationToken ct = default)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 200) pageSize = 50;

            return await _db.Set<StockMovement>()
                .AsNoTracking()
                .Where(m => m.StockItemId == stockItemId)
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);
        }
    }
}
