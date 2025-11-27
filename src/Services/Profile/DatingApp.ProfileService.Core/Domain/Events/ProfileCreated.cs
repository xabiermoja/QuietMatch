using DatingApp.ProfileService.Core.Domain.ValueObjects;

namespace DatingApp.ProfileService.Core.Domain.Events;

/// <summary>
/// Domain event raised when a new Member Profile is created.
/// </summary>
/// <remarks>
/// This event is typically raised when UserRegistered event is consumed from IdentityService,
/// creating a skeleton profile for the newly registered user.
///
/// Subscribers: MatchingService (to initialize match candidates pool)
/// </remarks>
public record ProfileCreated
{
    public MemberId UserId { get; init; }
    public string Email { get; init; }
    public DateTime CreatedAt { get; init; }
    public Guid CorrelationId { get; init; }

    public ProfileCreated(MemberId userId, string email, DateTime createdAt, Guid correlationId)
    {
        UserId = userId;
        Email = email;
        CreatedAt = createdAt;
        CorrelationId = correlationId;
    }
}
