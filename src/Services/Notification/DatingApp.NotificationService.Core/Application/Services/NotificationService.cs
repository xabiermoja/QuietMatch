using DatingApp.NotificationService.Core.Domain.Enums;
using DatingApp.NotificationService.Core.Domain.ValueObjects;
using DatingApp.NotificationService.Core.Ports;
using Microsoft.Extensions.Logging;

namespace DatingApp.NotificationService.Core.Application.Services;

/// <summary>
/// Application service for sending notifications across multiple channels.
/// </summary>
/// <remarks>
/// HEXAGONAL ARCHITECTURE: This service depends ONLY on ports (interfaces), NOT on adapters!
///
/// Dependencies:
/// - IEmailProvider (port) - NOT SendGridEmailProvider (adapter)
/// - ISmsProvider (port) - NOT TwilioSmsProvider (adapter)
/// - ITemplateProvider (port) - NOT FileTemplateProvider (adapter)
///
/// Adapters are injected via DI and can be swapped without changing this code.
///
/// Orchestrates notification use cases:
/// - Send welcome email on user registration
/// - Send profile completed email
/// - Send match notifications (future)
/// </remarks>
public class NotificationService
{
    private readonly IEmailProvider _emailProvider;
    private readonly ISmsProvider _smsProvider;
    private readonly ITemplateProvider _templateProvider;
    private readonly ILogger<NotificationService> _logger;

    /// <summary>
    /// Creates a new NotificationService.
    /// </summary>
    /// <param name="emailProvider">Email provider port (adapter injected via DI)</param>
    /// <param name="smsProvider">SMS provider port (adapter injected via DI)</param>
    /// <param name="templateProvider">Template provider port (adapter injected via DI)</param>
    /// <param name="logger">Logger for diagnostics</param>
    public NotificationService(
        IEmailProvider emailProvider,
        ISmsProvider smsProvider,
        ITemplateProvider templateProvider,
        ILogger<NotificationService> logger)
    {
        _emailProvider = emailProvider ?? throw new ArgumentNullException(nameof(emailProvider));
        _smsProvider = smsProvider ?? throw new ArgumentNullException(nameof(smsProvider));
        _templateProvider = templateProvider ?? throw new ArgumentNullException(nameof(templateProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Sends a welcome email to a newly registered user.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="email">User's email address</param>
    /// <param name="name">User's name (optional, defaults to email)</param>
    /// <returns>True if email sent successfully</returns>
    /// <remarks>
    /// Triggered by UserRegistered event from IdentityService.
    ///
    /// Flow:
    /// 1. Render welcome email template
    /// 2. Create EmailMessage with rendered content
    /// 3. Send via IEmailProvider port
    /// 4. Log result
    /// </remarks>
    public async Task<bool> SendWelcomeEmailAsync(Guid userId, string email, string? name = null)
    {
        _logger.LogInformation(
            "Sending welcome email to {Email} (UserId: {UserId})",
            email,
            userId);

        try
        {
            // Use name or fallback to email
            var displayName = string.IsNullOrWhiteSpace(name) ? email : name;

            // Render template with user data
            var templateData = new
            {
                Name = displayName,
                ProfileUrl = $"https://app.quietmatch.com/profile/complete"
            };

            var htmlBody = await _templateProvider.RenderAsync("WelcomeEmail", templateData);

            // Create email message
            var message = new EmailMessage(
                to: new Recipient(email, displayName),
                subject: "Welcome to QuietMatch! 🎉",
                body: htmlBody
            );

            // Send via email provider port
            var success = await _emailProvider.SendAsync(message);

            if (success)
            {
                _logger.LogInformation(
                    "Welcome email sent successfully to {Email}",
                    email);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to send welcome email to {Email}",
                    email);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error sending welcome email to {Email}",
                email);
            return false;
        }
    }

    /// <summary>
    /// Sends a profile completed email to a user who finished their profile.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="email">User's email address</param>
    /// <param name="name">User's name</param>
    /// <returns>True if email sent successfully</returns>
    /// <remarks>
    /// Triggered by ProfileCompleted event from ProfileService.
    /// Confirms matching process has started.
    /// </remarks>
    public async Task<bool> SendProfileCompletedEmailAsync(Guid userId, string email, string name)
    {
        _logger.LogInformation(
            "Sending profile completed email to {Email} (UserId: {UserId})",
            email,
            userId);

        try
        {
            // Render template
            var templateData = new
            {
                Name = name
            };

            var htmlBody = await _templateProvider.RenderAsync("ProfileCompletedEmail", templateData);

            // Create email message
            var message = new EmailMessage(
                to: new Recipient(email, name),
                subject: "Profile Complete - We're Finding Your Matches! ✓",
                body: htmlBody
            );

            // Send via email provider port
            var success = await _emailProvider.SendAsync(message);

            if (success)
            {
                _logger.LogInformation(
                    "Profile completed email sent successfully to {Email}",
                    email);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to send profile completed email to {Email}",
                    email);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error sending profile completed email to {Email}",
                email);
            return false;
        }
    }

    /// <summary>
    /// Sends a match notification email when a new match is found.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="email">User's email address</param>
    /// <param name="name">User's name</param>
    /// <param name="matchCount">Number of new matches</param>
    /// <returns>True if email sent successfully</returns>
    /// <remarks>
    /// Future: Triggered by MatchAccepted event from MatchingService.
    /// </remarks>
    public async Task<bool> SendMatchNotificationEmailAsync(
        Guid userId,
        string email,
        string name,
        int matchCount)
    {
        _logger.LogInformation(
            "Sending match notification email to {Email} (UserId: {UserId}, Matches: {MatchCount})",
            email,
            userId,
            matchCount);

        try
        {
            var subject = matchCount == 1
                ? "You Have a New Match! 💕"
                : $"You Have {matchCount} New Matches! 💕";

            // Simple HTML body (template can be added later)
            var htmlBody = $@"
                <html>
                <body>
                    <h1>Hi {name}!</h1>
                    <p>Great news! We've found {matchCount} compatible {(matchCount == 1 ? "match" : "matches")} for you.</p>
                    <p><a href='https://app.quietmatch.com/matches'>View Your Matches</a></p>
                </body>
                </html>";

            var message = new EmailMessage(
                to: new Recipient(email, name),
                subject: subject,
                body: htmlBody
            );

            var success = await _emailProvider.SendAsync(message);

            if (success)
            {
                _logger.LogInformation(
                    "Match notification email sent successfully to {Email}",
                    email);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error sending match notification email to {Email}",
                email);
            return false;
        }
    }

    /// <summary>
    /// Sends an SMS notification.
    /// </summary>
    /// <param name="phoneNumber">Recipient phone number (E.164 format)</param>
    /// <param name="message">SMS message text (max 160 chars)</param>
    /// <returns>True if SMS sent successfully</returns>
    /// <remarks>
    /// Generic SMS method. Can be used for:
    /// - Phone verification codes
    /// - Urgent notifications
    /// - Date reminders
    /// </remarks>
    public async Task<bool> SendSmsAsync(string phoneNumber, string message)
    {
        _logger.LogInformation(
            "Sending SMS to {PhoneNumber}",
            phoneNumber);

        try
        {
            var smsMessage = new SmsMessage(phoneNumber, message);
            var success = await _smsProvider.SendAsync(smsMessage);

            if (success)
            {
                _logger.LogInformation(
                    "SMS sent successfully to {PhoneNumber}",
                    phoneNumber);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to send SMS to {PhoneNumber}",
                    phoneNumber);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error sending SMS to {PhoneNumber}",
                phoneNumber);
            return false;
        }
    }
}
