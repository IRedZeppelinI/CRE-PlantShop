using PlantShop.Application.DTOs.Shop;

namespace PlantShop.Application.Interfaces.Services.Shop;


public interface ICategoryService
{
   
    Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync(CancellationToken cancellationToken = default);

    
    Task<CategoryDto?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default);

    
    Task<CategoryDto> CreateCategoryAsync(CategoryDto categoryDto, CancellationToken cancellationToken = default);

    
    Task UpdateCategoryAsync(CategoryDto categoryDto, CancellationToken cancellationToken = default);

    
    Task DeleteCategoryAsync(int id, CancellationToken cancellationToken = default);
}
