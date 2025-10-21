namespace PlantShop.Domain.Entities.Community;

public class ChallengeGuess
{
    public string UserId { get; set; } = string.Empty; 
    public string UserName { get; set; } = string.Empty;    
    public string Guess { get; set; } = string.Empty; 
    public DateTime Timestamp { get; set; }
    public bool IsCorrect { get; set; } 
}