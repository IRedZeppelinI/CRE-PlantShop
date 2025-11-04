using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using PlantShop.Application.Interfaces.Persistence.Cosmos;
using PlantShop.Domain.Entities.Community;

namespace PlantShop.Infrastructure.Persistence.Repositories.Cosmos;

public class DailyChallengeRepository : IDailyChallengeRepository
{
    private readonly Container _challengeContainer;
    private readonly ILogger<DailyChallengeRepository> _logger;

    public DailyChallengeRepository(CosmosDbContext cosmosDbContext, ILogger<DailyChallengeRepository> logger)
    {
        _challengeContainer = cosmosDbContext.DailyChallengesContainer; 
        _logger = logger;
    }

    public async Task CreateAsync(DailyChallenge challenge, CancellationToken cancellationToken = default)
    {
        try
        {
            await _challengeContainer.CreateItemAsync(
                challenge,
                new PartitionKey(challenge.Id.ToString()),
                cancellationToken: cancellationToken);
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Falha ao criar DailyChallenge (Id: {ChallengeId}) no Cosmos DB.", challenge.Id);
            throw;
        }
    }

    public async Task<DailyChallenge?> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        // Normaliza a data para garantir apenas Dia/Mês/Ano
        var dateOnly = date.Date;
                
        var query = new QueryDefinition("SELECT TOP 1 * FROM c WHERE c.challengeDate = @date")
            .WithParameter("@date", dateOnly);

        using var feed = _challengeContainer.GetItemQueryIterator<DailyChallenge>(query);

        if (feed.HasMoreResults)
        {
            var response = await feed.ReadNextAsync(cancellationToken);
            return response.Resource.FirstOrDefault();
        }

        return null;
    }

    public async Task<DailyChallenge?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var idString = id.ToString();
            var response = await _challengeContainer.ReadItemAsync<DailyChallenge>(
                idString,
                new PartitionKey(idString),
                cancellationToken: cancellationToken);

            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Falha ao obter DailyChallenge (Id: {ChallengeId}) do Cosmos DB.", id);
            throw;
        }
    }

    public async Task UpdateAsync(DailyChallenge challenge, CancellationToken cancellationToken = default)
    {
        try
        {
            await _challengeContainer.ReplaceItemAsync(
                challenge,
                challenge.Id.ToString(),
                new PartitionKey(challenge.Id.ToString()),
                cancellationToken: cancellationToken);
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Falha ao atualizar DailyChallenge (Id: {ChallengeId}) no Cosmos DB.", challenge.Id);
            throw;
        }
    }
}