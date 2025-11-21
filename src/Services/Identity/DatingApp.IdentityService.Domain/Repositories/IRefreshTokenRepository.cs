using DatingApp.IdentityService.Domain.Entities;

namespace DatingApp.IdentityService.Domain.Repositories;

/// <summary>
/// Repository interface for RefreshToken entity operations.
/// </summary>
/// <remarks>
/// Layered Architecture: Interface defined in Domain, implemented in Infrastructure.
/// Enables dependency inversion and testability.
/// </remarks>
public interface IRefreshTokenRepository
{
    /// <summary>
    /// Retrieves a refresh token by its hash.
    /// </summary>
    /// <param name="tokenHash">The SHA-256 hash of the refresh token</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The refresh token if found, null otherwise</returns>
    /// <remarks>
    /// Primary lookup method when validating a refresh token.
    /// Database has a unique index on TokenHash for fast lookup.
    /// Remember: We never store plain text tokens, only their hashes.
    /// </remarks>
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all active (non-revoked, non-expired) refresh tokens for a user.
    /// </summary>
    /// <param name="userId">The user's unique ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Collection of active refresh tokens</returns>
    /// <remarks>
    /// Used for security audits and managing user sessions.
    /// "Active" means: IsRevoked = false AND ExpiresAt > UtcNow
    /// A user may have multiple active tokens (logged in on multiple devices).
    /// </remarks>
    Task<IEnumerable<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all refresh tokens for a user (active, revoked, expired).
    /// </summary>
    /// <param name="userId">The user's unique ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Collection of all refresh tokens</returns>
    /// <remarks>
    /// Used for administrative purposes and security investigations.
    /// Returns all tokens regardless of status.
    /// </remarks>
    Task<IEnumerable<RefreshToken>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Adds a new refresh token to the repository.
    /// </summary>
    /// <param name="token">The refresh token entity to add</param>
    /// <param name="ct">Cancellation token</param>
    /// <remarks>
    /// Called when issuing a new refresh token during login or token refresh.
    /// Implementation should call SaveChangesAsync to persist to database.
    /// </remarks>
    Task AddAsync(RefreshToken token, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing refresh token in the repository.
    /// </summary>
    /// <param name="token">The refresh token entity to update</param>
    /// <param name="ct">Cancellation token</param>
    /// <remarks>
    /// Called when revoking a token (sets IsRevoked = true, RevokedAt = UtcNow).
    /// Implementation should call SaveChangesAsync to persist to database.
    /// </remarks>
    Task UpdateAsync(RefreshToken token, CancellationToken ct = default);

    /// <summary>
    /// Revokes all active refresh tokens for a user.
    /// </summary>
    /// <param name="userId">The user's unique ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <remarks>
    /// Used when logging out from all devices or during security incidents.
    /// Marks all active tokens as revoked with RevokedAt = UtcNow.
    /// </remarks>
    Task RevokeAllByUserIdAsync(Guid userId, CancellationToken ct = default);
}
