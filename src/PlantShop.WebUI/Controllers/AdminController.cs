using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PlantShop.Application.DTOs.Community;
using PlantShop.Application.DTOs.Shop;
using PlantShop.Application.Interfaces.Infrastructure;
using PlantShop.Application.Interfaces.Services.Community;
using PlantShop.Application.Interfaces.Services.Shop;
using PlantShop.Domain.Entities;
using PlantShop.WebUI.Models.Admin;

namespace PlantShop.WebUI.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IArticleService _articleService;
    private readonly ICategoryService _categoryService;
    private readonly IOrderService _orderService;
    private readonly IFileStorageService _fileStorageService;
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ICommunityService _communityService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IArticleService articleService,
        ICategoryService categoryService,
        IOrderService orderService,
        IFileStorageService fileStorageService,
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ICommunityService communityService,
        ILogger<AdminController> logger)
    {
        _articleService = articleService;
        _categoryService = categoryService;
        _orderService = orderService;
        _fileStorageService = fileStorageService;
        _userManager = userManager;
        _roleManager = roleManager;
        _communityService = communityService;
        _logger = logger;
    }

    
    // GET:  /Admin
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
        // O ArticleDto já inclui o CategoryName
        var articles = await _articleService.GetAllArticlesAsync();
        return View(articles);
    }

    // GET: /Admin/CreateArticle
    [HttpGet]
    public async Task<IActionResult> CreateArticle()
    {
        var viewModel = new ArticleFormViewModel
        {
            Article = new ArticleDto { StockQuantity = 10, IsFeatured = false }
        };

        // carregar as categorias para o dropdown
        await PopulateCategoriesDropdown();
        //return View(new ArticleDto { StockQuantity = 10, IsFeatured = false });
        return View(viewModel);
    }

    // POST: /Admin/CreateArticle
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateArticle(ArticleFormViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCategoriesDropdown();
            return View(viewModel);
        }

        try
        {            
            Stream? imageStream = null;
            string? imageFileName = null;
            string? imageContentType = null;

            if (viewModel.ImageFile != null && viewModel.ImageFile.Length > 0)
            {
                imageStream = viewModel.ImageFile.OpenReadStream();
                imageFileName = viewModel.ImageFile.FileName;
                imageContentType = viewModel.ImageFile.ContentType;
            }

            
            await _articleService.CreateArticleAsync(
                viewModel.Article,
                imageStream,
                imageFileName,
                imageContentType);

            
            return RedirectToAction(nameof(Articles));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Tentativa de criar artigo com CategoriaId inválida.");
            ModelState.AddModelError("Article.CategoryId", ex.Message);
            await PopulateCategoriesDropdown();
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar artigo.");
            ModelState.AddModelError(string.Empty, "Ocorreu um erro ao criar o artigo.");
            await PopulateCategoriesDropdown();
            return View(viewModel);
        }
    }

    // GET: /Admin/EditArticle/5
    [HttpGet]
    public async Task<IActionResult> EditArticle(int id)
    {
        var articleDto = await _articleService.GetArticleByIdAsync(id);
        if (articleDto == null)
        {
            return NotFound();
        }

        // Criar o ViewModel
        var viewModel = new ArticleFormViewModel
        {
            Article = articleDto
        };

        await PopulateCategoriesDropdown();
        return View(viewModel);
    }

    // POST: /Admin/EditArticle/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditArticle(int id, ArticleFormViewModel viewModel)
    {
        if (id != viewModel.Article.Id)
        {
            return BadRequest();
        }

        // Validar o ModelState
        if (!ModelState.IsValid)
        {
            await PopulateCategoriesDropdown();
            return View(viewModel);
        }

        try
        {
            Stream? imageStream = null;
            string? imageFileName = null;
            string? imageContentType = null;

            if (viewModel.ImageFile != null && viewModel.ImageFile.Length > 0)
            {
                imageStream = viewModel.ImageFile.OpenReadStream();
                imageFileName = viewModel.ImageFile.FileName;
                imageContentType = viewModel.ImageFile.ContentType;
            }
                        
            await _articleService.UpdateArticleAsync(
                viewModel.Article,
                imageStream,
                imageFileName,
                imageContentType);
                       
            return RedirectToAction(nameof(Articles));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Erro ao atualizar artigo (KeyNotFound).");
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateCategoriesDropdown();
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao editar artigo.");
            ModelState.AddModelError(string.Empty, "Ocorreu um erro ao editar o artigo.");
            await PopulateCategoriesDropdown();
            return View(viewModel);
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsShipped(int orderId)
    {
        // TempData para feedback de froma a redirecionar
        try
        {
            await _orderService.MarkOrderAsShippedAsync(orderId);
            _logger.LogInformation("Admin marcou a Encomenda {OrderId} como Enviada.", orderId);
            TempData["OrderMessage"] = "Encomenda marcada como 'Enviada' e mensagem enviada para a logística.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao tentar marcar a Encomenda {OrderId} como Enviada.", orderId);
            TempData["OrderError"] = $"Erro ao processar envio: {ex.Message}";
        }

        // Redireciona de volta para a pag de detalhes onde o admin estava
        return RedirectToAction(nameof(OrderDetails), new { id = orderId });
    }

    #endregion

    #region Users

    // GET: /Admin/Users
    [HttpGet]
    public async Task<IActionResult> Users()
    {
        var users = await _userManager.Users.ToListAsync();
        var userViewModels = new List<UserListViewModel>();

        foreach (var user in users)
        {
            var userViewModel = new UserListViewModel
            {
                UserId = user.Id,
                UserName = user.UserName ?? "N/A",
                Email = user.Email ?? "N/A",
                FullName = user.FullName,
                Roles = await _userManager.GetRolesAsync(user)
            };
            userViewModels.Add(userViewModel);
        }

        return View(userViewModels);
    }

    // GET: /Admin/ManageRoles/string-guid-id
    [HttpGet]
    public async Task<IActionResult> ManageRoles(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            _logger.LogWarning("Admin tentou gerir roles de utilizador inexistente (Id: {UserId})", id);
            return NotFound();
        }

        var userRoles = await _userManager.GetRolesAsync(user);
        var allRoles = await _roleManager.Roles.ToListAsync();

        var viewModel = new ManageUserRolesViewModel
        {
            UserId = user.Id,
            UserName = user.UserName ?? "N/A"
        };

        foreach (var role in allRoles)
        {
            viewModel.Roles.Add(new RoleCheckboxViewModel
            {
                RoleName = role.Name!,
                IsSelected = userRoles.Contains(role.Name!)
            });
        }

        return View(viewModel);
    }

    // POST: /Admin/ManageRoles
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManageRoles(ManageUserRolesViewModel model)
    {
        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user == null)
        {
            _logger.LogError("Falha ao atualizar roles: Utilizador (Id: {UserId}) não encontrado.", model.UserId);
            return NotFound();
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var newRoles = model.Roles
            .Where(r => r.IsSelected)
            .Select(r => r.RoleName)
            .ToList();

        // Calcular o que adicionar e o que remover
        var rolesToAdd = newRoles.Except(currentRoles).ToList();
        var rolesToRemove = currentRoles.Except(newRoles).ToList();

        // Executar as alterações
        // TempData para feedback na próxima página
        try
        {
            if (rolesToAdd.Any())
            {
                var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
                if (!addResult.Succeeded) throw new Exception(
                        string.Join(", ", addResult.Errors.Select(e => e.Description)));
            }

            if (rolesToRemove.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!removeResult.Succeeded) throw new Exception(
                        string.Join(", ", removeResult.Errors.Select(e => e.Description)));
            }

            _logger.LogInformation("Roles atualizadas para UserId: {UserId} pelo Admin.", model.UserId);
            TempData["UserMessage"] = "Roles atualizadas com sucesso.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar roles para UserId: {UserId}", model.UserId);
            TempData["UserError"] = $"Erro ao atualizar roles: {ex.Message}";
        }

        return RedirectToAction(nameof(Users));
    }

    #endregion

    #region Challenges

    // GET: /Admin/Challenges
    [HttpGet]
    public async Task<IActionResult> Challenges(DateTime? date)
    { 
        var challenges = await _communityService.GetAllChallengesAsync();
        
        if (TempData["AdminMessage"] != null)
        {
            ViewData["AdminMessage"] = TempData["AdminMessage"];
        }
        if (TempData["AdminError"] != null)
        {
            ViewData["AdminError"] = TempData["AdminError"];
        }

        return View(challenges);
    }

    [HttpGet]
    public IActionResult CreateChallenge()
    {
        var viewModel = new DailyChallengeFormViewModel
        {
            ChallengeDate = DateTime.UtcNow.Date,
            Challenge = new DailyChallengeDto()
        };
        return View(viewModel); 
    }

    // POST: /Admin/CreateChallenge
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateChallenge(DailyChallengeFormViewModel viewModel)
    {
        if (viewModel.Challenge.CorrectPlantName == null)
        {
            ModelState.AddModelError("Challenge.CorrectPlantName", "O nome da planta é obrigatório.");
        }
        if (viewModel.ImageFile == null || viewModel.ImageFile.Length == 0)
        {
            ModelState.AddModelError("ImageFile", "A imagem é obrigatória.");
        }

        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        try
        {
            await using var imageStream = viewModel.ImageFile!.OpenReadStream();

            var dto = new DailyChallengeDto
            {
                ChallengeDate = viewModel.ChallengeDate,
                CorrectPlantName = viewModel.Challenge.CorrectPlantName
            };

            await _communityService.CreateDailyChallengeAsync(
                dto,
                imageStream,
                viewModel.ImageFile.FileName,
                viewModel.ImageFile.ContentType
            );

            TempData["AdminMessage"] = $"Desafio para {viewModel.ChallengeDate.ToShortDateString()} criado com sucesso.";

            return RedirectToAction(nameof(Challenges));
        }
        catch (InvalidOperationException ex) 
        {
            _logger.LogWarning(ex, "Admin tentou criar desafio duplicado para {Date}", viewModel.ChallengeDate);
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar o Desafio Diário para {Date}", viewModel.ChallengeDate);
            ModelState.AddModelError(string.Empty, $"Erro ao criar: {ex.Message}");
            return View(viewModel);
        }
    }

    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteChallenge(Guid challengeId, DateTime challengeDate)
    {
        try
        {
            await _communityService.DeleteDailyChallengeAsync(challengeId);
            TempData["AdminMessage"] = $"Desafio apagado com sucesso.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao apagar o Desafio Diário {ChallengeId}", challengeId);
            TempData["AdminError"] = $"Erro ao apagar: {ex.Message}";
        }

        return RedirectToAction(nameof(Challenges));
    }

    #endregion

    private async Task PopulateCategoriesDropdown()
    {
        ViewBag.Categories = await _categoryService.GetAllCategoriesAsync();
    }

    
}