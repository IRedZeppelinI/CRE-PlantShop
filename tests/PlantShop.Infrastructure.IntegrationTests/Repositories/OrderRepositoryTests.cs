using Microsoft.EntityFrameworkCore;
using PlantShop.Domain.Entities;
using PlantShop.Domain.Entities.Shop;
using PlantShop.Infrastructure.Persistence;
using PlantShop.Infrastructure.Persistence.Repositories;

namespace PlantShop.Infrastructure.IntegrationTests.Repositories;

[Collection("DatabaseTests")]
public class OrderRepositoryTests : IAsyncLifetime
{
    private readonly ApplicationDbContext _context;
    private readonly OrderRepository _repository;

    private AppUser _seedUser1 = null!;
    private AppUser _seedUser2 = null!;
    private Category _seedCategory = null!;
    private Article _seedArticle1 = null!;
    private Article _seedArticle2 = null!;
    private Order _seedOrder1User1 = null!; // Order com 2 items
    private Order _seedOrder2User1 = null!; // Order com 1 item
    private Order _seedOrder1User2 = null!; 

    public OrderRepositoryTests()
    {
        _context = DbContextFactory.Create();
        _repository = new OrderRepository(_context);
    }

    public async Task InitializeAsync()
    {
        _seedUser1 = new AppUser 
            { Id = Guid.NewGuid().ToString(), UserName = "user1@test.com", NormalizedUserName = "USER1@TEST.COM", Email = "user1@test.com", NormalizedEmail = "USER1@TEST.COM", FullName = "User One" };
        _seedUser2 = new AppUser 
            { Id = Guid.NewGuid().ToString(), UserName = "user2@test.com", NormalizedUserName = "USER2@TEST.COM", Email = "user2@test.com", NormalizedEmail = "USER2@TEST.COM", FullName = "User Two" };
        _context.Users.AddRange(_seedUser1, _seedUser2);

        _seedCategory = new Category { Name = "Order Test Category" };
        _seedArticle1 = new Article { Name = "Article Order 1", Price = 10m, Category = _seedCategory };
        _seedArticle2 = new Article { Name = "Article Order 2", Price = 25m, Category = _seedCategory };
        _context.Categories.Add(_seedCategory);
        _context.Articles.AddRange(_seedArticle1, _seedArticle2);
        await _context.SaveChangesAsync();

        _seedOrder1User1 = new Order { UserId = _seedUser1.Id, OrderDate = DateTime.UtcNow.AddDays(-2), OrderStatus = "Delivered", TotalPrice = (10m * 1) + (25m * 2) };
        _seedOrder1User1.OrderItems.Add(new OrderItem { ArticleId = _seedArticle1.Id, Quantity = 1, UnitPrice = 10m });
        _seedOrder1User1.OrderItems.Add(new OrderItem { ArticleId = _seedArticle2.Id, Quantity = 2, UnitPrice = 25m });

        _seedOrder2User1 = new Order { UserId = _seedUser1.Id, OrderDate = DateTime.UtcNow.AddDays(-1), OrderStatus = "Shipped", TotalPrice = 25m * 3 };
        _seedOrder2User1.OrderItems.Add(new OrderItem { ArticleId = _seedArticle2.Id, Quantity = 3, UnitPrice = 25m });

        _seedOrder1User2 = new Order { UserId = _seedUser2.Id, OrderDate = DateTime.UtcNow, OrderStatus = "Pending", TotalPrice = 10m * 5 };
        _seedOrder1User2.OrderItems.Add(new OrderItem { ArticleId = _seedArticle1.Id, Quantity = 5, UnitPrice = 10m });

        _context.Orders.AddRange(_seedOrder1User1, _seedOrder2User1, _seedOrder1User2);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
    }

    public Task DisposeAsync()
    {
        _context?.Dispose();
        return Task.CompletedTask;
    }

    // --- Tests for GetOrderDetailsAsync ---

