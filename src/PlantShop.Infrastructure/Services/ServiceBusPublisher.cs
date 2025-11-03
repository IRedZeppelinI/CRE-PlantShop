using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PlantShop.Application.DTOs.Messaging;
using PlantShop.Application.Interfaces.Infrastructure;
using System.Text.Json;

namespace PlantShop.Infrastructure.Services;

public class ServiceBusPublisher : IMessagePublisher
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ServiceBusPublisher> _logger;
    private readonly string _queueName;

    public ServiceBusPublisher(
        ServiceBusClient serviceBusClient,
        IConfiguration configuration,
        ILogger<ServiceBusPublisher> logger)
    {
        _serviceBusClient = serviceBusClient;
        _configuration = configuration;
        _logger = logger;

        // Obter o nome da queue a partir da configuração em appsettings.json
        _queueName = _configuration["AzureServiceBus:QueueName"]
                     ?? throw new ArgumentNullException(
                         nameof(_queueName), "QueueName não está configurado em AzureServiceBus.");
    }

    public async Task PublishOrderForShippingAsync(
        OrderShippingDto orderMessage, CancellationToken cancellationToken = default)
    {
        if (orderMessage == null)
        {
            _logger.LogWarning("Tentativa de publicar mensagem nula para a queue {QueueName}.", _queueName);
            return;
        }

        // Criar o sender 
        await using var sender = _serviceBusClient.CreateSender(_queueName);

        try
        {
            //Serializar o DTO para JSON
            string messageBody = JsonSerializer.Serialize(orderMessage);

            //Criar a mensagem do Service Bus
            var serviceBusMessage = new ServiceBusMessage(messageBody)
            {
                // Definir um ID de mensagem para rastreabilidade
                MessageId = $"order-{orderMessage.OrderId}"
            };

            // Enviar a mensagem
            _logger.LogInformation(
                "A enviar mensagem para a queue {QueueName}. OrderId: {OrderId}",
                _queueName,
                orderMessage.OrderId);

            await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Erro ao publicar mensagem para a queue {QueueName}. OrderId: {OrderId}",
                _queueName, orderMessage.OrderId);
            // TODO se tiver tempo, implementar 'dead-letter' local
            throw;
        }
    }
}