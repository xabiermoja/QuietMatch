namespace DatingApp.NotificationService.Core.Domain.Enums;

/// <summary>
/// Represents the channel through which a notification is delivered.
/// </summary>
public enum NotificationChannel
{
    /// <summary>
    /// Email notification
    /// </summary>
    Email = 1,

    /// <summary>
    /// SMS text message notification
    /// </summary>
    Sms = 2,

    /// <summary>
    /// Push notification (mobile/web)
    /// </summary>
    Push = 3,

    /// <summary>
    /// In-app notification (via SignalR)
    /// </summary>
    InApp = 4
}
