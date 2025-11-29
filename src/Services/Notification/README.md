# QuietMatch Notification Service

**Architecture:** Hexagonal (Ports & Adapters)
**Status:** MVP Complete ✓
**Purpose:** Learning demonstration of strict Hexagonal Architecture

## 🎯 Why This Service Uses Hexagonal Architecture

This service was intentionally built using **strict Hexagonal Architecture** as a learning exercise. It demonstrates:

1. **Zero Dependencies in Core** - The domain has NO external package dependencies
2. **Ports in Domain** - Interfaces (IEmailProvider, ISmsProvider) defined in Core, not Infrastructure
3. **Swappable Adapters** - Change from Console → SendGrid → SMTP without touching domain code
4. **Dependency Inversion** - Infrastructure depends on Core, never the other way around

See [`docs/40_features/f0003_notification_service_mvp/WHY_HEXAGONAL.md`](../../docs/40_features/f0003_notification_service_mvp/WHY_HEXAGONAL.md) for detailed rationale.

## 🏗️ Project Structure

```
DatingApp.NotificationService/
├── Core/                              # 🔵 DOMAIN LAYER (ZERO dependencies!)
│   ├── Domain/
│   │   ├── Entities/
│   │   │   └── Notification.cs        # Aggregate root
│   │   ├── Enums/
│   │   │   ├── NotificationChannel.cs
│   │   │   └── NotificationStatus.cs
│   │   └── ValueObjects/
│   │       ├── Recipient.cs
│   │       ├── EmailMessage.cs
│   │       └── SmsMessage.cs
│   ├── Ports/                         # ⚡ CRITICAL: Ports defined in DOMAIN!
│   │   ├── IEmailProvider.cs          # Port (not in Infrastructure!)
│   │   ├── ISmsProvider.cs            # Port (not in Infrastructure!)
│   │   ├── ITemplateProvider.cs       # Port (not in Infrastructure!)
│   │   └── INotificationLogger.cs     # Even logging is a port!
│   └── Application/
│       └── Services/
│           └── NotificationService.cs # Depends ONLY on ports
│
├── Infrastructure/                    # 🔌 ADAPTERS LAYER
│   ├── Adapters/
│   │   ├── Email/
│   │   │   └── ConsoleEmailProvider.cs      # Adapter implementing IEmailProvider
│   │   ├── Sms/
│   │   │   └── ConsoleSmsProvider.cs        # Adapter implementing ISmsProvider
│   │   ├── Templates/
│   │   │   └── FileTemplateProvider.cs      # Adapter implementing ITemplateProvider
│   │   └── Logging/
│   │       └── MicrosoftLoggerAdapter.cs    # Adapter implementing INotificationLogger
│   └── Templates/
│       ├── WelcomeEmail.html
│       └── ProfileCompletedEmail.html
│
├── Api/                               # 🌐 API LAYER
│   └── Program.cs                     # DI configuration wiring ports to adapters
│
├── Tests.Unit/                        # 🧪 UNIT TESTS
└── Tests.Integration/                 # 🧪 INTEGRATION TESTS
```

## 🔄 Adapter Swapping Demo

This is the **key benefit** of Hexagonal Architecture. To swap adapters, **you only change DI configuration:**

### Current: Console Adapters (Development)

```csharp
// Program.cs - Current configuration
builder.Services.AddSingleton<IEmailProvider, ConsoleEmailProvider>();  // Outputs to console
builder.Services.AddSingleton<ISmsProvider, ConsoleSmsProvider>();      // Outputs to console
```

### Future: Production Adapters

```csharp
// Program.cs - Production configuration (when ready)
builder.Services.AddSingleton<IEmailProvider, SendGridEmailProvider>(); // Sends via SendGrid
builder.Services.AddSingleton<ISmsProvider, TwilioSmsProvider>();       // Sends via Twilio
```

### Testing: Mock Adapters

```csharp
// Unit tests
builder.Services.AddSingleton<IEmailProvider, MockEmailProvider>();     // In-memory for tests
builder.Services.AddSingleton<ISmsProvider, MockSmsProvider>();         // In-memory for tests
```

**IMPORTANT:** NotificationService.cs doesn't change! It only knows about the **ports** (interfaces), not the concrete **adapters** (implementations).

## 🚀 Running the Service

### 1. Build the solution

```bash
cd src/Services/Notification
dotnet build DatingApp.NotificationService.sln
```

### 2. Run the API

```bash
cd DatingApp.NotificationService.Api
dotnet run --urls "http://localhost:5003"
```

### 3. Test the endpoints

