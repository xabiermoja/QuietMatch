using DatingApp.IdentityService.Application.DTOs;
using FluentValidation;

namespace DatingApp.IdentityService.Application.Validators;

/// <summary>
/// Validator for LoginWithGoogleRequest.
/// </summary>
/// <remarks>
/// Input validation at the application boundary.
/// Fail fast before calling business logic.
/// FluentValidation provides clear, readable validation rules.
/// </remarks>
public class LoginWithGoogleRequestValidator : AbstractValidator<LoginWithGoogleRequest>
{
    public LoginWithGoogleRequestValidator()
    {
        RuleFor(x => x.IdToken)
            .NotEmpty()
            .WithMessage("ID token is required")
            .MaximumLength(2048)
            .WithMessage("ID token is too long (max 2048 characters)");
    }
}
