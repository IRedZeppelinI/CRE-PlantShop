using PlantShop.Application.Interfaces.Services.Shop; 
using PlantShop.Application.Interfaces.Persistence; 
using PlantShop.Application.DTOs.Shop; 
using PlantShop.Domain.Entities.Shop; 


namespace PlantShop.Application.Services.Shop;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public CategoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
    {
       
        var categories = await _unitOfWork.Categories.GetAllAsync(cancellationToken);
        
        return categories.Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description
        });
    }

    public async Task<CategoryDto?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id, cancellationToken);

        if (category == null)
        {
            return null;
        }

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description
        };
    }

    public async Task<CategoryDto> CreateCategoryAsync(CategoryDto categoryDto, CancellationToken cancellationToken = default)
    {
        var categoryEntity = new Category
        {
            Name = categoryDto.Name,
            Description = categoryDto.Description
        };

        await _unitOfWork.Categories.AddAsync(categoryEntity, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken); 

        categoryDto.Id = categoryEntity.Id;
        return categoryDto;
    }


    public async Task UpdateCategoryAsync(CategoryDto categoryDto, CancellationToken cancellationToken = default)
    {
        var categoryEntity = await _unitOfWork.Categories.GetByIdAsync(categoryDto.Id, cancellationToken);

        if (categoryEntity == null)
        {
            throw new KeyNotFoundException($"Category with Id {categoryDto.Id} not found for update.");
        }

        categoryEntity.Name = categoryDto.Name;
        categoryEntity.Description = categoryDto.Description;
                
        await _unitOfWork.SaveChangesAsync(cancellationToken); 
    }

    
    public async Task DeleteCategoryAsync(int id, CancellationToken cancellationToken = default)
    {
        var categoryEntity = await _unitOfWork.Categories.GetByIdAsync(id, cancellationToken);

        if (categoryEntity == null)
        {
            throw new KeyNotFoundException($"Category with Id {id} not found for deletion.");
        }

        
        bool hasArticles = await _unitOfWork.Articles.ExistsWithCategoryIdAsync(id, cancellationToken); 
        if (hasArticles)
        {
            throw new InvalidOperationException($"Cannot delete category '{categoryEntity.Name}' (Id: {id}) because it has associated articles.");
        }
        
        await _unitOfWork.Categories.DeleteAsync(categoryEntity, cancellationToken); 
        
        await _unitOfWork.SaveChangesAsync(cancellationToken); 
    }
}

