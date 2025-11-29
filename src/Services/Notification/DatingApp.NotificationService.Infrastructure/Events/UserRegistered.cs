namespace DatingApp.NotificationService.Infrastructure.Events;

/// <summary>
/// Integration event received when a new user registers.
/// </summary>
/// <remarks>
/// Published by: IdentityService
/// Consumed by: NotificationService (sends welcome email)
///
/// This is a CONTRACT - must match the event published by IdentityService!
/// </remarks>
public record UserRegistered(
    Guid UserId,
    string Email,
    string Provider,
    DateTime RegisteredAt,
    Guid CorrelationId
);
