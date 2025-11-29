using DatingApp.NotificationService.Core.Application.Services;
using DatingApp.NotificationService.Core.Ports;
using DatingApp.NotificationService.Infrastructure.Adapters.Email;
using DatingApp.NotificationService.Infrastructure.Adapters.Logging;
using DatingApp.NotificationService.Infrastructure.Adapters.Sms;
using DatingApp.NotificationService.Infrastructure.Adapters.Templates;
using DatingApp.NotificationService.Infrastructure.Consumers;
using MassTransit;
using SendGrid;
using SendGrid.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// ===========================================================================================
// HEXAGONAL ARCHITECTURE - DEPENDENCY INJECTION CONFIGURATION
// ===========================================================================================
// This is where we wire up PORTS (interfaces) to ADAPTERS (implementations).
// The Core/Domain knows nothing about these concrete implementations!
//
// Benefits:
// 1. Swap adapters without changing domain code (Console → SendGrid)
// 2. Test with mock adapters
// 3. Run different adapters in different environments (dev vs prod)
// ===========================================================================================

// Register logging adapter (wraps Microsoft.Extensions.Logging)
builder.Services.AddSingleton(typeof(INotificationLogger<>), typeof(MicrosoftLoggerAdapter<>));

// ===========================================================================================
// ADAPTER SWAPPING - The Power of Hexagonal Architecture!
// ===========================================================================================
// Read Email:Provider from configuration and register the appropriate adapter.
// This demonstrates how we can swap adapters WITHOUT changing domain code!
//
// Supported providers:
// - "Console" → ConsoleEmailProvider (development/testing)
// - "SendGrid" → SendGridEmailProvider (production)
// ===========================================================================================
var emailProvider = builder.Configuration.GetValue<string>("Email:Provider") ?? "Console";

if (emailProvider.Equals("SendGrid", StringComparison.OrdinalIgnoreCase))
{
    // Production: SendGrid adapter
    var sendGridApiKey = builder.Configuration.GetValue<string>("Email:SendGrid:ApiKey");
    var fromEmail = builder.Configuration.GetValue<string>("Email:SendGrid:FromEmail") ?? "noreply@quietmatch.com";
    var fromName = builder.Configuration.GetValue<string>("Email:SendGrid:FromName") ?? "QuietMatch";

    if (string.IsNullOrWhiteSpace(sendGridApiKey))
    {
        throw new InvalidOperationException(
            "SendGrid API key is required when Email:Provider is set to 'SendGrid'. " +
            "Set Email:SendGrid:ApiKey in appsettings.json or environment variable.");
    }

    // Register SendGrid client
    builder.Services.AddSendGrid(options => options.ApiKey = sendGridApiKey);

    // Register SendGrid adapter
    builder.Services.AddSingleton<IEmailProvider>(sp =>
    {
        var client = sp.GetRequiredService<ISendGridClient>();
        var logger = sp.GetRequiredService<INotificationLogger<SendGridEmailProvider>>();
        return new SendGridEmailProvider(client, logger, fromEmail, fromName);
    });

    Console.WriteLine($"✅ Email Provider: SendGrid (from: {fromEmail})");
}
else
{
    // Development: Console adapter (default)
    builder.Services.AddSingleton<IEmailProvider, ConsoleEmailProvider>();
    Console.WriteLine("✅ Email Provider: Console (development mode)");
}

// Register SMS provider (Console for now, can add Twilio later)
builder.Services.AddSingleton<ISmsProvider, ConsoleSmsProvider>();

// Register template provider with template path
var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "DatingApp.NotificationService.Infrastructure", "Templates");
builder.Services.AddSingleton<ITemplateProvider>(sp =>
{
    var logger = sp.GetRequiredService<INotificationLogger<FileTemplateProvider>>();
    return new FileTemplateProvider(templatePath, logger);
});

// Register application service
builder.Services.AddScoped<NotificationService>();

