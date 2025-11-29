using DatingApp.NotificationService.Core.Domain.ValueObjects;
using DatingApp.NotificationService.Core.Ports;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace DatingApp.NotificationService.Infrastructure.Adapters.Email;

/// <summary>
/// SendGrid-based email provider for production email sending.
/// </summary>
/// <remarks>
/// HEXAGONAL ARCHITECTURE: This is a PRODUCTION ADAPTER in Infrastructure!
///
/// Implements:
/// - IEmailProvider port (defined in Core/Ports/)
///
/// Purpose:
/// - Production email sending via SendGrid API
/// - Reliable, scalable transactional email delivery
/// - Replaces ConsoleEmailProvider in production environments
///
/// Swappable Adapters (all implement IEmailProvider):
/// - ConsoleEmailProvider (development/testing)
/// - SendGridEmailProvider (production - this class)
/// - SmtpEmailProvider (alternative production)
/// - MockEmailProvider (unit tests)
///
/// Configuration Required:
/// - SendGrid API Key (via appsettings or environment variable)
/// - From email address and name
///
/// CRITICAL: This demonstrates Hexagonal Architecture's key benefit!
/// We can swap from Console → SendGrid by changing ONLY the DI configuration.
/// NotificationService.cs doesn't change at all!
/// </remarks>
public class SendGridEmailProvider : IEmailProvider
{
    private readonly ISendGridClient _sendGridClient;
    private readonly INotificationLogger<SendGridEmailProvider> _logger;
    private readonly string _defaultFromEmail;
    private readonly string _defaultFromName;

    /// <summary>
    /// Creates a new SendGridEmailProvider.
    /// </summary>
    /// <param name="sendGridClient">SendGrid client (injected)</param>
    /// <param name="logger">Logger port (injected)</param>
    /// <param name="defaultFromEmail">Default sender email address</param>
    /// <param name="defaultFromName">Default sender display name</param>
    public SendGridEmailProvider(
        ISendGridClient sendGridClient,
        INotificationLogger<SendGridEmailProvider> logger,
        string defaultFromEmail,
        string defaultFromName)
    {
        _sendGridClient = sendGridClient ?? throw new ArgumentNullException(nameof(sendGridClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(defaultFromEmail))
            throw new ArgumentException("Default from email is required", nameof(defaultFromEmail));

        if (string.IsNullOrWhiteSpace(defaultFromName))
            throw new ArgumentException("Default from name is required", nameof(defaultFromName));

        _defaultFromEmail = defaultFromEmail;
        _defaultFromName = defaultFromName;
    }

    public async Task<bool> SendAsync(EmailMessage message)
    {
        try
        {
            _logger.LogInformation(
                "📧 [SENDGRID] Sending email to {Email} with subject '{Subject}'",
                message.To.Email,
                message.Subject);

            // Build SendGrid message
            var sendGridMessage = BuildSendGridMessage(message);

            // Send via SendGrid API
            var response = await _sendGridClient.SendEmailAsync(sendGridMessage);

            // SendGrid returns 2xx for success
            var isSuccess = ((int)response.StatusCode >= 200 && (int)response.StatusCode < 300);

            if (isSuccess)
            {
                _logger.LogInformation(
                    "✅ [SENDGRID] Email sent successfully to {Email} (Status: {StatusCode})",
                    message.To.Email,
                    response.StatusCode);

                return true;
            }
            else
            {
                var responseBody = await response.Body.ReadAsStringAsync();

                _logger.LogWarning(
                    "⚠️ [SENDGRID] Email sending failed to {Email} (Status: {StatusCode}, Response: {Response})",
                    message.To.Email,
                    response.StatusCode,
                    responseBody);

                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌ [SENDGRID] Exception occurred while sending email to {Email}",
                message.To.Email);

            // Return false for transient errors (don't throw)
            // This follows IEmailProvider contract
            return false;
        }
    }

    /// <summary>
    /// Builds a SendGrid message from our domain EmailMessage.
    /// </summary>
    /// <remarks>
    /// This is where we translate our domain model to SendGrid's API model.
    /// This translation logic is part of the ADAPTER responsibility!
    /// </remarks>
    private SendGridMessage BuildSendGridMessage(EmailMessage message)
    {
        // Determine sender (use message.From if provided, otherwise default)
        var fromEmail = message.From?.Email ?? _defaultFromEmail;
        var fromName = message.From?.Name ?? _defaultFromName;

        var sendGridMessage = new SendGridMessage
        {
            From = new EmailAddress(fromEmail, fromName),
            Subject = message.Subject,
            HtmlContent = message.Body
        };

        // Primary recipient
        sendGridMessage.AddTo(new EmailAddress(message.To.Email, message.To.Name));

        // CC recipients (if any)
        if (message.Cc != null && message.Cc.Count > 0)
        {
            foreach (var cc in message.Cc)
            {
                sendGridMessage.AddCc(new EmailAddress(cc.Email, cc.Name));
            }
        }

        // BCC recipients (if any)
        if (message.Bcc != null && message.Bcc.Count > 0)
        {
            foreach (var bcc in message.Bcc)
            {
                sendGridMessage.AddBcc(new EmailAddress(bcc.Email, bcc.Name));
            }
        }

        return sendGridMessage;
    }
}
