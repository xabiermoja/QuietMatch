# NotificationService - Hexagonal Architecture (Ports & Adapters)

## Why Hexagonal for NotificationService?

NotificationService uses **Hexagonal Architecture (Ports & Adapters)** to enable **swappable notification providers** without changing business logic.

---

## Architecture Decision

### Pattern: Hexagonal / Ports & Adapters

**Decision**: Use Hexagonal Architecture
**Date**: 2025-11-27
**Status**: Implemented

### Why NOT Other Patterns?

| Pattern | Why NOT |
|---------|---------|
| **Layered** | Hard to swap email providers (SendGrid → AWS SES). Would require changes to application layer. |
| **Onion** | Similar to Hexagonal, but doesn't emphasize ports/adapters terminology as clearly. |
| **CQRS** | Overkill - notifications don't need separate read/write models. |

### Why Hexagonal IS Perfect

✅ **Multiple Providers**: Email (SendGrid, SMTP, Console), SMS (Twilio, Console), Push (Firebase)
✅ **Swappable Adapters**: Change provider with 1 line of DI configuration
✅ **Testability**: Mock ports easily (no need to mock SendGrid SDK)
✅ **Progressive Enhancement**: Start with console adapters, add real providers later
✅ **Learning Value**: Pure demonstration of Dependency Inversion Principle

---

## Hexagonal Structure

```
NotificationService/
├── Core/ (Domain + Application - ZERO dependencies!)
│   ├── Domain/
│   │   ├── Entities/Notification.cs
│   │   ├── ValueObjects/EmailMessage.cs, SmsMessage.cs, Recipient.cs
│   │   └── Enums/NotificationChannel.cs, NotificationStatus.cs
│   ├── Ports/ (Interfaces defined in DOMAIN!)
│   │   ├── IEmailProvider.cs
│   │   ├── ISmsProvider.cs
│   │   ├── IPushProvider.cs
│   │   └── ITemplateProvider.cs
│   └── Application/
│       └── Services/NotificationService.cs
│
├── Infrastructure/ (Adapters - implements domain ports)
│   ├── Adapters/
│   │   ├── Email/
│   │   │   ├── ConsoleEmailProvider.cs (Development)
│   │   │   ├── SendGridEmailProvider.cs (Production)
│   │   │   └── SmtpEmailProvider.cs (Alternative)
│   │   ├── Sms/
│   │   │   ├── ConsoleSmsProvider.cs (Development)
│   │   │   └── TwilioSmsProvider.cs (Production)
│   │   └── Templates/
│   │       └── FileTemplateProvider.cs
│   └── Messaging/
│       └── Consumers/ (MassTransit consumers)
│
└── Api/ (HTTP endpoints - optional)
```

### Dependency Flow (Arrows Point Inward)

```
API → Infrastructure → Application → Domain
                          ↓
                        Ports (interfaces)
                          ↑ implements
                    Infrastructure
```

**Key**: All dependencies point toward the domain core. Infrastructure implements ports, but domain never depends on infrastructure.

---

## Hexagonal Principles Applied

### 1. Ports Defined in Domain, NOT Infrastructure

✅ **CORRECT** (Hexagonal):
```csharp
// Core/Ports/IEmailProvider.cs (in Domain project!)
namespace DatingApp.NotificationService.Core.Ports;

public interface IEmailProvider
{
    Task SendAsync(EmailMessage message);
}
```

❌ **WRONG** (Not Hexagonal):
```csharp
// Infrastructure/IEmailProvider.cs (coupling!)
namespace DatingApp.NotificationService.Infrastructure;

public interface IEmailProvider { ... }
```

**Why**: Ports are part of the domain contract. Domain defines what it needs, infrastructure provides it.

---

### 2. Application Depends on Ports, Not Adapters

✅ **CORRECT**:
```csharp
// Application/Services/NotificationService.cs
public class NotificationService
{
    private readonly IEmailProvider _emailProvider;  // Port (interface)!

    public NotificationService(IEmailProvider emailProvider)
    {
        _emailProvider = emailProvider;
    }

    public async Task SendWelcomeEmail(string email)
    {
        var message = new EmailMessage(...);
        await _emailProvider.SendAsync(message);  // Call through port
    }
}
```

❌ **WRONG**:
```csharp
public class NotificationService
{
    private readonly SendGridClient _sendGrid;  // Direct adapter dependency!

    public NotificationService(SendGridClient sendGrid) { ... }
}
```

**Why**: Application should only know about ports. Infrastructure is plugged in at runtime via DI.

---

### 3. Adapters Are Swappable via DI

