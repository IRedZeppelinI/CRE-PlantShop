using PlantShop.Domain.Entities.Shop;

namespace PlantShop.Application.Interfaces.Persistence;

public interface IArticleRepository
{
    Task<IEnumerable<Article>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Article?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(Article article, CancellationToken cancellationToken = default);
    Task UpdateAsync(Article article, CancellationToken cancellationToken = default);
    Task DeleteAsync(Article article, CancellationToken cancellationToken = default);
    Task<IEnumerable<Article>> GetArticlesByCategoryAsync(int categoryId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Article>> GetFeaturedArticlesAsync(CancellationToken cancellationToken = default);
}


