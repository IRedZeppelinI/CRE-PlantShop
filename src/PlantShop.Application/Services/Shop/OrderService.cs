using Microsoft.Extensions.Logging;
using PlantShop.Application.DTOs.Messaging;
using PlantShop.Application.DTOs.Shop;
using PlantShop.Application.Interfaces.Infrastructure;
using PlantShop.Application.Interfaces.Persistence;
using PlantShop.Application.Interfaces.Services.Shop;
using PlantShop.Domain.Entities.Shop;

namespace PlantShop.Application.Services.Shop;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderService> _logger;
    private readonly IMessagePublisher _messagePublisher;

    public OrderService(IUnitOfWork unitOfWork, ILogger<OrderService> logger, IMessagePublisher messagePublisher)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _messagePublisher = messagePublisher;
    }
        
    public async Task<OrderDto> CreateOrderAsync(
        string userId,
        IEnumerable<CartItemDto> cartItems,
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


    public async Task<IEnumerable<OrderDto>> GetAllOrdersAsync(CancellationToken cancellationToken = default)
    {        
        var orders = await _unitOfWork.Orders.GetAllOrdersAsync(cancellationToken);
                
        return orders.Select(MapOrderToDto);
    }


    public async Task MarkOrderAsShippedAsync(int orderId, CancellationToken cancellationToken = default)
    {
        // GetOrderDetailsAsync tem AsNoTracking() por isso uso GetByIdAsync e vou buscar o user separadamente         
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId, cancellationToken);

        if (order == null)
        {
            _logger.LogWarning("Tentativa de marcar encomenda inexistente ({OrderId}) como enviada.", orderId);
            throw new KeyNotFoundException($"Encomenda com ID {orderId} não encontrada.");
        }

        var user = await _unitOfWork.AppUsers.GetByIdAsync(order.UserId, cancellationToken);

        if (user == null)
        {
            // Sanity check, embora a FK não deva permitir isto
            _logger.LogError("Encomenda {OrderId} não tem utilizador associado.", orderId);
            throw new InvalidOperationException("A encomenda não tem um utilizador associado.");
        }

        //  Validar o estado
        if (order.OrderStatus != "Pendente")
        {
            _logger.LogWarning("Tentativa de marcar encomenda ({OrderId}) como enviada, mas o estado já é {Status}.", orderId, order.OrderStatus);
            throw new InvalidOperationException($"A encomenda não pode ser enviada (Estado atual: {order.OrderStatus}).");
        }

        //  Atualizar o estado na Base de Dados
        order.OrderStatus = "Enviada";
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Encomenda {OrderId} marcada como 'Enviada' na Base de Dados.", orderId);

        //  Mapear para o DTO da mensagem
        var shippingDto = new OrderShippingDto
        {
            OrderId = order.Id,
            CustomerFullName = user.FullName,
            // O CreateOrderAsync já valida que a morada existe
            ShippingAddress = user.Address ?? "Morada não fornecida"
        };

        // Publicar a mensagem 
        try
        {
            await _messagePublisher.PublishOrderForShippingAsync(shippingDto, cancellationToken);
            _logger.LogInformation("Mensagem para a Encomenda {OrderId} publicada no Service Bus.", orderId);
        }
        catch (Exception ex)
        {
            // Se a publicação falhar, a BD já foi alterada.           
            _logger.LogError(ex, "Falha ao publicar mensagem no Service Bus para a Encomenda {OrderId} (Estado já está 'Enviada').", orderId);
            
        }
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