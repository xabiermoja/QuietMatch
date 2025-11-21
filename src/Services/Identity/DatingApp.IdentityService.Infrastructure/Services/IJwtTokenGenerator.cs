namespace DatingApp.IdentityService.Infrastructure.Services;

/// <summary>
/// Service for generating and hashing JWT tokens.
/// </summary>
/// <remarks>
/// Interface defined in Infrastructure (not Domain) because JWT is infrastructure-specific.
/// Generates access tokens (short-lived) and refresh tokens (long-lived).
/// </remarks>
public interface IJwtTokenGenerator
{
    /// <summary>
    /// Generates a JWT access token for API authentication.
    /// </summary>
    /// <param name="userId">The user's unique identifier</param>
    /// <param name="email">The user's email address</param>
    /// <returns>A signed JWT access token</returns>
    /// <remarks>
    /// Access token lifetime: 15 minutes (configurable).
    /// Claims: sub (userId), email, jti (unique token ID), iat (issued at).
    /// Algorithm: HMAC-SHA256.
    /// </remarks>
    string GenerateAccessToken(Guid userId, string email);

    /// <summary>
    /// Generates a cryptographically secure refresh token.
    /// </summary>
    /// <returns>A random Base64-encoded string</returns>
    /// <remarks>
    /// Uses RandomNumberGenerator for cryptographic randomness (32 bytes).
    /// This is NOT a JWT - just a random identifier.
    /// Must be hashed (SHA-256) before storing in database.
    /// </remarks>
    string GenerateRefreshToken();

    /// <summary>
    /// Computes SHA-256 hash of a token for secure storage.
    /// </summary>
    /// <param name="token">The token to hash</param>
    /// <returns>Base64-encoded SHA-256 hash</returns>
    /// <remarks>
    /// SECURITY: Never store tokens in plain text.
    /// If database is compromised, hashed tokens cannot be used directly.
    /// Same input always produces same hash (deterministic).
    /// </remarks>
    string HashToken(string token);
}
