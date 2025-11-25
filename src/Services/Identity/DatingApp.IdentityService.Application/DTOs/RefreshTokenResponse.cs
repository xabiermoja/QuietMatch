namespace DatingApp.IdentityService.Application.DTOs;

/// <summary>
/// Response DTO for successful token refresh.
/// </summary>
/// <param name="AccessToken">New JWT access token for API authentication (15 min expiry)</param>
/// <param name="RefreshToken">New refresh token for session renewal (7 days expiry)</param>
/// <param name="ExpiresIn">Access token lifetime in seconds (900 = 15 minutes)</param>
/// <param name="TokenType">Token type for Authorization header ("Bearer")</param>
/// <remarks>
/// Token Rotation: Each refresh returns a NEW refresh token and revokes the old one.
/// This is a security best practice to limit token lifetime and detect stolen tokens.
/// If rotation is disabled, the same refresh token can be reused until expiry.
///
/// Decision for this implementation: Token rotation ENABLED for better security.
/// </remarks>
public record RefreshTokenResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType
);
