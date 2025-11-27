namespace DatingApp.ProfileService.Core.Application.DTOs;

/// <summary>
/// Request DTO for updating profile sections.
/// </summary>
/// <remarks>
/// Used by PUT /api/profiles/{userId} endpoint.
/// All fields are optional - only provided sections will be updated.
/// </remarks>
public class UpdateProfileRequest
{
    public PersonalityDto? Personality { get; set; }
    public ValuesDto? Values { get; set; }
    public LifestyleDto? Lifestyle { get; set; }
    public PreferencesDto? Preferences { get; set; }
    public ExposureLevelDto? ExposureLevel { get; set; }
}

/// <summary>
/// Personality profile DTO based on Big Five model.
/// </summary>
public class PersonalityDto
{
    public int Openness { get; set; }
    public int Conscientiousness { get; set; }
    public int Extraversion { get; set; }
    public int Agreeableness { get; set; }
    public int Neuroticism { get; set; }
    public string? AboutMe { get; set; }
    public string? LifePhilosophy { get; set; }
}

/// <summary>
/// Core values DTO for compatibility matching.
/// </summary>
public class ValuesDto
{
    public int FamilyOrientation { get; set; }
    public int CareerAmbition { get; set; }
    public int Spirituality { get; set; }
    public int Adventure { get; set; }
    public int IntellectualCuriosity { get; set; }
    public int SocialJustice { get; set; }
    public int FinancialSecurity { get; set; }
    public int Environmentalism { get; set; }
}

/// <summary>
/// Lifestyle information DTO.
/// </summary>
public class LifestyleDto
{
    public string ExerciseFrequency { get; set; } = string.Empty;
    public string DietType { get; set; } = string.Empty;
    public string SmokingStatus { get; set; } = string.Empty;
    public string DrinkingFrequency { get; set; } = string.Empty;
    public bool HasPets { get; set; }
    public string WantsChildren { get; set; } = string.Empty;
}

/// <summary>
/// Matching preferences DTO.
/// </summary>
public class PreferencesDto
{
    public AgeRangeDto PreferredAgeRange { get; set; } = null!;
    public int MaxDistanceKm { get; set; }
    public List<string> PreferredLanguages { get; set; } = new();
    public string GenderPreference { get; set; } = string.Empty;
}

/// <summary>
/// Age range DTO.
/// </summary>
public class AgeRangeDto
{
    public int Min { get; set; }
    public int Max { get; set; }
}

/// <summary>
/// Privacy exposure level DTO.
/// </summary>
public enum ExposureLevelDto
{
    MatchedOnly = 1,
    AllMatches = 2,
    Public = 3
}
