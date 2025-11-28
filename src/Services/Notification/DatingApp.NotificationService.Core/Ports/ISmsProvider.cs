using DatingApp.NotificationService.Core.Domain.ValueObjects;

namespace DatingApp.NotificationService.Core.Ports;

/// <summary>
/// Port (interface) for sending SMS notifications.
/// </summary>
/// <remarks>
/// HEXAGONAL ARCHITECTURE: Port defined in Domain, implemented in Infrastructure.
///
/// Multiple adapters can implement this port:
/// - ConsoleSmsProvider (development/testing)
/// - TwilioSmsProvider (production)
/// - AwsSnsProvider (alternative)
/// - MockSmsProvider (unit tests)
/// </remarks>
public interface ISmsProvider
{
    /// <summary>
    /// Sends an SMS message asynchronously.
    /// </summary>
    /// <param name="message">SMS message to send</param>
    /// <returns>True if sent successfully, false if failed</returns>
    /// <remarks>
    /// Implementations should:
    /// - Validate phone number format (E.164)
    /// - Handle API failures gracefully
    /// - Log delivery status
    /// - Return false on failure (don't throw for transient errors)
    /// </remarks>
    Task<bool> SendAsync(SmsMessage message);
}
