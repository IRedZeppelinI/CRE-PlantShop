namespace PlantShop.Application.Interfaces.Persistence;

public interface IUnitOfWork : IDisposable
{
    ICategoryRepository Categories { get; }
    IArticleRepository Articles { get; }
    IAppUserRepository AppUsers { get; }
    IOrderItemRepository OrderItems { get; }
    IOrderRepository Orders { get; }


    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
