using Microsoft.AspNetCore.Mvc;
using PlantShop.Application.Interfaces.Services.Shop;
using PlantShop.WebUI.Models.Shop; 

namespace PlantShop.WebUI.Controllers;

[Route("loja")]
public class ShopController : Controller
{
    private readonly IArticleService _articleService;
    private readonly ICategoryService _categoryService;
    private readonly ILogger<ShopController> _logger;

    public ShopController(
        IArticleService articleService,
        ICategoryService categoryService,
        ILogger<ShopController> logger)
    {
        _articleService = articleService;
        _categoryService = categoryService;
        _logger = logger;
    }

    
    [Route("")]
    [Route("index")]
    [Route("categoria/{categoryId:int}")]
    [HttpGet]
    public async Task<IActionResult> Index(int? categoryId = null)
    {
        try
        {
            var viewModel = new ShopViewModel();

            
            viewModel.Categories = await _categoryService.GetAllCategoriesAsync();

            
            if (categoryId.HasValue)
            {                
                viewModel.Articles = await _articleService.GetArticlesByCategoryAsync(categoryId.Value);
                viewModel.SelectedCategoryId = categoryId.Value;
            }
            else
            {                
                viewModel.Articles = await _articleService.GetAllArticlesAsync();
                viewModel.SelectedCategoryId = null;
            }

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar a página da loja.");
            // TODO melhorar erros
            return RedirectToAction("Error", "Home");
        }
    }

    
    [Route("detalhes/{id:int}")]
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var article = await _articleService.GetArticleByIdAsync(id);

            if (article == null)
            {
                _logger.LogWarning("Tentativa de aceder a detalhes de artigo não existente (Id: {Id})", id);
                return NotFound(); // Retorna uma página 404
            }

            return View(article); 
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar detalhes do artigo (Id: {Id})", id);
            return RedirectToAction("Error", "Home");
        }
    }
}