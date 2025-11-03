using Microsoft.Extensions.Logging;
using Moq;
using PlantShop.Application.DTOs.Shop;
using PlantShop.Application.Interfaces.Infrastructure;
using PlantShop.Application.Interfaces.Persistence;
using PlantShop.Application.Interfaces.Services.Shop;
using PlantShop.Application.Services.Shop;
using PlantShop.Domain.Entities.Shop;

namespace PlantShop.Application.UnitTests.Services.Shop;

public class ArticleServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IArticleRepository> _mockArticleRepository;
    private readonly Mock<ICategoryRepository> _mockCategoryRepository; // Para validações
    private readonly Mock<IOrderItemRepository> _mockOrderItemRepository; // Para validação Delete
    private readonly Mock<IFileStorageService> _mockFileStorageService;
    private readonly Mock<ILogger<ArticleService>> _mockLogger;

    private readonly IArticleService _articleService;

    public ArticleServiceTests()
    {
        _mockArticleRepository = new Mock<IArticleRepository>();
        _mockCategoryRepository = new Mock<ICategoryRepository>();
        _mockOrderItemRepository = new Mock<IOrderItemRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockFileStorageService = new Mock<IFileStorageService>();
        _mockLogger = new Mock<ILogger<ArticleService>>();

        _mockUnitOfWork.Setup(uow => uow.Articles).Returns(_mockArticleRepository.Object);
        _mockUnitOfWork.Setup(uow => uow.Categories).Returns(_mockCategoryRepository.Object);
        _mockUnitOfWork.Setup(uow => uow.OrderItems).Returns(_mockOrderItemRepository.Object);

        _articleService = new ArticleService(
            _mockUnitOfWork.Object,
            _mockFileStorageService.Object,
            _mockLogger.Object);
    }

    // Helper create Article
    private Article CreateTestArticle(int id = 1, int categoryId = 1, bool isFeatured = false, string? imageUrl = null)
    {
        return new Article
        {
            Id = id,
            Name = $"Article {id}",
            Price = 10m * id,
            CategoryId = categoryId,
            IsFeatured = isFeatured,
            ImageUrl = imageUrl,
            Category = new Category { Id = categoryId, Name = $"Category {categoryId}" } // Inclui Categoria mockada
        };
    }

    // Helper para criar um Mock de Stream
    private Stream CreateMockStream()
    {
        var mockStream = new MemoryStream();
        // Escreve alguns bytes para simular um ficheiro com conteúdo
        var writer = new StreamWriter(mockStream);
        writer.Write("mock file content");
        writer.Flush();
        mockStream.Position = 0;
        return mockStream;
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
    public async Task CreateArticleAsync_WithValidData_AndNoImage_ShouldAddAndSave()
    {
        var inputDto = new ArticleDto { Name = "New", Price = 5m, CategoryId = 1 };
        Article addedArticle = null!;
        var existingCategory = new Category { Id = 1, Name = "Existing Cat" };

        _mockCategoryRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingCategory);

        _mockArticleRepository.Setup(r => r.AddAsync(It.IsAny<Article>(), It.IsAny<CancellationToken>()))
            .Callback<Article, CancellationToken>((a, ct) => { a.Id = 99; addedArticle = a; });

        _mockArticleRepository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new Article { Id = 99, Name = addedArticle.Name, CategoryId = addedArticle.CategoryId, Category = existingCategory });

        _mockUnitOfWork.Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Chamar a nova assinatura
        var result = await _articleService.CreateArticleAsync(inputDto, null, null, null);

        Assert.NotNull(result);
        Assert.Equal(99, result.Id);
        Assert.Null(addedArticle.ImageUrl); // Garantir que não foi definida imagem
        _mockFileStorageService.Verify(fs => fs.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateArticleAsync_WithValidData_AndImage_ShouldUpload_AddAndSave()
    {
        var inputDto = new ArticleDto { Name = "New", Price = 5m, CategoryId = 1 };
        Article addedArticle = null!;
        var existingCategory = new Category { Id = 1, Name = "Existing Cat" };
        var fakeUrl = "http://storage.com/articles/new-guid.jpg";
        await using var mockStream = CreateMockStream();

        _mockCategoryRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingCategory);

        _mockArticleRepository.Setup(r => r.AddAsync(It.IsAny<Article>(), It.IsAny<CancellationToken>()))
            .Callback<Article, CancellationToken>((a, ct) => { a.Id = 99; addedArticle = a; });

        _mockFileStorageService.Setup(fs => fs.UploadAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                "image/jpeg",
                "articles",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeUrl);

        _mockArticleRepository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new Article { Id = 99, Name = addedArticle.Name, ImageUrl = addedArticle.ImageUrl, CategoryId = addedArticle.CategoryId, Category = existingCategory });

       
        var result = await _articleService.CreateArticleAsync(inputDto, mockStream, "file.jpg", "image/jpeg");

        Assert.NotNull(result);
        Assert.Equal(99, result.Id);
        Assert.Equal(fakeUrl, result.ImageUrl); // Garantir que o URL foi atribuído
        Assert.Equal(fakeUrl, addedArticle.ImageUrl); // Garantir que a entidade o tinha
        _mockFileStorageService.Verify(fs => fs.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), "image/jpeg", "articles", It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateArticleAsync_WithInvalidCategoryId_ShouldThrowArgumentException()
    {
        var inputDto = new ArticleDto { CategoryId = 0 };
        
        Func<Task> act = async () => await _articleService.CreateArticleAsync(inputDto, null, null, null);
        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    // --- UpdateArticleAsync Tests ---

    [Fact]
    public async Task UpdateArticleAsync_WithNoImageChange_ShouldUpdateAndSave()
    {
        var articleId = 1;
        var categoryId = 1;
        var existingUrl = "http://storage.com/articles/old.jpg";
        var updateDto = new ArticleDto { Id = articleId, Name = "Updated", CategoryId = categoryId, ImageUrl = existingUrl };
        var existingArticle = CreateTestArticle(articleId, categoryId, imageUrl: existingUrl);

        _mockArticleRepository.Setup(r => r.GetByIdAsync(articleId, It.IsAny<CancellationToken>())).ReturnsAsync(existingArticle);
        _mockCategoryRepository.Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>())).ReturnsAsync(new Category { Id = categoryId });

        
        await _articleService.UpdateArticleAsync(updateDto, null, null, null);

        Assert.Equal(updateDto.Name, existingArticle.Name);
        Assert.Equal(existingUrl, existingArticle.ImageUrl); // Imagem não mudou
        _mockFileStorageService.Verify(fs => fs.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockFileStorageService.Verify(fs => fs.DeleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateArticleAsync_WithNewImage_ShouldUploadDeleteOldAndSave()
    {
        var articleId = 1;
        var categoryId = 1;
        var oldUrl = "http://storage.com/articles/old.jpg";
        var newUrl = "http://storage.com/articles/new.jpg";
        var updateDto = new ArticleDto { Id = articleId, Name = "Updated", CategoryId = categoryId, ImageUrl = oldUrl }; // DTO ainda tem o URL antigo
        var existingArticle = CreateTestArticle(articleId, categoryId, imageUrl: oldUrl);
        await using var mockStream = CreateMockStream();

        _mockArticleRepository.Setup(r => r.GetByIdAsync(articleId, It.IsAny<CancellationToken>())).ReturnsAsync(existingArticle);
        _mockCategoryRepository.Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>())).ReturnsAsync(new Category { Id = categoryId });

        _mockFileStorageService.Setup(fs => fs.UploadAsync(
                It.IsAny<Stream>(), It.IsAny<string>(), "image/png", "articles", It.IsAny<CancellationToken>()))
            .ReturnsAsync(newUrl);

        // Chamar a nova assinatura (COM stream)
        await _articleService.UpdateArticleAsync(updateDto, mockStream, "new.png", "image/png");

        // Assert
        Assert.Equal(newUrl, existingArticle.ImageUrl); // URL foi atualizado na entidade
        _mockFileStorageService.Verify(fs => fs.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), "image/png", "articles", It.IsAny<CancellationToken>()), Times.Once);
        _mockFileStorageService.Verify(fs => fs.DeleteAsync(oldUrl, "articles", It.IsAny<CancellationToken>()), Times.Once); // Verifca se o antigo foi apagado
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateArticleAsync_WhenArticleDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        var updateDto = new ArticleDto { Id = 99 };
        _mockArticleRepository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Article?)null);
        // Chamar a nova assinatura
        Func<Task> act = async () => await _articleService.UpdateArticleAsync(updateDto, null, null, null);
        await Assert.ThrowsAsync<KeyNotFoundException>(act);
    }


    // --- DeleteArticleAsync Tests ---

    [Fact]
    public async Task DeleteArticleAsync_WhenArticleExists_AndNoImage_ShouldDeleteAndSave()
    {
        var articleId = 1;
        var existingArticle = CreateTestArticle(articleId, imageUrl: null); // Sem Imagem

        _mockArticleRepository.Setup(r => r.GetByIdAsync(articleId, It.IsAny<CancellationToken>())).ReturnsAsync(existingArticle);
        _mockOrderItemRepository.Setup(r => r.ExistsWithArticleIdAsync(articleId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _mockArticleRepository.Setup(r => r.DeleteAsync(existingArticle, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _articleService.DeleteArticleAsync(articleId);

        _mockArticleRepository.Verify(r => r.DeleteAsync(existingArticle, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockFileStorageService.Verify(fs => fs.DeleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never); // NUNCA deve ser chamado
    }

    [Fact]
    public async Task DeleteArticleAsync_WhenArticleExists_AndHasImage_ShouldDeleteBlobAndSave()
    {
        var articleId = 1;
        var imageUrl = "http://storage.com/articles/delete-me.jpg";
        var existingArticle = CreateTestArticle(articleId, imageUrl: imageUrl); // COM Imagem

        _mockArticleRepository.Setup(r => r.GetByIdAsync(articleId, It.IsAny<CancellationToken>())).ReturnsAsync(existingArticle);
        _mockOrderItemRepository.Setup(r => r.ExistsWithArticleIdAsync(articleId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _mockArticleRepository.Setup(r => r.DeleteAsync(existingArticle, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _articleService.DeleteArticleAsync(articleId);

        _mockArticleRepository.Verify(r => r.DeleteAsync(existingArticle, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockFileStorageService.Verify(fs => fs.DeleteAsync(imageUrl, "articles", It.IsAny<CancellationToken>()), Times.Once); // DEVE ser chamado
    }

    [Fact]
    public async Task DeleteArticleAsync_WhenArticleIsInOrders_ShouldThrowInvalidOperationException()
    {
        var articleId = 1;
        var existingArticle = CreateTestArticle(articleId);

        _mockArticleRepository.Setup(r => r.GetByIdAsync(articleId, It.IsAny<CancellationToken>())).ReturnsAsync(existingArticle);
        _mockOrderItemRepository.Setup(r => r.ExistsWithArticleIdAsync(articleId, It.IsAny<CancellationToken>())).ReturnsAsync(true); // ESTÁ em orders

        Func<Task> act = async () => await _articleService.DeleteArticleAsync(articleId);

        await Assert.ThrowsAsync<InvalidOperationException>(act);
        _mockFileStorageService.Verify(fs => fs.DeleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}