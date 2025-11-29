# Implementation Plan - F0003: NotificationService MVP

**Status**: 🔴 Not Started
**Feature File**: [f0003_notification_service_mvp.md](./f0003_notification_service_mvp.md)
**Architecture Pattern**: Hexagonal (Ports & Adapters)
**Started**: 2025-11-27
**Last Updated**: 2025-11-27
**Estimated Total Time**: 6-9 hours

---

## Overview

Implement NotificationService using strict **Hexagonal Architecture** to enable swappable notification providers (email, SMS, push) without changing business logic.

**Learning Focus**: Master Ports & Adapters pattern with progressive complexity.

---

## Phase 0: Project Setup (30-45 minutes)

### Create Solution and Projects

- [ ] Create `src/Services/Notification/` directory
- [ ] Create solution: `DatingApp.NotificationService.sln`
- [ ] Create **Core** project (Domain + Application):
  - `DatingApp.NotificationService.Core` (classlib, net8.0)
  - **Zero NuGet dependencies!** (strict Hexagonal rule)
- [ ] Create **Infrastructure** project:
  - `DatingApp.NotificationService.Infrastructure` (classlib, net8.0)
  - Dependencies: EF Core, MassTransit, SendGrid (optional)
- [ ] Create **Api** project (optional for MVP):
  - `DatingApp.NotificationService.Api` (webapi, net8.0)
- [ ] Create test projects:
  - `DatingApp.NotificationService.Tests.Unit` (xunit, net8.0)
  - `DatingApp.NotificationService.Tests.Integration` (xunit, net8.0)
- [ ] Add projects to solution
- [ ] Set up project references:
  - Infrastructure → Core
  - Api → Infrastructure, Core
  - Tests → All projects

### Create Folder Structure (Hexagonal)

