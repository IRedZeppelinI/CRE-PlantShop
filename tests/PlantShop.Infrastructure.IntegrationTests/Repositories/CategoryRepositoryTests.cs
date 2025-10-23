using Microsoft.EntityFrameworkCore;
using PlantShop.Domain.Entities.Shop;
using PlantShop.Infrastructure.Persistence;
using PlantShop.Infrastructure.Persistence.Repositories;

namespace PlantShop.Infrastructure.IntegrationTests.Repositories;

[Collection("DatabaseTests")]
public class CategoryRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CategoryRepository _repository;

    public CategoryRepositoryTests()
    {
        _context = DbContextFactory.Create();
        _repository = new CategoryRepository(_context);
    }


    // --- Tests for AddAsync ---
    [Fact]
    [Trait("Category", "Integration")]
    public async Task AddAsync_WhenCategoryIsValid_ShouldAddCategoryToDatabase()
    {
        var categoryToAdd = new Category { Name = "TestCategory", Description = "TestDescription" };

        await _repository.AddAsync(categoryToAdd);
        await _context.SaveChangesAsync();

        var result = await _context.Categories.FindAsync(categoryToAdd.Id);

        Assert.NotNull(result);
        Assert.Equal(categoryToAdd.Name, result.Name);
        Assert.Equal(categoryToAdd.Description, result.Description);
        Assert.True(result.Id > 0);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task AddAsync_WhenCategoryHasNullDescription_ShouldAddCategoryToDatabase()
    {
        var categoryToAdd = new Category { Name = "NullDescCategory", Description = null }; 

        await _repository.AddAsync(categoryToAdd);
        await _context.SaveChangesAsync();

        var result = await _context.Categories.FindAsync(categoryToAdd.Id);
        Assert.NotNull(result);
        Assert.Equal(categoryToAdd.Name, result.Name);
        Assert.Null(result.Description);
    }


    // --- Tests for GetAllAsync ---
    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetAllAsync_WhenDatabaseIsEmpty_ShouldReturnEmptyList()
    {
        
        var results = await _repository.GetAllAsync();

        
        Assert.NotNull(results); 
        Assert.Empty(results);   
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetAllAsync_WhenDatabaseHasCategories_ShouldReturnAllCategories()
    {        
        var category1 = new Category { Name = "Category A", Description = "Desc A" };
        var category2 = new Category { Name = "Category B", Description = "Desc B" };
        await _context.Categories.AddRangeAsync(category1, category2);
        await _context.SaveChangesAsync();
                
        var results = await _repository.GetAllAsync();
                
        Assert.NotNull(results);
        Assert.Equal(2, results.Count()); 
        Assert.Contains(results, c => c.Name == "Category A"); 
        Assert.Contains(results, c => c.Name == "Category B");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
