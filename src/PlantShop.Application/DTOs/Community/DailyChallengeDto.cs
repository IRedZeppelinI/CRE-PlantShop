namespace PlantShop.Application.DTOs.Community;

public class DailyChallengeDto
{
    public Guid Id { get; set; }
    public DateTime ChallengeDate { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string CorrectPlantName { get; set; } = string.Empty;

    // para veer se utilizador já adivinhou
    public bool HasCurrentUserGuessed { get; set; } = false;

    // o guess do utilizador avtual
    public ChallengeGuessDto? CurrentUserGuess { get; set; }

    public ICollection<ChallengeGuessDto> Guesses { get; set; } = new List<ChallengeGuessDto>();
}