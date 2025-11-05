using Microsoft.AspNetCore.Mvc;
using PlantShop.Application.Interfaces.Services.Shop;
using PlantShop.WebUI.Models;
using PlantShop.WebUI.Models.Home;
using System.Diagnostics;

namespace PlantShop.WebUI.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IArticleService _articleService;

    public HomeController(ILogger<HomeController> logger, IArticleService articleService)
    {
        _logger = logger;
        _articleService = articleService;
    }

    public async Task<IActionResult> Index()
    {
        var featured = await _articleService.GetFeaturedArticlesAsync();

        var viewModel = new HomeViewModel
        {
            FeaturedArticles = featured
        };

        return View(viewModel);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [Route("/Home/NotFoundPage")] 
    public IActionResult NotFoundPage()
    {        
        Response.StatusCode = 404;
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
