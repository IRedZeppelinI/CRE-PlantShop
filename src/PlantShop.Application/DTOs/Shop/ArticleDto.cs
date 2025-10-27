﻿namespace PlantShop.Application.DTOs.Shop;
public class ArticleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsFeatured { get; set; }

    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}
