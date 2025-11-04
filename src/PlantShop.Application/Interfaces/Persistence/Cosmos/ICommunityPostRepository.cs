using PlantShop.Domain.Entities.Community;

namespace PlantShop.Application.Interfaces.Persistence.Cosmos;

public interface ICommunityPostRepository
{
    Task<CommunityPost?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<CommunityPost>> GetAllAsync(CancellationToken cancellationToken = default);
    Task CreateAsync(CommunityPost post, CancellationToken cancellationToken = default);
    Task UpdateAsync(CommunityPost post, CancellationToken cancellationToken = default);
}