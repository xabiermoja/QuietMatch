namespace DatingApp.IdentityService.Infrastructure.Events;

/// <summary>
/// Integration event published when a new user registers via OAuth.
/// </summary>
/// <param name="UserId">User's unique identifier</param>
/// <param name="Email">User's email address</param>
/// <param name="Provider">OAuth provider used (Google, Apple)</param>
/// <param name="RegisteredAt">Registration timestamp (UTC)</param>
/// <param name="CorrelationId">Unique ID for distributed tracing</param>
/// <remarks>
/// This event is published to the message bus (RabbitMQ/Azure Service Bus).
/// Subscribers:
/// - ProfileService: Creates empty profile for new user
/// - NotificationService: Sends welcome email
/// - AnalyticsService: Tracks user registration
///
/// Event naming convention: Past tense (represents a fact that happened).
/// Integration event namespace: Infrastructure (not Domain) because it uses messaging infrastructure.
/// </remarks>
public record UserRegistered(
    Guid UserId,
    string Email,
    string Provider,
    DateTime RegisteredAt,
    Guid CorrelationId
);
