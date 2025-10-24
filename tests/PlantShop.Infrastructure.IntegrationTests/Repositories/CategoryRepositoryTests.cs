using Microsoft.EntityFrameworkCore;
using PlantShop.Domain.Entities.Shop;
using PlantShop.Infrastructure.Persistence;
using PlantShop.Infrastructure.Persistence.Repositories;

namespace PlantShop.Infrastructure.IntegrationTests.Repositories;

[Collection("DatabaseTests")]
public class CategoryRepositoryTests : IAsyncLifetime
{
    private readonly ApplicationDbContext _context;
    private readonly CategoryRepository _repository;

    public CategoryRepositoryTests()
    {
        _context = DbContextFactory.Create();
        _repository = new CategoryRepository(_context);
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask; 
    }
    
    public Task DisposeAsync()
    {
        _context?.Dispose(); 
        return Task.CompletedTask;
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


    // --- Tests for GetByIdAsync ---

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetByIdAsync_WhenCategoryExists_ShouldReturnCorrectCategory()
    {
        
        var seedCategory = new Category { Name = "TestCategoryName", Description = "TestCategoryDescription" };
        await _context.Categories.AddAsync(seedCategory);
        await _context.SaveChangesAsync();
        
        _context.ChangeTracker.Clear();

        
        var result = await _repository.GetByIdAsync(seedCategory.Id); 

        
        Assert.NotNull(result);
        Assert.Equal(seedCategory.Name, result.Name);
        Assert.Equal(seedCategory.Description, result.Description);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetByIdAsync_WhenCategoryDoesNotExist_ShouldReturnNull()
    {
        int nonExistentId = 999;

        var result = await _repository.GetByIdAsync(nonExistentId);

        Assert.Null(result);
    }


    // --- Tests for DeleteAsync ---

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DeleteAsync_WhenCategoryExists_ShouldMarkCategoryAsDeleted()
    {
        var categoryToDelete = new Category { Name = "DeleteMe", Description = "I will be deleted" };
        await _context.Categories.AddAsync(categoryToDelete);
        await _context.SaveChangesAsync();

        _context.ChangeTracker.Clear();

        
        var categoryEntity = await _context.Categories.FindAsync(categoryToDelete.Id);
        Assert.NotNull(categoryEntity); 

        await _repository.DeleteAsync(categoryEntity); 
        var stateBeforeSave = _context.Entry(categoryEntity).State; 

        
        Assert.Equal(EntityState.Deleted, stateBeforeSave); 

        
        await _context.SaveChangesAsync(); 

        
        var resultAfterDelete = await _context.Categories.FindAsync(categoryToDelete.Id); 
        Assert.Null(resultAfterDelete); 
    }


    // --- Test for UpdateAsync ---

    [Fact]
    [Trait("Category", "Integration")]
    public async Task UpdateAsync_WhenCategoryExists_ShouldUpdateCategoryInDatabase()
    {
        
        var initialCategory = new Category { Name = "Original Name", Description = "Original Desc" };
        await _context.Categories.AddAsync(initialCategory);
        await _context.SaveChangesAsync();
        
        var categoryId = initialCategory.Id;
        
        _context.ChangeTracker.Clear();

        
        var updatedCategoryData = new Category
        {
            Id = categoryId, 
            Name = "Updated Name",
            Description = "Updated Desc"
            
        };

        
        await _repository.UpdateAsync(updatedCategoryData);

        
        await _context.SaveChangesAsync();

        
        _context.ChangeTracker.Clear();
        var categoryFromDb = await _context.Categories.FindAsync(categoryId);

        Assert.NotNull(categoryFromDb);
        Assert.Equal(updatedCategoryData.Name, categoryFromDb.Name);
        Assert.Equal(updatedCategoryData.Description, categoryFromDb.Description);
    }
        
}
