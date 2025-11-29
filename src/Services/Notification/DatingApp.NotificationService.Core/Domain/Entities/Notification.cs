using DatingApp.NotificationService.Core.Domain.Enums;

namespace DatingApp.NotificationService.Core.Domain.Entities;

/// <summary>
/// Aggregate root representing a notification sent to a user.
/// </summary>
/// <remarks>
/// Tracks notification delivery status and provides domain methods for state transitions.
/// This is a simple aggregate - most logic is in the notification providers (adapters).
/// </remarks>
public class Notification
{
    /// <summary>
    /// Unique identifier for this notification
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// User who receives this notification
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Delivery channel (Email, SMS, Push, InApp)
    /// </summary>
    public NotificationChannel Channel { get; private set; }

    /// <summary>
    /// Notification subject/title
    /// </summary>
    public string Subject { get; private set; } = string.Empty;

    /// <summary>
    /// Notification body/content
    /// </summary>
    public string Body { get; private set; } = string.Empty;

    /// <summary>
    /// Current delivery status
    /// </summary>
    public NotificationStatus Status { get; private set; }

    /// <summary>
    /// When notification was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When notification was successfully sent (null if not sent yet)
    /// </summary>
    public DateTime? SentAt { get; private set; }

    /// <summary>
    /// Error message if delivery failed (null if successful)
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Number of delivery attempts
    /// </summary>
    public int AttemptCount { get; private set; }

    // Private constructor for EF Core
    private Notification() { }

    /// <summary>
    /// Factory method to create a new notification.
    /// </summary>
    /// <param name="userId">User to notify</param>
    /// <param name="channel">Delivery channel</param>
    /// <param name="subject">Notification subject</param>
    /// <param name="body">Notification content</param>
    /// <returns>New notification in Pending status</returns>
    public static Notification Create(
        Guid userId,
        NotificationChannel channel,
        string subject,
        string body)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty", nameof(userId));

        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject is required", nameof(subject));

        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Body is required", nameof(body));

        return new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Channel = channel,
            Subject = subject,
            Body = body,
            Status = NotificationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            AttemptCount = 0
        };
    }

    /// <summary>
    /// Marks notification as successfully sent.
    /// </summary>
    /// <remarks>
    /// Domain method enforcing valid state transitions.
    /// Can only mark as sent from Pending or Retrying status.
    /// </remarks>
    public void MarkAsSent()
    {
        if (Status == NotificationStatus.Sent)
            throw new InvalidOperationException("Notification is already marked as sent");

        Status = NotificationStatus.Sent;
        SentAt = DateTime.UtcNow;
        ErrorMessage = null; // Clear any previous error
    }

    /// <summary>
    /// Marks notification as failed with error message.
    /// </summary>
    /// <param name="errorMessage">Reason for failure</param>
    /// <remarks>
    /// Increments attempt count. Can be retried later if CanRetry() returns true.
    /// </remarks>
    public void MarkAsFailed(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            throw new ArgumentException("Error message is required", nameof(errorMessage));

        Status = NotificationStatus.Failed;
        ErrorMessage = errorMessage;
        AttemptCount++;
    }

    /// <summary>
    /// Marks notification for retry after a failure.
    /// </summary>
    /// <remarks>
    /// Can only retry from Failed status and if retry limit not exceeded.
    /// </remarks>
    public void MarkForRetry()
    {
        if (Status != NotificationStatus.Failed)
            throw new InvalidOperationException("Can only retry failed notifications");

        if (!CanRetry())
            throw new InvalidOperationException("Retry limit exceeded (max 3 attempts)");

        Status = NotificationStatus.Retrying;
    }

    /// <summary>
    /// Determines if notification can be retried.
    /// </summary>
    /// <returns>True if failed and retry limit not exceeded</returns>
    public bool CanRetry()
    {
        const int MaxRetryAttempts = 3;
        return Status == NotificationStatus.Failed && AttemptCount < MaxRetryAttempts;
    }

    /// <summary>
    /// Checks if notification is in a terminal state (cannot change anymore).
    /// </summary>
    public bool IsTerminal() => Status == NotificationStatus.Sent ||
                                (Status == NotificationStatus.Failed && !CanRetry());
}