**Send Welcome Email:**
```bash
curl -X POST http://localhost:5003/api/notifications/welcome \
  -H "Content-Type: application/json" \
  -d '{"userId": "123e4567-e89b-12d3-a456-426614174000", "email": "john@example.com", "name": "John"}'
```

**Send Profile Completed Email:**
```bash
curl -X POST http://localhost:5003/api/notifications/profile-completed \
  -H "Content-Type: application/json" \
  -d '{"userId": "123e4567-e89b-12d3-a456-426614174000", "email": "john@example.com", "name": "John"}'
```

**Send SMS:**
```bash
curl -X POST http://localhost:5003/api/notifications/sms \
  -H "Content-Type: application/json" \
  -d '{"phoneNumber": "+12025551234", "message": "Your verification code is: 123456"}'
```

### 4. View console output

You'll see beautifully formatted email and SMS messages in the console! This demonstrates the Console adapters in action.

## 🐳 Running with Docker

### Build the Docker image

```bash
# From the Notification service directory
cd src/Services/Notification
docker build -t quietmatch-notification:latest .
```

### Run standalone container

```bash
docker run -d \
  --name notification-service \
  -p 5003:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e RabbitMQ__Host=host.docker.internal \
  quietmatch-notification:latest
```

### Run with docker-compose (Recommended)

```bash
# From the repository root
cd /path/to/QuietMatch
docker-compose up -d notification-service
```

This will:
- Start RabbitMQ (dependency)
- Build and start NotificationService
- Expose service on `http://localhost:5003`

**Check service health:**
```bash
curl http://localhost:5003/health
```

**View logs:**
```bash
docker-compose logs -f notification-service
```

**Stop services:**
```bash
docker-compose down
```

### Docker Configuration

**Multi-stage build:**
- **Build stage:** Uses `mcr.microsoft.com/dotnet/sdk:8.0` to compile the application
- **Runtime stage:** Uses `mcr.microsoft.com/dotnet/aspnet:8.0` (smaller, production-ready)

**Security features:**
- Non-root user (appuser)
- Minimal attack surface (aspnet runtime only)
- Health check endpoint

**Template handling:**
- Email templates are copied from Infrastructure/Templates/ into the Docker image
- Available at `/app/Templates` in the container
- Configured via `Templates__BasePath` environment variable

## 📋 API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | Service information |
| GET | `/health` | Health check |
| POST | `/api/notifications/welcome` | Send welcome email |
| POST | `/api/notifications/profile-completed` | Send profile completed email |
| POST | `/api/notifications/sms` | Send SMS |

Swagger UI available at: `http://localhost:5003/swagger`

## ✅ Hexagonal Architecture Checklist

- [x] **Core has ZERO dependencies** - Run `dotnet list Core/DatingApp.NotificationService.Core.csproj package`
- [x] **Ports defined in Core/Ports/** - NOT in Infrastructure
- [x] **Adapters in Infrastructure/Adapters/** - Implement ports
- [x] **Application service depends ONLY on ports** - Never on concrete adapters
- [x] **DI configuration in API layer** - Wires ports to adapters
- [x] **Multiple adapters per port** - Console, production (future), mock (tests)
- [x] **Logging is a port** - Even ILogger is abstracted via INotificationLogger

## 🔮 Future Adapters (Phase 8)

When ready for production, we'll create:

- **SendGridEmailProvider** - Send emails via SendGrid API
- **TwilioSmsProvider** - Send SMS via Twilio API
- **RazorTemplateProvider** - Use Razor engine for templates

All without changing a single line in `NotificationService.cs`!

## 📚 Learning Resources

- [Hexagonal Architecture Explanation](../../docs/40_features/f0003_notification_service_mvp/WHY_HEXAGONAL.md)
- [Feature Specification](../../docs/40_features/f0003_notification_service_mvp/f0003_notification_service_mvp.md)
- [Implementation Plan](../../docs/40_features/f0003_notification_service_mvp/plan.md)
- [Pattern Documentation](PATTERNS.md)

## 🎓 Key Learning Points

1. **Ports are in the Domain** - This is what makes it "Hexagonal". The domain defines what it needs (ports), and infrastructure provides it (adapters).

2. **Zero External Dependencies in Core** - Strictest interpretation of Hexagonal. Even logging is abstracted.

3. **Swappable Without Code Changes** - Change DI configuration, not domain code, to swap providers.

4. **Testing is Trivial** - Mock adapters implement the same ports as production adapters.

---

**Architecture Pattern:** Hexagonal (Ports & Adapters)
**Status:** ✅ All Phases Complete (Phases 0-7)
**Implemented:** Domain, Application, Adapters, Events, DI, Demo, Unit Tests, Docker
**Optional Next:** Production adapters (SendGrid, Twilio) - Phase 8
