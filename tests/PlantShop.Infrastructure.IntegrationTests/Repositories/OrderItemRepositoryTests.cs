using Microsoft.EntityFrameworkCore;
using PlantShop.Domain.Entities;
using PlantShop.Domain.Entities.Shop;
using PlantShop.Infrastructure.Persistence;
using PlantShop.Infrastructure.Persistence.Repositories;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace PlantShop.Infrastructure.IntegrationTests.Repositories;

[Collection("DatabaseTests")]
public class OrderItemRepositoryTests : IAsyncLifetime
{
    private readonly ApplicationDbContext _context;
    private readonly OrderItemRepository _repository;

    private AppUser _seedUser = null!;
    private Category _seedCategory = null!;
    private Article _seedArticle = null!;
    private Order _seedOrder = null!;
    private OrderItem _seedOrderItem = null!;

    public OrderItemRepositoryTests()
    {
        _context = DbContextFactory.Create();
        _repository = new OrderItemRepository(_context);
    }

    public async Task InitializeAsync()
    {
        _seedUser = new AppUser { Id = Guid.NewGuid().ToString(), UserName = "orderitemuser@test.com", NormalizedUserName = "ORDERITEMUSER@TEST.COM", Email = "orderitemuser@test.com", NormalizedEmail = "ORDERITEMUSER@TEST.COM", FullName = "OrderItem Test User" };
        _context.Users.Add(_seedUser);

        _seedCategory = new Category { Name = "OrderItem Test Category" };
        _seedArticle = new Article { Name = "OrderItem Test Article", Price = 9.99m, Category = _seedCategory };
        _context.Categories.Add(_seedCategory);
        _context.Articles.Add(_seedArticle);
        await _context.SaveChangesAsync();

        _seedOrder = new Order { UserId = _seedUser.Id, OrderDate = DateTime.UtcNow, OrderStatus = "Seeded", TotalPrice = 19.98m };
        _context.Orders.Add(_seedOrder);
        await _context.SaveChangesAsync();

        _seedOrderItem = new OrderItem
        {
            OrderId = _seedOrder.Id,
            ArticleId = _seedArticle.Id,
            Quantity = 2,
            UnitPrice = 9.99m
        };
        _context.OrderItems.Add(_seedOrderItem);
        await _context.SaveChangesAsync();

        _context.ChangeTracker.Clear();
    }

    public Task DisposeAsync()
    {
        _context?.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    [Trait("OrderItem", "Integration")]
    public async Task GetByIdAsync_WhenItemExists_ShouldReturnCorrectItem()
    {
        var itemId = _seedOrderItem.Id;
        var result = await _repository.GetByIdAsync(itemId);

        Assert.NotNull(result);
        Assert.Equal(itemId, result.Id);
        Assert.Equal(_seedOrder.Id, result.OrderId);
        Assert.Equal(_seedArticle.Id, result.ArticleId);
        Assert.Equal(2, result.Quantity);
        Assert.Equal(9.99m, result.UnitPrice);
        Assert.Null(result.Order);
        Assert.Null(result.Article);
    }

    [Fact]
    [Trait("OrderItem", "Integration")]
    public async Task GetByIdAsync_WhenItemDoesNotExist_ShouldReturnNull()
    {
        int nonExistentId = 9999;
        var result = await _repository.GetByIdAsync(nonExistentId);
        Assert.Null(result);
    }

    [Fact]
    [Trait("OrderItem", "Integration")]
    public async Task AddAsync_WhenItemIsValid_ShouldAddItemToDatabase()
    {
        var newItem = new OrderItem
        {
            OrderId = _seedOrder.Id,
            ArticleId = _seedArticle.Id,
            Quantity = 5,
            UnitPrice = 9.99m
        };

        await _repository.AddAsync(newItem);
        await _context.SaveChangesAsync();

        var result = await _context.OrderItems.FindAsync(newItem.Id);
        Assert.NotNull(result);
        Assert.Equal(newItem.Quantity, result.Quantity);
        Assert.Equal(newItem.UnitPrice, result.UnitPrice);
        Assert.Equal(_seedOrder.Id, result.OrderId);
        Assert.Equal(_seedArticle.Id, result.ArticleId);
        Assert.True(result.Id > 0);
    }

    [Fact]
    [Trait("OrderItem", "Integration")]
    public async Task UpdateAsync_WhenItemExists_ShouldUpdateItemInDatabase()
    {
        var itemId = _seedOrderItem.Id;
        _context.ChangeTracker.Clear();

        var updatedItemData = new OrderItem
        {
            Id = itemId,
            OrderId = _seedOrder.Id,
            ArticleId = _seedArticle.Id,
            Quantity = 10,
            UnitPrice = 8.88m
        };

        await _repository.UpdateAsync(updatedItemData);
        await _context.SaveChangesAsync();

        _context.ChangeTracker.Clear();
        var itemFromDb = await _context.OrderItems.FindAsync(itemId);
        Assert.NotNull(itemFromDb);
        Assert.Equal(updatedItemData.Quantity, itemFromDb.Quantity);
        Assert.Equal(updatedItemData.UnitPrice, itemFromDb.UnitPrice);
    }

    [Fact]
    [Trait("OrderItem", "Integration")]
    public async Task DeleteAsync_WhenItemExists_ShouldRemoveItemFromDatabase()
    {
        var itemId = _seedOrderItem.Id;
        _context.ChangeTracker.Clear();

        var itemEntity = await _context.OrderItems.FindAsync(itemId);
        Assert.NotNull(itemEntity);

        await _repository.DeleteAsync(itemEntity);
        var stateBeforeSave = _context.Entry(itemEntity).State;
        await _context.SaveChangesAsync();

        Assert.Equal(EntityState.Deleted, stateBeforeSave);
        var resultAfterDelete = await _context.OrderItems.FindAsync(itemId);
        Assert.Null(resultAfterDelete);
    }
}