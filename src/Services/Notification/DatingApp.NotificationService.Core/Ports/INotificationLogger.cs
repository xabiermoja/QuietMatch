namespace DatingApp.NotificationService.Core.Ports;

/// <summary>
/// Port (interface) for logging within the notification domain.
/// </summary>
/// <remarks>
/// HEXAGONAL ARCHITECTURE: Even logging is a port!
///
/// Why?
/// - Core should not depend on Microsoft.Extensions.Logging
/// - Domain defines what logging it needs (interface)
/// - Infrastructure provides the adapter (wraps ILogger)
///
/// This maintains ZERO dependencies in Core while still allowing logging.
/// </remarks>
public interface INotificationLogger<T>
{
    /// <summary>
    /// Logs an informational message.
    /// </summary>
    void LogInformation(string message, params object[] args);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    void LogWarning(string message, params object[] args);

    /// <summary>
    /// Logs an error with exception details.
    /// </summary>
    void LogError(Exception exception, string message, params object[] args);
}
