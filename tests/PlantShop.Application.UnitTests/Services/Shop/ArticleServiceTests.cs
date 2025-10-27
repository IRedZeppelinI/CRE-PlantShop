using Moq;
using PlantShop.Application.Interfaces.Persistence;
using PlantShop.Application.Interfaces.Services.Shop;
using PlantShop.Application.Services.Shop;
using PlantShop.Application.DTOs.Shop;
using PlantShop.Domain.Entities.Shop;

namespace PlantShop.Application.UnitTests.Services.Shop;

public class ArticleServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IArticleRepository> _mockArticleRepository;
    private readonly Mock<ICategoryRepository> _mockCategoryRepository; // Para validações
    private readonly Mock<IOrderItemRepository> _mockOrderItemRepository; // Para validação Delete

    private readonly IArticleService _articleService;

    public ArticleServiceTests()
    {
        _mockArticleRepository = new Mock<IArticleRepository>();
        _mockCategoryRepository = new Mock<ICategoryRepository>();
        _mockOrderItemRepository = new Mock<IOrderItemRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        _mockUnitOfWork.Setup(uow => uow.Articles).Returns(_mockArticleRepository.Object);
        _mockUnitOfWork.Setup(uow => uow.Categories).Returns(_mockCategoryRepository.Object);
        _mockUnitOfWork.Setup(uow => uow.OrderItems).Returns(_mockOrderItemRepository.Object);

        _articleService = new ArticleService(_mockUnitOfWork.Object);
    }

    // Helper create Article
    private Article CreateTestArticle(int id = 1, int categoryId = 1, bool isFeatured = false)
    {
        return new Article
        {
            Id = id,
            Name = $"Article {id}",
            Price = 10m * id,
            CategoryId = categoryId,
            IsFeatured = isFeatured,
            Category = new Category { Id = categoryId, Name = $"Category {categoryId}" } // Inclui Categoria mockada
        };
    }

    // --- GetAllArticlesAsync Tests ---

    [Fact]
    public async Task GetAllArticlesAsync_ShouldReturnMappedDtos()
    {
        var articles = new List<Article> { CreateTestArticle(1), CreateTestArticle(2) };
        _mockArticleRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(articles);

        var result = await _articleService.GetAllArticlesAsync();

        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Equal("Article 1", result.First().Name);
        Assert.Equal("Category 1", result.First().CategoryName); 
    }

    // --- GetArticleByIdAsync Tests ---

    [Fact]
    public async Task GetArticleByIdAsync_WhenExists_ShouldReturnMappedDto()
    {
        var article = CreateTestArticle(1);
        _mockArticleRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(article);

        var result = await _articleService.GetArticleByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(article.Id, result.Id);
        Assert.Equal(article.Name, result.Name);
        Assert.Equal(article.Category.Name, result.CategoryName);
    }

    [Fact]
    public async Task GetArticleByIdAsync_WhenNotExists_ShouldReturnNull()
    {
        _mockArticleRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((Article?)null);

        var result = await _articleService.GetArticleByIdAsync(99);

        Assert.Null(result);
    }

    // --- GetArticlesByCategoryAsync Tests ---

    [Fact]
    public async Task GetArticlesByCategoryAsync_ShouldReturnMappedDtos()
    {
        var categoryId = 1;
        var articles = new List<Article> { CreateTestArticle(1, categoryId), CreateTestArticle(3, categoryId) };
        _mockArticleRepository.Setup(r => r.GetArticlesByCategoryAsync(categoryId, It.IsAny<CancellationToken>())).ReturnsAsync(articles);

        var result = await _articleService.GetArticlesByCategoryAsync(categoryId);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.True(result.All(a => a.CategoryId == categoryId));
    }

    // --- GetFeaturedArticlesAsync Tests ---

    [Fact]
    public async Task GetFeaturedArticlesAsync_ShouldReturnOnlyFeaturedMappedDtos()
    {
        var articles = new List<Article> { 
            CreateTestArticle(1, isFeatured: true),
            CreateTestArticle(2, isFeatured: false),
            CreateTestArticle(3, isFeatured: true) };

        _mockArticleRepository.Setup(r => r
            .GetFeaturedArticlesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(articles.Where(a => a.IsFeatured));

        var result = await _articleService.GetFeaturedArticlesAsync();

        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.True(result.All(a => a.IsFeatured));
        Assert.Contains(result, a => a.Id == 1);
        Assert.Contains(result, a => a.Id == 3);
    }

    // --- CreateArticleAsync Tests ---

    [Fact]
    public async Task CreateArticleAsync_WithValidData_ShouldAddAndSaveAndReturnDto()
    {
        var inputDto = new ArticleDto { Name = "New", Price = 5m, CategoryId = 1, CategoryName = "Will Be Ignored" }; 
        Article addedArticle = null!;
        var existingCategory = new Category { Id = 1, Name = "Existing Cat" };

        _mockCategoryRepository.Setup(r => r
            .GetByIdAsync(inputDto.CategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory); 

        _mockArticleRepository.Setup(r => r
            .AddAsync(It.IsAny<Article>(), It.IsAny<CancellationToken>()))
            .Callback<Article, CancellationToken>((a, ct) => { a.Id = 99; addedArticle = a; }) 
            .Returns(Task.CompletedTask);

        _mockArticleRepository.Setup(r => r
            .GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new Article{
                Id = 99,
                Name = addedArticle.Name,
                CategoryId = addedArticle.CategoryId,
                Category = existingCategory });

        _mockUnitOfWork.Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);


        var result = await _articleService.CreateArticleAsync(inputDto);

        Assert.NotNull(result);
        Assert.Equal(99, result.Id);
        Assert.Equal(inputDto.Name, result.Name);
        Assert.Equal(existingCategory.Name, result.CategoryName); 
        _mockArticleRepository.Verify(r => r.AddAsync(It.IsAny<Article>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateArticleAsync_WithInvalidCategoryId_ShouldThrowArgumentException()
    {
        var inputDto = new ArticleDto { Name = "New", Price = 5m, CategoryId = 0 };

        Func<Task> act = async () => await _articleService.CreateArticleAsync(inputDto);

        await Assert.ThrowsAsync<ArgumentException>(act);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateArticleAsync_WithNonExistentCategoryId_ShouldThrowKeyNotFoundException()
    {
        var inputDto = new ArticleDto { Name = "New", Price = 5m, CategoryId = 99 };
        _mockCategoryRepository.Setup(r => r
            .GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null); 

        Func<Task> act = async () => await _articleService.CreateArticleAsync(inputDto);

        await Assert.ThrowsAsync<KeyNotFoundException>(act);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // --- UpdateArticleAsync Tests ---

    [Fact]
    public async Task UpdateArticleAsync_WhenArticleExistsAndCategoryIsValid_ShouldUpdateAndSave()
    {
        var articleId = 1;
        var categoryId = 1;
        var updateDto = new ArticleDto { Id = articleId, Name = "Updated", Price = 15m, CategoryId = categoryId };
        var existingArticle = CreateTestArticle(articleId, categoryId); // Artigo original
        var existingCategory = new Category { Id = categoryId, Name = "Existing Cat" };

        _mockArticleRepository.Setup(r => r
            .GetByIdAsync(articleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingArticle);

        _mockCategoryRepository.Setup(r => r
            .GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory); 

        _mockUnitOfWork.Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _articleService.UpdateArticleAsync(updateDto);

        Assert.Equal(updateDto.Name, existingArticle.Name);
        Assert.Equal(updateDto.Price, existingArticle.Price);
        Assert.Equal(updateDto.CategoryId, existingArticle.CategoryId);
        _mockArticleRepository.Verify(r => r.GetByIdAsync(articleId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateArticleAsync_WhenArticleDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        var articleId = 99;
        var updateDto = new ArticleDto { Id = articleId, Name = "Updated" };
        _mockArticleRepository.Setup(r => r
            .GetByIdAsync(articleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Article?)null);

        Func<Task> act = async () => await _articleService.UpdateArticleAsync(updateDto);

        await Assert.ThrowsAsync<KeyNotFoundException>(act);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateArticleAsync_WithNonExistentCategoryId_ShouldThrowKeyNotFoundException()
    {
        var articleId = 1;
        var newCategoryId = 99;
        var updateDto = new ArticleDto { Id = articleId, Name = "Updated", CategoryId = newCategoryId };
        var existingArticle = CreateTestArticle(articleId, 1); // Categoria original é 1

        _mockArticleRepository.Setup(r => r
            .GetByIdAsync(articleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingArticle);

        _mockCategoryRepository.Setup(r => r
            .GetByIdAsync(newCategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null); // Nova categoria NÃO existe

        Func<Task> act = async () => await _articleService.UpdateArticleAsync(updateDto);

        await Assert.ThrowsAsync<KeyNotFoundException>(act);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }


    // --- DeleteArticleAsync Tests ---

    [Fact]
    public async Task DeleteArticleAsync_WhenArticleExistsAndNotInOrders_ShouldDeleteAndSave()
    {
        var articleId = 1;
        var existingArticle = CreateTestArticle(articleId);

        _mockArticleRepository.Setup(r => r
            .GetByIdAsync(articleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingArticle);

        _mockOrderItemRepository.Setup(r => r
            .ExistsWithArticleIdAsync(articleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Não está em orders

        _mockArticleRepository.Setup(r => r
            .DeleteAsync(existingArticle, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _articleService.DeleteArticleAsync(articleId);

        _mockArticleRepository.Verify(r => r.GetByIdAsync(articleId, It.IsAny<CancellationToken>()), Times.Once);
        _mockOrderItemRepository.Verify(r => r.ExistsWithArticleIdAsync(articleId, It.IsAny<CancellationToken>()), Times.Once);
        _mockArticleRepository.Verify(r => r.DeleteAsync(existingArticle, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteArticleAsync_WhenArticleDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        var articleId = 99;
        _mockArticleRepository.Setup(r => r
            .GetByIdAsync(articleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Article?)null);

        Func<Task> act = async () => await _articleService.DeleteArticleAsync(articleId);

        await Assert.ThrowsAsync<KeyNotFoundException>(act);
        _mockOrderItemRepository.Verify(r => r
            .ExistsWithArticleIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockArticleRepository.Verify(r => r
            .DeleteAsync(It.IsAny<Article>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteArticleAsync_WhenArticleIsInOrders_ShouldThrowInvalidOperationException()
    {
        var articleId = 1;
        var existingArticle = CreateTestArticle(articleId);

        _mockArticleRepository.Setup(r => r
            .GetByIdAsync(articleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingArticle);
        _mockOrderItemRepository.Setup(r => r
            .ExistsWithArticleIdAsync(articleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // ESTÁ em orders

        Func<Task> act = async () => await _articleService.DeleteArticleAsync(articleId);

        await Assert.ThrowsAsync<InvalidOperationException>(act);
        _mockArticleRepository.Verify(r => r.GetByIdAsync(articleId, It.IsAny<CancellationToken>()), Times.Once);
        _mockOrderItemRepository.Verify(r => r.ExistsWithArticleIdAsync(articleId, It.IsAny<CancellationToken>()), Times.Once);
        _mockArticleRepository.Verify(r => r.DeleteAsync(It.IsAny<Article>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}