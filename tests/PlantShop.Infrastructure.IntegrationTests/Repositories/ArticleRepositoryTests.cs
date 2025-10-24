using Microsoft.EntityFrameworkCore;
using PlantShop.Domain.Entities.Shop;
using PlantShop.Infrastructure.Persistence; 
using PlantShop.Infrastructure.Persistence.Repositories;

namespace PlantShop.Infrastructure.IntegrationTests.Repositories;

[Collection("DatabaseTests")] 
public class ArticleRepositoryTests : IAsyncLifetime
{
    
    private readonly ApplicationDbContext _context;
    private readonly ArticleRepository _repository;    
    private Category _seedCategory = null!;

    public ArticleRepositoryTests()    {
        
        _context = DbContextFactory.Create(); 
        _repository = new ArticleRepository(_context);        
    }
        
    public async Task InitializeAsync()
    {        
        _seedCategory = new Category { Name = "Test Seed Category", Description = "Used for Article tests" };
        await _context.Categories.AddAsync(_seedCategory);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear(); 
    }
    
    public Task DisposeAsync()
    {
        _context?.Dispose(); 
        return Task.CompletedTask;
    }


    // --- Tests for AddAsync ---

    [Fact]
    [Trait("Article", "Integration")]
    public async Task AddAsync_WhenArticleIsValid_ShouldAddArticleToDatabase()
    {
        
        var articleToAdd = new Article
        {
            Name = "Test Article 1",
            Description = "Desc 1",
            Price = 10.99m,
            StockQuantity = 100,
            ImageUrl = "http://example.com/img1.jpg",
            CategoryId = _seedCategory.Id
        };

        
        await _repository.AddAsync(articleToAdd);
        await _context.SaveChangesAsync(); 

        
        var result = await _context.Articles
                                   .Include(a => a.Category)
                                   .FirstOrDefaultAsync(a => a.Id == articleToAdd.Id);

        Assert.NotNull(result);
        Assert.Equal(articleToAdd.Name, result.Name);
        Assert.Equal(articleToAdd.Description, result.Description);
        Assert.Equal(articleToAdd.Price, result.Price);
        Assert.Equal(articleToAdd.StockQuantity, result.StockQuantity);
        Assert.Equal(articleToAdd.ImageUrl, result.ImageUrl);
        Assert.Equal(_seedCategory.Id, result.CategoryId); 
        Assert.NotNull(result.Category);
        Assert.Equal(_seedCategory.Name, result.Category.Name);
        Assert.True(result.Id > 0);
    }

    // --- Tests for GetAllAsync ---

    [Fact]
    [Trait("Article", "Integration")]
    public async Task GetAllAsync_WhenDatabaseIsEmpty_ShouldReturnEmptyList()
    {
        

        
        var results = await _repository.GetAllAsync();

        
        Assert.NotNull(results);
        Assert.Empty(results);
    }

    [Fact]
    [Trait("Article", "Integration")]
    public async Task GetAllAsync_WhenDatabaseHasArticles_ShouldReturnAllArticlesWithCategories()
    {
        
        var article1 = new Article { Name = "Article A", Price = 5.00m, CategoryId = _seedCategory.Id };
        var article2 = new Article { Name = "Article B", Price = 15.00m, CategoryId = _seedCategory.Id };
        await _context.Articles.AddRangeAsync(article1, article2);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear(); 

        
        var results = await _repository.GetAllAsync();

        
        Assert.NotNull(results);
        Assert.Equal(2, results.Count());
        Assert.Contains(results, a => a.Name == "Article A");
        Assert.Contains(results, a => a.Name == "Article B");
        //For include cat
        Assert.All(results, a => Assert.NotNull(a.Category));
        Assert.All(results, a => Assert.Equal(_seedCategory.Name, a.Category.Name));
    }

    // --- Tests for GetByIdAsync ---

    [Fact]
    [Trait("Article", "Integration")]
    public async Task GetByIdAsync_WhenArticleExists_ShouldReturnCorrectArticleWithCategory()
    {
        
        var seedArticle = new Article { Name = "FindMe Article", Price = 25.00m, CategoryId = _seedCategory.Id };
        await _context.Articles.AddAsync(seedArticle);
        await _context.SaveChangesAsync();
        var articleId = seedArticle.Id;
        _context.ChangeTracker.Clear();

        
        var result = await _repository.GetByIdAsync(articleId);

        
        Assert.NotNull(result);
        Assert.Equal(seedArticle.Name, result.Name);
        Assert.Equal(seedArticle.Price, result.Price);
        Assert.Equal(_seedCategory.Id, result.CategoryId);
        Assert.NotNull(result.Category);
        Assert.Equal(_seedCategory.Name, result.Category.Name);
    }

    [Fact]
    [Trait("Article", "Integration")]
    public async Task GetByIdAsync_WhenArticleDoesNotExist_ShouldReturnNull()
    {
        
        int nonExistentId = 999;

        
        var result = await _repository.GetByIdAsync(nonExistentId);

        
        Assert.Null(result);
    }


