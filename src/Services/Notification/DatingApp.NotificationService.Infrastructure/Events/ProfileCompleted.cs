namespace DatingApp.NotificationService.Infrastructure.Events;

/// <summary>
/// Integration event received when a user completes their profile.
/// </summary>
/// <remarks>
/// Published by: ProfileService
/// Consumed by: NotificationService (sends profile completed email)
///
/// This is a CONTRACT - must match the event published by ProfileService!
///
/// Note: ProfileService publishes this when the profile reaches 100% completion
/// for the first time. We need Email and Name to send the notification.
/// </remarks>
public record ProfileCompleted(
    Guid UserId,
    string Email,
    string Name,
    DateTime CompletedAt,
    Guid CorrelationId
);
