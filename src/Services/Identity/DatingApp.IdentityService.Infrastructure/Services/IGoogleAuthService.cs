namespace DatingApp.IdentityService.Infrastructure.Services;

/// <summary>
/// Service for validating Google OAuth ID tokens.
/// </summary>
/// <remarks>
/// Interface defined in Infrastructure (not Domain) because this is infrastructure-specific.
/// Validates ID tokens server-side using Google's public keys.
/// </remarks>
public interface IGoogleAuthService
{
    /// <summary>
    /// Validates a Google ID token and extracts user information.
    /// </summary>
    /// <param name="idToken">The Google ID token (JWT) from the client</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>GoogleUserInfo if valid, null if invalid</returns>
    /// <remarks>
    /// Server-side validation prevents client token tampering.
    /// Validates: Signature, Issuer (accounts.google.com), Audience (our Client ID), Expiry.
    /// </remarks>
    Task<GoogleUserInfo?> ValidateIdTokenAsync(string idToken, CancellationToken ct = default);
}

/// <summary>
/// User information extracted from a validated Google ID token.
/// </summary>
/// <param name="Sub">Google's unique user identifier (subject claim)</param>
/// <param name="Email">User's email address</param>
/// <param name="Name">User's full name (optional)</param>
/// <param name="EmailVerified">Whether Google verified the email</param>
/// <remarks>
/// This is a record for immutability - user info from Google should not change after validation.
/// Sub is the primary identifier - use this for ExternalUserId in User entity.
/// </remarks>
public record GoogleUserInfo(
    string Sub,
    string Email,
    string? Name,
    bool EmailVerified
);
