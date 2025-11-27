using DatingApp.ProfileService.Core.Domain.Events;
using DatingApp.ProfileService.Core.Domain.Exceptions;
using DatingApp.ProfileService.Core.Domain.ValueObjects;

namespace DatingApp.ProfileService.Core.Domain.Entities;

/// <summary>
/// Aggregate Root representing a Member's complete profile.
/// </summary>
/// <remarks>
/// MemberProfile is the central aggregate in the Profile bounded context.
/// It encapsulates all profile data and enforces business rules via domain methods.
///
/// Domain Rules Enforced:
/// - Completion percentage calculation based on filled fields
/// - IsComplete flag when >= 80% complete
/// - Privacy rules via CanShareWith() method based on ExposureLevel
/// - Domain events for profile lifecycle (Created, Updated, Completed)
///
/// This follows Onion Architecture - rich domain model with behavior, not anemic data model.
/// </remarks>
public class MemberProfile
{
    // Identity
    public MemberId UserId { get; private set; }

    // Personal Information
    public string FullName { get; private set; } // Will be encrypted via Infrastructure
    public DateTime DateOfBirth { get; private set; }
    public string Gender { get; private set; }
    public Location Location { get; private set; }

    // Profile Data (Value Objects)
    public PersonalityProfile? Personality { get; private set; }
    public Values? Values { get; private set; }
    public Lifestyle? Lifestyle { get; private set; }
    public PreferenceSet? Preferences { get; private set; }

    // Privacy
    public ExposureLevel ExposureLevel { get; private set; }

