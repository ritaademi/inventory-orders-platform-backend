using Inventory.Api.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Inventory.Api.Services
{
    public interface IStockMovementService
    {
        Task<StockMovement> CreateStockMovementAsync(StockMovement movement);
        Task<List<StockMovement>> GetAllAsync();
        Task<StockMovement> GetByIdAsync(int id);
    }
}
