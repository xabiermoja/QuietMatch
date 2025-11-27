using DatingApp.ProfileService.Core.Domain.Interfaces;
using MassTransit;

namespace DatingApp.ProfileService.Infrastructure.Messaging;

/// <summary>
/// MassTransit implementation of IMessagePublisher (adapter for domain port).
/// </summary>
/// <remarks>
/// Implements IMessagePublisher port defined in Domain layer.
///
/// Publishes domain events to RabbitMQ/Azure Service Bus via MassTransit.
/// MassTransit handles:
/// - Message serialization (JSON)
/// - Exchange/topic routing
/// - Retry policies
/// - Dead letter queues
/// - Message headers (CorrelationId, etc.)
///
/// This decouples the domain layer from messaging infrastructure.
/// </remarks>
public class MassTransitMessagePublisher : IMessagePublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public MassTransitMessagePublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
    }

    /// <summary>
    /// Publishes a message to the message bus.
    /// </summary>
    /// <typeparam name="T">The message type (domain event)</typeparam>
    /// <param name="message">The message to publish</param>
    /// <param name="ct">Cancellation token</param>
    public async Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class
    {
        await _publishEndpoint.Publish(message, ct);
    }
}
