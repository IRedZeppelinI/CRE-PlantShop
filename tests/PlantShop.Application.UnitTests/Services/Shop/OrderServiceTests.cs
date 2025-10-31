using Microsoft.Extensions.Logging;
using Moq;
using PlantShop.Application.Interfaces.Persistence;
using PlantShop.Application.Interfaces.Services.Shop;
using PlantShop.Application.Services.Shop;
using PlantShop.Domain.Entities;
using PlantShop.Domain.Entities.Shop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantShop.Application.UnitTests.Services.Shop;

public class OrderServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<OrderService>> _mockLogger;

    private readonly Mock<IAppUserRepository> _mockAppUserRepo;
    private readonly Mock<IArticleRepository> _mockArticleRepo;
    private readonly Mock<IOrderRepository> _mockOrderRepo;

    private readonly IOrderService _orderService;

    public OrderServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<OrderService>>();

        _mockAppUserRepo = new Mock<IAppUserRepository>();
        _mockArticleRepo = new Mock<IArticleRepository>();
        _mockOrderRepo = new Mock<IOrderRepository>();

        _mockUnitOfWork.Setup(uow => uow.AppUsers).Returns(_mockAppUserRepo.Object);
        _mockUnitOfWork.Setup(uow => uow.Articles).Returns(_mockArticleRepo.Object);
        _mockUnitOfWork.Setup(uow => uow.Orders).Returns(_mockOrderRepo.Object);

        _orderService = new OrderService(_mockUnitOfWork.Object, _mockLogger.Object);
    }

    // --- Tests for CreateOrderAsync ---
    [Fact]
    public async Task CreateOrderAsync_Should_ThrowKeyNotFoundException_WhenUserNotFound()
    {
        // Arrange
        var userId = "invalid-user";
        var cartItems = new List<CartItemCreateDto> { new CartItemCreateDto { ArticleId = 1, Quantity = 1 } };
        _mockAppUserRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _orderService.CreateOrderAsync(userId, cartItems, CancellationToken.None));

        Assert.Equal("Utilizador não encontrado.", ex.Message);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // --- Tests for CreateOrderAsync ---
    [Fact]
    public async Task CreateOrderAsync_Should_ThrowInvalidOperationException_WhenUserHasNoAddress()
    {
        // Arrange
        var userId = "user-no-address";
        var user = new AppUser { Id = userId, FullName = "Test User", Address = null };
        var cartItems = new List<CartItemCreateDto> { new CartItemCreateDto { ArticleId = 1, Quantity = 1 } };

        _mockAppUserRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _orderService.CreateOrderAsync(userId, cartItems, CancellationToken.None));

        Assert.Equal("Não tem uma morada de envio associada. Por favor, adicione uma morada no seu perfil antes de finalizar a compra.", ex.Message);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // --- Tests for CreateOrderAsync ---
    [Fact]
    public async Task CreateOrderAsync_Should_ThrowInvalidOperationException_WhenCartIsEmpty()
    {
        // Arrange
        var userId = "valid-user";
        var user = new AppUser { Id = userId, FullName = "Test User", Address = "123 Main St" };
        var emptyCart = new List<CartItemCreateDto>();

        _mockAppUserRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _orderService.CreateOrderAsync(userId, emptyCart, CancellationToken.None));

        Assert.Equal("O carrinho de compras não pode estar vazio.", ex.Message);
    }

    // --- Tests for CreateOrderAsync ---
    [Fact]
    public async Task CreateOrderAsync_Should_ThrowKeyNotFoundException_WhenArticleNotFound()
    {
        // Arrange
        var userId = "valid-user";
        var user = new AppUser { Id = userId, FullName = "Test User", Address = "123 Main St" };
        var cartItems = new List<CartItemCreateDto> { new CartItemCreateDto { ArticleId = 99, Quantity = 1 } };

        _mockAppUserRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockArticleRepo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Article?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _orderService.CreateOrderAsync(userId, cartItems, CancellationToken.None));

        Assert.Equal("O artigo com Id 99 já não existe.", ex.Message);
    }

    // --- Tests for CreateOrderAsync ---
    [Fact]
    public async Task CreateOrderAsync_Should_ThrowInvalidOperationException_WhenStockIsInsufficient()
    {
        // Arrange
        var userId = "valid-user";
        var user = new AppUser { Id = userId, FullName = "Test User", Address = "123 Main St" };
        var article = new Article { Id = 1, Name = "Rosa", Price = 10, StockQuantity = 1 };
        var cartItems = new List<CartItemCreateDto> { new CartItemCreateDto { ArticleId = 1, Quantity = 2 } };

        _mockAppUserRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockArticleRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(article);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _orderService.CreateOrderAsync(userId, cartItems, CancellationToken.None));

        Assert.Equal("Stock insuficiente para 'Rosa'. Pedido: 2, Disponível: 1.", ex.Message);
    }

    // --- Tests for CreateOrderAsync ---
    [Fact]
    public async Task CreateOrderAsync_Should_CreateOrderAndReduceStock_WhenValid()
    {
        // Arrange
        var userId = "valid-user";
        var user = new AppUser { Id = userId, FullName = "Test User", Address = "123 Main St" };

        var article1 = new Article { Id = 1, Name = "Rosa", Price = 10m, StockQuantity = 5 };
        var article2 = new Article { Id = 2, Name = "Lírio", Price = 5m, StockQuantity = 10 };

        var cartItems = new List<CartItemCreateDto>
        {
            new CartItemCreateDto { ArticleId = 1, Quantity = 2 },
            new CartItemCreateDto { ArticleId = 2, Quantity = 1 }
        };

        Order? capturedOrder = null;

        _mockAppUserRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockArticleRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(article1);
        _mockArticleRepo.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(article2);

        _mockOrderRepo.Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, ct) =>
            {
                order.Id = 123;
                capturedOrder = order;
            });

        _mockOrderRepo.Setup(r => r.GetOrderDetailsAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                if (capturedOrder != null)
                {
                    capturedOrder.User = user;
                    capturedOrder.OrderItems.First(oi => oi.ArticleId == 1).Article = article1;
                    capturedOrder.OrderItems.First(oi => oi.ArticleId == 2).Article = article2;
                }
                return capturedOrder;
            });

        // Act
        var resultDto = await _orderService.CreateOrderAsync(userId, cartItems, CancellationToken.None);

        // Assert
        Assert.NotNull(resultDto);
        Assert.Equal(123, resultDto.Id);
        Assert.Equal(25m, resultDto.TotalPrice);
        Assert.Equal("Pendente", resultDto.OrderStatus);
        Assert.Equal(2, resultDto.OrderItems.Count);
        Assert.Equal("Test User", resultDto.FullName);
        Assert.Equal("123 Main St", resultDto.Address);

        Assert.Equal(3, article1.StockQuantity);
        Assert.Equal(9, article2.StockQuantity);

        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // --- Tests for GetOrderDetailsAsync ---
    [Fact]
    public async Task GetOrderDetailsAsync_Should_ReturnNull_WhenOrderNotFound()
    {
        // Arrange
        var orderId = 99;
        _mockOrderRepo.Setup(r => r.GetOrderDetailsAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _orderService.GetOrderDetailsAsync(orderId, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    // --- Tests for GetOrderDetailsAsync ---
    [Fact]
    public async Task GetOrderDetailsAsync_Should_ReturnMappedDto_WhenOrderFound()
    {
        // Arrange
        var orderId = 1;
        var user = new AppUser { Id = "user1", FullName = "Test User", Address = "123 Main St" };
        var article = new Article { Id = 1, Name = "Rosa", ImageUrl = "/img.jpg" };
        var order = new Order
        {
            Id = orderId,
            UserId = "user1",
            User = user,
            TotalPrice = 10m,
            OrderStatus = "Enviado",
            OrderDate = new DateTime(2025, 1, 1),
            OrderItems = new List<OrderItem>
            {
                new OrderItem { ArticleId = 1, Article = article, Quantity = 1, UnitPrice = 10m }
            }
        };

        _mockOrderRepo.Setup(r => r.GetOrderDetailsAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var result = await _orderService.GetOrderDetailsAsync(orderId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(orderId, result.Id);
        Assert.Equal("Enviado", result.OrderStatus);
        Assert.Equal("Test User", result.FullName);
        Assert.Equal(1, result.OrderItems.Count);
        Assert.Equal("Rosa", result.OrderItems.First().ArticleName);
    }

    // --- Tests for GetOrdersForUserAsync ---
    [Fact]
    public async Task GetOrdersForUserAsync_Should_ReturnUserOrders()
    {
        // Arrange
        var userId = "user-with-orders";
        var user = new AppUser { Id = userId, FullName = "Test User" };
        var orders = new List<Order>
        {
            new Order { Id = 1, UserId = userId, User = user, TotalPrice = 100 },
            new Order { Id = 2, UserId = userId, User = user, TotalPrice = 50 }
        };

        _mockOrderRepo.Setup(r => r.GetOrdersByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        // Act
        var result = await _orderService.GetOrdersForUserAsync(userId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Equal(100, result.First().TotalPrice);
    }

    // --- Tests for GetOrdersForUserAsync ---
    [Fact]
    public async Task GetOrdersForUserAsync_Should_ReturnEmptyList_WhenUserHasNoOrders()
    {
        // Arrange
        var userId = "user-no-orders";
        _mockOrderRepo.Setup(r => r.GetOrdersByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());

        // Act
        var result = await _orderService.GetOrdersForUserAsync(userId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
