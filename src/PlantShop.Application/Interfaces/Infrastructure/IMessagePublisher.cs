using PlantShop.Application.DTOs.Messaging;

namespace PlantShop.Application.Interfaces.Infrastructure;

public interface IMessagePublisher
{    
    Task PublishOrderForShippingAsync(OrderShippingDto orderMessage, CancellationToken cancellationToken = default);
}