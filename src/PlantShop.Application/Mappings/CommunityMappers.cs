using PlantShop.Application.DTOs.Community;
using PlantShop.Domain.Entities.Community;

namespace PlantShop.Application.Mappings;


internal static class CommunityMappers
{    

    public static PostCommentDto ToDto(this PostComment entity)
    {
        return new PostCommentDto
        {
            Id = entity.Id,
            AuthorId = entity.AuthorId,
            AuthorName = entity.AuthorName,
            Text = entity.Text,
            CreatedAt = entity.CreatedAt
        };
    }

    public static CommunityPostDto ToDto(this CommunityPost entity)
    {
        return new CommunityPostDto
        {
            Id = entity.Id,
            AuthorId = entity.AuthorId,
            AuthorName = entity.AuthorName,
            Title = entity.Title,
            Description = entity.Description,
            ImageUrl = entity.ImageUrl,
            CreatedAt = entity.CreatedAt,
            Comments = entity.Comments.Select(c => c.ToDto()).ToList()
        };
    }
        

    public static ChallengeGuessDto ToDto(this ChallengeGuess entity)
    {
        return new ChallengeGuessDto
        {
            UserId = entity.UserId,
            UserName = entity.UserName,
            Guess = entity.Guess,
            Timestamp = entity.Timestamp,
            IsCorrect = entity.IsCorrect
        };
    }
    

    public static DailyChallengeDto ToDto(this DailyChallenge entity, string? currentUserId = null)
    {
        // ver se o desafio já terminou 
        bool isChallengeOver = entity.ChallengeDate.Date < DateTime.UtcNow.Date;

        // buscar os guesses 
        var allGuesses = entity.Guesses ?? new List<ChallengeGuess>();

        // palpite do user actual
        var currentUserGuessEntity = allGuesses.FirstOrDefault(g => g.UserId == currentUserId);

        var dto = new DailyChallengeDto
        {
            Id = entity.Id,
            ChallengeDate = entity.ChallengeDate,
            ImageUrl = entity.ImageUrl,
            HasCurrentUserGuessed = (currentUserGuessEntity != null),
            CurrentUserGuess = currentUserGuessEntity?.ToDto() 
        };

        // se challenge já terminou, mostrar todos os guesses
        if (isChallengeOver)
        {
            dto.CorrectPlantName = entity.CorrectPlantName;
            dto.Guesses = allGuesses.Select(g => g.ToDto()).ToList();
        }
        else
        {
            // challenge ainda não terminou
            dto.CorrectPlantName = ""; // esconder a resposta certa
            dto.Guesses = new List<ChallengeGuessDto>(); 
        }

        return dto;
    }
}