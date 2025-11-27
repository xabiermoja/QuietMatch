using DatingApp.ProfileService.Core.Application.DTOs;
using FluentValidation;

namespace DatingApp.ProfileService.Core.Application.Validators;

/// <summary>
/// Validator for CreateProfileRequest DTO.
/// </summary>
/// <remarks>
/// Validates basic profile information before calling domain methods.
/// Business rules:
/// - FullName: Required, max 200 characters
/// - DateOfBirth: Must result in age >= 18
/// - Gender: Required, max 50 characters
/// - Location: Required with City and Country
/// </remarks>
public class CreateProfileRequestValidator : AbstractValidator<CreateProfileRequest>
{
    public CreateProfileRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .MaximumLength(200).WithMessage("Full name must not exceed 200 characters");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required")
            .Must(BeAtLeast18YearsOld).WithMessage("Member must be at least 18 years old");

        RuleFor(x => x.Gender)
            .NotEmpty().WithMessage("Gender is required")
            .MaximumLength(50).WithMessage("Gender must not exceed 50 characters");

        RuleFor(x => x.Location)
            .NotNull().WithMessage("Location is required")
            .SetValidator(new LocationDtoValidator());
    }

    private static bool BeAtLeast18YearsOld(DateTime dateOfBirth)
    {
        var today = DateTime.UtcNow;
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > today.AddYears(-age))
            age--;
        return age >= 18;
    }
}

/// <summary>
/// Validator for LocationDto.
/// </summary>
public class LocationDtoValidator : AbstractValidator<LocationDto>
{
    public LocationDtoValidator()
    {
        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required")
            .MaximumLength(100).WithMessage("City must not exceed 100 characters");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required")
            .MaximumLength(100).WithMessage("Country must not exceed 100 characters");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue)
            .WithMessage("Latitude must be between -90 and 90");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue)
            .WithMessage("Longitude must be between -180 and 180");
    }
}
