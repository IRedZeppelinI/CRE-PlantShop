using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PlantShop.Domain.Entities; 
using PlantShop.WebUI.Models.Account; 

namespace PlantShop.WebUI.Controllers;

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

    // --- REGISTO ---

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new AppUser
        {
            FullName = model.FullName,
            UserName = model.UserName, // O campo que adicionámos
            Email = model.Email
            // O seu 'Address' será preenchido noutra altura (ex: checkout ou perfil)
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            _logger.LogInformation("Utilizador criou uma nova conta com password.");

            // Faz o login automático do utilizador após o registo
            await _signInManager.SignInAsync(user, isPersistent: false);

            // Redireciona para a página principal
            return RedirectToAction("Index", "Home");
        }

        // Se chegou aqui, algo falhou. Adiciona os erros ao ModelState.
        foreach (var error in result.Errors)
        {
            // Erros comuns: "Username 'x' is already taken.", "Email 'y' is already taken."
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    // --- LOGIN ---

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        // Guarda o returnUrl para o caso de o login falhar e ter de 
        // submeter o formulário novamente.
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        // Atribui o returnUrl do ViewData (caso o modelo falhe) ou do próprio modelo.
        // O "??" assegura que / (raiz) é o fallback.
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


        // Tenta fazer o login. 
        // O 'lockoutOnFailure: false' significa que não bloqueamos a conta 
        // após X tentativas falhadas (pode ser ativado na configuração do Identity)
        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!,    // Nota: O Identity permite fazer login com Email ou UserName
            model.Password,
            model.RememberMe, // isPersistent
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            _logger.LogInformation("Utilizador fez login.");

            // Verifica se o returnUrl é local para evitar "Open Redirect Attacks"
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

    // --- LOGOUT ---

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("Utilizador fez logout.");

        // Após o logout, o melhor sítio para ir é a página principal.
        return RedirectToAction("Index", "Home");
    }
}