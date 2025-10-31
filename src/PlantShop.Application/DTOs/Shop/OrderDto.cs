namespace PlantShop.Application.DTOs.Shop;

public class OrderDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty; 
    public DateTime OrderDate { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public string? Address { get; set; } 

    public List<OrderItemDto> OrderItems { get; set; } = new List<OrderItemDto>();
}