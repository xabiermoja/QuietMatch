namespace DatingApp.IdentityService.Application.DTOs;

/// <summary>
/// Response DTO for successful authentication.
/// </summary>
/// <param name="AccessToken">JWT access token for API authentication (15 min expiry)</param>
/// <param name="RefreshToken">Refresh token for session renewal (7 days expiry)</param>
/// <param name="ExpiresIn">Access token lifetime in seconds (900 = 15 minutes)</param>
/// <param name="TokenType">Token type for Authorization header ("Bearer")</param>
/// <param name="UserId">User's unique identifier</param>
/// <param name="IsNewUser">True if first-time login (redirect to profile creation)</param>
/// <param name="Email">User's email address</param>
/// <remarks>
/// This response matches the API specification in F0001 feature file.
/// IsNewUser flag determines client navigation: profile creation vs. dashboard.
/// Tokens should be stored securely: localStorage (web), SecureStorage (mobile).
/// </remarks>
public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType,
    Guid UserId,
    bool IsNewUser,
    string Email
);
