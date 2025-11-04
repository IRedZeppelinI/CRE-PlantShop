using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace PlantShop.Infrastructure.Persistence;

public class CosmosDbContext
{
    private readonly CosmosClient _cosmosClient;
    private readonly ILogger<CosmosDbContext> _logger;
    private readonly string _databaseName;

    // Nomes dos Containers
    private readonly string _postsContainerName;
    private readonly string _challengesContainerName;

    // Propriedades públicas para os repositórios usarem
    public Container CommunityPostsContainer { get; }
    public Container DailyChallengesContainer { get; }

    public CosmosDbContext(
        CosmosClient cosmosClient,
        IConfiguration configuration,
        ILogger<CosmosDbContext> logger)
    {
        _cosmosClient = cosmosClient;
        _logger = logger;

        _databaseName = configuration["CosmosDbSettings:DatabaseName"]
            ?? throw new ArgumentNullException("CosmosDbSettings:DatabaseName");

        _postsContainerName = configuration["CosmosDbSettings:CommunityPostsContainer"]
            ?? throw new ArgumentNullException("CosmosDbSettings:CommunityPostsContainer");

        _challengesContainerName = configuration["CosmosDbSettings:DailyChallengesContainer"]
            ?? throw new ArgumentNullException("CosmosDbSettings:DailyChallengesContainer");

        // Obter as referências aos containers
        // (A criação é feita num método Async separado)
        var database = _cosmosClient.GetDatabase(_databaseName);
        CommunityPostsContainer = database.GetContainer(_postsContainerName);
        DailyChallengesContainer = database.GetContainer(_challengesContainerName);
    }

    /// <summary>
    /// Método de 'arranque' para garantir que a BD e os Containers existem.
    /// Deve ser chamado no arranque da aplicação.
    /// </summary>
    public async Task InitializeDatabaseAsync()
    {
        try
        {
            _logger.LogInformation("A inicializar a base de dados Cosmos DB: {DatabaseName}...", _databaseName);

            var databaseResponse = await _cosmosClient.CreateDatabaseIfNotExistsAsync(_databaseName);
            var database = databaseResponse.Database;

            
            await database.CreateContainerIfNotExistsAsync(
                new ContainerProperties
                {
                    Id = _postsContainerName,
                    PartitionKeyPath = "/id"
                });

            
            await database.CreateContainerIfNotExistsAsync(
                new ContainerProperties
                {
                    Id = _challengesContainerName,
                    PartitionKeyPath = "/id"
                });

            _logger.LogInformation("Base de dados Cosmos DB e containers inicializados com sucesso.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Falha ao inicializar a base de dados Cosmos DB.");
            throw;
        }
    }
}