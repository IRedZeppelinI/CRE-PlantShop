using PlantShop.Domain.Entities.Community;

namespace PlantShop.Application.Interfaces.Persistence.Cosmos;

public interface IDailyChallengeRepository
{
    Task<DailyChallenge?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DailyChallenge?> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default);
    Task CreateAsync(DailyChallenge challenge, CancellationToken cancellationToken = default);
    Task UpdateAsync(DailyChallenge challenge, CancellationToken cancellationToken = default);
}