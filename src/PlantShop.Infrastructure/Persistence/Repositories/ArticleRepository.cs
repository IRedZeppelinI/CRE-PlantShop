using Microsoft.EntityFrameworkCore;
using PlantShop.Application.Interfaces.Persistence;
using PlantShop.Domain.Entities.Shop;

namespace PlantShop.Infrastructure.Persistence.Repositories;

public class ArticleRepository : IArticleRepository
{
    private readonly ApplicationDbContext _context;

    public ArticleRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Article article, CancellationToken cancellationToken = default)
    {
        await _context.Articles.AddAsync(article, cancellationToken);
    }

    public Task DeleteAsync(Article article, CancellationToken cancellationToken = default)
    {
        _context.Articles.Remove(article);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<Article>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Articles
                .Include(a => a.Category) 
                .AsNoTracking()
                .ToListAsync(cancellationToken);
    }    

    public async Task<Article?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Articles
                .Include(a => a.Category) 
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public Task UpdateAsync(Article article, CancellationToken cancellationToken = default)
    {
        _context.Articles.Update(article);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<Article>> GetArticlesByCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        return await _context.Articles
                .Include(a => a.Category)
                .Where(a => a.CategoryId == categoryId) 
                .AsNoTracking()
                .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Article>> GetFeaturedArticlesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Articles
            .Include(a => a.Category) 
            .Where(a => a.IsFeatured == true) 
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsWithCategoryIdAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        return await _context.Articles.AnyAsync(a => a.CategoryId == categoryId, cancellationToken);
    }
}
