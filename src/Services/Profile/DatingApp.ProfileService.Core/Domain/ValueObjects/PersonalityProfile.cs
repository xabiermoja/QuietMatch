using DatingApp.ProfileService.Core.Domain.Exceptions;

namespace DatingApp.ProfileService.Core.Domain.ValueObjects;

/// <summary>
/// Value Object representing a Member's personality profile using the Big Five personality traits.
/// </summary>
/// <remarks>
/// The Big Five (OCEAN) personality model:
/// - Openness: Creativity, curiosity, openness to new experiences
/// - Conscientiousness: Organization, dependability, self-discipline
/// - Extraversion: Sociability, assertiveness, energy level
/// - Agreeableness: Compassion, cooperation, trust
/// - Neuroticism: Emotional stability, anxiety, moodiness
///
/// All traits are scored on a 1-5 scale (1=Low, 5=High).
/// Free-text fields (AboutMe, LifePhilosophy) are used for semantic matching via embeddings.
/// </remarks>
public record PersonalityProfile
{
    private const int MinScore = 1;
    private const int MaxScore = 5;
    private const int AboutMeMaxLength = 500;
    private const int LifePhilosophyMaxLength = 500;

    public int Openness { get; init; }
    public int Conscientiousness { get; init; }
    public int Extraversion { get; init; }
    public int Agreeableness { get; init; }
    public int Neuroticism { get; init; }

    public string? AboutMe { get; init; }
    public string? LifePhilosophy { get; init; }

    // Private constructor for EF Core
    private PersonalityProfile() { }

    public PersonalityProfile(
        int openness,
        int conscientiousness,
        int extraversion,
        int agreeableness,
        int neuroticism,
        string? aboutMe = null,
        string? lifePhilosophy = null)
    {
        // Validate Big Five scores (1-5 scale)
        ValidateScore(openness, nameof(Openness));
        ValidateScore(conscientiousness, nameof(Conscientiousness));
        ValidateScore(extraversion, nameof(Extraversion));
        ValidateScore(agreeableness, nameof(Agreeableness));
        ValidateScore(neuroticism, nameof(Neuroticism));

        // Validate text field lengths
        if (aboutMe?.Length > AboutMeMaxLength)
            throw new ProfileDomainException($"AboutMe cannot exceed {AboutMeMaxLength} characters");

        if (lifePhilosophy?.Length > LifePhilosophyMaxLength)
            throw new ProfileDomainException($"LifePhilosophy cannot exceed {LifePhilosophyMaxLength} characters");

        Openness = openness;
        Conscientiousness = conscientiousness;
        Extraversion = extraversion;
        Agreeableness = agreeableness;
        Neuroticism = neuroticism;
        AboutMe = aboutMe;
        LifePhilosophy = lifePhilosophy;
    }

    private static void ValidateScore(int score, string traitName)
    {
        if (score < MinScore || score > MaxScore)
            throw new ProfileDomainException($"{traitName} must be between {MinScore} and {MaxScore} (received: {score})");
    }
}
