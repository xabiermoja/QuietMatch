using DatingApp.NotificationService.Core.Domain.ValueObjects;

namespace DatingApp.NotificationService.Core.Ports;

/// <summary>
/// Port (interface) for sending email notifications.
/// </summary>
/// <remarks>
/// HEXAGONAL ARCHITECTURE: This interface is defined in the DOMAIN layer (Core/Ports),
/// NOT in Infrastructure!
///
/// Why?
/// - The domain defines what it needs (a way to send emails)
/// - Infrastructure provides implementations (ConsoleEmailProvider, SendGridEmailProvider, etc.)
/// - This inverts the dependency: Infrastructure depends on Domain, not the other way around
///
/// Multiple adapters can implement this port:
/// - ConsoleEmailProvider (development/testing)
/// - SendGridEmailProvider (production)
/// - SmtpEmailProvider (alternative)
/// - MockEmailProvider (unit tests)
///
/// Swap adapters via DI configuration without changing domain code!
/// </remarks>
public interface IEmailProvider
{
    /// <summary>
    /// Sends an email message asynchronously.
    /// </summary>
    /// <param name="message">Email message to send</param>
    /// <returns>True if sent successfully, false if failed</returns>
    /// <remarks>
    /// Implementations should:
    /// - Handle network failures gracefully
    /// - Log success/failure
    /// - Return false on failure (don't throw exceptions for transient errors)
    /// - Throw exceptions only for configuration errors
    /// </remarks>
    Task<bool> SendAsync(EmailMessage message);
}
