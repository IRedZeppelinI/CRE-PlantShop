using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PlantShop.Domain.Entities; 
using PlantShop.WebUI.Models.Account; 

namespace PlantShop.WebUI.Controllers;

[Route("conta")]
public class AccountController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    //  REGISTO     
    [HttpGet]
    [Route("registar")]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]    
    [ValidateAntiForgeryToken]
    [Route("registar")]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new AppUser
        {
            FullName = model.FullName,
            UserName = model.UserName, 
            Email = model.Email
            // sem address que só é pedido quando necessário
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            _logger.LogInformation("Utilizador criou uma nova conta com password.");

            await _userManager.AddToRoleAsync(user, "Customer");

            await _signInManager.SignInAsync(user, isPersistent: false);
                        
            return RedirectToAction("Index", "Home");
        }

        
        foreach (var error in result.Errors)
        {            
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    //LOGIN   

    [HttpGet]
    [Route("login")]
    public IActionResult Login(string? returnUrl = null)
    {        
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("login")]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        // Atribui o returnUrl do ViewData (caso o modelo falhe) ou do próprio modelo.
        // returnUrl do viewData ou do modelo ou em último caso +ra "/"
        string returnUrl = ViewData["ReturnUrl"] as string ?? model.ReturnUrl ?? "/";

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            _logger.LogWarning($"Tentativa de login falhada: Email '{model.Email}' não encontrado.");
            ModelState.AddModelError(string.Empty, "Tentativa de login inválida. Verifique o seu email e password.");
            return View(model);
        }



        // lockoutOnFailure para não bloquear a conta após tentaticas falhadas
        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!,    
            model.Password,
            model.RememberMe, 
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            _logger.LogInformation("Utilizador fez login.");

            // verifica se o returnUrl é local para evitar "Open Redirect Attacks"
            // padrão sugerido pela microsodft
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }
        else
        {
            _logger.LogWarning("Tentativa de login inválida.");
            ModelState.AddModelError(string.Empty, "Tentativa de login inválida. Verifique o seu email e password.");
            return View(model);
        }
    }

    //LOGOUT

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("Utilizador fez logout.");
        
        return RedirectToAction("Index", "Home");
    }
}