    // Metadata
    public int CompletionPercentage { get; private set; }
    public bool IsComplete { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    // Domain Events (not persisted)
    private readonly List<object> _domainEvents = new();
    public IReadOnlyList<object> DomainEvents => _domainEvents.AsReadOnly();

    // Private constructor for EF Core
    private MemberProfile() { }

    /// <summary>
    /// Creates a skeleton profile for a newly registered user.
    /// </summary>
    /// <remarks>
    /// Called when UserRegistered event is received from IdentityService.
    /// Profile starts at 0% completion with only UserId and Email.
    /// </remarks>
    public static MemberProfile CreateSkeleton(MemberId userId, string email)
    {
        var profile = new MemberProfile
        {
            UserId = userId,
            FullName = string.Empty, // To be filled later
            DateOfBirth = default,
            Gender = string.Empty,
            Location = null!,
            Personality = null,
            Values = null,
            Lifestyle = null,
            Preferences = null,
            ExposureLevel = ExposureLevel.MatchedOnly, // Default: highest privacy
            CompletionPercentage = 0,
            IsComplete = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Raise ProfileCreated event
        profile._domainEvents.Add(new ProfileCreated(
            userId,
            email,
            profile.CreatedAt,
            Guid.NewGuid()
        ));

        return profile;
    }

    /// <summary>
    /// Updates basic profile information (name, DOB, gender, location).
    /// </summary>
    public void UpdateBasicInfo(string fullName, DateTime dateOfBirth, string gender, Location location)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ProfileDomainException("Full name is required");

        if (string.IsNullOrWhiteSpace(gender))
            throw new ProfileDomainException("Gender is required");

        if (location == null)
            throw new ProfileDomainException("Location is required");

        // Validate age (must be 18+)
        var age = CalculateAge(dateOfBirth);
        if (age < 18)
            throw new ProfileDomainException($"Member must be at least 18 years old (current age: {age})");

        FullName = fullName;
        DateOfBirth = dateOfBirth;
        Gender = gender;
        Location = location;

        UpdateMetadata(new List<string> { "FullName", "DateOfBirth", "Gender", "Location" });
    }

    /// <summary>
    /// Updates personality profile.
    /// </summary>
    public void UpdatePersonality(PersonalityProfile personality)
    {
        Personality = personality ?? throw new ArgumentNullException(nameof(personality));
        UpdateMetadata(new List<string> { "Personality" });
    }

    /// <summary>
    /// Updates values.
    /// </summary>
    public void UpdateValues(Values values)
    {
        Values = values ?? throw new ArgumentNullException(nameof(values));
        UpdateMetadata(new List<string> { "Values" });
    }

    /// <summary>
    /// Updates lifestyle.
    /// </summary>
    public void UpdateLifestyle(Lifestyle lifestyle)
    {
        Lifestyle = lifestyle ?? throw new ArgumentNullException(nameof(lifestyle));
        UpdateMetadata(new List<string> { "Lifestyle" });
    }

    /// <summary>
    /// Updates matching preferences.
    /// </summary>
    public void UpdatePreferences(PreferenceSet preferences)
    {
        Preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        UpdateMetadata(new List<string> { "Preferences" });
    }

    /// <summary>
    /// Updates exposure level (privacy setting).
    /// </summary>
    public void UpdateExposureLevel(ExposureLevel exposureLevel)
    {
        ExposureLevel = exposureLevel;
        UpdateMetadata(new List<string> { "ExposureLevel" });
    }

    /// <summary>
    /// Soft-deletes the profile (GDPR compliance - right to erasure).
    /// </summary>
    /// <remarks>
    /// Soft delete allows for a 30-day retention period before hard deletion.
    /// During this period, the user can be reminded or data can be used for fraud detection.
    /// </remarks>
    public void SoftDelete()
    {
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Determines if this profile can be shared with another member based on privacy rules.
    /// </summary>
    /// <param name="matchStatus">The match status with the other member</param>
    /// <returns>True if profile can be shared, false otherwise</returns>
    /// <remarks>
    /// Privacy rules based on ExposureLevel:
    /// - MatchedOnly: Only if matchStatus is Accepted (mutual match)
    /// - AllMatches: If matchStatus is anything except None (candidate or accepted)
    /// - Public: Always (future feature for profile discovery)
    /// </remarks>
    public bool CanShareWith(MatchStatus matchStatus)
    {
        return ExposureLevel switch
        {
            ExposureLevel.MatchedOnly => matchStatus == MatchStatus.Accepted,
            ExposureLevel.AllMatches => matchStatus != MatchStatus.None,
            ExposureLevel.Public => true,
            _ => false
        };
    }

    /// <summary>
    /// Calculates profile completion percentage based on filled fields.
    /// </summary>
    /// <remarks>
    /// Completion calculation:
    /// - Basic info (name, DOB, gender, location): 20%
    /// - Personality: 20%
    /// - Values: 20%
    /// - Lifestyle: 20%
    /// - Preferences: 20%
    ///
    /// Total: 100% when all sections filled
    /// </remarks>
    private int CalculateCompletion()
    {
        int completion = 0;

        // Basic info (20%)
        if (!string.IsNullOrWhiteSpace(FullName) &&
            DateOfBirth != default &&
            !string.IsNullOrWhiteSpace(Gender) &&
            Location != null)
        {
            completion += 20;
        }

        // Personality (20%)
        if (Personality != null)
            completion += 20;

        // Values (20%)
        if (Values != null)
            completion += 20;

        // Lifestyle (20%)
        if (Lifestyle != null)
            completion += 20;

        // Preferences (20%)
        if (Preferences != null)
            completion += 20;

        return completion;
    }

    /// <summary>
    /// Checks if profile is complete (>= 80%) and raises ProfileCompleted event if transitioning.
    /// </summary>
    private void CheckIfComplete()
    {
        bool wasCompleted = IsComplete;
        IsComplete = CompletionPercentage >= 80;

        // Raise ProfileCompleted event only on first completion (false → true transition)
        if (!wasCompleted && IsComplete)
        {
            _domainEvents.Add(new ProfileCompleted(
                UserId,
                DateTime.UtcNow,
                Guid.NewGuid()
            ));
        }
    }

    /// <summary>
    /// Updates metadata (completion %, timestamps) and raises ProfileUpdated event.
    /// </summary>
    private void UpdateMetadata(List<string> updatedFields)
    {
        UpdatedAt = DateTime.UtcNow;
        CompletionPercentage = CalculateCompletion();
        CheckIfComplete();

        // Raise ProfileUpdated event
        _domainEvents.Add(new ProfileUpdated(
            UserId,
            updatedFields,
            UpdatedAt,
            Guid.NewGuid()
        ));
    }

    /// <summary>
    /// Calculates age from date of birth.
    /// </summary>
    private static int CalculateAge(DateTime dateOfBirth)
    {
        var today = DateTime.UtcNow;
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > today.AddYears(-age))
            age--;
        return age;
    }

    /// <summary>
    /// Clears domain events (called after publishing events to message bus).
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

/// <summary>
/// Enum representing match status between two members.
/// </summary>
/// <remarks>
/// Used by CanShareWith() privacy logic.
/// Defined here rather than in MatchingService to avoid circular dependencies.
/// </remarks>
public enum MatchStatus
{
    None,           // No match relationship
    Candidate,      // Presented as a match candidate (not yet accepted)
    Accepted        // Mutual match (both accepted)
}
