namespace DatingApp.ProfileService.Core.Domain.Interfaces;

/// <summary>
/// Message publisher interface (Port) for publishing domain events to the message bus.
/// </summary>
/// <remarks>
/// This is a port defined in the Domain layer following Onion Architecture.
/// The concrete implementation (adapter) will be in the Infrastructure layer using MassTransit.
///
/// Used to publish domain events (ProfileCreated, ProfileUpdated, ProfileCompleted) to RabbitMQ/Azure Service Bus
/// for consumption by other microservices (MatchingService, NotificationService).
///
/// This interface keeps the domain layer independent of messaging infrastructure.
/// </remarks>
public interface IMessagePublisher
{
    /// <summary>
    /// Publishes a message to the message bus.
    /// </summary>
    /// <typeparam name="T">The message type (must be a class)</typeparam>
    /// <param name="message">The message to publish</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class;
}
