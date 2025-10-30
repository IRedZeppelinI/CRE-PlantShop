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
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IArticleService articleService,
        ICategoryService categoryService,
        ILogger<AdminController> logger)
    {
        _articleService = articleService;
        _categoryService = categoryService;
        _logger = logger;
    }

    // --- DASHBOARD PRINCIPAL ---

    // GET: /Admin/Index ou /Admin
    public IActionResult Index()
    {
        // Por agora, um simples dashboard.
        // No futuro, podemos mostrar estatísticas.
        return View();
    }

    // --- GESTÃO DE CATEGORIAS ---

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
}