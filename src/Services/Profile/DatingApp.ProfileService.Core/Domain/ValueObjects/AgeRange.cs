using DatingApp.ProfileService.Core.Domain.Exceptions;

namespace DatingApp.ProfileService.Core.Domain.ValueObjects;

/// <summary>
/// Value Object representing an age range (used for matching preferences).
/// </summary>
/// <remarks>
/// Enforces business rules:
/// - Minimum age must be at least 18 (legal adult)
/// - Maximum age cannot exceed 100 (realistic constraint)
/// - Min must be less than or equal to Max (logical constraint)
/// </remarks>
public record AgeRange
{
    private const int AbsoluteMinimumAge = 18;
    private const int AbsoluteMaximumAge = 100;

    public int Min { get; init; }
    public int Max { get; init; }

    // Private constructor for EF Core
    private AgeRange() { }

    public AgeRange(int min, int max)
    {
        // Validate minimum age
        if (min < AbsoluteMinimumAge)
            throw new ProfileDomainException($"Minimum age must be at least {AbsoluteMinimumAge} (received: {min})");

        // Validate maximum age
        if (max > AbsoluteMaximumAge)
            throw new ProfileDomainException($"Maximum age cannot exceed {AbsoluteMaximumAge} (received: {max})");

        // Validate range consistency
        if (min > max)
            throw new ProfileDomainException($"Minimum age ({min}) cannot be greater than maximum age ({max})");

        Min = min;
        Max = max;
    }
}
