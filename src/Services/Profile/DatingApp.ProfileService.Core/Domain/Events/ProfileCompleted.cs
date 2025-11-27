using DatingApp.ProfileService.Core.Domain.ValueObjects;

namespace DatingApp.ProfileService.Core.Domain.Events;

/// <summary>
/// Domain event raised when a Member Profile reaches 100% completion for the first time.
/// </summary>
/// <remarks>
/// This is a significant milestone - the member has filled in all required information
/// and is now eligible for matching.
///
/// The event is raised ONLY the first time the profile becomes complete (when IsComplete changes from false → true).
/// Subsequent updates do not trigger this event again.
///
/// Subscribers:
/// - MatchingService: Enable matching algorithm for this user
/// - NotificationService: Send "Profile complete!" congratulatory message
/// </remarks>
public record ProfileCompleted
{
    public MemberId UserId { get; init; }
    public DateTime CompletedAt { get; init; }
    public Guid CorrelationId { get; init; }

    public ProfileCompleted(MemberId userId, DateTime completedAt, Guid correlationId)
    {
        UserId = userId;
        CompletedAt = completedAt;
        CorrelationId = correlationId;
    }
}
