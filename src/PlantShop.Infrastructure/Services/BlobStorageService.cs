using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using PlantShop.Application.Interfaces.Infrastructure; // A nossa interface
using Microsoft.Extensions.Logging;

namespace PlantShop.Infrastructure.Services;

public class BlobStorageService : IFileStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(
        BlobServiceClient blobServiceClient,
        ILogger<BlobStorageService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _logger = logger;
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, string containerName, CancellationToken cancellationToken = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        var blobClient = containerClient.GetBlobClient(fileName);

        await blobClient.UploadAsync(fileStream, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: cancellationToken);

        _logger.LogInformation("Ficheiro {FileName} carregado para {ContainerName}", fileName, containerName);

        return blobClient.Uri.ToString();
    }

    public async Task DeleteAsync(string fileUrl, string containerName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(fileUrl))
            return;

        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            // Extrai o nome do blob a partir do URL completo
            var blobName = new Uri(fileUrl).Segments.LastOrDefault();
            if (string.IsNullOrEmpty(blobName))
            {
                _logger.LogWarning("Não foi possível extrair o nome do blob do URL: {FileUrl}", fileUrl);
                return;
            }

            var blobClient = containerClient.GetBlobClient(blobName);

            await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);
            _logger.LogInformation("Ficheiro {BlobName} apagado de {ContainerName}", blobName, containerName);
        }
        catch (Exception ex)
        {
            // Não falhar a operação se o delete falhar (ex: ficheiro não existe)
            _logger.LogWarning(ex, "Falha ao tentar apagar o blob: {FileUrl}", fileUrl);
        }
    }
}