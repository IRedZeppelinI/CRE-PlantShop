using PlantShop.Application.Interfaces.Persistence;
using PlantShop.Infrastructure.Persistence.Repositories; 

namespace PlantShop.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
        
    private ICategoryRepository? _categories;
    private IArticleRepository? _articles;
    private IAppUserRepository? _appUsers;
    private IOrderItemRepository? _orderItems;
    private IOrderRepository? _orders;


    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }


    public ICategoryRepository Categories => _categories ??= new CategoryRepository(_context);
    public IArticleRepository Articles => _articles ??= new ArticleRepository(_context);
    public IAppUserRepository AppUsers => _appUsers ??= new AppUserRepository(_context);
    public IOrderItemRepository OrderItems => _orderItems ??= new OrderItemRepository(_context);
    public IOrderRepository Orders => _orders ??= new OrderRepository(_context);


    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    
    public void Dispose()
    {
        _context.Dispose();
        
        GC.SuppressFinalize(this);
    }
}