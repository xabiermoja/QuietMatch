using DatingApp.NotificationService.Core.Domain.ValueObjects;
using DatingApp.NotificationService.Core.Ports;

namespace DatingApp.NotificationService.Infrastructure.Adapters.Email;

/// <summary>
/// Console-based email provider for development and testing.
/// </summary>
/// <remarks>
/// HEXAGONAL ARCHITECTURE: This is an ADAPTER in Infrastructure!
///
/// Implements:
/// - IEmailProvider port (defined in Core/Ports/)
///
/// Purpose:
/// - Development/testing without real email service
/// - Outputs email to console instead of sending
/// - No external dependencies (SendGrid, SMTP)
///
/// Later, we'll create:
/// - SendGridEmailProvider (production adapter)
/// - SmtpEmailProvider (alternative production adapter)
/// - MockEmailProvider (unit test adapter)
///
/// All implement the same IEmailProvider port - can be swapped via DI!
/// </remarks>
public class ConsoleEmailProvider : IEmailProvider
{
    private readonly INotificationLogger<ConsoleEmailProvider> _logger;

    public ConsoleEmailProvider(INotificationLogger<ConsoleEmailProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<bool> SendAsync(EmailMessage message)
    {
        try
        {
            _logger.LogInformation(
                "📧 [CONSOLE EMAIL] Sending email to {Email}",
                message.To.Email);

            // Print email to console with nice formatting
            Console.WriteLine();
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                      📧 EMAIL MESSAGE                          ║");
            Console.WriteLine("╠════════════════════════════════════════════════════════════════╣");
            Console.WriteLine($"║ To:      {message.To.Name} <{message.To.Email}>");

            if (message.From != null)
            {
                Console.WriteLine($"║ From:    {message.From.Name} <{message.From.Email}>");
            }

            Console.WriteLine($"║ Subject: {message.Subject}");

            if (message.Cc != null && message.Cc.Count > 0)
            {
                Console.WriteLine($"║ Cc:      {string.Join(", ", message.Cc.Select(r => r.Email))}");
            }

            if (message.Bcc != null && message.Bcc.Count > 0)
            {
                Console.WriteLine($"║ Bcc:     {string.Join(", ", message.Bcc.Select(r => r.Email))}");
            }

            Console.WriteLine("╠════════════════════════════════════════════════════════════════╣");
            Console.WriteLine("║ BODY:");
            Console.WriteLine("╠════════════════════════════════════════════════════════════════╣");

            // Print body (truncate if too long)
            var bodyLines = message.Body.Split('\n');
            foreach (var line in bodyLines.Take(20))
            {
                var truncated = line.Length > 60 ? line.Substring(0, 57) + "..." : line;
                Console.WriteLine($"║ {truncated}");
            }

            if (bodyLines.Length > 20)
            {
                Console.WriteLine($"║ ... ({bodyLines.Length - 20} more lines) ...");
            }

            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            _logger.LogInformation(
                "✅ [CONSOLE EMAIL] Email sent successfully to {Email}",
                message.To.Email);

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌ [CONSOLE EMAIL] Failed to send email to {Email}",
                message.To.Email);

            return Task.FromResult(false);
        }
    }
}
