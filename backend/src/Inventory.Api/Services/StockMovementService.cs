using Inventory.Api.Data;
using Inventory.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Inventory.Api.Services
{
    public class StockMovementService : IStockMovementService
    {
        private readonly ApplicationDbContext _context;

        public StockMovementService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<StockMovement> CreateStockMovementAsync(StockMovement movement)
        {
            // Update StockItem quantity based on movement type
            var item = await _context.StockItems.FirstOrDefaultAsync(i => i.Id == movement.StockItemId);
            if (item == null)
            {
                throw new KeyNotFoundException("StockItem not found.");
            }

            if (movement.Type == "IN")
            {
                item.Quantity += movement.Quantity;
            }
            else if (movement.Type == "OUT")
            {
                if (item.Quantity < movement.Quantity)
                    throw new InvalidOperationException("Insufficient stock.");
                item.Quantity -= movement.Quantity;
            }
            else if (movement.Type == "TRANSFER")
            {
                // Future: Implement logic for transfer between warehouses
                throw new NotImplementedException("Transfer logic not implemented yet.");
            }

            _context.StockMovements.Add(movement);
            await _context.SaveChangesAsync();
            return movement;
        }

        public async Task<List<StockMovement>> GetAllAsync()
        {
            return await _context.StockMovements.Include(m => m.StockItem).ToListAsync();
        }

        public async Task<StockMovement> GetByIdAsync(int id)
        {
            return await _context.StockMovements.Include(m => m.StockItem)
                                                .FirstOrDefaultAsync(m => m.Id == id);
        }
    }
}
