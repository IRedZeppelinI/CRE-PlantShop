using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PlantShop.Application.Interfaces.Services.Shop;
using PlantShop.Domain.Entities;
using PlantShop.WebUI.Models.Checkout;
using PlantShop.WebUI.Models.User;

namespace PlantShop.WebUI.Controllers;

[Authorize] 
[Route("utilizador")]
public class UserController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IOrderService _orderService;
    private readonly ILogger<UserController> _logger;

    public UserController(
        UserManager<AppUser> userManager,
        IOrderService orderService,
        ILogger<UserController> logger)
    {
        _userManager = userManager;
        _orderService = orderService;
        _logger = logger;
    }

    
    [HttpGet("perfil")]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        
        var userOrders = await _orderService.GetOrdersForUserAsync(user.Id);
                
        var viewModel = new ProfileViewModel
        {
            FullName = user.FullName,
            OrderHistory = userOrders,
            AddressInfo = new AddressViewModel
            {
                Address = user.Address ?? string.Empty
            }
        };

        // ver se há msgs de sucesso (após guardar morada)
        if (TempData["ProfileMessage"] != null)
        {
            ViewData["ProfileMessage"] = TempData["ProfileMessage"];
        }

        return View(viewModel);
    }

    // para atualizar a morada
    [HttpPost("perfil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (!ModelState.IsValid || model.AddressInfo == null)
        {
            
            _logger.LogWarning("Falha na validação da morada para UserId: {UserId}", user.Id);
            model.OrderHistory = await _orderService.GetOrdersForUserAsync(user.Id);
            model.FullName = user.FullName;
            return View(model);
        }

        
        user.Address = model.AddressInfo.Address;
        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            _logger.LogInformation("UserId: {UserId} atualizou a morada no perfil.", user.Id);
            TempData["ProfileMessage"] = "Morada atualizada com sucesso!";
        }
        else
        {
            _logger.LogError("Falha ao atualizar morada para UserId: {UserId}", user.Id);
            //  erros vêm de MS.Identity
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("AddressInfo.Address", error.Description);
            }

            // recarregar data para devolver view
            model.OrderHistory = await _orderService.GetOrdersForUserAsync(user.Id);
            model.FullName = user.FullName;
            return View(model);
        }

        return RedirectToAction(nameof(Profile));
    }

    
    [HttpGet("encomenda/{orderId:int}")]
    public async Task<IActionResult> OrderDetails(int orderId)
    {
        var userId = _userManager.GetUserId(User);
        var order = await _orderService.GetOrderDetailsAsync(orderId);

        if (order == null || order.UserId != userId)
        {
            _logger.LogWarning("Acesso negado à encomenda {OrderId} para UserId {UserId}", orderId, userId);
            return NotFound("Encomenda não encontrada ou não lhe pertence.");
        }
                
        return View(order);
    }
}