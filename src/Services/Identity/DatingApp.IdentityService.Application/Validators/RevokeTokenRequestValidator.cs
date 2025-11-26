using DatingApp.IdentityService.Application.DTOs;
using FluentValidation;

namespace DatingApp.IdentityService.Application.Validators;

/// <summary>
/// Validator for RevokeTokenRequest.
/// </summary>
public class RevokeTokenRequestValidator : AbstractValidator<RevokeTokenRequest>
{
    public RevokeTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("Refresh token is required")
            .MaximumLength(512)
            .WithMessage("Refresh token is too long (max 512 characters)");
    }
}
