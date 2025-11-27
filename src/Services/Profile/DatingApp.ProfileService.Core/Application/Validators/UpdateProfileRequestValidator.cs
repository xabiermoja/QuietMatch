using DatingApp.ProfileService.Core.Application.DTOs;
using FluentValidation;

namespace DatingApp.ProfileService.Core.Application.Validators;

/// <summary>
/// Validator for UpdateProfileRequest DTO.
/// </summary>
/// <remarks>
/// Validates profile update sections before calling domain methods.
/// All sections are optional - validation only runs if section is provided.
/// </remarks>
public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.Personality)
            .SetValidator(new PersonalityDtoValidator()!)
            .When(x => x.Personality != null);

        RuleFor(x => x.Values)
            .SetValidator(new ValuesDtoValidator()!)
            .When(x => x.Values != null);

        RuleFor(x => x.Lifestyle)
            .SetValidator(new LifestyleDtoValidator()!)
            .When(x => x.Lifestyle != null);

        RuleFor(x => x.Preferences)
            .SetValidator(new PreferencesDtoValidator()!)
            .When(x => x.Preferences != null);

        RuleFor(x => x.ExposureLevel)
            .IsInEnum().WithMessage("Invalid exposure level")
            .When(x => x.ExposureLevel.HasValue);
    }
}

/// <summary>
/// Validator for PersonalityDto.
/// </summary>
public class PersonalityDtoValidator : AbstractValidator<PersonalityDto>
{
    private const int MinScore = 1;
    private const int MaxScore = 5;

