using DatingApp.IdentityService.Application.DTOs;
using FluentValidation;

namespace DatingApp.IdentityService.Application.Validators;

/// <summary>
/// Validator for RefreshTokenRequest.
/// </summary>
/// <remarks>
/// Input validation at the application boundary.
/// Validates that the refresh token is present and well-formed.
/// Security: Prevents empty or malformed tokens from reaching business logic.
/// </remarks>
public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("Refresh token is required")
            .MaximumLength(512)
            .WithMessage("Refresh token is too long (max 512 characters)");
    }
}
