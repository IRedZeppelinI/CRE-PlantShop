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

    // --- PASSO 1: Mostrar Resumo (ou pedir morada) ---
    // GET: /checkout
    [HttpGet]
    [Route("")]
    [Route("index")]
    public async Task<IActionResult> Index()
    {
        // 2. Verificar se o utilizador tem morada
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            // Isto não deve acontecer se [Authorize] estiver ativo
            return Challenge();
        }

        if (string.IsNullOrEmpty(user.Address))
        {
            _logger.LogInformation("Utilizador {UserId} sem morada, a redirecionar para /checkout/morada", user.Id);
            // Guarda a intenção para o caso de querermos redirecionar
            TempData["InfoMessage"] = "Por favor, adicione uma morada de envio para continuar.";
            return RedirectToAction(nameof(Address));
        }

        // Se tem morada, mostramos o resumo do carrinho
        var cartItems = GetCartFromSession();
        if (cartItems.Count == 0)
        {
            _logger.LogWarning("Utilizador {UserId} chegou ao checkout com carrinho vazio.", user.Id);
            return RedirectToAction("Index", "Cart");
        }

        // Reutilizamos o CartViewModel para mostrar o resumo
        var viewModel = new CartViewModel
        {
            Items = cartItems
        };

        // Passamos a morada para a View
        ViewData["UserAddress"] = user.Address;

        return View(viewModel);
    }

    // --- PASSO 2: Adicionar/Editar Morada ---
    // GET: /checkout/morada
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

    // POST: /checkout/morada
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
            // Morada guardada, manda-o de volta para o resumo do checkout
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
        return View(model);
    }


    // --- PASSO 3: Processar a Encomenda (Simulação de Pagamento) ---
    // POST: /checkout
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

        // Mapear os ViewModels do Carrinho para os DTOs do Application
        var orderItemsDto = cartItems.Select(item => new CartItemCreateDto
        {
            ArticleId = item.ArticleId,
            Quantity = item.Quantity
        }).ToList();

        try
        {
            // 3. Chamar o OrderService
            // A lógica de verificar morada e stock está DENTRO do serviço.
            OrderDto createdOrder = await _orderService.CreateOrderAsync(userId, orderItemsDto);

            // 4. Limpar o carrinho da sessão
            SaveCartToSession(new List<CartItemViewModel>());

            // 5. Redirecionar para a confirmação
            return RedirectToAction(nameof(Confirmation), new { orderId = createdOrder.Id });
        }
        catch (Exception ex)
        {
            // 4. Tratar de exceções (Ex: Falta de stock, morada (embora já tenhamos verificado))
            _logger.LogError(ex, "Falha ao processar checkout para UserId: {UserId}", userId);

            // Usamos TempData porque vamos redirecionar
            TempData["CheckoutError"] = $"Erro ao processar encomenda: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }


    // --- PASSO 4: Confirmação da Encomenda ---
    // GET: /checkout/confirmacao/5
    [HttpGet]
    [Route("confirmacao/{orderId:int}")]
    public async Task<IActionResult> Confirmation(int orderId)
    {
        var userId = _userManager.GetUserId(User);
        var orderDetails = await _orderService.GetOrderDetailsAsync(orderId);

        // Segurança: Verifica se a encomenda existe e se pertence ao 
        // utilizador que está logado.
        if (orderDetails == null || orderDetails.UserId != userId)
        {
            _logger.LogWarning("Tentativa de acesso não autorizado à encomenda {OrderId} por UserId {UserId}", orderId, userId);
            return NotFound();
        }

        // Passa o OrderDto para a view
        return View(orderDetails);
    }


    // --- Métodos Helper (copiados do CartController) ---
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