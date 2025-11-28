using System.Text.RegularExpressions;

namespace DatingApp.NotificationService.Core.Domain.ValueObjects;

/// <summary>
/// Value object representing an SMS message to be sent.
/// </summary>
/// <remarks>
/// Immutable value object for SMS notifications.
/// Phone number must be in E.164 format (+12025551234).
/// Message limited to 160 characters (standard SMS length).
/// </remarks>
public record SmsMessage
{
    private static readonly Regex PhoneRegex = new(
        @"^\+?[1-9]\d{1,14}$",
        RegexOptions.Compiled);

    /// <summary>
    /// Recipient phone number (E.164 format: +12025551234)
    /// </summary>
    public string PhoneNumber { get; }

    /// <summary>
    /// SMS message text (max 160 characters)
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Creates a new SmsMessage with validation.
    /// </summary>
    /// <param name="phoneNumber">Phone number in E.164 format</param>
    /// <param name="message">Message text (max 160 chars for single SMS)</param>
    public SmsMessage(string phoneNumber, string message)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Phone number is required", nameof(phoneNumber));

        // Normalize phone number (remove spaces, dashes)
        var normalized = phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

        if (!PhoneRegex.IsMatch(normalized))
            throw new ArgumentException(
                $"Invalid phone number format: {phoneNumber}. Expected E.164 format (e.g., +12025551234)",
                nameof(phoneNumber));

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message is required", nameof(message));

        if (message.Length > 160)
            throw new ArgumentException(
                $"Message exceeds 160 characters ({message.Length} chars). Consider splitting into multiple SMS.",
                nameof(message));

        PhoneNumber = normalized;
        Message = message;
    }

    public override string ToString() => $"SMS to {PhoneNumber}: {Message}";
}
