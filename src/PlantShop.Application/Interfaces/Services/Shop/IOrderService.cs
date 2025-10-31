using PlantShop.Application.DTOs.Shop;

namespace PlantShop.Application.Interfaces.Services.Shop;

// Simple DTO auxiliar
public class CartItemCreateDto
{
    public int ArticleId { get; set; }
    public int Quantity { get; set; }
}


public interface IOrderService
{   
    Task<OrderDto> CreateOrderAsync(string userId, IEnumerable<CartItemCreateDto> cartItems, CancellationToken cancellationToken = default);
       
    Task<OrderDto?> GetOrderDetailsAsync(int orderId, CancellationToken cancellationToken = default);
        
    Task<IEnumerable<OrderDto>> GetOrdersForUserAsync(string userId, CancellationToken cancellationToken = default);

    Task<IEnumerable<OrderDto>> GetAllOrdersAsync(CancellationToken cancellationToken = default);

}