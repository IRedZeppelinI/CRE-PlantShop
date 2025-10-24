using Microsoft.EntityFrameworkCore;
using PlantShop.Application.Interfaces.Persistence;
using PlantShop.Domain.Entities;

namespace PlantShop.Infrastructure.Persistence.Repositories;

public class AppUserRepository : IAppUserRepository
{

    private readonly ApplicationDbContext _context;

    public AppUserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AppUser?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _context.Users.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<AppUser?> GetUserWithOrdersAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _context.Users 
        .Include(u => u.Orders) 
        .ThenInclude(o => o.OrderItems) 
        .AsNoTracking() 
        .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }
}
