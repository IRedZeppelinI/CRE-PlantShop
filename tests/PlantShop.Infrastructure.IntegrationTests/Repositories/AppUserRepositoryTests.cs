using PlantShop.Domain.Entities;
using PlantShop.Domain.Entities.Shop;
using PlantShop.Infrastructure.Persistence;
using PlantShop.Infrastructure.Persistence.Repositories;

namespace PlantShop.Infrastructure.IntegrationTests.Repositories;

[Collection("DatabaseTests")]
public class AppUserRepositoryTests : IAsyncLifetime
{
    private readonly ApplicationDbContext _context;
    private readonly AppUserRepository _repository;
    private AppUser _seedUser = null!;
    private Order _seedOrder = null!;

    public AppUserRepositoryTests()
    {
        _context = DbContextFactory.Create();
        _repository = new AppUserRepository(_context);
    }

    public async Task InitializeAsync()
    {
        _seedUser = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "testuser@example.com",
            NormalizedUserName = "TESTUSER@EXAMPLE.COM",
            Email = "testuser@example.com",
            NormalizedEmail = "TESTUSER@EXAMPLE.COM",
            EmailConfirmed = true,
            FullName = "Test User Seed",
        };
        _context.Users.Add(_seedUser);

        var category = new Category { Name = "Order Test Cat" };
        var article = new Article { Name = "Order Test Article", Price = 10m, Category = category };
        _context.Categories.Add(category);
        _context.Articles.Add(article);
        await _context.SaveChangesAsync();

        _seedOrder = new Order
        {
            UserId = _seedUser.Id,
            OrderDate = DateTime.UtcNow,
            OrderStatus = "Pending",
            TotalPrice = 10m * 2
        };
        var orderItem = new OrderItem
        {
            Order = _seedOrder,
            ArticleId = article.Id,
            Quantity = 2,
            UnitPrice = 10m
        };
        _context.Orders.Add(_seedOrder);
        _context.OrderItems.Add(orderItem);

        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
    }

    public Task DisposeAsync()
    {
        _context?.Dispose();
        return Task.CompletedTask;
    }


    [Fact]
    [Trait("AppUser", "Integration")]
    public async Task GetByIdAsync_WhenUserExists_ShouldReturnCorrectUser()
    {
        var userId = _seedUser.Id;

        var result = await _repository.GetByIdAsync(userId);

        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal(_seedUser.FullName, result.FullName);
        Assert.Equal(_seedUser.Email, result.Email);
        Assert.Empty(result.Orders);
    }

    [Fact]
    [Trait("AppUser", "Integration")]
    public async Task GetByIdAsync_WhenUserDoesNotExist_ShouldReturnNull()
    {
        string nonExistentId = Guid.NewGuid().ToString();

        var result = await _repository.GetByIdAsync(nonExistentId);

        Assert.Null(result);
    }

    [Fact]
    [Trait("AppUser", "Integration")]
    public async Task GetUserWithOrdersAsync_WhenUserExistsAndHasOrders_ShouldReturnUserWithOrdersAndItems()
    {
        var userId = _seedUser.Id;

        var result = await _repository.GetUserWithOrdersAsync(userId);

        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal(_seedUser.FullName, result.FullName);

        Assert.NotNull(result.Orders);
        Assert.Single(result.Orders);

        var order = result.Orders.First();
        Assert.Equal(_seedOrder.Id, order.Id);
        Assert.NotNull(order.OrderItems);
        Assert.Single(order.OrderItems);

        var item = order.OrderItems.First();
        Assert.Equal(2, item.Quantity);
        Assert.Equal(10m, item.UnitPrice);
    }

    [Fact]
    [Trait("AppUser", "Integration")]
    public async Task GetUserWithOrdersAsync_WhenUserExistsButHasNoOrders_ShouldReturnUserWithEmptyOrders()
    {
        var userWithoutOrders = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "noorders@example.com",
            NormalizedUserName = "NOORDERS@EXAMPLE.COM",
            Email = "noorders@example.com",
            NormalizedEmail = "NOORDERS@EXAMPLE.COM",
            FullName = "No Orders User"
        };
        _context.Users.Add(userWithoutOrders);
        await _context.SaveChangesAsync();
        var userId = userWithoutOrders.Id;
        _context.ChangeTracker.Clear();

        var result = await _repository.GetUserWithOrdersAsync(userId);

        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.NotNull(result.Orders);
        Assert.Empty(result.Orders);
    }

    [Fact]
    [Trait("AppUser", "Integration")]
    public async Task GetUserWithOrdersAsync_WhenUserDoesNotExist_ShouldReturnNull()
    {
        string nonExistentId = Guid.NewGuid().ToString();

        var result = await _repository.GetUserWithOrdersAsync(nonExistentId);

        Assert.Null(result);
    }


}