**Core/Domain/**
- [ ] `Entities/` - Notification.cs
- [ ] `ValueObjects/` - EmailMessage.cs, Recipient.cs, SmsMessage.cs
- [ ] `Enums/` - NotificationChannel.cs, NotificationStatus.cs

**Core/Ports/** (interfaces defined in DOMAIN!)
- [ ] `IEmailProvider.cs`
- [ ] `ISmsProvider.cs`
- [ ] `ITemplateProvider.cs`

**Core/Application/**
- [ ] `Services/NotificationService.cs`

**Infrastructure/Adapters/**
- [ ] `Email/ConsoleEmailProvider.cs`
- [ ] `Email/SendGridEmailProvider.cs` (optional for Phase 2)
- [ ] `Sms/ConsoleSmsProvider.cs`
- [ ] `Templates/FileTemplateProvider.cs`

**Infrastructure/Messaging/**
- [ ] `Consumers/UserRegisteredConsumer.cs`
- [ ] `Consumers/ProfileCompletedConsumer.cs`

**Infrastructure/Data/** (optional)
- [ ] `NotificationDbContext.cs`
- [ ] `Repositories/NotificationRepository.cs`

### Documentation

- [ ] Create `PATTERNS.md` explaining Hexagonal choice
- [ ] Copy template from `docs/40_features/f0003_notification_service_mvp/WHY_HEXAGONAL.md`

---

## Phase 1: Domain Layer (1-1.5 hours)

### Create Value Objects

- [ ] **Recipient.cs**
  ```csharp
  public record Recipient(string Email, string Name);
  ```
  - Validation: Email must be valid format
  - Validation: Name required

- [ ] **EmailMessage.cs**
  ```csharp
  public record EmailMessage(
      Recipient To,
      string Subject,
      string Body,
      Recipient? From = null,
      List<Recipient>? Cc = null
  );
  ```
  - Validation: To, Subject, Body required
  - Validation: Subject max 255 chars

- [ ] **SmsMessage.cs**
  ```csharp
  public record SmsMessage(string PhoneNumber, string Message);
  ```

### Create Enums

- [ ] **NotificationChannel.cs**
  ```csharp
  public enum NotificationChannel { Email, Sms, Push, InApp }
  ```

- [ ] **NotificationStatus.cs**
  ```csharp
  public enum NotificationStatus { Pending, Sent, Failed, Retrying }
  ```

### Create Notification Entity

- [ ] **Notification.cs** (Aggregate Root)
  - Properties: Id, UserId, Channel, Subject, Body, Status, CreatedAt, SentAt, ErrorMessage
  - Factory method: `Create(userId, channel, subject, body)`
  - Domain methods:
    - `MarkAsSent()`
    - `MarkAsFailed(string errorMessage)`
    - `CanRetry()` - checks if failed and retryable

### Create Ports (Interfaces in Domain!)

- [ ] **IEmailProvider.cs**
  ```csharp
  // Core/Ports/IEmailProvider.cs (in Domain project!)
  public interface IEmailProvider
  {
      Task<bool> SendAsync(EmailMessage message);
  }
  ```

- [ ] **ISmsProvider.cs**
  ```csharp
  public interface ISmsProvider
  {
      Task<bool> SendAsync(SmsMessage message);
  }
  ```

- [ ] **ITemplateProvider.cs**
  ```csharp
  public interface ITemplateProvider
  {
      Task<string> RenderAsync(string templateName, object data);
  }
  ```

---

## Phase 2: Application Layer (1-1.5 hours)

### Create Application Service

- [ ] **NotificationService.cs**
  - Constructor dependencies: IEmailProvider, ISmsProvider, ITemplateProvider, ILogger
  - **Important**: Depend on ports (interfaces), not adapters!

- [ ] Implement `SendWelcomeEmailAsync(Guid userId, string email, string name)`
  - Get template via ITemplateProvider
  - Create EmailMessage
  - Send via IEmailProvider.SendAsync()
  - Log result

- [ ] Implement `SendProfileCompletedEmailAsync(Guid userId)`
  - Fetch user data (future: via IProfileDataProvider port)
  - Get template
  - Create EmailMessage
  - Send via IEmailProvider

- [ ] Implement `SendSmsAsync(string phoneNumber, string message)`
  - Create SmsMessage
  - Send via ISmsProvider

### Create DTOs (if needed for API)

- [ ] **SendEmailRequest.cs**
- [ ] **SendSmsRequest.cs**

---

## Phase 3: Infrastructure Layer - Console Adapters (1.5-2 hours)

### Email Adapter (Console)

- [ ] **ConsoleEmailProvider.cs**
  - Implements `IEmailProvider`
  - Constructor: ILogger<ConsoleEmailProvider>
  - `SendAsync()`:
    - Log to console with colored output
    - Format: "📧 Email to {email}: {subject}"
    - Print full email body
    - Return true (always succeeds)

### SMS Adapter (Console)

- [ ] **ConsoleSmsProvider.cs**
  - Implements `ISmsProvider`
  - Constructor: ILogger<ConsoleSmsProvider>
  - `SendAsync()`:
    - Log to console with colored output
    - Format: "📱 SMS to {phone}: {message}"
    - Return true

### Template Provider

- [ ] **FileTemplateProvider.cs**
  - Implements `ITemplateProvider`
  - Read HTML template files from `Infrastructure/Templates/`
  - Replace {{Name}}, {{ProfileUrl}} placeholders with data
  - Return rendered HTML

- [ ] Create template files:
  - `Templates/WelcomeEmail.html`
  - `Templates/ProfileCompletedEmail.html`

---

## Phase 4: Event Consumers (1-1.5 hours)

### UserRegistered Consumer

- [ ] **UserRegisteredConsumer.cs**
  - Implements `IConsumer<UserRegistered>`
  - Constructor: NotificationService, ILogger
  - `Consume()`:
    - Extract userId, email from event
    - Call `NotificationService.SendWelcomeEmailAsync()`
    - Log success/failure

### ProfileCompleted Consumer

- [ ] **ProfileCompletedConsumer.cs**
  - Implements `IConsumer<ProfileCompleted>`
  - Constructor: NotificationService, ILogger
  - `Consume()`:
    - Extract userId from event
    - Call `NotificationService.SendProfileCompletedEmailAsync()`

### MassTransit Configuration

- [ ] Add MassTransit NuGet package to Infrastructure
- [ ] Configure in Program.cs (if Api project exists):
  ```csharp
  builder.Services.AddMassTransit(x =>
  {
      x.AddConsumer<UserRegisteredConsumer>();
      x.AddConsumer<ProfileCompletedConsumer>();

      x.UsingRabbitMq((context, cfg) =>
      {
          cfg.Host("localhost", "/", h =>
          {
              h.Username("guest");
              h.Password("guest");
          });

          cfg.ConfigureEndpoints(context);
      });
  });
  ```

---

## Phase 5: DI Configuration & Adapter Swapping (30 minutes)

### Register Services in DI

- [ ] **Program.cs** configuration:
  ```csharp
  // Domain/Application Services
  builder.Services.AddScoped<Application.Services.NotificationService>();

  // Adapters (Development - Console)
  builder.Services.AddScoped<IEmailProvider, ConsoleEmailProvider>();
  builder.Services.AddScoped<ISmsProvider, ConsoleSmsProvider>();
  builder.Services.AddScoped<ITemplateProvider, FileTemplateProvider>();

  // MassTransit
  builder.Services.AddMassTransit(/* ... */);
  ```

### Demonstrate Adapter Swapping

- [ ] Create appsettings.Development.json:
  ```json
  {
    "NotificationProviders": {
      "Email": "Console",
      "Sms": "Console"
    }
  }
  ```

- [ ] Create appsettings.Production.json:
  ```json
  {
    "NotificationProviders": {
      "Email": "SendGrid",
      "Sms": "Twilio"
    },
    "SendGrid": {
      "ApiKey": "SG.xxx",
      "FromEmail": "noreply@quietmatch.com",
      "FromName": "QuietMatch"
    }
  }
  ```

- [ ] Add conditional registration:
  ```csharp
  var emailProvider = builder.Configuration["NotificationProviders:Email"];
  if (emailProvider == "SendGrid")
  {
      builder.Services.AddScoped<IEmailProvider, SendGridEmailProvider>();
  }
  else
  {
      builder.Services.AddScoped<IEmailProvider, ConsoleEmailProvider>();
  }
  ```

---

## Phase 6: Testing (2-3 hours)

### Unit Tests - Domain

- [ ] **NotificationTests.cs**
  - Test `Create()` factory method
  - Test `MarkAsSent()` updates status and SentAt
  - Test `MarkAsFailed()` stores error message
  - Test `CanRetry()` logic

### Unit Tests - Application

- [ ] **NotificationServiceTests.cs**
  - Mock IEmailProvider
  - Test `SendWelcomeEmailAsync()` calls provider with correct message
  - Test error handling when provider returns false
  - Verify logging calls

### Integration Tests - Adapters

- [ ] **ConsoleEmailProviderTests.cs**
  - Test `SendAsync()` returns true
  - Capture console output (optional)

### Integration Tests - Consumers

- [ ] **UserRegisteredConsumerTests.cs**
  - Use Testcontainers for RabbitMQ
  - Publish UserRegistered event
  - Verify consumer processes event
  - Verify welcome email logged

---

## Phase 7: Docker Integration (30 minutes)

### Dockerfile

- [ ] Create `Dockerfile` in NotificationService.Api:
  ```dockerfile
  FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
  WORKDIR /app
  EXPOSE 5004

  FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
  WORKDIR /src
  COPY ["DatingApp.NotificationService.Api/", "DatingApp.NotificationService.Api/"]
  COPY ["DatingApp.NotificationService.Core/", "DatingApp.NotificationService.Core/"]
  COPY ["DatingApp.NotificationService.Infrastructure/", "DatingApp.NotificationService.Infrastructure/"]

  RUN dotnet restore "DatingApp.NotificationService.Api/DatingApp.NotificationService.Api.csproj"
  RUN dotnet build "DatingApp.NotificationService.Api/DatingApp.NotificationService.Api.csproj" -c Release -o /app/build

  FROM build AS publish
  RUN dotnet publish "DatingApp.NotificationService.Api/DatingApp.NotificationService.Api.csproj" -c Release -o /app/publish

  FROM base AS final
  WORKDIR /app
  COPY --from=publish /app/publish .
  ENTRYPOINT ["dotnet", "DatingApp.NotificationService.Api.dll"]
  ```

### docker-compose Integration

- [ ] Add NotificationService to root `docker-compose.yml`:
  ```yaml
  notification-service:
    build:
      context: ./src/Services/Notification
      dockerfile: Dockerfile
    ports:
      - "5004:5004"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - RABBITMQ_HOST=rabbitmq
    depends_on:
      - rabbitmq
  ```

---

## Phase 8 (Optional): SendGrid Adapter (1-2 hours)

### Install SendGrid

- [ ] Add NuGet package: `SendGrid` to Infrastructure

### Implement SendGridEmailProvider

- [ ] **SendGridEmailProvider.cs**
  - Implements `IEmailProvider`
  - Constructor: ISendGridClient, IConfiguration, ILogger
  - `SendAsync()`:
    - Create SendGrid message
    - Call `SendGridClient.SendEmailAsync()`
    - Handle response (check status code)
    - Return true/false

### Configuration

- [ ] Add SendGrid settings to appsettings.json
- [ ] Register ISendGridClient in DI:
  ```csharp
  builder.Services.AddSingleton<ISendGridClient>(new SendGridClient(apiKey));
  ```

### Test SendGrid Adapter

- [ ] Integration test with real SendGrid account (optional)
- [ ] Manual testing: Send test email

---

## Acceptance Criteria Checklist

### Hexagonal Compliance

- [ ] ✅ Core project has ZERO NuGet dependencies (verify in .csproj)
- [ ] ✅ Ports (IEmailProvider, ISmsProvider) defined in `Core/Ports`, NOT Infrastructure
- [ ] ✅ Application depends ONLY on ports (check imports)
- [ ] ✅ Adapters swappable via DI configuration (1 line change)
- [ ] ✅ All dependency arrows point inward (Infrastructure → Application → Domain)

### Functionality

- [ ] ✅ Welcome email sent when UserRegistered event consumed
- [ ] ✅ Profile completed email sent when ProfileCompleted event consumed
- [ ] ✅ Console adapter logs emails to console
- [ ] ✅ Email templates rendered with user data

### Testing

- [ ] ✅ Unit tests: 15+ tests passing
- [ ] ✅ Integration tests: 5+ tests passing
- [ ] ✅ Consumer tests: 2+ tests with Testcontainers

### Documentation

- [ ] ✅ WHY_HEXAGONAL.md complete
- [ ] ✅ Feature spec complete
- [ ] ✅ PATTERNS.md created
- [ ] ✅ README.md with setup instructions

---

## Implementation Notes

### Strict Hexagonal Rules to Follow

1. **Domain Layer**:
   - ✅ NO external dependencies (check .csproj)
   - ✅ Ports (interfaces) defined HERE, not in Infrastructure
   - ✅ Pure C# business logic

2. **Application Layer**:
   - ✅ Depends on domain ports (interfaces)
   - ✅ NEVER depends on adapters directly
   - ✅ Orchestrates use cases

3. **Infrastructure Layer**:
   - ✅ Implements domain ports (IEmailProvider → ConsoleEmailProvider)
   - ✅ All external dependencies live here (SendGrid, EF Core, MassTransit)
   - ✅ Adapters are swappable

4. **Dependency Flow**:
   ```
   API → Infrastructure → Application → Domain
                              ↓
                           Ports (interfaces)
                              ↑ implements
                      Infrastructure
   ```

### Key Learning Moments

**Moment 1**: Defining IEmailProvider in `Core/Ports`, not Infrastructure
- **Why**: Ports are part of the domain contract
- **Benefit**: Domain defines what it needs, infrastructure provides it

**Moment 2**: Swapping adapters via DI (console → SendGrid)
- **Change**: 1 line in Program.cs
- **Result**: Zero changes to domain or application
- **Lesson**: True dependency inversion

**Moment 3**: Testing domain without mocking infrastructure
- **Approach**: Mock IEmailProvider (port), not SendGrid SDK
- **Benefit**: Fast, pure unit tests

---

## Time Breakdown

| Phase | Task | Estimated Time |
|-------|------|---------------|
| 0 | Project Setup | 30-45 min |
| 1 | Domain Layer | 1-1.5 hours |
| 2 | Application Layer | 1-1.5 hours |
| 3 | Infrastructure (Console Adapters) | 1.5-2 hours |
| 4 | Event Consumers | 1-1.5 hours |
| 5 | DI Configuration | 30 min |
| 6 | Testing | 2-3 hours |
| 7 | Docker Integration | 30 min |
| 8 | SendGrid Adapter (Optional) | 1-2 hours |
| **Total** | | **6-9 hours** (MVP) |
| **With SendGrid** | | **8-11 hours** |

---

## Success Metrics

✅ **You've mastered Hexagonal when:**
- You can explain ports vs adapters to someone else
- You can swap email providers in 30 seconds
- Your unit tests run in milliseconds (no external dependencies)
- Your domain layer has zero NuGet packages
- You understand when Hexagonal is overkill vs valuable

---

**Next Steps**: Start Phase 0 - Project Setup
**Commands**: See implementation commands in next section

---

**Last Updated**: 2025-11-27
**Status**: Ready to implement
