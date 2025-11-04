using PlantShop.Application.DTOs.Community;

namespace PlantShop.Application.Interfaces.Services.Community;

public interface ICommunityService
{
    // --- Desafios ---
    // Leitura
    Task<DailyChallengeDto?> GetDailyChallengeForTodayAsync(string? currentUserId);

    // Escrita
    Task<bool> SubmitGuessAsync(Guid challengeId, string guessText, string userId, string userName);

    //--- Posts ---
    //Leitura
    Task<IEnumerable<CommunityPostDto>> GetAllCommunityPostsAsync();
    Task<CommunityPostDto?> GetCommunityPostAsync(Guid postId);

    // Escrita)
    Task<CommunityPostDto> CreateCommunityPostAsync(CommunityPostDto postDto, Stream imageStream, string imageFileName, string imageContentType, string authorId, string authorName);
    Task AddCommentToPostAsync(Guid postId, string commentText, string authorId, string authorName);


    // --- Admin ---
    Task CreateOrUpdateDailyChallengeAsync(DailyChallengeDto challengeDto, Stream? imageStream, string? imageFileName, string? imageContentType);

    Task<DailyChallengeDto?> GetChallengeForAdminByDateAsync(DateTime date);
}