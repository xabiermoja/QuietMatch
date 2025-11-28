using DatingApp.NotificationService.Core.Ports;
using DatingApp.NotificationService.Infrastructure.Events;
using MassTransit;
using ApplicationServices = DatingApp.NotificationService.Core.Application.Services;

namespace DatingApp.NotificationService.Infrastructure.Consumers;

/// <summary>
/// Consumer that handles UserRegistered events from IdentityService.
/// </summary>
/// <remarks>
/// EVENT-DRIVEN ARCHITECTURE:
/// 1. IdentityService publishes UserRegistered event to RabbitMQ
/// 2. RabbitMQ routes event to NotificationService queue
/// 3. MassTransit delivers event to this consumer
/// 4. Consumer calls NotificationService to send welcome email
///
/// HEXAGONAL ARCHITECTURE:
/// This consumer is an ADAPTER (Infrastructure layer).
/// It translates integration events into domain service calls.
///
/// Error Handling:
/// - If email sending fails, MassTransit will retry (configured in Program.cs)
/// - After max retries, message goes to error queue
/// - We log errors but don't throw exceptions (graceful degradation)
/// </remarks>
public class UserRegisteredConsumer : IConsumer<UserRegistered>
{
    private readonly ApplicationServices.NotificationService _notificationService;
    private readonly INotificationLogger<UserRegisteredConsumer> _logger;

    public UserRegisteredConsumer(
        ApplicationServices.NotificationService notificationService,
        INotificationLogger<UserRegisteredConsumer> logger)
    {
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Consume(ConsumeContext<UserRegistered> context)
    {
        var @event = context.Message;

        _logger.LogInformation(
            "🎉 Received UserRegistered event: UserId={UserId}, Email={Email}, Provider={Provider}, CorrelationId={CorrelationId}",
            @event.UserId,
            @event.Email,
            @event.Provider,
            @event.CorrelationId);

        try
        {
            // Extract name from email (before @) as fallback
            // In real scenario, we'd get name from the event if IdentityService provides it
            var name = @event.Email.Split('@')[0];

            // Send welcome email via domain service
            var success = await _notificationService.SendWelcomeEmailAsync(
                @event.UserId,
                @event.Email,
                name);

            if (success)
            {
                _logger.LogInformation(
                    "✅ Welcome email sent successfully for UserId={UserId}, CorrelationId={CorrelationId}",
                    @event.UserId,
                    @event.CorrelationId);
            }
            else
            {
                _logger.LogWarning(
                    "⚠️ Failed to send welcome email for UserId={UserId}, CorrelationId={CorrelationId}",
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
                "❌ Error processing UserRegistered event for UserId={UserId}, CorrelationId={CorrelationId}",
                @event.UserId,
                @event.CorrelationId);

            // Rethrow to trigger MassTransit retry logic
            throw;
        }
    }
}
