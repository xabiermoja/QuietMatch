using DatingApp.NotificationService.Core.Ports;
using DatingApp.NotificationService.Infrastructure.Events;
using MassTransit;
using ApplicationServices = DatingApp.NotificationService.Core.Application.Services;

namespace DatingApp.NotificationService.Infrastructure.Consumers;

/// <summary>
/// Consumer that handles ProfileCompleted events from ProfileService.
/// </summary>
/// <remarks>
/// EVENT-DRIVEN ARCHITECTURE:
/// 1. ProfileService publishes ProfileCompleted event to RabbitMQ
/// 2. RabbitMQ routes event to NotificationService queue
/// 3. MassTransit delivers event to this consumer
/// 4. Consumer calls NotificationService to send profile completed email
///
/// HEXAGONAL ARCHITECTURE:
/// This consumer is an ADAPTER (Infrastructure layer).
/// It translates integration events into domain service calls.
///
/// Business Logic:
/// - Profile completed email is sent ONLY the first time profile reaches 100%
/// - ProfileService ensures this event is published only once per user
/// - Email congratulates user and explains matching process has started
/// </remarks>
public class ProfileCompletedConsumer : IConsumer<ProfileCompleted>
{
    private readonly ApplicationServices.NotificationService _notificationService;
    private readonly INotificationLogger<ProfileCompletedConsumer> _logger;

    public ProfileCompletedConsumer(
        ApplicationServices.NotificationService notificationService,
        INotificationLogger<ProfileCompletedConsumer> logger)
    {
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Consume(ConsumeContext<ProfileCompleted> context)
    {
        var @event = context.Message;

        _logger.LogInformation(
            "🎊 Received ProfileCompleted event: UserId={UserId}, Email={Email}, Name={Name}, CorrelationId={CorrelationId}",
            @event.UserId,
            @event.Email,
            @event.Name,
            @event.CorrelationId);

        try
        {
            // Send profile completed email via domain service
            var success = await _notificationService.SendProfileCompletedEmailAsync(
                @event.UserId,
                @event.Email,
                @event.Name);

            if (success)
            {
                _logger.LogInformation(
                    "✅ Profile completed email sent successfully for UserId={UserId}, CorrelationId={CorrelationId}",
                    @event.UserId,
                    @event.CorrelationId);
            }
            else
            {
                _logger.LogWarning(
                    "⚠️ Failed to send profile completed email for UserId={UserId}, CorrelationId={CorrelationId}",
                    @event.UserId,
                    @event.CorrelationId);

                // Don't throw - we don't want to retry endlessly for email failures
                // Email provider already logged the specific error
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌ Error processing ProfileCompleted event for UserId={UserId}, CorrelationId={CorrelationId}",
                @event.UserId,
                @event.CorrelationId);

            // Rethrow to trigger MassTransit retry logic
            throw;
        }
    }
}
