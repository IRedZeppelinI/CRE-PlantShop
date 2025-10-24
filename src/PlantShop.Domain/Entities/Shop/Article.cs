namespace PlantShop.Domain.Entities.Shop;

public class Article
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsFeatured { get; set; } = false;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!; 
}
