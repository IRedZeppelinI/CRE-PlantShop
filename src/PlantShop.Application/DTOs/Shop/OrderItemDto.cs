namespace PlantShop.Application.DTOs.Shop;

public class OrderItemDto
{
    public int ArticleId { get; set; }
    public string ArticleName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string? ImageUrl { get; set; }
}