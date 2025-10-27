using PlantShop.Application.DTOs.Shop;

namespace PlantShop.Application.Interfaces.Services.Shop;

public interface IArticleService
{    
    Task<IEnumerable<ArticleDto>> GetAllArticlesAsync(CancellationToken cancellationToken = default);
       
    Task<ArticleDto?> GetArticleByIdAsync(int id, CancellationToken cancellationToken = default);
        
    Task<IEnumerable<ArticleDto>> GetArticlesByCategoryAsync(int categoryId, CancellationToken cancellationToken = default);
        
    Task<IEnumerable<ArticleDto>> GetFeaturedArticlesAsync(CancellationToken cancellationToken = default);
        
    Task<ArticleDto> CreateArticleAsync(ArticleDto articleDto, CancellationToken cancellationToken = default);
        
    Task UpdateArticleAsync(ArticleDto articleDto, CancellationToken cancellationToken = default);
        
    Task DeleteArticleAsync(int id, CancellationToken cancellationToken = default);
}