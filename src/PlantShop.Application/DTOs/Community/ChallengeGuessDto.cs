namespace PlantShop.Application.DTOs.Community;

public class ChallengeGuessDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Guess { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool IsCorrect { get; set; }
}