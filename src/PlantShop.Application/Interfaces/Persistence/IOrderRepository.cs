using PlantShop.Domain.Entities.Shop;

namespace PlantShop.Application.Interfaces.Persistence;

public interface IOrderRepository
{    
    Task<Order?> GetOrderDetailsAsync(int id, CancellationToken cancellationToken = default);
        
    Task<IEnumerable<Order>> GetOrdersByUserIdAsync(string userId, CancellationToken cancellationToken = default);
        
    Task<IEnumerable<Order>> GetAllOrdersAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Order order, CancellationToken cancellationToken = default);

    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);

    Task DeleteAsync(Order order, CancellationToken cancellationToken = default);

    Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
