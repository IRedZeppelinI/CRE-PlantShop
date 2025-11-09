using PlantShop.Application.DTOs.Community;

namespace PlantShop.Application.Interfaces.Services.Community;

public interface ICommunityService
{
    // Challenges    
    Task<DailyChallengeDto?> GetDailyChallengeForTodayAsync(string? currentUserId);
    
    Task<bool> SubmitGuessAsync(Guid challengeId, string guessText, string userId, string userName);


    // Posts     
    Task<IEnumerable<CommunityPostDto>> GetAllCommunityPostsAsync();
    Task<CommunityPostDto?> GetCommunityPostAsync(Guid postId);
        
    Task<CommunityPostDto> CreateCommunityPostAsync(CommunityPostDto postDto, Stream imageStream, string imageFileName, string imageContentType, string authorId, string authorName);

    Task AddCommentToPostAsync(Guid postId, string commentText, string authorId, string authorName);


    // Admin    
    Task CreateDailyChallengeAsync(DailyChallengeDto challengeDto, Stream imageStream, string imageFileName, string imageContentType);

    Task<DailyChallengeDto?> GetChallengeByDateAsync(DateTime date);

    Task DeleteDailyChallengeAsync(Guid challengeId);

    //já não está a ser utilizado mas sem necessidade de eliminar 
    Task<DailyChallengeDto?> GetChallengeByIdAsync(Guid id);
    Task<IEnumerable<DailyChallengeDto>> GetAllChallengesAsync();
        
    Task<IEnumerable<DailyChallengeDto>> GetChallengeArchiveAsync(string? currentUserId);

    Task<DailyChallengeDto?> GetChallengeDetailsAsync(Guid challengeId, string? currentUserId);
}