// ===========================================================================================
// EVENT-DRIVEN ARCHITECTURE - MASSTRANSIT & RABBITMQ CONFIGURATION
// ===========================================================================================
// Configure MassTransit for consuming events from other services
builder.Services.AddMassTransit(x =>
{
    // Register consumers (event handlers)
    x.AddConsumer<UserRegisteredConsumer>();
    x.AddConsumer<ProfileCompletedConsumer>();

    // Configure RabbitMQ as message broker
    x.UsingRabbitMq((context, cfg) =>
    {
        // RabbitMQ connection (matches docker-compose.yml configuration)
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        // Configure receive endpoint for this service
        cfg.ReceiveEndpoint("notification-service", e =>
        {
            // Retry configuration: 3 retries with exponential backoff
            e.UseMessageRetry(r => r.Exponential(
                retryLimit: 3,
                minInterval: TimeSpan.FromSeconds(1),
                maxInterval: TimeSpan.FromSeconds(30),
                intervalDelta: TimeSpan.FromSeconds(2)
            ));

            // Configure consumers for this endpoint
            e.ConfigureConsumer<UserRegisteredConsumer>(context);
            e.ConfigureConsumer<ProfileCompletedConsumer>(context);
        });
    });
});

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "QuietMatch Notification Service API", Version = "v1" });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ===========================================================================================
// API ENDPOINTS - Demonstrating Hexagonal Architecture
// ===========================================================================================

app.MapGet("/", () => new
{
    service = "QuietMatch Notification Service",
    version = "1.0.0",
    architecture = "Hexagonal (Ports & Adapters)",
    status = "Running",
    endpoints = new[]
    {
        "GET  /health - Health check",
        "POST /api/notifications/welcome - Send welcome email",
        "POST /api/notifications/profile-completed - Send profile completed email",
        "POST /api/notifications/sms - Send SMS"
    }
})
.WithName("ServiceInfo")
.WithOpenApi();

app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow })
    .WithName("HealthCheck")
    .WithOpenApi();

// ===========================================================================================
// DEMO: Send Welcome Email
// ===========================================================================================
// This endpoint demonstrates:
// - How application service uses PORTS (not concrete adapters)
// - How we can swap ConsoleEmailProvider → SendGridEmailProvider via DI
// - Template rendering through ITemplateProvider port
// ===========================================================================================
app.MapPost("/api/notifications/welcome", async (
    NotificationService notificationService,
    WelcomeEmailRequest request) =>
{
    var result = await notificationService.SendWelcomeEmailAsync(
        request.UserId,
        request.Email,
        request.Name);

    return result
        ? Results.Ok(new { success = true, message = "Welcome email sent successfully" })
        : Results.StatusCode(500);
})
.WithName("SendWelcomeEmail")
.WithOpenApi();

// ===========================================================================================
// DEMO: Send Profile Completed Email
// ===========================================================================================
app.MapPost("/api/notifications/profile-completed", async (
    NotificationService notificationService,
    ProfileCompletedRequest request) =>
{
    var result = await notificationService.SendProfileCompletedEmailAsync(
        request.UserId,
        request.Email,
        request.Name);

    return result
        ? Results.Ok(new { success = true, message = "Profile completed email sent successfully" })
        : Results.StatusCode(500);
})
.WithName("SendProfileCompletedEmail")
.WithOpenApi();

// ===========================================================================================
// DEMO: Send SMS
// ===========================================================================================
app.MapPost("/api/notifications/sms", async (
    NotificationService notificationService,
    SmsRequest request) =>
{
    var result = await notificationService.SendSmsAsync(
        request.PhoneNumber,
        request.Message);

    return result
        ? Results.Ok(new { success = true, message = "SMS sent successfully" })
        : Results.StatusCode(500);
})
.WithName("SendSms")
.WithOpenApi();

app.Run();

// ===========================================================================================
// REQUEST DTOs
// ===========================================================================================

record WelcomeEmailRequest(Guid UserId, string Email, string? Name);
record ProfileCompletedRequest(Guid UserId, string Email, string Name);
record SmsRequest(string PhoneNumber, string Message);
