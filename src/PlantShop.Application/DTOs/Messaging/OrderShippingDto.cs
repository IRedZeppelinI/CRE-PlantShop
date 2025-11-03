namespace PlantShop.Application.DTOs.Messaging;

public class OrderShippingDto
{
    public int OrderId { get; set; }
    public string CustomerFullName { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
        
}