using PlantShop.Application.DTOs.Shop;

namespace PlantShop.WebUI.Models.Shop;

public class ShopViewModel
{    
    public IEnumerable<ArticleDto> Articles { get; set; } = new List<ArticleDto>();
        
    public IEnumerable<CategoryDto> Categories { get; set; } = new List<CategoryDto>();
        
    //para filtrar por categoria se necessário
    public int? SelectedCategoryId { get; set; } 
}