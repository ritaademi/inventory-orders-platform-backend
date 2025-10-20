using Inventory.Domain.Products;

namespace Inventory.Api.Services
{
    public interface IStockMovementService
    {
        Task<StockMovement> CreateAsync(int stockItemId, int quantityChange, string? note, CancellationToken ct = default);
        Task<IReadOnlyList<StockMovement>> ListAsync(int stockItemId, int page = 1, int pageSize = 50, CancellationToken ct = default);
    }
}
