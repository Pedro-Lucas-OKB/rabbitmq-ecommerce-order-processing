using OrderProcessing.Core.Entities;

namespace OrderProcessing.Infrastructure.Messaging;

public interface IRabbitMqPublisher
{
    Task PublishOrderCreatedAsync(Order order, CancellationToken cancellationToken = default);
}