    [Fact]
    [Trait("Order", "Integration")]
    public async Task GetOrderDetailsAsync_WhenOrderExists_ShouldReturnOrderWithUserAndItemsAndArticles()
    {
        var orderId = _seedOrder1User1.Id;
        var result = await _repository.GetOrderDetailsAsync(orderId);

        Assert.NotNull(result);
        Assert.Equal(orderId, result.Id);
        Assert.NotNull(result.User);
        Assert.Equal(_seedUser1.Id, result.UserId);
        Assert.Equal(_seedUser1.FullName, result.User.FullName);
        Assert.NotNull(result.OrderItems);
        Assert.Equal(2, result.OrderItems.Count);
        Assert.Contains(result.OrderItems, oi => oi.ArticleId == _seedArticle1.Id && oi.Quantity == 1);
        Assert.Contains(result.OrderItems, oi => oi.ArticleId == _seedArticle2.Id && oi.Quantity == 2);
        Assert.All(result.OrderItems, oi => Assert.NotNull(oi.Article)); // Check articles are included
        Assert.Contains(result.OrderItems.Select(oi => oi.Article.Name), name => name == _seedArticle1.Name);
    }

    [Fact]
    [Trait("Order", "Integration")]
    public async Task GetOrderDetailsAsync_WhenOrderDoesNotExist_ShouldReturnNull()
    {
        int nonExistentId = 9999;
        var result = await _repository.GetOrderDetailsAsync(nonExistentId);
        Assert.Null(result);
    }

    // --- Tests for GetOrdersByUserIdAsync ---

    [Fact]
    [Trait("Order", "Integration")]
    public async Task GetOrdersByUserIdAsync_WhenUserHasOrders_ShouldReturnOnlyUserOrdersOrderedByDateDesc()
    {
        var userId = _seedUser1.Id;
        var results = await _repository.GetOrdersByUserIdAsync(userId);

        Assert.NotNull(results);
        Assert.Equal(2, results.Count());
        Assert.True(results.All(o => o.UserId == userId));
        Assert.Contains(results, o => o.Id == _seedOrder1User1.Id);
        Assert.Contains(results, o => o.Id == _seedOrder2User1.Id);
        Assert.DoesNotContain(results, o => o.Id == _seedOrder1User2.Id); 
        Assert.Equal(_seedOrder2User1.Id, results.First().Id); 
        Assert.Equal(_seedOrder1User1.Id, results.Last().Id);
        Assert.All(results, o => Assert.NotNull(o.User)); 
        Assert.All(results, o => Assert.Empty(o.OrderItems)); 
    }

    [Fact]
    [Trait("Order", "Integration")]
    public async Task GetOrdersByUserIdAsync_WhenUserHasNoOrders_ShouldReturnEmptyList()
    {
        var newUser = new AppUser 
            { Id = Guid.NewGuid().ToString(), UserName = "noorder@test.com", NormalizedUserName = "NOORDER@TEST.COM", Email = "noorder@test.com", NormalizedEmail = "NOORDER@TEST.COM", FullName = "No Order User" };
        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var results = await _repository.GetOrdersByUserIdAsync(newUser.Id);

        Assert.NotNull(results);
        Assert.Empty(results);
    }

    [Fact]
    [Trait("Order", "Integration")]
    public async Task GetOrdersByUserIdAsync_WhenUserDoesNotExist_ShouldReturnEmptyList()
    {
        string nonExistentUserId = Guid.NewGuid().ToString();
        var results = await _repository.GetOrdersByUserIdAsync(nonExistentUserId);
        Assert.NotNull(results);
        Assert.Empty(results);
    }

    // --- Tests for GetAllOrdersAsync ---

    [Fact]
    [Trait("Order", "Integration")]
    public async Task GetAllOrdersAsync_WhenOrdersExist_ShouldReturnAllOrdersOrderedByDateDesc()
    {
        var results = await _repository.GetAllOrdersAsync();

        Assert.NotNull(results);
        Assert.Equal(3, results.Count()); 
        Assert.Equal(_seedOrder1User2.Id, results.First().Id); 
        Assert.Equal(_seedOrder2User1.Id, results.Skip(1).First().Id);
        Assert.Equal(_seedOrder1User1.Id, results.Last().Id); 
        Assert.All(results, o => Assert.NotNull(o.User)); 
        Assert.All(results, o => Assert.Empty(o.OrderItems)); 
    }

