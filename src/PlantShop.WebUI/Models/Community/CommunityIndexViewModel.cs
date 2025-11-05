using PlantShop.Application.DTOs.Community;

namespace PlantShop.WebUI.Models.Community;

public class CommunityIndexViewModel
{
    // nullable pra o caso de não existir challenge no dia
    public DailyChallengeDto? TodayChallenge { get; set; }
        
    public IEnumerable<CommunityPostDto> Posts { get; set; } = new List<CommunityPostDto>();

    public IEnumerable<DailyChallengeDto> ChallengeArchive { get; set; } = new List<DailyChallengeDto>();
}