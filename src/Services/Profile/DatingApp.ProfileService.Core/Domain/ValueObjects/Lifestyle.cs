namespace DatingApp.ProfileService.Core.Domain.ValueObjects;

/// <summary>
/// Value Object representing a Member's lifestyle choices and habits.
/// </summary>
/// <remarks>
/// Lifestyle attributes help assess day-to-day compatibility:
/// - Exercise and diet preferences
/// - Substance use (smoking, drinking)
/// - Pet ownership and stance on children
///
/// These are key compatibility factors for long-term relationships.
/// </remarks>
public record Lifestyle
{
    public ExerciseFrequency ExerciseFrequency { get; init; }
    public DietType DietType { get; init; }
    public SmokingStatus SmokingStatus { get; init; }
    public DrinkingFrequency DrinkingFrequency { get; init; }
    public bool HasPets { get; init; }
    public ChildrenPreference WantsChildren { get; init; }

    public Lifestyle(
        ExerciseFrequency exerciseFrequency,
        DietType dietType,
        SmokingStatus smokingStatus,
        DrinkingFrequency drinkingFrequency,
        bool hasPets,
        ChildrenPreference wantsChildren)
    {
        ExerciseFrequency = exerciseFrequency;
        DietType = dietType;
        SmokingStatus = smokingStatus;
        DrinkingFrequency = drinkingFrequency;
        HasPets = hasPets;
        WantsChildren = wantsChildren;
    }
}

public enum ExerciseFrequency
{
    Never,
    Occasionally,    // 1-2 times per week
    Regularly,       // 3-4 times per week
    Daily            // 5+ times per week
}

public enum DietType
{
    Omnivore,
    Vegetarian,
    Vegan,
    Pescatarian,
    Other
}

public enum SmokingStatus
{
    Never,
    Occasionally,
    Regularly,
    TryingToQuit
}

public enum DrinkingFrequency
{
    Never,
    Occasionally,    // Less than once per week
    Socially,        // 1-2 times per week
    Regularly        // 3+ times per week
}

public enum ChildrenPreference
{
    Yes,             // Wants children
    No,              // Does not want children
    Maybe,           // Undecided or open
    HasChildren      // Already has children
}
