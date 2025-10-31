using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantShop.Application.DTOs.Shop;
using PlantShop.Application.Interfaces.Services.Shop;

namespace PlantShop.WebUI.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IArticleService _articleService;
    private readonly ICategoryService _categoryService;
    private readonly IOrderService _orderService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IArticleService articleService,
        ICategoryService categoryService,
        IOrderService orderService,
        ILogger<AdminController> logger)
    {
        _articleService = articleService;
        _categoryService = categoryService;
        _orderService = orderService;
        _logger = logger;
    }

    // --- DASHBOARD PRINCIPAL ---

    // GET: /Admin/Index ou /Admin
    public IActionResult Index()
    {        
        return View();
    }

    #region Categories

    // GET: /Admin/Categories
    [HttpGet]
    public async Task<IActionResult> Categories()
    {
        var categories = await _categoryService.GetAllCategoriesAsync();
        return View(categories);
    }

    // GET: /Admin/CreateCategory
    [HttpGet]
    public IActionResult CreateCategory()
    {
        // Passa um DTO vazio para o formulário
        return View(new CategoryDto());
    }

    // POST: /Admin/CreateCategory
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(CategoryDto categoryDto)
    {
        if (!ModelState.IsValid)
        {
            return View(categoryDto);
        }

        try
        {
            await _categoryService.CreateCategoryAsync(categoryDto);
            // TODO: Adicionar TempData para mensagem de sucesso
            return RedirectToAction(nameof(Categories));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar categoria.");
            ModelState.AddModelError(string.Empty, "Ocorreu um erro ao criar a categoria.");
            return View(categoryDto);
        }
    }

    // GET: /Admin/EditCategory/5
    [HttpGet]
    public async Task<IActionResult> EditCategory(int id)
    {
        var category = await _categoryService.GetCategoryByIdAsync(id);
        if (category == null)
        {
            return NotFound();
        }
        return View(category);
    }

    // POST: /Admin/EditCategory/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCategory(int id, CategoryDto categoryDto)
    {
        if (id != categoryDto.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(categoryDto);
        }

        try
        {
            await _categoryService.UpdateCategoryAsync(categoryDto);
            // TODO: Adicionar TempData para mensagem de sucesso
            return RedirectToAction(nameof(Categories));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao editar categoria.");
            ModelState.AddModelError(string.Empty, "Ocorreu um erro ao editar a categoria.");
            return View(categoryDto);
        }
    }

    // GET: /Admin/DeleteCategory/5
    [HttpGet]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _categoryService.GetCategoryByIdAsync(id);
        if (category == null)
        {
            return NotFound();
        }
        return View(category);
    }

    // POST: /Admin/DeleteCategory/5
    [HttpPost, ActionName("DeleteCategory")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategoryConfirmed(int id)
    {
        try
        {
            await _categoryService.DeleteCategoryAsync(id);
            // TODO: Adicionar TempData para mensagem de sucesso
            return RedirectToAction(nameof(Categories));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex) // Ex: Tentar apagar com artigos
        {
            _logger.LogWarning(ex, "Tentativa de apagar categoria com artigos.");
            // Idealmente, usamos TempData para mostrar este erro na view Categories
            // Por agora, redirecionamos para uma view de erro
            var category = await _categoryService.GetCategoryByIdAsync(id);
            ModelState.AddModelError(string.Empty, ex.Message); // Apanha a mensagem "Cannot delete..."
            return View(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao apagar categoria.");
            // Redireciona de volta para a view de confirmação com o erro
            var category = await _categoryService.GetCategoryByIdAsync(id);
            ModelState.AddModelError(string.Empty, "Ocorreu um erro inesperado.");
            return View(category);
        }
    }

    #endregion

    #region Articles

    [HttpGet]
    public async Task<IActionResult> Articles()
    {
        // O ArticleDto já inclui o CategoryName, perfeito!
        var articles = await _articleService.GetAllArticlesAsync();
        return View(articles);
    }

    // GET: /Admin/CreateArticle
    [HttpGet]
    public async Task<IActionResult> CreateArticle()
    {
        // Precisamos de carregar as categorias para o dropdown
        await PopulateCategoriesDropdown();
        return View(new ArticleDto { StockQuantity = 10, IsFeatured = false }); // Valores por defeito
    }

    // POST: /Admin/CreateArticle
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateArticle(ArticleDto articleDto)
    {
        if (!ModelState.IsValid)
        {
            // Se o modelo falhar, recarrega o dropdown e retorna
            await PopulateCategoriesDropdown();
            return View(articleDto);
        }

        try
        {
            await _articleService.CreateArticleAsync(articleDto);
            return RedirectToAction(nameof(Articles));
        }
        catch (KeyNotFoundException ex) // Se a CategoriaId for inválida
        {
            _logger.LogWarning(ex, "Tentativa de criar artigo com CategoriaId inválida.");
            ModelState.AddModelError("CategoryId", ex.Message);
            await PopulateCategoriesDropdown();
            return View(articleDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar artigo.");
            ModelState.AddModelError(string.Empty, "Ocorreu um erro ao criar o artigo.");
            await PopulateCategoriesDropdown();
            return View(articleDto);
        }
    }

    // GET: /Admin/EditArticle/5
    [HttpGet]
    public async Task<IActionResult> EditArticle(int id)
    {
        var article = await _articleService.GetArticleByIdAsync(id);
        if (article == null)
        {
            return NotFound();
        }

        await PopulateCategoriesDropdown();
        return View(article);
    }

    // POST: /Admin/EditArticle/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditArticle(int id, ArticleDto articleDto)
    {
        if (id != articleDto.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await PopulateCategoriesDropdown();
            return View(articleDto);
        }

        try
        {
            await _articleService.UpdateArticleAsync(articleDto);
            return RedirectToAction(nameof(Articles));
        }
        catch (KeyNotFoundException ex) // Se o Artigo ou Categoria não forem encontrados
        {
            _logger.LogWarning(ex, "Erro ao atualizar artigo (KeyNotFound).");
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateCategoriesDropdown();
            return View(articleDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao editar artigo.");
            ModelState.AddModelError(string.Empty, "Ocorreu um erro ao editar o artigo.");
            await PopulateCategoriesDropdown();
            return View(articleDto);
        }
    }

    // GET: /Admin/DeleteArticle/5
    [HttpGet]
    public async Task<IActionResult> DeleteArticle(int id)
    {
        var article = await _articleService.GetArticleByIdAsync(id);
        if (article == null)
        {
            return NotFound();
        }
        // O Dto já tem o CategoryName, por isso não precisamos de mais nada
        return View(article);
    }

    // POST: /Admin/DeleteArticle/5
    [HttpPost, ActionName("DeleteArticle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteArticleConfirmed(int id)
    {
        try
        {
            await _articleService.DeleteArticleAsync(id);
            return RedirectToAction(nameof(Articles));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex) // Ex: Tentar apagar com OrderItems
        {
            _logger.LogWarning(ex, "Tentativa de apagar artigo associado a encomendas.");
            var article = await _articleService.GetArticleByIdAsync(id);
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("DeleteArticle", article);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao apagar artigo.");
            var article = await _articleService.GetArticleByIdAsync(id);
            ModelState.AddModelError(string.Empty, "Ocorreu um erro inesperado.");
            return View("DeleteArticle", article);
        }
    }
    #endregion

    #region Orders

    // GET: /Admin/Orders
    [HttpGet]
    public async Task<IActionResult> Orders()
    {
        try
        {            
            var orders = await _orderService.GetAllOrdersAsync();
            return View(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar a lista de encomendas no Admin.");
            // TODO: Criar uma view de erro melhor para o Admin
            return View("Error", "Home");
        }
    }

    // GET: /Admin/OrderDetails/5
    [HttpGet]
    public async Task<IActionResult> OrderDetails(int id)
    {
        try
        {            
            var order = await _orderService.GetOrderDetailsAsync(id); //o repo usa Inlcude()
            if (order == null)
            {
                _logger.LogWarning("Admin tentou aceder a encomenda inexistente (Id: {OrderId})", id);
                return NotFound();
            }

            return View(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar detalhes da encomenda {OrderId} no Admin.", id);
            return View("Error", "Home");
        }
    }

    // TODO: Adicionar [HttpPost] para atualizar o OrderStatus

    #endregion


    private async Task PopulateCategoriesDropdown()
    {
        ViewBag.Categories = await _categoryService.GetAllCategoriesAsync();
    }
}