namespace DatingApp.IdentityService.Application.DTOs;

/// <summary>
/// Request DTO for revoking a refresh token.
/// </summary>
/// <param name="RefreshToken">The refresh token to revoke</param>
/// <remarks>
/// This endpoint is used when a user explicitly logs out or wants to revoke a specific session.
/// Revoking a token makes it unusable for future refresh operations.
/// The operation is idempotent - revoking an already-revoked token succeeds silently.
/// </remarks>
public record RevokeTokenRequest(string RefreshToken);
