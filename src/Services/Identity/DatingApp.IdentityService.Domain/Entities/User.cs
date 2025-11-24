using DatingApp.IdentityService.Domain.Enums;

namespace DatingApp.IdentityService.Domain.Entities;

/// <summary>
/// Represents a user in the QuietMatch platform.
/// </summary>
/// <remarks>
/// This is a rich domain entity that encapsulates user creation and authentication logic.
/// Users are created via OAuth providers (Google, Apple) - we never store passwords.
/// </remarks>
public class User
{
    /// <summary>
    /// Unique identifier for the user.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// User's email address from the OAuth provider.
    /// </summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>
    /// OAuth provider used for authentication (Google, Apple).
    /// </summary>
    public AuthProvider Provider { get; private set; }

    /// <summary>
    /// Unique identifier from the OAuth provider (e.g., Google 'sub' claim).
    /// </summary>
    /// <remarks>
    /// This is the provider's unique user ID, not their email.
    /// For Google: This is the 'sub' claim from the ID token.
    /// </remarks>
    public string ExternalUserId { get; private set; } = string.Empty;

    /// <summary>
    /// Timestamp when the user first registered.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Timestamp of the user's last successful login.
    /// </summary>
    /// <remarks>
    /// Null if the user has never logged in (registration only).
    /// Updated on every successful authentication.
    /// </remarks>
    public DateTime? LastLoginAt { get; private set; }

    /// <summary>
    /// Navigation property to the user's refresh tokens.
    /// </summary>
    public ICollection<RefreshToken> RefreshTokens { get; private set; } = new List<RefreshToken>();

    // Private parameterless constructor for EF Core
    private User() { }

    /// <summary>
    /// Factory method to create a new user from Google authentication.
    /// </summary>
    /// <param name="email">User's email from Google</param>
    /// <param name="googleUserId">Google's unique user ID (sub claim)</param>
    /// <returns>A new User entity with Provider set to Google</returns>
    /// <remarks>
    /// This is the primary way to create a User - encapsulates business rules.
    /// Sets Provider to Google and initializes CreatedAt to current UTC time.
    /// </remarks>
    public static User CreateFromGoogle(string email, string googleUserId)
    {
        // Business rule: Email and external user ID are required
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        if (string.IsNullOrWhiteSpace(googleUserId))
            throw new ArgumentException("Google user ID cannot be empty", nameof(googleUserId));

        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Provider = AuthProvider.Google,
            ExternalUserId = googleUserId,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = null // First-time user, no login yet
        };
    }

    /// <summary>
    /// Factory method to create a new user from Apple authentication.
    /// </summary>
    /// <param name="email">User's email from Apple</param>
    /// <param name="appleUserId">Apple's unique user ID</param>
    /// <returns>A new User entity with Provider set to Apple</returns>
    /// <remarks>
    /// Placeholder for F0002 (Apple Sign-In feature).
    /// Same pattern as CreateFromGoogle for consistency.
    /// </remarks>
    public static User CreateFromApple(string email, string appleUserId)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        if (string.IsNullOrWhiteSpace(appleUserId))
            throw new ArgumentException("Apple user ID cannot be empty", nameof(appleUserId));

        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Provider = AuthProvider.Apple,
            ExternalUserId = appleUserId,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = null
        };
    }

    /// <summary>
    /// Records a successful login by updating the LastLoginAt timestamp.
    /// </summary>
    /// <remarks>
    /// Business logic: Track user activity for analytics and security.
    /// Called by AuthService after successful token validation.
    /// </remarks>
    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if this is a newly registered user (has never logged in).
    /// </summary>
    /// <returns>True if the user has never logged in (LastLoginAt is null)</returns>
    /// <remarks>
    /// Used by AuthService to set isNewUser flag in login response.
    /// New users need to be redirected to profile creation.
    /// </remarks>
    public bool IsNewUser() => LastLoginAt is null;
}