    [Fact]
    [Trait("Order", "Integration")]
    public async Task GetAllOrdersAsync_WhenNoOrdersExist_ShouldReturnEmptyList()
    {        
        await DisposeAsync(); 
        using var localContext = DbContextFactory.Create(); 
        var localRepository = new OrderRepository(localContext);

        var results = await localRepository.GetAllOrdersAsync();

        Assert.NotNull(results);
        Assert.Empty(results);
    }


    // --- Tests for AddAsync ---

    [Fact]
    [Trait("Order", "Integration")]
    public async Task AddAsync_WhenOrderWithItemsIsValid_ShouldAddOrderAndItemsToDatabase()
    {
        var newUser = new AppUser 
            { Id = Guid.NewGuid().ToString(), UserName = "addorder@test.com", NormalizedUserName = "ADDORDER@TEST.COM", Email = "addorder@test.com", NormalizedEmail = "ADDORDER@TEST.COM", FullName = "Add Order User" };
        _context.Users.Add(newUser);
        await _context.SaveChangesAsync(); // Need user ID
        _context.ChangeTracker.Clear();

        var newOrder = new Order
        {
            UserId = newUser.Id,
            OrderDate = DateTime.UtcNow,
            OrderStatus = "New",
            TotalPrice = _seedArticle1.Price * 3
        };
        // Add items directly to the collection BEFORE AddAsync
        newOrder.OrderItems.Add(new OrderItem { ArticleId = _seedArticle1.Id, Quantity = 3, UnitPrice = _seedArticle1.Price });

        await _repository.AddAsync(newOrder);
        await _context.SaveChangesAsync(); // Commit

        _context.ChangeTracker.Clear();
        var result = await _context.Orders
                                .Include(o => o.OrderItems)
                                .FirstOrDefaultAsync(o => o.Id == newOrder.Id);

        Assert.NotNull(result);
        Assert.Equal(newUser.Id, result.UserId);
        Assert.Equal("New", result.OrderStatus);
        Assert.NotNull(result.OrderItems);
        Assert.Single(result.OrderItems);
        Assert.Equal(3, result.OrderItems.First().Quantity);
        Assert.Equal(_seedArticle1.Id, result.OrderItems.First().ArticleId);
    }

    // --- Tests for UpdateAsync ---

    [Fact]
    [Trait("Order", "Integration")]
    public async Task UpdateAsync_WhenUpdatingOrderStatus_ShouldUpdateOrderInDatabase()
    {
        var orderId = _seedOrder1User2.Id; // Get ID of the "Pending" order
        _context.ChangeTracker.Clear();

        
        var orderToUpdate = await _context.Orders.FindAsync(orderId);
        Assert.NotNull(orderToUpdate);

        
        orderToUpdate.OrderStatus = "Processing";

        
        await _repository.UpdateAsync(orderToUpdate); 
        await _context.SaveChangesAsync(); 

        _context.ChangeTracker.Clear();
        var updatedOrder = await _context.Orders.FindAsync(orderId);
        Assert.NotNull(updatedOrder);
        Assert.Equal("Processing", updatedOrder.OrderStatus);
    }

    // --- Tests for DeleteAsync ---

    [Fact]
    [Trait("Order", "Integration")]
    public async Task DeleteAsync_WhenOrderExists_ShouldRemoveOrderAndCascadeDeleteItems()
    {
        var orderIdToDelete = _seedOrder1User1.Id;
        var initialItemCount = await _context.OrderItems.CountAsync(oi => oi.OrderId == orderIdToDelete);
        Assert.True(initialItemCount > 0); // Ensure items exist before delete
        _context.ChangeTracker.Clear();

        // Load the entity to delete
        var orderEntity = await _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == orderIdToDelete);
        Assert.NotNull(orderEntity);

        
        await _repository.DeleteAsync(orderEntity); 
        await _context.SaveChangesAsync(); 

        
        var deletedOrder = await _context.Orders.FindAsync(orderIdToDelete);
        Assert.Null(deletedOrder); // Order should be gone

        // Verify cascade delete worked for items
        var remainingItems = await _context.OrderItems.CountAsync(oi => oi.OrderId == orderIdToDelete);
        Assert.Equal(0, remainingItems); // Items should also be gone
    }
}