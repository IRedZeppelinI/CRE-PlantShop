using Microsoft.AspNetCore.Mvc;
using PlantShop.Application.Interfaces.Services.Shop;
using PlantShop.WebUI.Extensions; 
using PlantShop.WebUI.Models.Cart; 

namespace PlantShop.WebUI.Controllers;

[Route("carrinho")]
public class CartController : Controller
{
    private readonly IArticleService _articleService;
    private readonly ILogger<CartController> _logger;
    private const string CartSessionKey = "ShoppingCart";

    public CartController(
        IArticleService articleService,
        ILogger<CartController> logger)
    {
        _articleService = articleService;
        _logger = logger;
    }

    
    [HttpGet]
    [Route("")]
    [Route("index")]
    public IActionResult Index()
    {
        var cartItems = GetCartFromSession();

        var viewModel = new CartViewModel
        {
            Items = cartItems
            // O Subtotal e Total são calculados automaticamente pelo ViewModel
        };

        return View(viewModel);
    }


    [HttpPost]
    [Route("adicionar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int articleId, int quantity = 1, string? returnUrl = null)
    {        
        var article = await _articleService.GetArticleByIdAsync(articleId);
        if (article == null)
        {
            _logger.LogWarning("Tentativa de adicionar artigo inválido (Id: {ArticleId}) ao carrinho.", articleId);
            return NotFound("Artigo não encontrado.");
        }

        var cart = GetCartFromSession();
        var existingItem = cart.FirstOrDefault(item => item.ArticleId == articleId);

        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
        }
        else
        {
            cart.Add(new CartItemViewModel
            {
                ArticleId = article.Id,
                ArticleName = article.Name,
                UnitPrice = article.Price,
                Quantity = quantity,
                ImageUrl = article.ImageUrl
            });
        }

        SaveCartToSession(cart);
        _logger.LogInformation("Artigo (Id: {ArticleId}) adicionado ao carrinho.", articleId);
                

        
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        else
        {            
            return RedirectToAction("Index", "Shop");
        }
        
    }


    [HttpPost]
    [Route("remover")]
    [ValidateAntiForgeryToken]
    public IActionResult Remove(int articleId)
    {
        var cart = GetCartFromSession();
        var itemToRemove = cart.FirstOrDefault(item => item.ArticleId == articleId);

        if (itemToRemove != null)
        {
            cart.Remove(itemToRemove);
            SaveCartToSession(cart);
            _logger.LogInformation("Artigo (Id: {ArticleId}) removido do carrinho.", articleId);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Route("atualizar")]
    [ValidateAntiForgeryToken]
    public IActionResult Update(int articleId, int quantity)
    {
        // Se a quantidade for 0 ou menos, tratamos como uma remoção
        if (quantity <= 0)
        {
            return Remove(articleId);
        }

        var cart = GetCartFromSession();
        var itemToUpdate = cart.FirstOrDefault(item => item.ArticleId == articleId);

        if (itemToUpdate != null)
        {
            itemToUpdate.Quantity = quantity;
            SaveCartToSession(cart);
            _logger.LogInformation("Quantidade do artigo (Id: {ArticleId}) atualizada para {Quantity} no carrinho.", articleId, quantity);
        }

        return RedirectToAction(nameof(Index));
    }



    // Obtém o carrinho da sessão. Se não existir, cria um novo.    
    private List<CartItemViewModel> GetCartFromSession()
    {
        
        var cart = HttpContext.Session.GetObjectFromJson<List<CartItemViewModel>>(CartSessionKey);

        if (cart == null)
        {
            cart = new List<CartItemViewModel>();
        }

        return cart;
    }

   
    private void SaveCartToSession(List<CartItemViewModel> cart)
    {        
        HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);
    }
}