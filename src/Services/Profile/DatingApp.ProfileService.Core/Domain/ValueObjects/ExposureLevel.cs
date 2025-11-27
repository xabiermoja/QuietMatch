namespace DatingApp.ProfileService.Core.Domain.ValueObjects;

/// <summary>
/// Enum representing privacy levels for profile data sharing.
/// </summary>
/// <remarks>
/// Controls what data is shared with other members based on match status.
/// This implements GDPR's Privacy by Design principle.
///
/// - MatchedOnly: Highest privacy - full profile only visible to accepted matches
/// - AllMatches: Moderate privacy - full profile visible to all presented match candidates
/// - Public: Lowest privacy - profile visible in search (future feature)
///
/// Default exposure level is MatchedOnly to maximize privacy.
/// </remarks>
public enum ExposureLevel
{
    MatchedOnly = 1,    // Only matched members can see full profile
    AllMatches = 2,     // All match candidates can see full profile
    Public = 3          // Profile visible in search (future feature)
}
