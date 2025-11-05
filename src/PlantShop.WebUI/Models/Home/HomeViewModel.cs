using PlantShop.Application.DTOs.Shop;

namespace PlantShop.WebUI.Models.Home;

public class HomeViewModel
{
    public IEnumerable<ArticleDto> FeaturedArticles { get; set; } = new List<ArticleDto>();
}