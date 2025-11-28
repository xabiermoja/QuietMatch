namespace DatingApp.NotificationService.Core.Domain.ValueObjects;

/// <summary>
/// Value object representing an email message to be sent.
/// </summary>
/// <remarks>
/// Immutable value object containing all data needed to send an email.
/// Used as input to IEmailProvider.SendAsync().
/// </remarks>
public record EmailMessage
{
    /// <summary>
    /// Primary recipient
    /// </summary>
    public Recipient To { get; }

    /// <summary>
    /// Email subject line
    /// </summary>
    public string Subject { get; }

    /// <summary>
    /// Email body (HTML or plain text)
    /// </summary>
    public string Body { get; }

    /// <summary>
    /// Optional sender (defaults to configured from address)
    /// </summary>
    public Recipient? From { get; }

    /// <summary>
    /// Optional CC recipients
    /// </summary>
    public IReadOnlyList<Recipient>? Cc { get; }

    /// <summary>
    /// Optional BCC recipients
    /// </summary>
    public IReadOnlyList<Recipient>? Bcc { get; }

    /// <summary>
    /// Creates a new EmailMessage with validation.
    /// </summary>
    /// <param name="to">Primary recipient (required)</param>
    /// <param name="subject">Subject line (required, max 255 chars)</param>
    /// <param name="body">Email body (required)</param>
    /// <param name="from">Optional sender</param>
    /// <param name="cc">Optional CC recipients</param>
    /// <param name="bcc">Optional BCC recipients</param>
    public EmailMessage(
        Recipient to,
        string subject,
        string body,
        Recipient? from = null,
        IReadOnlyList<Recipient>? cc = null,
        IReadOnlyList<Recipient>? bcc = null)
    {
        if (to == null)
            throw new ArgumentNullException(nameof(to));

        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject is required", nameof(subject));

        if (subject.Length > 255)
            throw new ArgumentException("Subject must be 255 characters or less", nameof(subject));

        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Body is required", nameof(body));

        To = to;
        Subject = subject.Trim();
        Body = body;
        From = from;
        Cc = cc;
        Bcc = bcc;
    }

    /// <summary>
    /// Creates a simple email message with just To, Subject, and Body.
    /// </summary>
    public static EmailMessage Create(string toEmail, string toName, string subject, string body)
        => new(new Recipient(toEmail, toName), subject, body);
}