    // --- Tests for UpdateAsync ---

    [Fact]
    [Trait("Article", "Integration")]
    public async Task UpdateAsync_WhenArticleExists_ShouldUpdateArticleInDatabase()
    {        
        var initialArticle = new Article
        {
            Name = "Original Article",
            Price = 50.00m,
            StockQuantity = 50,
            CategoryId = _seedCategory.Id
        };
        await _context.Articles.AddAsync(initialArticle);
        await _context.SaveChangesAsync();
        var articleId = initialArticle.Id;
        _context.ChangeTracker.Clear(); 
        

        var updatedArticleData = new Article
        {
            Id = articleId, 
            Name = "Updated Article Name",
            Description = "Updated Description",
            Price = 55.50m,
            StockQuantity = 45,
            ImageUrl = "http://new.image.url/updated.jpg",
            CategoryId = _seedCategory.Id 
        };

        
        await _repository.UpdateAsync(updatedArticleData); 
        await _context.SaveChangesAsync(); 
                
        _context.ChangeTracker.Clear(); 
        var articleFromDb = await _context.Articles.FindAsync(articleId);

        Assert.NotNull(articleFromDb);
        Assert.Equal(updatedArticleData.Name, articleFromDb.Name);
        Assert.Equal(updatedArticleData.Description, articleFromDb.Description);
        Assert.Equal(updatedArticleData.Price, articleFromDb.Price);
        Assert.Equal(updatedArticleData.StockQuantity, articleFromDb.StockQuantity);
        Assert.Equal(updatedArticleData.ImageUrl, articleFromDb.ImageUrl);
        Assert.Equal(_seedCategory.Id, articleFromDb.CategoryId); 
    }

    // --- Tests for DeleteAsync ---

    [Fact]
    [Trait("Article", "Integration")]
    public async Task DeleteAsync_WhenArticleExists_ShouldRemoveArticleFromDatabase()
    {        
        var articleToDelete = new Article { Name = "Delete Me Article", Price = 1.00m, CategoryId = _seedCategory.Id };
        await _context.Articles.AddAsync(articleToDelete);
        await _context.SaveChangesAsync();
        var articleId = articleToDelete.Id;
        _context.ChangeTracker.Clear(); 

        var articleEntity = await _context.Articles.FindAsync(articleId);
        Assert.NotNull(articleEntity); 

        
        await _repository.DeleteAsync(articleEntity);
        var stateBeforeSave = _context.Entry(articleEntity).State;
        await _context.SaveChangesAsync(); 

        
        Assert.Equal(EntityState.Deleted, stateBeforeSave); 
        var resultAfterDelete = await _context.Articles.FindAsync(articleId);
        Assert.Null(resultAfterDelete); 
    }
    
    // --- Tests for GetArticlesByCategoryAsync ---

    [Fact]
    [Trait("Article", "Integration")]
    public async Task GetArticlesByCategoryAsync_WhenCategoryHasArticles_ShouldReturnOnlyThoseArticles()
    {
        var categoryA = _seedCategory; 
        var categoryB = new Category { Name = "Category B" };        

        var articleA1 = new Article { Name = "Article A1", Price = 10m, CategoryId = categoryA.Id }; 
        var articleA2 = new Article { Name = "Article A2", Price = 12m, CategoryId = categoryA.Id }; 

        // --- ALTERAÇÃO PRINCIPAL ---
        var articleB1 = new Article
        {
            Name = "Article B1",
            Price = 20m,            
            Category = categoryB       
        };
        await _context.Articles.AddRangeAsync(articleA1, articleA2, articleB1);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        
        
        var results = await _repository.GetArticlesByCategoryAsync(categoryA.Id); 
                

        Assert.NotNull(results);
        Assert.Equal(2, results.Count()); 
        Assert.Contains(results, a => a.Name == "Article A1");
        Assert.Contains(results, a => a.Name == "Article A2");
        Assert.DoesNotContain(results, a => a.Name == "Article B1"); 
        Assert.All(results, a => Assert.NotNull(a.Category)); 
        Assert.All(results, a => Assert.Equal(categoryA.Name, a.Category.Name));
    }

    [Fact]
    [Trait("Article", "Integration")]
    public async Task GetArticlesByCategoryAsync_WhenCategoryHasNoArticles_ShouldReturnEmptyList()
    {
        
        var emptyCategory = new Category { Name = "Empty Category" };
        await _context.Categories.AddAsync(emptyCategory);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        

        var results = await _repository.GetArticlesByCategoryAsync(emptyCategory.Id);
                

        Assert.NotNull(results);
        Assert.Empty(results);
    }

    [Fact]
    [Trait("Article", "Integration")]
    public async Task GetArticlesByCategoryAsync_WhenCategoryDoesNotExist_ShouldReturnEmptyList()
    {       
        int nonExistentCategoryId = 999;
       

        var results = await _repository.GetArticlesByCategoryAsync(nonExistentCategoryId);
                

        Assert.NotNull(results);
        Assert.Empty(results);
    }

}