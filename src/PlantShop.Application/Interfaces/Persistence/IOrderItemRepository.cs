using PlantShop.Domain.Entities.Shop;

namespace PlantShop.Application.Interfaces.Persistence;

public interface IOrderItemRepository
{
    
    Task<OrderItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    
    Task AddAsync(OrderItem orderItem, CancellationToken cancellationToken = default);
    
    Task UpdateAsync(OrderItem orderItem, CancellationToken cancellationToken = default);
    
    Task DeleteAsync(OrderItem orderItem, CancellationToken cancellationToken = default);

    Task<bool> ExistsWithArticleIdAsync(int articleId, CancellationToken cancellationToken = default);
}
