using PlantShop.Application.DTOs.Shop;
using PlantShop.Domain.Entities.Shop;

namespace PlantShop.Application.Interfaces.Persistence;

public interface ICategoryRepository
{
    Task<IEnumerable<Category>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(Category category, CancellationToken cancellationToken = default);
    Task UpdateAsync(Category category, CancellationToken cancellationToken = default);
    Task DeleteAsync(Category category, CancellationToken cancellationToken = default);
    Task<IEnumerable<Article>> GetArticlesByCategoryAsync(int categoryId, CancellationToken cancellationToken = default);
}
