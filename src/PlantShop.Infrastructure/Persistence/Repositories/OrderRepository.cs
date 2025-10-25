using Microsoft.EntityFrameworkCore;
using PlantShop.Application.Interfaces.Persistence;
using PlantShop.Domain.Entities.Shop;

namespace PlantShop.Infrastructure.Persistence.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _context;

    public OrderRepository(ApplicationDbContext context)
    {
        _context = context;
    }


    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {        
        await _context.Orders.AddAsync(order, cancellationToken);        
    }

    public Task DeleteAsync(Order order, CancellationToken cancellationToken = default)
    {        
        _context.Orders.Remove(order);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<Order>> GetAllOrdersAsync(CancellationToken cancellationToken = default)
    {        
        return await _context.Orders
            .Include(o => o.User)
            .OrderByDescending(o => o.OrderDate)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Orders.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<Order?> GetOrderDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
                .Include(o => o.User)          
                .Include(o => o.OrderItems)    
                    .ThenInclude(oi => oi.Article) 
                .AsNoTracking() 
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Order>> GetOrdersByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {        
        return await _context.Orders
            .Where(o => o.UserId == userId)
            .Include(o => o.User) 
            .OrderByDescending(o => o.OrderDate) 
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {        
        _context.Orders.Update(order); 
        return Task.CompletedTask;
    }
}