    public PersonalityDtoValidator()
    {
        RuleFor(x => x.Openness)
            .InclusiveBetween(MinScore, MaxScore)
            .WithMessage($"Openness must be between {MinScore} and {MaxScore}");

        RuleFor(x => x.Conscientiousness)
            .InclusiveBetween(MinScore, MaxScore)
            .WithMessage($"Conscientiousness must be between {MinScore} and {MaxScore}");

        RuleFor(x => x.Extraversion)
            .InclusiveBetween(MinScore, MaxScore)
            .WithMessage($"Extraversion must be between {MinScore} and {MaxScore}");

        RuleFor(x => x.Agreeableness)
            .InclusiveBetween(MinScore, MaxScore)
            .WithMessage($"Agreeableness must be between {MinScore} and {MaxScore}");

        RuleFor(x => x.Neuroticism)
            .InclusiveBetween(MinScore, MaxScore)
            .WithMessage($"Neuroticism must be between {MinScore} and {MaxScore}");

        RuleFor(x => x.AboutMe)
            .MaximumLength(500).WithMessage("About me must not exceed 500 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.AboutMe));

        RuleFor(x => x.LifePhilosophy)
            .MaximumLength(500).WithMessage("Life philosophy must not exceed 500 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.LifePhilosophy));
    }
}

/// <summary>
/// Validator for ValuesDto.
/// </summary>
public class ValuesDtoValidator : AbstractValidator<ValuesDto>
{
    private const int MinScore = 1;
    private const int MaxScore = 5;

    public ValuesDtoValidator()
    {
        RuleFor(x => x.FamilyOrientation)
            .InclusiveBetween(MinScore, MaxScore)
            .WithMessage($"FamilyOrientation must be between {MinScore} and {MaxScore}");

        RuleFor(x => x.CareerAmbition)
            .InclusiveBetween(MinScore, MaxScore)
            .WithMessage($"CareerAmbition must be between {MinScore} and {MaxScore}");

        RuleFor(x => x.Spirituality)
            .InclusiveBetween(MinScore, MaxScore)
            .WithMessage($"Spirituality must be between {MinScore} and {MaxScore}");

        RuleFor(x => x.Adventure)
            .InclusiveBetween(MinScore, MaxScore)
            .WithMessage($"Adventure must be between {MinScore} and {MaxScore}");

        RuleFor(x => x.IntellectualCuriosity)
            .InclusiveBetween(MinScore, MaxScore)
            .WithMessage($"IntellectualCuriosity must be between {MinScore} and {MaxScore}");

        RuleFor(x => x.SocialJustice)
            .InclusiveBetween(MinScore, MaxScore)
            .WithMessage($"SocialJustice must be between {MinScore} and {MaxScore}");

        RuleFor(x => x.FinancialSecurity)
            .InclusiveBetween(MinScore, MaxScore)
            .WithMessage($"FinancialSecurity must be between {MinScore} and {MaxScore}");

        RuleFor(x => x.Environmentalism)
            .InclusiveBetween(MinScore, MaxScore)
            .WithMessage($"Environmentalism must be between {MinScore} and {MaxScore}");
    }
}

/// <summary>
/// Validator for LifestyleDto.
/// </summary>
public class LifestyleDtoValidator : AbstractValidator<LifestyleDto>
{
    private static readonly string[] ValidExerciseFrequencies = { "Never", "Occasionally", "Regularly", "Daily" };
    private static readonly string[] ValidDietTypes = { "Omnivore", "Vegetarian", "Vegan", "Pescatarian", "Other" };
    private static readonly string[] ValidSmokingStatuses = { "Never", "Occasionally", "Regularly", "TryingToQuit" };
    private static readonly string[] ValidDrinkingFrequencies = { "Never", "Occasionally", "Socially", "Regularly" };
    private static readonly string[] ValidChildrenPreferences = { "Yes", "No", "Maybe", "HasChildren" };

    public LifestyleDtoValidator()
    {
        RuleFor(x => x.ExerciseFrequency)
            .NotEmpty().WithMessage("Exercise frequency is required")
            .Must(x => ValidExerciseFrequencies.Contains(x))
            .WithMessage($"Exercise frequency must be one of: {string.Join(", ", ValidExerciseFrequencies)}");

        RuleFor(x => x.DietType)
            .NotEmpty().WithMessage("Diet type is required")
            .Must(x => ValidDietTypes.Contains(x))
            .WithMessage($"Diet type must be one of: {string.Join(", ", ValidDietTypes)}");

        RuleFor(x => x.SmokingStatus)
            .NotEmpty().WithMessage("Smoking status is required")
            .Must(x => ValidSmokingStatuses.Contains(x))
            .WithMessage($"Smoking status must be one of: {string.Join(", ", ValidSmokingStatuses)}");

        RuleFor(x => x.DrinkingFrequency)
            .NotEmpty().WithMessage("Drinking frequency is required")
            .Must(x => ValidDrinkingFrequencies.Contains(x))
            .WithMessage($"Drinking frequency must be one of: {string.Join(", ", ValidDrinkingFrequencies)}");

        RuleFor(x => x.WantsChildren)
            .NotEmpty().WithMessage("Children preference is required")
            .Must(x => ValidChildrenPreferences.Contains(x))
            .WithMessage($"Children preference must be one of: {string.Join(", ", ValidChildrenPreferences)}");
    }
}

/// <summary>
/// Validator for PreferencesDto.
/// </summary>
public class PreferencesDtoValidator : AbstractValidator<PreferencesDto>
{
    private static readonly string[] ValidGenderPreferences = { "Men", "Women", "NonBinary", "NoPreference" };

    public PreferencesDtoValidator()
    {
        RuleFor(x => x.PreferredAgeRange)
            .NotNull().WithMessage("Preferred age range is required")
            .SetValidator(new AgeRangeDtoValidator());

        RuleFor(x => x.MaxDistanceKm)
            .InclusiveBetween(1, 500).WithMessage("Maximum distance must be between 1 and 500 km");

        RuleFor(x => x.PreferredLanguages)
            .NotEmpty().WithMessage("At least one preferred language is required")
            .Must(x => x.Count <= 10).WithMessage("Maximum 10 preferred languages allowed");

        RuleFor(x => x.GenderPreference)
            .NotEmpty().WithMessage("Gender preference is required")
            .Must(x => ValidGenderPreferences.Contains(x))
            .WithMessage($"Gender preference must be one of: {string.Join(", ", ValidGenderPreferences)}");
    }
}

/// <summary>
/// Validator for AgeRangeDto.
/// </summary>
public class AgeRangeDtoValidator : AbstractValidator<AgeRangeDto>
{
    private const int AbsoluteMinimumAge = 18;
    private const int AbsoluteMaximumAge = 100;

    public AgeRangeDtoValidator()
    {
        RuleFor(x => x.Min)
            .GreaterThanOrEqualTo(AbsoluteMinimumAge)
            .WithMessage($"Minimum age must be at least {AbsoluteMinimumAge}");

        RuleFor(x => x.Max)
            .LessThanOrEqualTo(AbsoluteMaximumAge)
            .WithMessage($"Maximum age must not exceed {AbsoluteMaximumAge}");

        RuleFor(x => x)
            .Must(x => x.Min <= x.Max)
            .WithMessage("Minimum age must be less than or equal to maximum age");
    }
}
