namespace DatingApp.NotificationService.Core.Domain.Enums;

/// <summary>
/// Represents the delivery status of a notification.
/// </summary>
public enum NotificationStatus
{
    /// <summary>
    /// Notification created but not yet sent
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Notification successfully sent
    /// </summary>
    Sent = 2,

    /// <summary>
    /// Notification delivery failed
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Notification is being retried after a failure
    /// </summary>
    Retrying = 4
}
