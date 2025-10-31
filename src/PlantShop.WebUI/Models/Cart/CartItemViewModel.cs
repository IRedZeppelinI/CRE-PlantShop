namespace PlantShop.WebUI.Models.Cart;

public class CartItemViewModel
{
    public int ArticleId { get; set; }
    public string ArticleName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }

    
    public decimal TotalPrice => UnitPrice * Quantity;
}