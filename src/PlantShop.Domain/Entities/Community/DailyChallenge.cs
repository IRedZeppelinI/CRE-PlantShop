namespace PlantShop.Domain.Entities.Community;

public class DailyChallenge
{
    public Guid Id { get; set; }

    
    public DateTime ChallengeDate { get; set; } 
    public string ImageUrl { get; set; } = string.Empty; 
    public string CorrectPlantName { get; set; } = string.Empty; 

    
    public ICollection<ChallengeGuess> Guesses { get; set; } = new List<ChallengeGuess>();
}
