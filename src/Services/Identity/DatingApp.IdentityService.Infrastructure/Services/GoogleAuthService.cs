using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DatingApp.IdentityService.Infrastructure.Services;

/// <summary>
/// Implementation of Google OAuth ID token validation.
/// </summary>
/// <remarks>
/// Uses Google.Apis.Auth library for server-side token validation.
/// Validates signature, issuer, audience, and expiry automatically.
/// </remarks>
public class GoogleAuthService : IGoogleAuthService
{
    private readonly string _clientId;
    private readonly ILogger<GoogleAuthService> _logger;

    public GoogleAuthService(IConfiguration configuration, ILogger<GoogleAuthService> logger)
    {
        _clientId = configuration["Google:ClientId"]
            ?? throw new InvalidOperationException("Google:ClientId is not configured");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<GoogleUserInfo?> ValidateIdTokenAsync(string idToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            _logger.LogWarning("ID token is null or empty");
            return null;
        }

        try
        {
            // Validate ID token with Google
            // This validates: Signature (using Google's public keys), Issuer, Audience, Expiry
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _clientId }
            });

            _logger.LogInformation(
                "Successfully validated Google ID token for user {Email} (sub: {Sub})",
                payload.Email,
                payload.Subject);

            return new GoogleUserInfo(
                Sub: payload.Subject, // Google's unique user ID
                Email: payload.Email,
                Name: payload.Name,
                EmailVerified: payload.EmailVerified
            );
        }
        catch (InvalidJwtException ex)
        {
            // Token is invalid (wrong signature, expired, wrong audience, etc.)
            _logger.LogWarning(ex, "Invalid Google ID token");
            return null;
        }
        catch (Exception ex)
        {
            // Unexpected error (network issues, Google API down, etc.)
            _logger.LogError(ex, "Error validating Google ID token");
            throw; // Re-throw for upstream error handling
        }
    }
}
