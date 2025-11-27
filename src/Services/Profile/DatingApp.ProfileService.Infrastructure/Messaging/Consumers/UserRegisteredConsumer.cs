using DatingApp.ProfileService.Core.Domain.Entities;
using DatingApp.ProfileService.Core.Domain.Interfaces;
using DatingApp.ProfileService.Core.Domain.ValueObjects;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace DatingApp.ProfileService.Infrastructure.Messaging.Consumers;

/// <summary>
/// Integration event consumed from IdentityService (published from IdentityService).
/// </summary>
/// <remarks>
/// This is a message contract shared between IdentityService and ProfileService.
/// When a user registers in IdentityService, this event is published.
///
/// IMPORTANT: This contract must match the event published by IdentityService.
/// In a production system, this would be in a shared "Contracts" assembly.
/// </remarks>
public record UserRegistered
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public DateTime RegisteredAt { get; init; }
    public Guid CorrelationId { get; init; }
}

/// <summary>
/// MassTransit consumer for UserRegistered event from IdentityService.
/// </summary>
/// <remarks>
/// Use Case: AC1 - Create skeleton profile when user registers
///
/// When a user registers in IdentityService, this consumer:
/// 1. Receives UserRegistered event from message bus
/// 2. Creates skeleton MemberProfile (0% completion, default privacy)
/// 3. Persists to database
/// 4. Publishes ProfileCreated event
///
/// This implements event-driven choreography between microservices.
/// ProfileService reacts to IdentityService events without tight coupling.
///
/// Error Handling:
/// - Idempotent: Check if profile already exists before creating
/// - Transient errors: MassTransit automatic retries (configured in Program.cs)
/// - Permanent errors: Message moved to dead letter queue for manual review
/// </remarks>
public class UserRegisteredConsumer : IConsumer<UserRegistered>
{
    private readonly IProfileRepository _repository;
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<UserRegisteredConsumer> _logger;

    public UserRegisteredConsumer(
        IProfileRepository repository,
        IMessagePublisher messagePublisher,
        ILogger<UserRegisteredConsumer> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _messagePublisher = messagePublisher ?? throw new ArgumentNullException(nameof(messagePublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Consume(ConsumeContext<UserRegistered> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Received UserRegistered event for UserId: {UserId}, Email: {Email}, CorrelationId: {CorrelationId}",
            message.UserId, message.Email, message.CorrelationId);

        var memberId = new MemberId(message.UserId);

        // Idempotency: Check if profile already exists
        if (await _repository.ExistsAsync(memberId, context.CancellationToken))
        {
            _logger.LogWarning(
                "Profile already exists for UserId: {UserId}. Skipping skeleton creation. This is likely a duplicate message.",
                message.UserId);
            return; // Idempotent - safely ignore duplicate messages
        }

        try
        {
            // Create skeleton profile
            var profile = MemberProfile.CreateSkeleton(memberId, message.Email);

            // Persist
            await _repository.AddAsync(profile, context.CancellationToken);

            _logger.LogInformation(
                "Created skeleton profile for UserId: {UserId}",
                message.UserId);

            // Publish domain events (ProfileCreated)
            foreach (var domainEvent in profile.DomainEvents)
            {
                await _messagePublisher.PublishAsync(domainEvent, context.CancellationToken);
            }

            profile.ClearDomainEvents();

            _logger.LogInformation(
                "Published ProfileCreated event for UserId: {UserId}",
                message.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to create skeleton profile for UserId: {UserId}, CorrelationId: {CorrelationId}",
                message.UserId, message.CorrelationId);

            // Rethrow to trigger MassTransit retry/dead letter queue
            throw;
        }
    }
}
