using PlantShop.Domain.Entities;

namespace PlantShop.Application.Interfaces.Persistence;

public interface IAppUserRepository
{
    Task<AppUser?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<AppUser?> GetUserWithOrdersAsync(string id, CancellationToken cancellationToken = default);
}
