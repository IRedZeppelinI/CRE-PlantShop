using PlantShop.Application.Interfaces.Persistence;
using PlantShop.Domain.Entities.Shop;

namespace PlantShop.Infrastructure.Persistence.Repositories;

public class OrderItemRepository : IOrderItemRepository
{
    private readonly ApplicationDbContext _context;

    public OrderItemRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(OrderItem orderItem, CancellationToken cancellationToken = default)
    {
        await _context.OrderItems.AddAsync(orderItem, cancellationToken);
    }

    public Task DeleteAsync(OrderItem orderItem, CancellationToken cancellationToken = default)
    {
        _context.OrderItems.Remove(orderItem);
        return Task.CompletedTask;
    }

    public async Task<OrderItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.OrderItems.FindAsync(new object[] { id }, cancellationToken);
    }

    public Task UpdateAsync(OrderItem orderItem, CancellationToken cancellationToken = default)
    {
        _context.OrderItems.Update(orderItem);
        return Task.CompletedTask;
    }
}
