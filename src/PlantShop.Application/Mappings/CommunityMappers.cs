using PlantShop.Application.DTOs.Community;
using PlantShop.Domain.Entities.Community;

namespace PlantShop.Application.Mappings;


internal static class CommunityMappers
{
    // --- Posts e Comentários ---

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

    // --- Desafios e Palpites ---

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
        var dto = new DailyChallengeDto
        {
            Id = entity.Id,
            ChallengeDate = entity.ChallengeDate,
            ImageUrl = entity.ImageUrl,
            // A resposta correta só é revelada se o utilizador já adivinhou ou seo Admin adicionar
            CorrectPlantName = "", // Por defeito não é revelado
            Guesses = entity.Guesses.Select(g => g.ToDto()).ToList()
        };

        // Verificar se o utilizador atual já adivinhou
        var currentUserGuess = entity.Guesses.FirstOrDefault(g => g.UserId == currentUserId);
        if (currentUserGuess != null)
        {
            dto.HasCurrentUserGuessed = true;
            // Se já adivinhou (certo ou errado), revela a resposta
            dto.CorrectPlantName = entity.CorrectPlantName;
        }

        return dto;
    }
}