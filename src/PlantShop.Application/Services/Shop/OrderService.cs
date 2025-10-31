using Microsoft.Extensions.Logging;
using PlantShop.Application.DTOs.Shop;
using PlantShop.Application.Interfaces.Persistence;
using PlantShop.Application.Interfaces.Services.Shop;
using PlantShop.Domain.Entities.Shop;

namespace PlantShop.Application.Services.Shop;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IUnitOfWork unitOfWork, ILogger<OrderService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }
        
    public async Task<OrderDto> CreateOrderAsync(
        string userId,
        IEnumerable<CartItemCreateDto> cartItems,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Iniciando criação de encomenda para UserId: {UserId}", userId);

        var user = await _unitOfWork.AppUsers.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("Tentativa de criar encomenda para utilizador não existente (Id: {UserId})", userId);
            throw new KeyNotFoundException("Utilizador não encontrado.");
        }

        if (string.IsNullOrEmpty(user.Address))
        {
            _logger.LogWarning("Utilizador (Id: {UserId}) tentou finalizar compra sem morada.", userId);
            throw new InvalidOperationException("Não tem uma morada de envio associada. Por favor, adicione uma morada no seu perfil antes de finalizar a compra.");
        }

        if (cartItems == null || !cartItems.Any())
        {
            _logger.LogWarning("Utilizador (Id: {UserId}) tentou finalizar compra com carrinho vazio.", userId);
            throw new InvalidOperationException("O carrinho de compras não pode estar vazio.");
        }

        var orderItemsList = new List<OrderItem>();
        decimal totalOrderPrice = 0;

        foreach (var cartItem in cartItems)
        {
            // Obtém o artigo da base de dados (fonte de verdade)
            var article = await _unitOfWork.Articles.GetByIdAsync(cartItem.ArticleId, cancellationToken);
            if (article == null)
            {
                _logger.LogError("FALHA DE INTEGRIDADE: Artigo (Id: {ArticleId}) no carrinho não existe na BD.", cartItem.ArticleId);
                throw new KeyNotFoundException($"O artigo com Id {cartItem.ArticleId} já não existe.");
            }

            // Validar stock
            if (article.StockQuantity < cartItem.Quantity)
            {
                _logger.LogWarning("Stock insuficiente para Artigo (Id: {ArticleId}). Pedido: {Quantity}, Stock: {Stock}",
                    cartItem.ArticleId, cartItem.Quantity, article.StockQuantity);
                throw new InvalidOperationException($"Stock insuficiente para '{article.Name}'. Pedido: {cartItem.Quantity}, Disponível: {article.StockQuantity}.");
            }

            // Reduzir o stock (o EF Core tracking fará o Update)
            article.StockQuantity -= cartItem.Quantity;

            var orderItem = new OrderItem
            {
                ArticleId = article.Id,
                Quantity = cartItem.Quantity,
                UnitPrice = article.Price 
            };

            orderItemsList.Add(orderItem);
            totalOrderPrice += (orderItem.UnitPrice * orderItem.Quantity);
        }

        // 4. Criar a Entidade Order
        var newOrder = new Order
        {
            UserId = userId,
            OrderDate = DateTime.UtcNow, // Usar UTC no servidor
            OrderStatus = "Pendente",    
            TotalPrice = totalOrderPrice,
            OrderItems = orderItemsList  
        };

       
        await _unitOfWork.Orders.AddAsync(newOrder, cancellationToken);

        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Encomenda {OrderId} criada com sucesso para UserId: {UserId}. Total: {Total}",
            newOrder.Id, userId, newOrder.TotalPrice);

        
        var createdOrderDetails = await _unitOfWork.Orders.GetOrderDetailsAsync(newOrder.Id, cancellationToken);

        if (createdOrderDetails == null)
        {
            _logger.LogError("FALHA ao obter detalhes da encomenda recém-criada (Id: {OrderId})", newOrder.Id);
            throw new InvalidOperationException("A encomenda foi criada, mas ocorreu um erro ao obter os seus detalhes.");
        }

        return MapOrderToDto(createdOrderDetails);
    }

    
    public async Task<OrderDto?> GetOrderDetailsAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.Orders.GetOrderDetailsAsync(orderId, cancellationToken);

        if (order == null)
        {
            return null;
        }

        return MapOrderToDto(order);
    }

    
    public async Task<IEnumerable<OrderDto>> GetOrdersForUserAsync(string userId, CancellationToken cancellationToken = default)
    {        
        var orders = await _unitOfWork.Orders.GetOrdersByUserIdAsync(userId, cancellationToken);

        
        return orders.Select(MapOrderToDto);
    }


    
    private OrderDto MapOrderToDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            UserId = order.UserId,
            OrderDate = order.OrderDate,
            OrderStatus = order.OrderStatus,
            TotalPrice = order.TotalPrice,

            // Verifica se 'User' foi incluído
            FullName = order.User?.FullName ?? "N/A",
            Address = order.User?.Address,

            // Verifica se 'OrderItems' e 'Article' foram incluídos
            OrderItems = order.OrderItems?.Select(oi => new OrderItemDto
            {
                ArticleId = oi.ArticleId,
                ArticleName = oi.Article?.Name ?? "N/A",
                ImageUrl = oi.Article?.ImageUrl,
                UnitPrice = oi.UnitPrice,
                Quantity = oi.Quantity
            }).ToList() ?? new List<OrderItemDto>()
        };
    }
}