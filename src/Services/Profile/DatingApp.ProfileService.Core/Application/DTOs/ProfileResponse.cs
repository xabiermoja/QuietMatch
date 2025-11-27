namespace DatingApp.ProfileService.Core.Application.DTOs;

/// <summary>
/// Response DTO for profile data.
/// </summary>
/// <remarks>
/// Used by GET /api/profiles/{userId} and GET /api/profiles/me endpoints.
/// Returns all profile information including completion status.
/// </remarks>
public class ProfileResponse
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public LocationDto Location { get; set; } = null!;

    public PersonalityDto? Personality { get; set; }
    public ValuesDto? Values { get; set; }
    public LifestyleDto? Lifestyle { get; set; }
    public PreferencesDto? Preferences { get; set; }

    public ExposureLevelDto ExposureLevel { get; set; }

    public int CompletionPercentage { get; set; }
    public bool IsComplete { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
