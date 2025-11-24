namespace DatingApp.IdentityService.Domain.Enums;

/// <summary>
/// Supported OAuth authentication providers for QuietMatch.
/// </summary>
/// <remarks>
/// This enum represents the social login providers we support.
/// Values are stored as strings in the database for readability and future extensibility.
/// </remarks>
public enum AuthProvider
{
    /// <summary>
    /// Google OAuth 2.0 Sign-In
    /// </summary>
    Google,

    /// <summary>
    /// Apple Sign-In (future implementation)
    /// </summary>
    Apple
}
