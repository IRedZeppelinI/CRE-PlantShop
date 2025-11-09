using PlantShop.Application.DTOs.Shop;

namespace PlantShop.Application.Interfaces.Services.Shop;


public interface IOrderService
{   
    Task<OrderDto> CreateOrderAsync(string userId, IEnumerable<CartItemDto> cartItems, CancellationToken cancellationToken = default);
       
    Task<OrderDto?> GetOrderDetailsAsync(int orderId, CancellationToken cancellationToken = default);
        
    Task<IEnumerable<OrderDto>> GetOrdersForUserAsync(string userId, CancellationToken cancellationToken = default);

    Task<IEnumerable<OrderDto>> GetAllOrdersAsync(CancellationToken cancellationToken = default);

    Task MarkOrderAsShippedAsync(int orderId, CancellationToken cancellationToken = default);
}