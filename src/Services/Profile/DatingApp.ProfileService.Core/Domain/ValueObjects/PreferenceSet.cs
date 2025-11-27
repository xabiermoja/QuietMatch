using DatingApp.ProfileService.Core.Domain.Exceptions;

namespace DatingApp.ProfileService.Core.Domain.ValueObjects;

/// <summary>
/// Value Object representing a Member's matching preferences.
/// </summary>
/// <remarks>
/// Defines the criteria for potential matches:
/// - Age range of preferred partners
/// - Maximum geographic distance
/// - Preferred languages for communication
/// - Gender preference
///
/// These preferences filter the matching pool before compatibility scoring.
/// </remarks>
public record PreferenceSet
{
    private const int MinDistanceLimit = 1;
    private const int MaxDistanceLimit = 500;

    public AgeRange PreferredAgeRange { get; init; }
    public int MaxDistanceKm { get; init; }
    public List<string> PreferredLanguages { get; init; }
    public GenderPreference GenderPreference { get; init; }

    // Private constructor for EF Core
    private PreferenceSet()
    {
        PreferredAgeRange = null!;
        PreferredLanguages = new List<string>();
    }

    public PreferenceSet(
        AgeRange preferredAgeRange,
        int maxDistanceKm,
        List<string> preferredLanguages,
        GenderPreference genderPreference)
    {
        // AgeRange validates itself via its constructor
        PreferredAgeRange = preferredAgeRange ?? throw new ArgumentNullException(nameof(preferredAgeRange));

        // Validate distance
        if (maxDistanceKm < MinDistanceLimit || maxDistanceKm > MaxDistanceLimit)
            throw new ProfileDomainException($"MaxDistanceKm must be between {MinDistanceLimit} and {MaxDistanceLimit} (received: {maxDistanceKm})");

        // Validate languages
        if (preferredLanguages == null || preferredLanguages.Count == 0)
            throw new ProfileDomainException("At least one preferred language must be specified");

        MaxDistanceKm = maxDistanceKm;
        PreferredLanguages = preferredLanguages.ToList(); // Defensive copy
        GenderPreference = genderPreference;
    }
}

public enum GenderPreference
{
    Men,
    Women,
    NonBinary,
    NoPreference
}
