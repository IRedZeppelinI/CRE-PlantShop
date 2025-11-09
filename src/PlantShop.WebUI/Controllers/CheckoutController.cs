using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PlantShop.Application.DTOs.Shop;
using PlantShop.Application.Interfaces.Services.Shop;
using PlantShop.Domain.Entities;
using PlantShop.WebUI.Extensions; 
using PlantShop.WebUI.Models.Cart; 
using PlantShop.WebUI.Models.Checkout; 

namespace PlantShop.WebUI.Controllers;


[Authorize]
[Route("checkout")]
public class CheckoutController : Controller
{
    private readonly IOrderService _orderService;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<CheckoutController> _logger;
    private const string CartSessionKey = "ShoppingCart";

    public CheckoutController(
        IOrderService orderService,
        UserManager<AppUser> userManager,
        ILogger<CheckoutController> logger)
    {
        _orderService = orderService;
        _userManager = userManager;
        _logger = logger;
    }

    
    [HttpGet]
    [Route("")]
    [Route("index")]
    public async Task<IActionResult> Index()
    {
        
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            // envia status code 401 ou 403 com identity. [authorize] na view previne
            return Challenge();
        }

        //pedir morada se user nao tiver        
        if (string.IsNullOrEmpty(user.Address))
        {
            _logger.LogInformation("Utilizador {UserId} sem morada, a redirecionar para /checkout/morada", user.Id);
            
            TempData["InfoMessage"] = "Por favor, adicione uma morada de envio para continuar.";
            return RedirectToAction(nameof(Address));
        }

        
        var cartItems = GetCartFromSession();
        if (cartItems.Count == 0)
        {
            _logger.LogWarning("Utilizador {UserId} chegou ao checkout com carrinho vazio.", user.Id);
            return RedirectToAction("Index", "Cart");
        }

        
        var viewModel = new CartViewModel
        {
            Items = cartItems
        };

        
        ViewData["UserAddress"] = user.Address;

        return View(viewModel);
    }
    
    
    [HttpGet]
    [Route("morada")]
    public async Task<IActionResult> Address()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var viewModel = new AddressViewModel
        {
            Address = user.Address ?? string.Empty
        };

        return View(viewModel);
    }

    
    [HttpPost]
    [Route("morada")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Address(AddressViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        user.Address = model.Address;
        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            _logger.LogInformation("Utilizador {UserId} atualizou a morada.", user.Id);
            
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
        return View(model);
    }
            
    
    [HttpPost]
    [Route("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessCheckout()
    {
        var userId = _userManager.GetUserId(User);
        var cartItems = GetCartFromSession();

        if (userId == null) return Challenge();

        if (cartItems.Count == 0)
        {
            return RedirectToAction("Index", "Cart");
        }

        // Mapear do cart viewModel para cart dto
        var cartItemsDto = cartItems.Select(item => new CartItemDto
        {
            ArticleId = item.ArticleId,
            Quantity = item.Quantity
        }).ToList();

        try
        {
                        
            OrderDto createdOrder = await _orderService.CreateOrderAsync(userId, cartItemsDto);

            //limpar carrinho da sessão
            SaveCartToSession(new List<CartItemViewModel>());

            
            return RedirectToAction(nameof(Confirmation), new { orderId = createdOrder.Id });
        }
        catch (Exception ex)
        {            
            _logger.LogError(ex, "Falha ao processar checkout para UserId: {UserId}", userId);

            
            TempData["CheckoutError"] = $"Erro ao processar encomenda: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }


    
    [HttpGet]
    [Route("confirmacao/{orderId:int}")]
    public async Task<IActionResult> Confirmation(int orderId)
    {
        var userId = _userManager.GetUserId(User);
        var orderDetails = await _orderService.GetOrderDetailsAsync(orderId);
        
        
        if (orderDetails == null || orderDetails.UserId != userId)
        {
            _logger.LogWarning("Tentativa de acesso não autorizado à encomenda {OrderId} por UserId {UserId}", orderId, userId);
            return NotFound();
        }
                
        return View(orderDetails);
    }


    //helper TODO: refactor por duplicação em cartk controller 
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