using DatingApp.ProfileService.Core.Domain.ValueObjects;

namespace DatingApp.ProfileService.Core.Domain.Events;

/// <summary>
/// Domain event raised when a Member Profile is updated.
/// </summary>
/// <remarks>
/// This event is raised whenever any profile field changes (personality, values, preferences, etc.).
/// It includes a list of field names that were updated to allow subscribers to react selectively.
///
/// Subscribers: MatchingService (to re-evaluate match compatibility based on changed fields)
/// </remarks>
public record ProfileUpdated
{
    public MemberId UserId { get; init; }
    public List<string> UpdatedFields { get; init; }
    public DateTime UpdatedAt { get; init; }
    public Guid CorrelationId { get; init; }

    public ProfileUpdated(MemberId userId, List<string> updatedFields, DateTime updatedAt, Guid correlationId)
    {
        UserId = userId;
        UpdatedFields = updatedFields ?? new List<string>();
        UpdatedAt = updatedAt;
        CorrelationId = correlationId;
    }
}
