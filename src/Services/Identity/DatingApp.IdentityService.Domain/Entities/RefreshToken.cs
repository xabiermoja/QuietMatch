namespace DatingApp.IdentityService.Domain.Entities;

/// <summary>
/// Represents a refresh token for session management.
/// </summary>
/// <remarks>
/// Refresh tokens allow users to obtain new access tokens without re-authenticating.
/// Security: Tokens are hashed (SHA-256) before storage to prevent theft.
/// Lifecycle: 7 days validity, can be revoked for security.
/// </remarks>
public class RefreshToken
{
    /// <summary>
    /// Unique identifier for the refresh token.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The user this refresh token belongs to.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// SHA-256 hash of the refresh token.
    /// </summary>
    /// <remarks>
    /// SECURITY: Never store refresh tokens in plain text.
    /// If the database is compromised, hashed tokens cannot be used directly.
    /// When validating, we hash the incoming token and compare with this value.
    /// </remarks>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>
    /// When the refresh token expires.
    /// </summary>
    /// <remarks>
    /// Default: 7 days from creation (configurable).
    /// After expiration, users must re-authenticate.
    /// </remarks>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>
    /// When the refresh token was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When the refresh token was revoked (null if still active).
    /// </summary>
    /// <remarks>
    /// Revocation reasons: User logout, security breach, password change (future).
    /// </remarks>
    public DateTime? RevokedAt { get; private set; }

    /// <summary>
    /// Whether the refresh token has been revoked.
    /// </summary>
    /// <remarks>
    /// Indexed in database for fast lookup of active tokens.
    /// </remarks>
    public bool IsRevoked { get; private set; }

    /// <summary>
    /// Navigation property to the user.
    /// </summary>
    public User User { get; private set; } = null!;

    // Private parameterless constructor for EF Core
    private RefreshToken() { }

    /// <summary>
    /// Factory method to create a new refresh token.
    /// </summary>
    /// <param name="userId">The user this token belongs to</param>
    /// <param name="tokenHash">SHA-256 hash of the refresh token</param>
    /// <param name="validityDays">Number of days until expiration (default: 7)</param>
    /// <returns>A new RefreshToken entity</returns>
    /// <remarks>
    /// Business rule: Token hash is required (never store plain text tokens).
    /// Expiry is calculated as CreatedAt + validityDays.
    /// </remarks>
    public static RefreshToken Create(Guid userId, string tokenHash, int validityDays = 7)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException("Token hash cannot be empty", nameof(tokenHash));

        if (validityDays <= 0)
            throw new ArgumentException("Validity days must be positive", nameof(validityDays));

        var now = DateTime.UtcNow;

        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            CreatedAt = now,
            ExpiresAt = now.AddDays(validityDays),
            IsRevoked = false,
            RevokedAt = null
        };
    }

    /// <summary>
    /// Revokes the refresh token, making it invalid for future use.
    /// </summary>
    /// <remarks>
    /// Business logic: Once revoked, a token cannot be un-revoked (immutable).
    /// Called when user logs out or security breach detected.
    /// </remarks>
    public void Revoke()
    {
        // Business rule: Cannot revoke an already revoked token
        if (IsRevoked)
            throw new InvalidOperationException("Token is already revoked");

        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the refresh token is valid for use.
    /// </summary>
    /// <returns>True if the token is not revoked and not expired</returns>
    /// <remarks>
    /// Business logic: A token is valid only if:
    /// 1. It has not been revoked (IsRevoked == false)
    /// 2. It has not expired (ExpiresAt > UtcNow)
    /// </remarks>
    public bool IsValid()
    {
        if (IsRevoked)
            return false;

        if (ExpiresAt <= DateTime.UtcNow)
            return false;

        return true;
    }

    /// <summary>
    /// Checks if the refresh token has expired.
    /// </summary>
    /// <returns>True if the current time is past the expiration time</returns>
    public bool IsExpired() => ExpiresAt <= DateTime.UtcNow;
}
