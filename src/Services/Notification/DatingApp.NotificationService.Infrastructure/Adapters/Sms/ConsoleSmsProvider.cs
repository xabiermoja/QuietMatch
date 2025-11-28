using DatingApp.NotificationService.Core.Domain.ValueObjects;
using DatingApp.NotificationService.Core.Ports;

namespace DatingApp.NotificationService.Infrastructure.Adapters.Sms;

/// <summary>
/// Console-based SMS provider for development and testing.
/// </summary>
/// <remarks>
/// HEXAGONAL ARCHITECTURE: This is an ADAPTER in Infrastructure!
///
/// Implements:
/// - ISmsProvider port (defined in Core/Ports/)
///
/// Purpose:
/// - Development/testing without real SMS service
/// - Outputs SMS to console instead of sending
/// - No external dependencies (Twilio, AWS SNS)
///
/// Later, we'll create:
/// - TwilioSmsProvider (production adapter)
/// - AwsSnsProvider (alternative production adapter)
/// - MockSmsProvider (unit test adapter)
///
/// All implement the same ISmsProvider port - can be swapped via DI!
/// </remarks>
public class ConsoleSmsProvider : ISmsProvider
{
    private readonly INotificationLogger<ConsoleSmsProvider> _logger;

    public ConsoleSmsProvider(INotificationLogger<ConsoleSmsProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<bool> SendAsync(SmsMessage message)
    {
        try
        {
            _logger.LogInformation(
                "📱 [CONSOLE SMS] Sending SMS to {PhoneNumber}",
                message.PhoneNumber);

            // Print SMS to console with nice formatting
            Console.WriteLine();
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                      📱 SMS MESSAGE                            ║");
            Console.WriteLine("╠════════════════════════════════════════════════════════════════╣");
            Console.WriteLine($"║ To:      {message.PhoneNumber}");
            Console.WriteLine($"║ Length:  {message.Message.Length} characters");
            Console.WriteLine("╠════════════════════════════════════════════════════════════════╣");
            Console.WriteLine("║ MESSAGE:");
            Console.WriteLine("╠════════════════════════════════════════════════════════════════╣");

            // Print message (split into lines if needed)
            var lines = message.Message.Split('\n');
            foreach (var line in lines)
            {
                // Wrap long lines
                for (int i = 0; i < line.Length; i += 60)
                {
                    var segment = line.Substring(i, Math.Min(60, line.Length - i));
                    Console.WriteLine($"║ {segment}");
                }
            }

            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            _logger.LogInformation(
                "✅ [CONSOLE SMS] SMS sent successfully to {PhoneNumber}",
                message.PhoneNumber);

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌ [CONSOLE SMS] Failed to send SMS to {PhoneNumber}",
                message.PhoneNumber);

            return Task.FromResult(false);
        }
    }
}
