using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using PlantShop.Application.Interfaces.Persistence.Cosmos;
using PlantShop.Domain.Entities.Community;

namespace PlantShop.Infrastructure.Persistence.Repositories.Cosmos;

public class CommunityPostRepository : ICommunityPostRepository
{
    private readonly Container _postsContainer;
    private readonly ILogger<CommunityPostRepository> _logger;

    public CommunityPostRepository(CosmosDbContext cosmosDbContext, ILogger<CommunityPostRepository> logger)
    {
        _postsContainer = cosmosDbContext.CommunityPostsContainer; 
        _logger = logger;
    }

    public async Task CreateAsync(CommunityPost post, CancellationToken cancellationToken = default)
    {
        try
        {            
            await _postsContainer.CreateItemAsync(
                post,
                new PartitionKey(post.Id.ToString()),
                cancellationToken: cancellationToken);
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Falha ao criar CommunityPost (Id: {PostId}) no Cosmos DB.", post.Id);
            throw;
        }
    }

    public async Task<IEnumerable<CommunityPost>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition("SELECT * FROM c ORDER BY c.createdAt DESC");

        var results = new List<CommunityPost>();
        using var feed = _postsContainer.GetItemQueryIterator<CommunityPost>(query);

        while (feed.HasMoreResults)
        {
            var response = await feed.ReadNextAsync(cancellationToken);
            results.AddRange(response.Resource);
        }

        return results;
    }

    public async Task<CommunityPost?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {            
            var idString = id.ToString();
            var response = await _postsContainer.ReadItemAsync<CommunityPost>(
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
            _logger.LogError(ex, "Falha ao obter CommunityPost (Id: {PostId}) do Cosmos DB.", id);
            throw;
        }
    }

    public async Task UpdateAsync(CommunityPost post, CancellationToken cancellationToken = default)
    {
        try
        {            
            await _postsContainer.ReplaceItemAsync(
                post,
                post.Id.ToString(),
                new PartitionKey(post.Id.ToString()),
                cancellationToken: cancellationToken);
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Falha ao atualizar CommunityPost (Id: {PostId}) no Cosmos DB.", post.Id);
            throw;
        }
    }
}