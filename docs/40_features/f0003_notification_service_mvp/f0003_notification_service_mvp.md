# F0003: NotificationService MVP

**Feature ID**: F0003
**Service**: NotificationService
**Architecture**: Hexagonal (Ports & Adapters)
**Status**: 🔴 Not Started
**Estimated Effort**: 6-9 hours
**Phase**: Phase 1 (Foundation) - Learning Hexagonal Architecture

---

## Table of Contents

- [Overview](#overview)
- [Learning Objectives](#learning-objectives)
- [User Stories](#user-stories)
- [Architecture](#architecture)
- [Technical Specification](#technical-specification)
- [Event Consumption](#event-consumption)
- [Notification Templates](#notification-templates)
- [Testing Strategy](#testing-strategy)
- [Acceptance Criteria](#acceptance-criteria)

---

## Overview

NotificationService is responsible for sending notifications to users across multiple channels (email, SMS, push). It implements **Hexagonal Architecture (Ports & Adapters)** to enable swappable notification providers without changing business logic.

### Purpose

- Send welcome emails when users register
- Send profile completion notifications
- Send match notifications (future)
- Support multiple notification channels
- Enable easy provider swapping (SendGrid → AWS SES, Twilio → AWS SNS)

### Why This Service?

This is the **first Hexagonal implementation** in QuietMatch, chosen specifically for **learning purposes**:
- ✅ Multiple swappable adapters (email, SMS, push)
- ✅ Minimal dependencies (no blockers)
- ✅ Immediate feedback (see results quickly)
- ✅ Progressive complexity (console → real providers)

See [WHY_HEXAGONAL.md](./WHY_HEXAGONAL.md) for detailed rationale.

---

## Learning Objectives

### Primary Goal: Master Hexagonal Architecture

By implementing NotificationService, you will learn:

1. **Ports & Adapters Pattern**
   - Define ports (interfaces) in domain core
   - Implement adapters in infrastructure layer
   - Swap adapters without changing business logic

2. **Dependency Inversion**
   - Domain has zero external dependencies
   - Application depends on ports (interfaces)
   - Infrastructure implements ports

3. **Testability**
   - Mock ports easily (no need to mock SendGrid SDK)
   - Test domain logic in isolation

4. **Progressive Enhancement**
   - Start with console adapters (learning)
   - Add real providers incrementally (SendGrid, Twilio)

---

## User Stories

### US-F0003-01: Welcome Email on Sign Up

**As a** new user
**I want** to receive a welcome email after signing up with Google
**So that** I feel welcomed and know my account was created successfully

**Acceptance Criteria**:
- AC1: Email sent within 5 seconds of sign-up
- AC2: Email contains user's name and welcome message
- AC3: Email includes link to complete profile
- AC4: Email template is branded (QuietMatch logo, colors)

---

### US-F0003-02: Profile Completion Notification

**As a** user who completes their profile
**I want** to receive a notification
**So that** I know my profile is ready for matching

**Acceptance Criteria**:
- AC1: Email sent when profile reaches 100% completion
- AC2: Email confirms matching process has started
- AC3: Email includes profile summary

---

### US-F0003-03: Swappable Email Providers

**As a** developer
**I want** to easily swap email providers (e.g., SendGrid → AWS SES)
**So that** I can change providers without modifying business logic

**Acceptance Criteria**:
- AC1: Swap providers via DI configuration only (1 line change)
- AC2: No changes to domain or application layer required
- AC3: All tests still pass after provider swap

---

## Architecture

### Hexagonal Architecture Structure

```
DatingApp.NotificationService/
├── Core/ (Domain + Application - ZERO dependencies)
│   ├── Domain/
│   │   ├── Entities/Notification.cs
│   │   ├── ValueObjects/
│   │   │   ├── EmailMessage.cs
│   │   │   ├── SmsMessage.cs
│   │   │   └── Recipient.cs
│   │   └── Enums/
│   │       ├── NotificationChannel.cs (Email, Sms, Push)
│   │       └── NotificationStatus.cs (Pending, Sent, Failed)
│   ├── Ports/ (Interfaces - defined in DOMAIN!)
│   │   ├── IEmailProvider.cs
│   │   ├── ISmsProvider.cs
│   │   ├── IPushProvider.cs
│   │   └── ITemplateProvider.cs
│   └── Application/
│       └── Services/NotificationService.cs
│
├── Infrastructure/ (Adapters - implements ports)
│   ├── Adapters/
│   │   ├── Email/
│   │   │   ├── ConsoleEmailProvider.cs (Development)
│   │   │   ├── SendGridEmailProvider.cs (Production)
│   │   │   └── SmtpEmailProvider.cs (Alternative)
│   │   ├── Sms/
│   │   │   ├── ConsoleSmsProvider.cs (Development)
│   │   │   └── TwilioSmsProvider.cs (Production)
│   │   └── Templates/
│   │       ├── FileTemplateProvider.cs (Read from files)
│   │       └── RazorTemplateProvider.cs (Razor templates)
│   ├── Data/ (Optional: Notification history)
│   │   ├── NotificationDbContext.cs
│   │   └── Repositories/NotificationRepository.cs
│   └── Messaging/
│       └── Consumers/
│           ├── UserRegisteredConsumer.cs
│           ├── ProfileCompletedConsumer.cs
│           └── MatchAcceptedConsumer.cs (future)
│
├── Api/ (Optional HTTP endpoints)
│   └── Controllers/NotificationController.cs
│
└── Tests/
    ├── DatingApp.NotificationService.Tests.Unit/
    └── DatingApp.NotificationService.Tests.Integration/
```

### Hexagonal Principles Applied

1. **Ports (Interfaces) Defined in Domain**
   ```csharp
   // Core/Ports/IEmailProvider.cs (in Domain project!)
   public interface IEmailProvider
   {
       Task SendAsync(EmailMessage message);
   }
   ```

2. **Adapters Implement Ports**
   ```csharp
   // Infrastructure/Adapters/Email/ConsoleEmailProvider.cs
   public class ConsoleEmailProvider : IEmailProvider
   {
       public Task SendAsync(EmailMessage message)
       {
           Console.WriteLine($"📧 Email to {message.To}: {message.Subject}");
           return Task.CompletedTask;
       }
   }
   ```

3. **Application Depends on Ports, Not Adapters**
   ```csharp
   // Application/Services/NotificationService.cs
   public class NotificationService
   {
       private readonly IEmailProvider _emailProvider;  // Port!

       public async Task SendWelcomeEmail(string email, string name)
       {
           var message = new EmailMessage(
               to: new Recipient(email, name),
               subject: "Welcome to QuietMatch!",
               body: "..."
           );
           await _emailProvider.SendAsync(message);
       }
   }
   ```

4. **DI Configuration Swaps Adapters**
   ```csharp
   // Program.cs - Development
   builder.Services.AddScoped<IEmailProvider, ConsoleEmailProvider>();

   // Program.cs - Production
   builder.Services.AddScoped<IEmailProvider, SendGridEmailProvider>();
   ```

---

## Technical Specification

### Domain Layer

#### Entities

**Notification** (Aggregate Root)
```csharp
public class Notification
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public string Subject { get; private set; }
    public string Body { get; private set; }
    public NotificationStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? SentAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    public void MarkAsSent()
    {
        Status = NotificationStatus.Sent;
        SentAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = NotificationStatus.Failed;
        ErrorMessage = errorMessage;
    }
}
```

#### Value Objects

**EmailMessage**
```csharp
public record EmailMessage(
    Recipient To,
    string Subject,
    string Body,
    Recipient? From = null,
    List<Recipient>? Cc = null,
    List<Recipient>? Bcc = null
);

public record Recipient(string Email, string Name);
```

**SmsMessage**
```csharp
public record SmsMessage(
    string PhoneNumber,
    string Message
);
```

#### Enums

```csharp
public enum NotificationChannel
{
    Email,
    Sms,
    Push,
    InApp
}

public enum NotificationStatus
{
    Pending,
    Sent,
    Failed,
    Retrying
}
```

---

### Ports (Interfaces)

#### IEmailProvider
```csharp
// Core/Ports/IEmailProvider.cs
public interface IEmailProvider
{
    /// <summary>
    /// Sends an email message asynchronously.
    /// </summary>
    /// <param name="message">Email message to send</param>
    /// <returns>True if sent successfully, false otherwise</returns>
    Task<bool> SendAsync(EmailMessage message);
}
```

#### ISmsProvider
```csharp
// Core/Ports/ISmsProvider.cs
public interface ISmsProvider
{
    /// <summary>
    /// Sends an SMS message asynchronously.
    /// </summary>
    /// <param name="message">SMS message to send</param>
    /// <returns>True if sent successfully, false otherwise</returns>
    Task<bool> SendAsync(SmsMessage message);
}
```

#### ITemplateProvider
```csharp
// Core/Ports/ITemplateProvider.cs
public interface ITemplateProvider
{
    /// <summary>
    /// Renders a notification template with data.
    /// </summary>
    /// <param name="templateName">Name of the template (e.g., "WelcomeEmail")</param>
    /// <param name="data">Template data</param>
    /// <returns>Rendered HTML string</returns>
    Task<string> RenderAsync(string templateName, object data);
}
```

---

### Adapters (Infrastructure)

#### ConsoleEmailProvider (Development)
```csharp
public class ConsoleEmailProvider : IEmailProvider
{
    private readonly ILogger<ConsoleEmailProvider> _logger;

    public ConsoleEmailProvider(ILogger<ConsoleEmailProvider> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendAsync(EmailMessage message)
    {
        _logger.LogInformation(
            "📧 [CONSOLE EMAIL] To: {To}, Subject: {Subject}",
            message.To.Email,
            message.Subject);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Console.WriteLine($"📧 EMAIL SENT (Console Adapter)");
        Console.WriteLine($"To: {message.To.Name} <{message.To.Email}>");
        Console.WriteLine($"Subject: {message.Subject}");
        Console.WriteLine($"Body:\n{message.Body}");
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Console.ResetColor();

        return Task.FromResult(true);
    }
}
```

#### SendGridEmailProvider (Production)
```csharp
public class SendGridEmailProvider : IEmailProvider
{
    private readonly ISendGridClient _sendGridClient;
    private readonly ILogger<SendGridEmailProvider> _logger;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public SendGridEmailProvider(
        ISendGridClient sendGridClient,
        IConfiguration configuration,
        ILogger<SendGridEmailProvider> logger)
    {
        _sendGridClient = sendGridClient;
        _logger = logger;
        _fromEmail = configuration["SendGrid:FromEmail"] ?? "noreply@quietmatch.com";
        _fromName = configuration["SendGrid:FromName"] ?? "QuietMatch";
    }

    public async Task<bool> SendAsync(EmailMessage message)
    {
        try
        {
            var from = new EmailAddress(_fromEmail, _fromName);
            var to = new EmailAddress(message.To.Email, message.To.Name);

            var sendGridMessage = MailHelper.CreateSingleEmail(
                from,
                to,
                message.Subject,
                plainTextContent: message.Body,
                htmlContent: message.Body
            );

            var response = await _sendGridClient.SendEmailAsync(sendGridMessage);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Email sent successfully via SendGrid to {Email}",
                    message.To.Email);
                return true;
            }
            else
            {
                _logger.LogError(
                    "SendGrid email failed: {StatusCode}, {Body}",
                    response.StatusCode,
                    await response.Body.ReadAsStringAsync());
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email via SendGrid");
            return false;
        }
    }
}
```

---

## Event Consumption

NotificationService consumes events from other services to trigger notifications.

### UserRegistered Event (from IdentityService)

**Event**:
```csharp
public record UserRegistered(
    Guid UserId,
    string Email,
    string Provider,
    DateTime RegisteredAt,
    Guid CorrelationId
);
```

**Consumer**:
```csharp
public class UserRegisteredConsumer : IConsumer<UserRegistered>
{
    private readonly NotificationService _notificationService;

    public async Task Consume(ConsumeContext<UserRegistered> context)
    {
        var @event = context.Message;

        await _notificationService.SendWelcomeEmailAsync(
            @event.UserId,
            @event.Email
        );
    }
}
```

---

### ProfileCompleted Event (from ProfileService)

**Event**:
```csharp
public record ProfileCompleted(
    Guid UserId,
    DateTime CompletedAt,
    Guid CorrelationId
);
```

**Consumer**:
```csharp
public class ProfileCompletedConsumer : IConsumer<ProfileCompleted>
{
    private readonly NotificationService _notificationService;

    public async Task Consume(ConsumeContext<ProfileCompleted> context)
    {
        var @event = context.Message;

        await _notificationService.SendProfileCompletedEmailAsync(
            @event.UserId
        );
    }
}
```

---

## Notification Templates

### Welcome Email Template

**Template File**: `Infrastructure/Templates/WelcomeEmail.html`

```html
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #6366f1; color: white; padding: 20px; text-align: center; }
        .content { padding: 20px; background: #f9fafb; }
        .button { background: #6366f1; color: white; padding: 12px 24px; text-decoration: none; display: inline-block; border-radius: 6px; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>Welcome to QuietMatch! 🎉</h1>
        </div>
        <div class="content">
            <p>Hi {{Name}},</p>
            <p>Welcome to QuietMatch - where connections are made through values, not appearances.</p>
            <p>Your account has been successfully created. Now let's get to know you better!</p>
            <p style="text-align: center; margin: 30px 0;">
                <a href="{{ProfileUrl}}" class="button">Complete Your Profile</a>
            </p>
            <p>Once your profile is complete, we'll start finding compatible matches for you based on your personality, values, and lifestyle.</p>
            <p>Looking forward to helping you find meaningful connections!</p>
            <p>Best regards,<br>The QuietMatch Team</p>
        </div>
    </div>
</body>
</html>
```

### Profile Completed Email Template

**Template File**: `Infrastructure/Templates/ProfileCompletedEmail.html`

```html
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #10b981; color: white; padding: 20px; text-align: center; }
        .content { padding: 20px; background: #f9fafb; }
        .checkmark { font-size: 48px; color: #10b981; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <div class="checkmark">✓</div>
            <h1>Profile Complete!</h1>
        </div>
        <div class="content">
            <p>Hi {{Name}},</p>
            <p>Great news! Your profile is now 100% complete.</p>
            <p>We're already working on finding compatible matches for you based on:</p>
            <ul>
                <li>Your personality traits</li>
                <li>Your core values</li>
                <li>Your lifestyle preferences</li>
            </ul>
            <p>You'll receive a notification when we find great matches!</p>
            <p>In the meantime, feel free to browse the app and explore your settings.</p>
            <p>Happy matching!</p>
            <p>Best regards,<br>The QuietMatch Team</p>
        </div>
    </div>
</body>
</html>
```

---

## Testing Strategy

### Unit Tests

**Test Ports in Isolation**:
```csharp
public class NotificationServiceTests
{
    [Fact]
    public async Task SendWelcomeEmail_ShouldCallEmailProvider()
    {
        // Arrange
        var mockEmailProvider = new Mock<IEmailProvider>();
        mockEmailProvider.Setup(x => x.SendAsync(It.IsAny<EmailMessage>()))
            .ReturnsAsync(true);

        var service = new NotificationService(mockEmailProvider.Object);

        // Act
        await service.SendWelcomeEmailAsync(Guid.NewGuid(), "test@example.com");

        // Assert
        mockEmailProvider.Verify(
            x => x.SendAsync(It.Is<EmailMessage>(m => m.Subject.Contains("Welcome"))),
            Times.Once
        );
    }
}
```

### Integration Tests

**Test Real Adapters**:
```csharp
public class ConsoleEmailProviderTests
{
    [Fact]
    public async Task SendAsync_ShouldWriteToConsole()
    {
        // Arrange
        var provider = new ConsoleEmailProvider(Mock.Of<ILogger<ConsoleEmailProvider>>());
        var message = new EmailMessage(
            new Recipient("test@example.com", "Test User"),
            "Test Subject",
            "Test Body"
        );

        // Act
        var result = await provider.SendAsync(message);

        // Assert
        Assert.True(result);
    }
}
```

**Test Event Consumers** (with Testcontainers for RabbitMQ):
```csharp
public class UserRegisteredConsumerTests : IAsyncLifetime
{
    private RabbitMqContainer _rabbitMqContainer;

    public async Task InitializeAsync()
    {
        _rabbitMqContainer = new RabbitMqBuilder().Build();
        await _rabbitMqContainer.StartAsync();
    }

    [Fact]
    public async Task Consume_UserRegistered_ShouldSendWelcomeEmail()
    {
        // Test consumer with real RabbitMQ
    }
}
```

---

## Acceptance Criteria

### MVP Completion Checklist

**Phase 1: Foundation (Console Adapter)**
- [ ] Core project created with zero dependencies
- [ ] `Notification` entity implemented
- [ ] `EmailMessage`, `Recipient` value objects implemented
- [ ] `IEmailProvider` port defined in `Core/Ports`
- [ ] `ConsoleEmailProvider` adapter implemented
- [ ] `NotificationService` application service implemented
- [ ] `UserRegisteredConsumer` implemented
- [ ] Welcome email sent to console on sign-up
- [ ] Unit tests passing (minimum 5 tests)

**Phase 2: Real Provider (SendGrid)**
- [ ] SendGrid NuGet package installed
- [ ] `SendGridEmailProvider` adapter implemented
- [ ] Configuration added for SendGrid API key
- [ ] Adapter swap via DI configuration working
- [ ] Real emails sent via SendGrid
- [ ] Integration tests with SendGrid (or mock)

**Phase 3: Multi-Channel**
- [ ] `ISmsProvider` port defined
- [ ] `ConsoleSmsProvider` adapter implemented
- [ ] Optional: `TwilioSmsProvider` implemented
- [ ] SMS notifications working

**Documentation**
- [ ] WHY_HEXAGONAL.md complete
- [ ] Feature spec complete (this document)
- [ ] Implementation plan created
- [ ] PATTERNS.md created explaining Hexagonal for this service
- [ ] API documentation (if HTTP endpoints added)

**Testing**
- [ ] Unit tests: 15+ tests covering domain + application
- [ ] Integration tests: 5+ tests with real adapters
- [ ] Consumer tests: 2+ tests with Testcontainers

**Hexagonal Compliance**
- [ ] Domain has ZERO NuGet dependencies
- [ ] Ports defined in Domain, not Infrastructure
- [ ] Application depends on ports only
- [ ] Adapters swappable via DI configuration
- [ ] All dependency arrows point inward

---

## References

- [Hexagonal Architecture (Alistair Cockburn)](https://alistair.cockburn.us/hexagonal-architecture/)
- [Ports & Adapters Pattern](https://herbertograca.com/2017/09/14/ports-adapters-architecture/)
- [SendGrid .NET Documentation](https://github.com/sendgrid/sendgrid-csharp)
- [MassTransit Consumers](https://masstransit.io/documentation/concepts/consumers)
- [Why Hexagonal (Project-Specific)](./WHY_HEXAGONAL.md)

---

**Last Updated**: 2025-11-27
**Status**: Ready for implementation
**Next**: Create implementation plan and start Phase 1
