using Moq;
using PlantShop.Application.Interfaces.Persistence;
using PlantShop.Application.Interfaces.Services.Shop;
using PlantShop.Application.Services.Shop;
using PlantShop.Application.DTOs.Shop;
using PlantShop.Domain.Entities.Shop;

namespace PlantShop.Application.UnitTests.Services.Shop;

public class CategoryServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ICategoryRepository> _mockCategoryRepository;
    private readonly Mock<IArticleRepository> _mockArticleRepository;
    private readonly ICategoryService _categoryService;

    public CategoryServiceTests()
    {
        _mockCategoryRepository = new Mock<ICategoryRepository>();
        _mockArticleRepository = new Mock<IArticleRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        _mockUnitOfWork.Setup(uow => uow.Categories).Returns(_mockCategoryRepository.Object);
        _mockUnitOfWork.Setup(uow => uow.Articles).Returns(_mockArticleRepository.Object);

        _categoryService = new CategoryService(_mockUnitOfWork.Object);
    }

    // --- Tests for GetAllCategoriesAsync ---
    [Fact]
    public async Task GetAllCategoriesAsync_WhenCategoriesExist_ShouldReturnMappedDtos()
    {
        var categories = new List<Category>
        {
            new Category { Id = 1, Name = "Cat1", Description = "Desc1" },
            new Category { Id = 2, Name = "Cat2", Description = "Desc2" }
        };
        _mockCategoryRepository.Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
                               .ReturnsAsync(categories);

        var result = await _categoryService.GetAllCategoriesAsync();

        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Equal(1, result.First().Id);
        Assert.Equal("Cat1", result.First().Name);
        Assert.Equal(2, result.Last().Id);
        Assert.Equal("Cat2", result.Last().Name);
    }

    [Fact]
    public async Task GetAllCategoriesAsync_WhenNoCategoriesExist_ShouldReturnEmptyList()
    {
        _mockCategoryRepository.Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
                               .ReturnsAsync(new List<Category>());

        var result = await _categoryService.GetAllCategoriesAsync();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    // --- Tests for GetCategoryByIdAsync ---

    [Fact]
    public async Task GetCategoryByIdAsync_WhenCategoryExists_ShouldReturnMappedDto()
    {
        var categoryId = 1;
        var category = new Category { Id = categoryId, Name = "Test Cat", Description = "Test Desc" };
        _mockCategoryRepository.Setup(repo => repo.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
                               .ReturnsAsync(category);

        var result = await _categoryService.GetCategoryByIdAsync(categoryId);

        Assert.NotNull(result);
        Assert.Equal(categoryId, result.Id);
        Assert.Equal(category.Name, result.Name);
        Assert.Equal(category.Description, result.Description);
    }

    [Fact]
    public async Task GetCategoryByIdAsync_WhenCategoryDoesNotExist_ShouldReturnNull()
    {
        var categoryId = 99;
        _mockCategoryRepository.Setup(repo => repo.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
                               .ReturnsAsync((Category?)null);

        var result = await _categoryService.GetCategoryByIdAsync(categoryId);

        Assert.Null(result);
    }

    // --- Tests for CreateCategoryAsync ---

    [Fact]
    public async Task CreateCategoryAsync_WithValidData_ShouldCallAddAsyncAndSaveChangesAsyncAndReturnDtoWithId()
    {
        var inputDto = new CategoryDto { Name = "New Cat", Description = "New Desc" };
        Category? addedCategory = null;

        _mockCategoryRepository.Setup(repo => repo.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
                               .Callback<Category, CancellationToken>((cat, ct) =>
                               {
                                   cat.Id = 5;
                                   addedCategory = cat;
                               })
                               .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _categoryService.CreateCategoryAsync(inputDto);

        Assert.NotNull(result);
        Assert.Equal(5, result.Id);
        Assert.Equal(inputDto.Name, result.Name);
        Assert.NotNull(addedCategory);
        Assert.Equal(inputDto.Name, addedCategory.Name);
        Assert.Equal(inputDto.Description, addedCategory.Description);
        _mockCategoryRepository.Verify(repo => repo.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // --- Tests for UpdateCategoryAsync ---
    [Fact]
    public async Task UpdateCategoryAsync_WhenCategoryExists_ShouldLoadCategoryUpdatePropertiesAndSaveChangesAsync()
    {
        var categoryId = 1;
        var updateDto = new CategoryDto { Id = categoryId, Name = "Updated Name", Description = "Updated Desc" };
        var existingCategory = new Category { Id = categoryId, Name = "Original Name", Description = "Original Desc" };

        _mockCategoryRepository.Setup(repo => repo.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
                               .ReturnsAsync(existingCategory);
        _mockUnitOfWork.Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _categoryService.UpdateCategoryAsync(updateDto);

        Assert.Equal(updateDto.Name, existingCategory.Name);
        Assert.Equal(updateDto.Description, existingCategory.Description);
        _mockCategoryRepository.Verify(repo => repo.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockCategoryRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateCategoryAsync_WhenCategoryDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        var categoryId = 99;
        var updateDto = new CategoryDto { Id = categoryId, Name = "Updated Name" };
        _mockCategoryRepository.Setup(repo => repo.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
                               .ReturnsAsync((Category?)null);

        Func<Task> act = async () => await _categoryService.UpdateCategoryAsync(updateDto);

        await Assert.ThrowsAsync<KeyNotFoundException>(act);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // --- Tests for DeleteCategoryAsync ---
    [Fact]
    public async Task DeleteCategoryAsync_WhenCategoryExistsAndHasNoArticles_ShouldLoadCategoryCallDeleteAsyncAndSaveChangesAsync()
    {
        var categoryId = 1;
        var existingCategory = new Category { Id = categoryId, Name = "ToDelete" };

        _mockCategoryRepository.Setup(repo => repo.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
                               .ReturnsAsync(existingCategory);
        _mockArticleRepository.Setup(repo => repo.ExistsWithCategoryIdAsync(categoryId, It.IsAny<CancellationToken>()))
                              .ReturnsAsync(false);
        _mockCategoryRepository.Setup(repo => repo.DeleteAsync(existingCategory, It.IsAny<CancellationToken>()))
                               .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _categoryService.DeleteCategoryAsync(categoryId);

        _mockCategoryRepository.Verify(repo => repo.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()), Times.Once);
        _mockArticleRepository.Verify(repo => repo.ExistsWithCategoryIdAsync(categoryId, It.IsAny<CancellationToken>()), Times.Once);
        _mockCategoryRepository.Verify(repo => repo.DeleteAsync(existingCategory, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteCategoryAsync_WhenCategoryDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        var categoryId = 99;
        _mockCategoryRepository.Setup(repo => repo.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
                               .ReturnsAsync((Category?)null);

        Func<Task> act = async () => await _categoryService.DeleteCategoryAsync(categoryId);

        await Assert.ThrowsAsync<KeyNotFoundException>(act);
        _mockArticleRepository.Verify(repo => repo.ExistsWithCategoryIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockCategoryRepository.Verify(repo => repo.DeleteAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCategoryAsync_WhenCategoryExistsAndHasArticles_ShouldThrowInvalidOperationException()
    {
        var categoryId = 1;
        var existingCategory = new Category { Id = categoryId, Name = "ToDeleteWithArticles" };

        _mockCategoryRepository.Setup(repo => repo.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
                               .ReturnsAsync(existingCategory);
        _mockArticleRepository.Setup(repo => repo.ExistsWithCategoryIdAsync(categoryId, It.IsAny<CancellationToken>()))
                              .ReturnsAsync(true);

        Func<Task> act = async () => await _categoryService.DeleteCategoryAsync(categoryId);

        await Assert.ThrowsAsync<InvalidOperationException>(act);
        _mockCategoryRepository.Verify(repo => repo.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()), Times.Once);
        _mockArticleRepository.Verify(repo => repo.ExistsWithCategoryIdAsync(categoryId, It.IsAny<CancellationToken>()), Times.Once);
        _mockCategoryRepository.Verify(repo => repo.DeleteAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}