using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PlantShop.Application.DTOs.Community;
using PlantShop.Application.Interfaces.Services.Community;
using PlantShop.Domain.Entities;
using PlantShop.WebUI.Models.Community;
using System.Security.Claims;

namespace PlantShop.WebUI.Controllers;

[Route("comunidade")]
public class CommunityController : Controller
{
    private readonly ICommunityService _communityService;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<CommunityController> _logger;

    public CommunityController(
        ICommunityService communityService,
        UserManager<AppUser> userManager,
        ILogger<CommunityController> logger)
    {
        _communityService = communityService;
        _userManager = userManager;
        _logger = logger;
    }

    
    // GET: /comunidade
    [HttpGet]
    [Route("")]
    [Route("index")]
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        var today = DateTime.UtcNow.Date;

        // buscar o challenge do dia
        var challenge = await _communityService.GetDailyChallengeForTodayAsync(userId);

        // buscar todos os posts
        var posts = await _communityService.GetAllCommunityPostsAsync();

        // buscar os challengses
        var archive = await _communityService.GetChallengeArchiveAsync(userId);

        var viewModel = new CommunityIndexViewModel
        {
            TodayChallenge = challenge,
            Posts = posts,

            //separar o challende do dia com os challenges antigos
            ChallengeArchive = archive.Where(c => c.ChallengeDate.Date != today)
        };

        return View(viewModel);
    }

    //para abrir challenges antigos
    [HttpGet("desafio/{id:guid}")]
    public async Task<IActionResult> ChallengeDetails(Guid id)
    {
        var userId = _userManager.GetUserId(User);
        var challenge = await _communityService.GetChallengeDetailsAsync(id, userId);

        if (challenge == null)
        {
            return NotFound("Desafio não encontrado.");
        }

        return View(challenge);
    }

    // GET: /comunidade/criar
    [HttpGet("criar")]
    [Authorize]
    public IActionResult CreatePost()
    {        
        return View(new CommunityPostCreateViewModel());
    }

    // POST: /comunidade/criar
    [HttpPost("criar")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePost(CommunityPostCreateViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge(); // Não deve acontecer por csausa do Authorize

        var postDto = new CommunityPostDto
        {
            Title = viewModel.Title,
            Description = viewModel.Description ?? string.Empty
        };

        try
        {
            //stream do ficheiro
            await using var imageStream = viewModel.ImageFile.OpenReadStream();

            var createdPost = await _communityService.CreateCommunityPostAsync(
                postDto,
                imageStream,
                viewModel.ImageFile.FileName,
                viewModel.ImageFile.ContentType,
                user.Id,
                user.UserName! 
            );

            _logger.LogInformation("Novo Community Post {PostId} criado por {UserId}", createdPost.Id, user.Id);

            // Redirecionar para pág d novo post
            return RedirectToAction(nameof(PostDetails), new { id = createdPost.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar Community Post por {UserId}", user.Id);
            ModelState.AddModelError(string.Empty, "Ocorreu um erro ao criar o seu pedido. Por favor, tente novamente.");
            return View(viewModel);
        }
    }

    
    // GET: /comunidade/post/guid-id
    [HttpGet("post/{id:guid}")]
    public async Task<IActionResult> PostDetails(Guid id)
    {
        var post = await _communityService.GetCommunityPostAsync(id);
        if (post == null)
        {
            return NotFound("Pedido de ajuda não encontrado.");
        }

        var viewModel = new PostDetailsViewModel
        {
            Post = post,
            NewCommentText = string.Empty
        };

        return View(viewModel);
    }

    //para comentar post
    // POST: /comunidade/post/guid-id 
    [HttpPost("post/{id:guid}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(Guid id, PostDetailsViewModel viewModel)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (string.IsNullOrEmpty(viewModel.NewCommentText))
        {
            TempData["CommentError"] = "O comentário não pode estar vazio.";

            return RedirectToAction(nameof(PostDetails), new { id = id });
        }

        try
        {
            await _communityService.AddCommentToPostAsync(
                id, // vem do route
                viewModel.NewCommentText,
                user.Id,
                user.UserName!
            );
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Tentativa de comentar Post {PostId} inexistente.", id);
            return NotFound("Pedido de ajuda não encontrado.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao adicionar comentário ao Post {PostId} por {UserId}", id, user.Id);
            TempData["CommentError"] = "Ocorreu um erro ao adicionar o seu comentário.";
        }

        return RedirectToAction(nameof(PostDetails), new { id = id });
    }


    // tentar advinhar challenge 
    // POST: /comunidade/submeter-palpite
    [HttpPost("submeter-palpite")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitGuess(Guid challengeId, string guess)
    {
        if (string.IsNullOrEmpty(guess))
        {
            TempData["ChallengeError"] = "O seu palpite não pode estar vazio.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        try
        {
            bool isCorrect = await _communityService.SubmitGuessAsync(
                challengeId,
                guess,
                user.Id,
                user.UserName!
            );

            if (isCorrect)
            {
                TempData["ChallengeSuccess"] = "Correto! Acertou no desafio de hoje.";
            }
            else
            {
                TempData["ChallengeError"] = "Incorreto. Tente novamente amanhã!";
            }
        }
        catch (InvalidOperationException ex) // para caso de já ter  adivinhsdo
        {
            _logger.LogWarning(ex, "Falha ao submeter palpite para {ChallengeId} por {UserId}", challengeId, user.Id);
            TempData["ChallengeError"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao submeter palpite para {ChallengeId} por {UserId}", challengeId, user.Id);
            TempData["ChallengeError"] = "Ocorreu um erro inesperado.";
        }

        return RedirectToAction(nameof(Index));
    }
}