**Development Configuration**:
```csharp
// Program.cs
builder.Services.AddScoped<IEmailProvider, ConsoleEmailProvider>();
builder.Services.AddScoped<ISmsProvider, ConsoleSmsProvider>();
```

**Production Configuration** (same code, different adapters!):
```csharp
builder.Services.AddScoped<IEmailProvider, SendGridEmailProvider>();
builder.Services.AddScoped<ISmsProvider, TwilioSmsProvider>();
```

**Swap = 1 line change, zero domain/application changes!**

---

### 4. Domain Has ZERO Dependencies

```xml
<!-- Core/DatingApp.NotificationService.Core.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <!-- NO external dependencies! -->
    <!-- ❌ No SendGrid -->
    <!-- ❌ No Twilio -->
    <!-- ❌ No EF Core -->
    <!-- ❌ No MassTransit -->
  </ItemGroup>
</Project>
```

**Why**: Domain logic should be pure business rules, testable without any infrastructure.

---

## Example: Swappable Email Providers

### Port (Interface)

```csharp
// Core/Ports/IEmailProvider.cs
public interface IEmailProvider
{
    Task<bool> SendAsync(EmailMessage message);
}
```

### Adapter 1: Console (Development)

```csharp
// Infrastructure/Adapters/Email/ConsoleEmailProvider.cs
public class ConsoleEmailProvider : IEmailProvider
{
    public Task<bool> SendAsync(EmailMessage message)
    {
        Console.WriteLine($"📧 Email to {message.To}: {message.Subject}");
        return Task.FromResult(true);
    }
}
```

### Adapter 2: SendGrid (Production)

```csharp
// Infrastructure/Adapters/Email/SendGridEmailProvider.cs
public class SendGridEmailProvider : IEmailProvider
{
    private readonly ISendGridClient _sendGrid;

    public async Task<bool> SendAsync(EmailMessage message)
    {
        var sendGridMessage = MailHelper.CreateSingleEmail(...);
        var response = await _sendGrid.SendEmailAsync(sendGridMessage);
        return response.IsSuccessStatusCode;
    }
}
```

### Adapter 3: SMTP (Alternative)

```csharp
// Infrastructure/Adapters/Email/SmtpEmailProvider.cs
public class SmtpEmailProvider : IEmailProvider
{
    private readonly SmtpClient _smtp;

    public async Task<bool> SendAsync(EmailMessage message)
    {
        await _smtp.SendMailAsync(...);
        return true;
    }
}
```

**All 3 adapters implement the same port!** Swap anytime via DI configuration.

---

## Testing Benefits

### Unit Tests (Mock Ports, Not Adapters)

```csharp
public class NotificationServiceTests
{
    [Fact]
    public async Task SendWelcomeEmail_CallsEmailProvider()
    {
        // Arrange
        var mockEmailProvider = new Mock<IEmailProvider>();  // Mock port!
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

**Benefits**:
- No need to mock SendGrid SDK
- Fast, pure unit tests
- Test domain logic in isolation

---

## Comparison with Other Services

| Service | Pattern | Why | Swappable Components |
|---------|---------|-----|---------------------|
| **IdentityService** | Layered | Simple CRUD, no swappable providers | None |
| **ProfileService** | Onion | Rich domain logic (privacy rules) | None |
| **NotificationService** | **Hexagonal** | **Multiple notification providers** | Email, SMS, Push adapters |
| **MatchingService** | Hexagonal | Swappable matching engines | Rule-based → ML-based |

**Hexagonal shines when**: You need to swap external dependencies (providers, algorithms, data sources).

---

## Progressive Enhancement Path

**Phase 1** (Learning): Console adapters
**Phase 2** (Real providers): SendGrid, Twilio
**Phase 3** (Advanced): Firebase Push, SignalR

Each phase just adds new adapters - domain stays unchanged!

---

## Success Criteria

✅ **You've mastered Hexagonal when:**
- Core project has zero NuGet dependencies
- Ports defined in `Core/Ports`, not Infrastructure
- Application depends only on ports (interfaces)
- You can swap email providers in 30 seconds
- Unit tests run in milliseconds (no SendGrid mocking)
- You understand when Hexagonal is overkill vs valuable

---

## References

- [Hexagonal Architecture (Alistair Cockburn)](https://alistair.cockburn.us/hexagonal-architecture/)
- [Ports & Adapters Pattern](https://herbertograca.com/2017/09/14/ports-adapters-architecture/)
- [Why Hexagonal (Project-Specific)](../../docs/40_features/f0003_notification_service_mvp/WHY_HEXAGONAL.md)

---

**Last Updated**: 2025-11-27
**Author**: QuietMatch Engineering Team
