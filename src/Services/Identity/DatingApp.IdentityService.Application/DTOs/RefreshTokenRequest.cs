namespace DatingApp.IdentityService.Application.DTOs;

/// <summary>
/// Request DTO for refreshing an expired access token.
/// </summary>
/// <param name="RefreshToken">The refresh token issued during login</param>
/// <remarks>
/// Client sends this when their access token expires (after 15 minutes).
/// The refresh token allows obtaining a new access token without re-authenticating.
/// Security: Refresh tokens are validated server-side (hash comparison).
/// </remarks>
public record RefreshTokenRequest(string RefreshToken);
