using DatingApp.NotificationService.Core.Ports;
using Microsoft.Extensions.Logging;

namespace DatingApp.NotificationService.Infrastructure.Adapters.Logging;

/// <summary>
/// Adapter that wraps Microsoft.Extensions.Logging.ILogger to implement our domain's INotificationLogger port.
/// </summary>
/// <remarks>
/// HEXAGONAL ARCHITECTURE: This is an ADAPTER in Infrastructure!
///
/// Flow:
/// 1. Domain defines INotificationLogger port (what it needs)
/// 2. This adapter implements INotificationLogger (satisfies domain's needs)
/// 3. Internally wraps Microsoft's ILogger (external framework)
/// 4. DI injects this adapter when domain requests INotificationLogger
///
/// Benefits:
/// - Core project has ZERO dependencies on Microsoft.Extensions.Logging
/// - Can swap to different logging frameworks (Serilog, NLog) by creating new adapter
/// - Domain code doesn't change when switching logging frameworks
/// </remarks>
public class MicrosoftLoggerAdapter<T> : INotificationLogger<T>
{
    private readonly ILogger<T> _logger;

    public MicrosoftLoggerAdapter(ILogger<T> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void LogInformation(string message, params object[] args)
    {
        _logger.LogInformation(message, args);
    }

    public void LogWarning(string message, params object[] args)
    {
        _logger.LogWarning(message, args);
    }

    public void LogError(Exception exception, string message, params object[] args)
    {
        _logger.LogError(exception, message, args);
    }
}
