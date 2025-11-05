using Microsoft.Extensions.Logging;
using PlantShop.Application.DTOs.Community;
using PlantShop.Application.Interfaces.Infrastructure;
using PlantShop.Application.Interfaces.Persistence.Cosmos;
using PlantShop.Application.Interfaces.Services.Community;
using PlantShop.Application.Mappings;
using PlantShop.Domain.Entities.Community;

namespace PlantShop.Application.Services.Community;

public class CommunityService : ICommunityService
{
    private readonly IDailyChallengeRepository _challengeRepo;
    private readonly ICommunityPostRepository _postRepo;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<CommunityService> _logger;

    public CommunityService(
        IDailyChallengeRepository challengeRepo,
        ICommunityPostRepository postRepo,
        IFileStorageService fileStorageService,
        ILogger<CommunityService> logger)
    {
        _challengeRepo = challengeRepo;
        _postRepo = postRepo;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    

    

    // --- Desafios (Escrita) ---

    public async Task<bool> SubmitGuessAsync(Guid challengeId, string guessText, string userId, string userName)
    {
        var challenge = await _challengeRepo.GetByIdAsync(challengeId);
        if (challenge == null)
        {
            throw new KeyNotFoundException("Desafio não encontrado.");
        }

        // ver se utilizador já tentou adivinhar
        if (challenge.Guesses.Any(g => g.UserId == userId))
        {
            _logger.LogWarning("Utilizador {UserId} tentou adivinhar múltiplas vezes no desafio {ChallengeId}", userId, challengeId);
            throw new InvalidOperationException("Já submeteu um palpite para este desafio.");
        }

        bool isCorrect = string.Equals(
            guessText.Trim(),
            challenge.CorrectPlantName.Trim(),
            StringComparison.OrdinalIgnoreCase);

        var newGuess = new ChallengeGuess
        {
            UserId = userId,
            UserName = userName,
            Guess = guessText,
            Timestamp = DateTime.UtcNow,
            IsCorrect = isCorrect
        };

        challenge.Guesses.Add(newGuess);

        await _challengeRepo.UpdateAsync(challenge);
        _logger.LogInformation("Novo palpite (Correto: {IsCorrect}) submetido por {UserId} para o desafio {ChallengeId}", isCorrect, userId, challengeId);

        return isCorrect;
    }

    // --- Posts (Leitura) ---

    public async Task<IEnumerable<CommunityPostDto>> GetAllCommunityPostsAsync()
    {
        var posts = await _postRepo.GetAllAsync();
        return posts.Select(p => p.ToDto());
    }

    public async Task<CommunityPostDto?> GetCommunityPostAsync(Guid postId)
    {
        var post = await _postRepo.GetByIdAsync(postId);
        return post?.ToDto();
    }

    // --- Posts (Escrita) ---

    public async Task<CommunityPostDto> CreateCommunityPostAsync(CommunityPostDto postDto, Stream imageStream, string imageFileName, string imageContentType, string authorId, string authorName)
    {
        //  Upload da Imagem para o Blob Storage 
        var fileExtension = Path.GetExtension(imageFileName);
        var newFileName = $"{Guid.NewGuid()}{fileExtension}";

        _logger.LogInformation("A fazer upload de imagem para o Community Post: {FileName}", newFileName);

        var imageUrl = await _fileStorageService.UploadAsync(
            imageStream, newFileName, imageContentType, "posts"); 

        // Criar a Entidade de Domínio
        var newPost = new CommunityPost
        {
            Id = Guid.NewGuid(),
            Title = postDto.Title,
            Description = postDto.Description,
            ImageUrl = imageUrl, 
            CreatedAt = DateTime.UtcNow,
            AuthorId = authorId,
            AuthorName = authorName,
            Comments = new List<PostComment>() 
        };

        
        await _postRepo.CreateAsync(newPost);

        return newPost.ToDto();
    }

    public async Task AddCommentToPostAsync(Guid postId, string commentText, string authorId, string authorName)
    {
        var post = await _postRepo.GetByIdAsync(postId);
        if (post == null)
        {
            throw new KeyNotFoundException("Post não encontrado.");
        }

        var newComment = new PostComment
        {
            Id = Guid.NewGuid(),
            AuthorId = authorId,
            AuthorName = authorName,
            Text = commentText,
            CreatedAt = DateTime.UtcNow
        };

        post.Comments.Add(newComment);

        await _postRepo.UpdateAsync(post);
        _logger.LogInformation("Novo comentário adicionado ao Post {PostId} por {UserId}", postId, authorId);
    }


    // --- Challenges ---

    //este esconde as guesses para jogadores
    public async Task<DailyChallengeDto?> GetDailyChallengeForTodayAsync(string? currentUserId)
    {
        var today = DateTime.UtcNow.Date;
        var challenge = await _challengeRepo.GetByDateAsync(today);

        if (challenge == null)
        {
            _logger.LogWarning("Nenhum Desafio Diário encontrado para a data: {Date}", today);
            return null;
        }

        return challenge.ToDto(currentUserId);
    }

    //este nao esconde as repostas, para o admin
    public async Task<DailyChallengeDto?> GetChallengeByDateAsync(DateTime date)
    {
        var challengeDate = date.Date;
        var challengeEntity = await _challengeRepo.GetByDateAsync(challengeDate); 

        if (challengeEntity == null) return null;

        return new DailyChallengeDto
        {
            Id = challengeEntity.Id,
            ChallengeDate = challengeEntity.ChallengeDate,
            ImageUrl = challengeEntity.ImageUrl,
            CorrectPlantName = challengeEntity.CorrectPlantName,
            Guesses = (challengeEntity.Guesses ?? new List<ChallengeGuess>())
                        .Select(g => g.ToDto())
                        .ToList()
        };
    }



    public async Task CreateDailyChallengeAsync(DailyChallengeDto challengeDto, Stream imageStream, string imageFileName, string imageContentType)
    {
        var challengeDate = challengeDto.ChallengeDate.Date;
                
        //Ver se já existe para esse dia
        var existingChallenge = await GetChallengeByDateAsync(challengeDate);
        if (existingChallenge != null) 
        {
            
            throw new InvalidOperationException($"Já existe um desafio agendado para o dia {challengeDate.ToShortDateString()}.");
        }

        if (imageStream == null || imageStream.Length == 0)
        {
            throw new ArgumentException("A imagem é obrigatória para criar um novo desafio.");
        }

        // upload imagem
        var fileExtension = Path.GetExtension(imageFileName);
        var newFileName = $"{Guid.NewGuid()}{fileExtension}";
        var imageUrl = await _fileStorageService.UploadAsync(
            imageStream, newFileName, imageContentType, "challenges");

        var newChallenge = new DailyChallenge
        {
            Id = Guid.NewGuid(),
            ChallengeDate = challengeDate.Date, 
            CorrectPlantName = challengeDto.CorrectPlantName,
            ImageUrl = imageUrl,
            Guesses = new List<ChallengeGuess>()
        };

        await _challengeRepo.CreateAsync(newChallenge);
        _logger.LogInformation("Novo Desafio Diário criado para {Date}", challengeDate);
    }

    public async Task DeleteDailyChallengeAsync(Guid challengeId)
    {
        var challenge = await _challengeRepo.GetByIdAsync(challengeId);
        if (challenge == null)
        {
            throw new KeyNotFoundException("Desafio não encontrado para apagar.");
        }

        
        await _challengeRepo.DeleteAsync(challenge);

        // apagar imagem
        if (!string.IsNullOrEmpty(challenge.ImageUrl))
        {
            _logger.LogInformation("A apagar imagem associada {ImageUrl}", challenge.ImageUrl);
            await _fileStorageService.DeleteAsync(challenge.ImageUrl, "challenges");
        }
    }


    
    public async Task<DailyChallengeDto?> GetChallengeByIdAsync(Guid id)
    {
        var challengeEntity = await _challengeRepo.GetByIdAsync(id);
        if (challengeEntity == null) return null;

        return new DailyChallengeDto
        {
            Id = challengeEntity.Id,
            ChallengeDate = challengeEntity.ChallengeDate,
            ImageUrl = challengeEntity.ImageUrl,
            CorrectPlantName = challengeEntity.CorrectPlantName,
            Guesses = (challengeEntity.Guesses ?? new List<ChallengeGuess>())
                        .Select(g => g.ToDto())
                        .ToList()
        };
    }

    public async Task<IEnumerable<DailyChallengeDto>> GetAllChallengesAsync()
    {
        var challenges = await _challengeRepo.GetAllAsync();

        return challenges.Select(entity => new DailyChallengeDto
        {
            Id = entity.Id,
            ChallengeDate = entity.ChallengeDate,
            ImageUrl = entity.ImageUrl,
            CorrectPlantName = entity.CorrectPlantName,
            Guesses = new List<ChallengeGuessDto>() //sem carregar as guesses por perfomance
        });
    }

    public async Task<IEnumerable<DailyChallengeDto>> GetChallengeArchiveAsync(string? currentUserId)
    {
        var challenges = await _challengeRepo.GetAllAsync();

        
        // mapper é que vê se o challenge já acabou por cada challende 
        return challenges
            .OrderByDescending(c => c.ChallengeDate)
            .Select(c => c.ToDto(currentUserId));
    }

    public async Task<DailyChallengeDto?> GetChallengeDetailsAsync(Guid challengeId, string? currentUserId)
    {
        var challengeEntity = await _challengeRepo.GetByIdAsync(challengeId);
        if (challengeEntity == null)
        {
            return null;
        }
        
        return challengeEntity.ToDto(currentUserId);
    }

}