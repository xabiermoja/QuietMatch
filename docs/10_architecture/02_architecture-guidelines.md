# Architecture Guidelines - QuietMatch

> **The Authoritative Rulebook for QuietMatch Architecture**
>
> This document defines the architectural principles, patterns, technology choices, and implementation standards for the QuietMatch platform. Every microservice MUST follow these guidelines.

---

## Table of Contents

- [Architectural Principles](#architectural-principles)
- [Architecture Patterns](#architecture-patterns)
- [Service Communication Patterns](#service-communication-patterns)
- [Data Management](#data-management)
- [Security & Authentication](#security--authentication)
- [Technology Stack & Alternatives](#technology-stack--alternatives)
- [Local vs Azure Deployment](#local-vs-azure-deployment)
- [Testing Strategy](#testing-strategy)
- [Observability & Monitoring](#observability--monitoring)
- [Adding a New Microservice](#adding-a-new-microservice)
- [Code Quality Standards](#code-quality-standards)

---

## Architectural Principles

### 1. **Domain-Driven Design (DDD)**

**Principle**: Align software structure with business domains using ubiquitous language.

**Application**:
- Each microservice represents a **bounded context** (Identity, Profile, Matching, etc.)
- Use **domain terminology** consistently across code, APIs, database, and UI
- Identify **aggregates** (entities with clear boundaries and lifecycle)
- Define **domain events** that represent state changes

**Example**:
```csharp
// Good: Uses ubiquitous language
public class BlindDate
{
    public DateId Id { get; private set; }
    public MemberId InitiatorId { get; private set; }
    public MemberId PartnerId { get; private set; }
    public AvailabilitySlot TimeSlot { get; private set; }
    public VenueType VenueType { get; private set; }

    public void Confirm()
    {
        // Domain logic: Can only confirm if both members accepted
        if (!IsAcceptedByBoth())
            throw new DomainException("Cannot confirm date until both members accept");

        Status = DateStatus.Confirmed;
        RaiseDomainEvent(new BlindDateConfirmed(Id));
    }
}

// Bad: Technical language, anemic domain model
public class Appointment
{
    public int Id { get; set; }
    public int User1 { get; set; }
    public int User2 { get; set; }
    public DateTime Time { get; set; }
    public string Type { get; set; }
    public string Status { get; set; } // No behavior, just data
}
```

---

### 2. **Cloud-Agnostic Abstractions**

**Principle**: Design for portability between local development and cloud environments.

**Implementation**:
- **Use interfaces** for all external dependencies
- **Configuration via environment variables** (12-factor app)
- **Avoid cloud-specific SDKs** in domain/application layers

**Example**:
```csharp
// Good: Cloud-agnostic abstraction
public interface IMessagePublisher
{
    Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class;
}

// Local implementation (RabbitMQ)
public class RabbitMqMessagePublisher : IMessagePublisher { ... }

// Azure implementation (Service Bus)
public class AzureServiceBusMessagePublisher : IMessagePublisher { ... }

// Application code never knows which implementation is used
public class MatchingService
{
    private readonly IMessagePublisher _publisher;

    public async Task AcceptMatch(MatchId id)
    {
        // ... business logic ...
        await _publisher.PublishAsync(new MatchAccepted(id)); // Works locally and in Azure
    }
}
```

---

### 3. **Privacy by Design**

**Principle**: Build privacy and GDPR compliance into the architecture, not as an afterthought.

**Implementation**:
- **Minimal data collection**: Only collect what's necessary
- **Field-level encryption**: Encrypt PII at rest
- **Data sovereignty**: Each service owns its data, no shared databases
- **Audit logging**: Track all access to sensitive data
- **Right to erasure**: SAGA orchestrates deletion across all services

**Example**:
```csharp
// EF Core value converter for transparent encryption
public class EncryptedStringConverter : ValueConverter<string, string>
{
    public EncryptedStringConverter(IEncryptionService encryptionService)
        : base(
            plainText => encryptionService.Encrypt(plainText),
            cipherText => encryptionService.Decrypt(cipherText))
    {
    }
}

// Domain entity with encrypted fields
public class MemberProfile
{
    public MemberId Id { get; private set; }

    // Encrypted at rest, decrypted on read
    public string FullName { get; private set; } // Encrypted via EF Core value converter

    // Privacy by default: exposure level controls what data is shared
    public ExposureLevel ExposureLevel { get; private set; }
}
```

---

### 4. **Testability**

**Principle**: Design for testability from the start (unit, integration, API tests).

**Implementation**:
- **Dependency injection**: All dependencies injected, easy to mock
- **Pure functions**: Separate pure business logic from side effects
- **Integration tests with real dependencies**: Use Testcontainers for PostgreSQL, RabbitMQ, Redis

**Example**:
```csharp
// Testable design
public class CompatibilityScorer
{
    public CompatibilityScore Calculate(MemberProfile profile1, MemberProfile profile2)
    {
        // Pure function: No dependencies, easy to unit test
        var valuesScore = CalculateValuesAlignment(profile1.Values, profile2.Values);
        var lifestyleScore = CalculateLifestyleCompatibility(profile1.Lifestyle, profile2.Lifestyle);

        return new CompatibilityScore(valuesScore, lifestyleScore);
    }
}

// Integration test with Testcontainers
public class ProfileServiceIntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer _dbContainer;
    private RabbitMqContainer _mqContainer;

    public async Task InitializeAsync()
    {
        _dbContainer = new PostgreSqlBuilder().Build();
        _mqContainer = new RabbitMqBuilder().Build();
        await _dbContainer.StartAsync();
        await _mqContainer.StartAsync();
    }

    [Fact]
    public async Task CreateProfile_ShouldPersistAndPublishEvent()
    {
        // Test with real PostgreSQL and RabbitMQ
    }
}
```

---

### 5. **Observability**

**Principle**: Build instrumentation (logging, tracing, metrics) from day one.

**Implementation**:
- **Structured logging**: Serilog with correlation IDs
- **Distributed tracing**: OpenTelemetry
- **Health checks**: `/health` endpoint for every service
- **Metrics**: Prometheus-compatible metrics

---

## Architecture Patterns

QuietMatch uses **4 distinct architecture patterns** across its microservices to teach each pattern's strengths and use cases.

### Pattern Selection Matrix

| Service | Pattern | Why This Pattern | Folder Structure |
|---------|---------|------------------|------------------|
| IdentityService | **Layered** | Simple CRUD, clear layers | Presentation → Application → Domain → Infrastructure |
| ProfileService | **Onion** | Rich domain logic, privacy rules | Core (Domain + Application) → Infrastructure |
| MatchingService | **Hexagonal** | Swappable matching engines | Domain + Ports → Adapters |
| SchedulingService | **Layered + CQRS** | Separate read/write optimization | Presentation → Commands/Queries → Domain → Infrastructure |
| NotificationService | **Hexagonal** | Multiple delivery channels (email, push, SignalR) | Domain + Ports → Adapters |
| VerificationService | **Hexagonal** | Multiple providers (Twilio, Veriff) | Domain + Ports → Adapters |
| PaymentService | **Hexagonal** | Stripe adapter, future: other providers | Domain + Ports → Adapters |
| RealTimeService | **Layered** | SignalR hub, simple message routing | Hubs → Services → Infrastructure |
| GraphQLGateway | **Layered** | Resolvers → Service clients → External APIs | Resolvers → Application → Infrastructure |

###Layered Architecture

**When to Use**:
- Simple CRUD services
- Clear separation of concerns needed
- Straightforward business logic
- No need for swappable adapters

**Structure**:
```
DatingApp.IdentityService/
├── Presentation/           # Controllers, DTOs, API models
│   ├── Controllers/
│   ├── Models/
│   └── Validators/
├── Application/            # Use cases, application services
│   ├── Services/
│   ├── Commands/
│   └── Queries/
├── Domain/                 # Domain entities, value objects, domain services
│   ├── Entities/
│   ├── ValueObjects/
│   └── Services/
└── Infrastructure/         # Database, external APIs, repositories
    ├── Data/
    ├── Repositories/
    └── ExternalServices/
```

**Dependency Flow**: Presentation → Application → Domain ← Infrastructure (Infrastructure depends on Domain)

**Pros**:
- Simple to understand and implement
- Clear layer boundaries
- Works well for most CRUD scenarios

**Cons**:
- Can become coupling-heavy if not disciplined
- Domain layer can become anemic (just data, no behavior)
- Hard to swap implementations of external dependencies

**Alternatives Considered**:
- **Onion/Hexagonal**: Overkill for simple CRUD services like IdentityService
- **No Architecture**: Would lead to spaghetti code, hard to test

**Example (IdentityService - Layered)**:
```csharp
// Presentation/Controllers/AuthController.cs
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    [HttpPost("login/google")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        var result = await _authService.LoginWithGoogleAsync(request.IdToken);
        return Ok(result);
    }
}

// Application/Services/AuthService.cs
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IJwtTokenGenerator _tokenGenerator;

    public async Task<LoginResult> LoginWithGoogleAsync(string idToken)
    {
        var googleUser = await _googleApiClient.ValidateTokenAsync(idToken);
        var user = await _userRepo.FindByExternalIdAsync(googleUser.Sub);

        if (user == null)
        {
            user = new User(googleUser.Email, "Google", googleUser.Sub);
            await _userRepo.AddAsync(user);
        }

        var accessToken = _tokenGenerator.GenerateAccessToken(user);
        var refreshToken = _tokenGenerator.GenerateRefreshToken(user);

        return new LoginResult(accessToken, refreshToken);
    }
}

// Domain/Entities/User.cs
public class User
{
    public UserId Id { get; private set; }
    public string Email { get; private set; }
    public string Provider { get; private set; }
    public string ExternalUserId { get; private set; }

    public User(string email, string provider, string externalUserId)
    {
        Id = UserId.New();
        Email = email;
        Provider = provider;
        ExternalUserId = externalUserId;
    }
}

// Infrastructure/Data/UserRepository.cs
public class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _dbContext;

    public async Task<User> FindByExternalIdAsync(string externalId)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalUserId == externalId);
    }
}
```

---

### Onion Architecture

**When to Use**:
- Rich domain logic
- Domain should be isolated from infrastructure concerns
- Business rules are complex and change frequently

**Structure**:
```
DatingApp.ProfileService/
├── Core/                   # Domain + Application (no external dependencies)
│   ├── Domain/
│   │   ├── Entities/
│   │   ├── ValueObjects/
│   │   ├── DomainEvents/
│   │   └── Interfaces/    # Ports (IProfileRepository, etc.)
│   └── Application/
│       ├── UseCases/
│       ├── DTOs/
│       └── Services/
├── Infrastructure/         # Adapters (depends on Core)
│   ├── Data/
│   ├── Repositories/
│   └── Messaging/
└── Api/                    # Entry point (depends on Core and Infrastructure)
    ├── Controllers/
    └── Program.cs
```

**Dependency Flow**: All dependencies point **inward** to the Core (Domain layer has zero dependencies)

**Pros**:
- Domain logic isolated and testable
- Domain is the center of the design
- Business rules protected from infrastructure changes

**Cons**:
- More complex than Layered
- Requires discipline to avoid dependencies creeping into Core
- More folders and indirection

**Alternatives Considered**:
- **Layered**: Domain layer would depend on Infrastructure (coupling)
- **Hexagonal**: Similar to Onion, but Hexagonal emphasizes ports/adapters more explicitly (could work, but Onion is more domain-centric)

**Example (ProfileService - Onion)**:
```csharp
// Core/Domain/Entities/MemberProfile.cs
public class MemberProfile : AggregateRoot
{
    public MemberId Id { get; private set; }
    public string FullName { get; private set; } // Encrypted via EF Core value converter
    public PersonalityProfile Personality { get; private set; }
    public PreferenceSet Preferences { get; private set; }
    public ExposureLevel ExposureLevel { get; private set; }

    // Domain logic: Privacy rules
    public bool CanShareWith(MemberId otherId, MatchStatus matchStatus)
    {
        return ExposureLevel switch
        {
            ExposureLevel.MatchedOnly => matchStatus == MatchStatus.Accepted,
            ExposureLevel.AllMatches => matchStatus != MatchStatus.None,
            ExposureLevel.Public => true,
            _ => false
        };
    }

    public void UpdatePreferences(PreferenceSet newPreferences)
    {
        // Domain validation
        if (!newPreferences.IsValid())
            throw new DomainException("Invalid preferences");

        Preferences = newPreferences;
        RaiseDomainEvent(new ProfilePreferencesUpdated(Id, newPreferences));
    }
}

// Core/Domain/Interfaces/IProfileRepository.cs (Port)
public interface IProfileRepository
{
    Task<MemberProfile> GetByIdAsync(MemberId id);
    Task AddAsync(MemberProfile profile);
    Task UpdateAsync(MemberProfile profile);
}

// Infrastructure/Repositories/ProfileRepository.cs (Adapter)
public class ProfileRepository : IProfileRepository
{
    private readonly ProfileDbContext _dbContext;

    public async Task<MemberProfile> GetByIdAsync(MemberId id)
    {
        return await _dbContext.Profiles.FindAsync(id);
    }
}

// Core/Application/UseCases/UpdatePreferencesUseCase.cs
public class UpdatePreferencesUseCase
{
    private readonly IProfileRepository _profileRepo;
    private readonly IMessagePublisher _publisher;

    public async Task ExecuteAsync(MemberId id, PreferenceSet newPreferences)
    {
        var profile = await _profileRepo.GetByIdAsync(id);
        profile.UpdatePreferences(newPreferences); // Domain logic
        await _profileRepo.UpdateAsync(profile);

        // Publish domain events
        foreach (var domainEvent in profile.DomainEvents)
            await _publisher.PublishAsync(domainEvent);
    }
}
```

---

### Hexagonal Architecture (Ports & Adapters)

**When to Use**:
- Multiple implementations of external dependencies
- Need to swap adapters (e.g., RuleBasedMatchingEngine → EmbeddingBasedMatchingEngine)
- Testability is critical (mock ports easily)

**Structure**:
```
DatingApp.MatchingService/
├── Domain/                 # Core business logic (no dependencies)
│   ├── Entities/
│   ├── ValueObjects/
│   └── Services/
├── Ports/                  # Interfaces (contracts)
│   ├── IMatchingEngine.cs
│   ├── IMatchRepository.cs
│   └── IMessagePublisher.cs
├── Adapters/               # Implementations
│   ├── Inbound/            # Controllers, gRPC services (driving adapters)
│   │   ├── Rest/
│   │   └── Grpc/
│   └── Outbound/           # Repositories, external APIs (driven adapters)
│       ├── Data/
│       ├── Messaging/
│       └── MatchingEngines/
│           ├── RuleBasedMatchingEngine.cs
│           └── EmbeddingBasedMatchingEngine.cs (future)
└── Api/
    └── Program.cs
```

**Dependency Flow**: Domain depends on nothing. Ports define contracts. Adapters implement ports and depend on Domain.

**Pros**:
- Extremely testable (mock all ports)
- Easy to swap implementations
- Clear boundaries between domain and infrastructure

**Cons**:
- More indirection (interfaces everywhere)
- Can be overkill for simple services
- More files and folders

**Alternatives Considered**:
- **Layered**: Would tightly couple matching algorithm to infrastructure
- **Onion**: Could work, but Hexagonal makes adapter swapping more explicit

**Example (MatchingService - Hexagonal)**:
```csharp
// Ports/IMatchingEngine.cs
public interface IMatchingEngine
{
    Task<IEnumerable<MatchCandidate>> FindCandidatesAsync(MemberId memberId, int limit);
    CompatibilityScore CalculateCompatibility(MemberProfile profile1, MemberProfile profile2);
}

// Adapters/Outbound/MatchingEngines/RuleBasedMatchingEngine.cs
public class RuleBasedMatchingEngine : IMatchingEngine
{
    public async Task<IEnumerable<MatchCandidate>> FindCandidatesAsync(MemberId memberId, int limit)
    {
        // Rule-based logic: Filter by age, location, deal-breakers
        // Score by values alignment, lifestyle compatibility
    }

    public CompatibilityScore CalculateCompatibility(MemberProfile p1, MemberProfile p2)
    {
        var valuesScore = CalculateValuesAlignment(p1.Values, p2.Values);
        var lifestyleScore = CalculateLifestyleCompatibility(p1.Lifestyle, p2.Lifestyle);
        return new CompatibilityScore(valuesScore, lifestyleScore);
    }
}

// Adapters/Outbound/MatchingEngines/EmbeddingBasedMatchingEngine.cs (Future)
public class EmbeddingBasedMatchingEngine : IMatchingEngine
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorSearchRepository _vectorRepo;

    public async Task<IEnumerable<MatchCandidate>> FindCandidatesAsync(MemberId memberId, int limit)
    {
        var profileEmbedding = await _embeddingService.GenerateEmbeddingAsync(memberId);
        var similarProfiles = await _vectorRepo.FindSimilarAsync(profileEmbedding, limit);
        return similarProfiles;
    }
}

// Domain/Services/MatchingService.cs
public class MatchingService
{
    private readonly IMatchingEngine _matchingEngine; // Port injected

    public async Task<IEnumerable<MatchSuggestion>> GenerateMatchesAsync(MemberId memberId)
    {
        var candidates = await _matchingEngine.FindCandidatesAsync(memberId, 10);

        // Domain logic: Filter, rank, create match suggestions
        return candidates
            .Where(c => MeetsDealBreakers(c))
            .OrderByDescending(c => c.CompatibilityScore)
            .Take(3)
            .Select(c => new MatchSuggestion(memberId, c.CandidateId, c.CompatibilityScore));
    }
}

// Dependency injection in Program.cs
services.AddScoped<IMatchingEngine, RuleBasedMatchingEngine>(); // Easy to swap to EmbeddingBasedMatchingEngine later
```

---

### CQRS (Command Query Responsibility Segregation)

**When to Use**:
- Read and write operations have different optimization needs
- High read/write ratio (many reads, few writes)
- Event sourcing is used

**Structure**:
```
DatingApp.SchedulingService/
├── Application/
│   ├── Commands/           # Write operations (CreateAvailabilitySlot, ScheduleBlindDate)
│   │   ├── CreateAvailabilitySlotCommand.cs
│   │   ├── CreateAvailabilitySlotHandler.cs
│   │   └── CreateAvailabilitySlotValidator.cs
│   ├── Queries/            # Read operations (GetAvailableSlots, GetUpcomingDates)
│   │   ├── GetAvailableSlotsQuery.cs
│   │   ├── GetAvailableSlotsHandler.cs
│   │   └── AvailabilitySlotDto.cs
│   └── Services/
├── Domain/
│   ├── Entities/
│   └── ValueObjects/
└── Infrastructure/
    ├── Data/
    │   ├── WriteModel/     # Optimized for writes (normalized)
    │   └── ReadModel/      # Optimized for reads (denormalized, materialized views)
    └── Repositories/
```

**Dependency Flow**: Commands/Queries → Domain → Infrastructure

**Pros**:
- Optimized read and write models
- Scalability (scale reads and writes independently)
- Clear separation of concerns

**Cons**:
- More complex (two models to maintain)
- Eventual consistency between read/write models
- Overkill for simple CRUD

**Alternatives Considered**:
- **Layered without CQRS**: Would work, but less scalable for read-heavy queries
- **Event Sourcing + CQRS**: More complex, not needed for MVP (future consideration)

**Example (SchedulingService - CQRS)**:
```csharp
// Application/Commands/CreateAvailabilitySlotCommand.cs
public record CreateAvailabilitySlotCommand(
    MemberId MemberId,
    DateTime StartTime,
    DateTime EndTime,
    VenueType PreferredVenueType);

// Application/Commands/CreateAvailabilitySlotHandler.cs
public class CreateAvailabilitySlotHandler : IRequestHandler<CreateAvailabilitySlotCommand, AvailabilitySlotId>
{
    private readonly IAvailabilityRepository _repository;
    private readonly IMessagePublisher _publisher;

    public async Task<AvailabilitySlotId> Handle(CreateAvailabilitySlotCommand command, CancellationToken ct)
    {
        var slot = new AvailabilitySlot(command.MemberId, command.StartTime, command.EndTime, command.PreferredVenueType);
        await _repository.AddAsync(slot);

        await _publisher.PublishAsync(new AvailabilitySlotCreated(slot.Id, slot.MemberId));
        return slot.Id;
    }
}

// Application/Queries/GetAvailableSlotsQuery.cs
public record GetAvailableSlotsQuery(MemberId MemberId, DateTime StartDate, DateTime EndDate);

// Application/Queries/GetAvailableSlotsHandler.cs
public class GetAvailableSlotsHandler : IRequestHandler<GetAvailableSlotsQuery, IEnumerable<AvailabilitySlotDto>>
{
    private readonly IAvailabilityReadRepository _readRepository; // Separate read repository

    public async Task<IEnumerable<AvailabilitySlotDto>> Handle(GetAvailableSlotsQuery query, CancellationToken ct)
    {
        // Query optimized read model (could be materialized view, Redis cache, etc.)
        return await _readRepository.GetAvailableSlotsAsync(query.MemberId, query.StartDate, query.EndDate);
    }
}

// API Controller uses MediatR
[ApiController]
[Route("api/availability")]
public class AvailabilityController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpPost]
    public async Task<IActionResult> CreateSlot([FromBody] CreateAvailabilitySlotRequest request)
    {
        var command = new CreateAvailabilitySlotCommand(request.MemberId, request.StartTime, request.EndTime, request.VenueType);
        var slotId = await _mediator.Send(command); // Command
        return Ok(slotId);
    }

    [HttpGet]
    public async Task<IActionResult> GetAvailableSlots([FromQuery] GetAvailableSlotsRequest request)
    {
        var query = new GetAvailableSlotsQuery(request.MemberId, request.StartDate, request.EndDate);
        var slots = await _mediator.Send(query); // Query
        return Ok(slots);
    }
}
```

---

## Service Communication Patterns

### Synchronous Communication

#### REST APIs (External Clients)

**When to Use**: Web app, mobile app, external integrations

**Technology**: ASP.NET Core Web API

**Standards**:
- **Versioning**: URL versioning (`/api/v1/profiles`)
- **Authentication**: JWT in `Authorization: Bearer {token}` header
- **Error Handling**: RFC 7807 Problem Details
- **Documentation**: Swagger/Scalar auto-generated

**Example**:
```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/profiles")]
[Authorize] // JWT required
public class ProfilesController : ControllerBase
{
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(Guid id)
    {
        var profile = await _profileService.GetByIdAsync(new MemberId(id));
        if (profile == null)
            return NotFound(new ProblemDetails { Title = "Profile not found" });

        return Ok(profile);
    }
}
```

#### gRPC (Internal Service-to-Service)

**When to Use**: High-performance internal APIs, microservice-to-microservice calls

**Technology**: gRPC .NET

**Pros**:
- 7-10x faster than REST (binary protocol buffers)
- Strongly typed contracts (.proto files)
- Streaming support

**Cons**:
- Not browser-friendly (needs gRPC-Web)
- More complex debugging

**Alternatives Considered**:
- **REST**: Easier to debug, but slower
- **Message Bus Only**: Would make synchronous queries impossible (some queries need immediate responses)

**Example**:
```protobuf
// Protos/matching.proto
syntax = "proto3";

option csharp_namespace = "QuietMatch.Matching.Grpc";

service MatchingService {
    rpc GetCompatibilityScore(CompatibilityRequest) returns (CompatibilityResponse);
}

message CompatibilityRequest {
    string member_id_1 = 1;
    string member_id_2 = 2;
}

message CompatibilityResponse {
    double compatibility_score = 1;
    map<string, double> breakdown = 2; // e.g., "values": 0.92, "lifestyle": 0.84
}
```

```csharp
// Server implementation
public class MatchingGrpcService : Matching.MatchingServiceBase
{
    private readonly IMatchingEngine _engine;

    public override async Task<CompatibilityResponse> GetCompatibilityScore(
        CompatibilityRequest request, ServerCallContext context)
    {
        var profile1 = await _profileClient.GetProfileAsync(request.MemberId1);
        var profile2 = await _profileClient.GetProfileAsync(request.MemberId2);

        var score = _engine.CalculateCompatibility(profile1, profile2);

        return new CompatibilityResponse
        {
            CompatibilityScore = score.Overall,
            Breakdown = { { "values", score.Values }, { "lifestyle", score.Lifestyle } }
        };
    }
}

// Client usage (from SchedulingService calling MatchingService)
public class BlindDateOrchestrator
{
    private readonly Matching.MatchingServiceClient _matchingClient;

    public async Task<bool> VerifyCompatibility(MemberId id1, MemberId id2)
    {
        var response = await _matchingClient.GetCompatibilityScoreAsync(
            new CompatibilityRequest { MemberId1 = id1.Value.ToString(), MemberId2 = id2.Value.ToString() });

        return response.CompatibilityScore >= 0.7; // Threshold
    }
}
```

---

### Asynchronous Communication (Message Bus)

**When to Use**: Event notifications, long-running workflows, decoupled services

**Technology**: RabbitMQ (local) → Azure Service Bus (cloud), abstracted by MassTransit

**Patterns**:
- **Publish/Subscribe**: One event, multiple subscribers (e.g., `MatchAccepted` → Scheduling, Notifications, Analytics all subscribe)
- **Point-to-Point**: One command, one handler (e.g., `SendEmail` command)

**Message Types**:
- **Events**: Something happened (past tense): `MatchAccepted`, `ProfileUpdated`, `BlindDateScheduled`
- **Commands**: Do something (imperative): `SendNotification`, `CreateBlindDate`

**Technology Alternatives**:

| Technology | Pros | Cons | Decision |
|------------|------|------|----------|
| **RabbitMQ** | Easy local setup, mature, well-documented | Requires management, not cloud-native | **Use for local dev** |
| **Azure Service Bus** | Fully managed, cloud-native, FIFO guarantees | Expensive, no local emulator | **Use for Azure production** |
| **Kafka** | High throughput, event sourcing-friendly | Complex setup, overkill for MVP | **Not needed for MVP** |
| **In-Memory (MediatR only)** | Simplest, no external dependency | No durability, not distributed | **Only for single-service dev** |
| **AWS SQS/SNS** | Fully managed | Vendor lock-in (we're targeting Azure) | **Not using** |

**Decision**: Use **MassTransit** to abstract RabbitMQ (local) and Azure Service Bus (cloud), enabling seamless migration.

**Example (Event Publishing)**:
```csharp
// Domain event
public record MatchAccepted(MatchId MatchId, MemberId InitiatorId, MemberId PartnerId, DateTime AcceptedAt);

// Publishing (from MatchingService)
public class MatchingService
{
    private readonly IMessagePublisher _publisher;

    public async Task AcceptMatchAsync(MatchId matchId)
    {
        var match = await _matchRepo.GetByIdAsync(matchId);
        match.Accept(); // Domain logic

        await _matchRepo.UpdateAsync(match);
        await _publisher.PublishAsync(new MatchAccepted(matchId, match.InitiatorId, match.PartnerId, DateTime.UtcNow));
    }
}

// Consuming (in SchedulingService)
public class MatchAcceptedConsumer : IConsumer<MatchAccepted>
{
    private readonly IMediator _mediator;

    public async Task Consume(ConsumeContext<MatchAccepted> context)
    {
        var evt = context.Message;

        // Start Blind Date Creation SAGA
        await _mediator.Send(new StartBlindDateSagaCommand(evt.MatchId, evt.InitiatorId, evt.PartnerId));
    }
}

// Consuming (in NotificationService)
public class MatchAcceptedNotificationConsumer : IConsumer<MatchAccepted>
{
    private readonly INotificationSender _sender;

    public async Task Consume(ConsumeContext<MatchAccepted> context)
    {
        // Send notification to both members
        await _sender.SendAsync(new MatchAcceptedNotification(context.Message.InitiatorId, context.Message.PartnerId));
    }
}
```

**Outbox Pattern** (Transactional Messaging):
```csharp
// Ensures event publishing and DB write are atomic
public class OutboxMessage
{
    public Guid Id { get; set; }
    public string EventType { get; set; }
    public string Payload { get; set; } // JSON serialized event
    public DateTime CreatedAt { get; set; }
    public bool Published { get; set; }
}

// Save to DB in same transaction as domain entity
public async Task AcceptMatchAsync(MatchId matchId)
{
    using var transaction = await _dbContext.Database.BeginTransactionAsync();

    var match = await _matchRepo.GetByIdAsync(matchId);
    match.Accept();
    await _matchRepo.UpdateAsync(match);

    var outboxMessage = new OutboxMessage
    {
        EventType = nameof(MatchAccepted),
        Payload = JsonSerializer.Serialize(new MatchAccepted(matchId, match.InitiatorId, match.PartnerId, DateTime.UtcNow)),
        CreatedAt = DateTime.UtcNow,
        Published = false
    };
    _dbContext.OutboxMessages.Add(outboxMessage);

    await _dbContext.SaveChangesAsync();
    await transaction.CommitAsync();
}

// Background service publishes outbox messages
public class OutboxPublisher : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var unpublished = await _dbContext.OutboxMessages.Where(m => !m.Published).ToListAsync();
            foreach (var message in unpublished)
            {
                await _publisher.PublishRawAsync(message.EventType, message.Payload);
                message.Published = true;
            }
            await _dbContext.SaveChangesAsync();
            await Task.Delay(1000, stoppingToken); // Poll every second
        }
    }
}
```

---

## Data Management

### Database per Service

**Principle**: Each microservice owns its database schema. No shared databases.

**Why**:
- **Data sovereignty**: Service owns its data lifecycle
- **Independent deployment**: Schema changes don't affect other services
- **Technology flexibility**: Could use PostgreSQL for one service, MongoDB for another (though we're standardizing on PostgreSQL)

**Implementation**:
- **Local**: Single PostgreSQL container, multiple databases (`identity_db`, `profile_db`, `matching_db`, etc.)
- **Azure**: Separate Azure Database for PostgreSQL Flexible Server instances per service (or shared server with separate databases for cost savings)

**Cross-Service Data Access**:
- **Never query another service's database directly**
- Use **API calls** (REST/gRPC) or **events** (async messaging)

**Example**:
```sql
-- init-db.sql (runs on PostgreSQL container startup)
CREATE DATABASE identity_db;
CREATE DATABASE profile_db;
CREATE DATABASE matching_db;
CREATE DATABASE scheduling_db;
CREATE DATABASE notification_db;
CREATE DATABASE verification_db;
CREATE DATABASE payment_db;
CREATE DATABASE analytics_db;
```

---

### SAGA Pattern (Distributed Transactions)

**Problem**: Microservices need atomicity across multiple services (e.g., "accept match" → "create blind date" → "send notifications" must all succeed or all rollback).

**Solution**: SAGA pattern with compensating transactions.

**Pattern Choice**: **Orchestration-based SAGA** (centralized coordinator)

**Why Orchestration vs. Choreography**:

| Orchestration | Choreography |
|---------------|--------------|
| Centralized coordinator (SAGA state machine) | Decentralized (services react to events) |
| Easier to understand and debug | More complex, harder to trace |
| Single point of control | No single point of failure |
| **Our choice for learning clarity** | Better for advanced scenarios |

**Technology Alternatives**:

| Technology | Pros | Cons | Decision |
|------------|------|------|----------|
| **MassTransit SAGA** | Built-in support, state persistence, timeout handling | Learning curve, opinionated | **Use this** (industry standard) |
| **NServiceBus** | Mature, excellent SAGA support | Commercial license ($$$) | **Not using** (cost) |
| **Custom Implementation** | Full control, educational | Reinventing the wheel, error-prone | **Not using** (too risky) |

**Example (Blind Date Creation SAGA)**:
```csharp
// SAGA State
public class BlindDateSagaState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; }

    public MatchId MatchId { get; set; }
    public MemberId InitiatorId { get; set; }
    public MemberId PartnerId { get; set; }
    public AvailabilitySlotId? ReservedSlotId { get; set; }
    public BlindDateId? BlindDateId { get; set; }
}

// SAGA State Machine
public class BlindDateSaga : MassTransitStateMachine<BlindDateSagaState>
{
    public BlindDateSaga()
    {
        InstanceState(x => x.CurrentState);

        Event(() => MatchAccepted);
        Event(() => SlotReserved);
        Event(() => SlotReservationFailed);
        Event(() => NotificationsSent);

        Initially(
            When(MatchAccepted)
                .Then(context => {
                    context.Instance.MatchId = context.Data.MatchId;
                    context.Instance.InitiatorId = context.Data.InitiatorId;
                    context.Instance.PartnerId = context.Data.PartnerId;
                })
                .PublishAsync(context => context.Init<ReserveAvailabilitySlot>(new {
                    context.Instance.InitiatorId,
                    context.Instance.PartnerId
                }))
                .TransitionTo(AwaitingSlotReservation));

        During(AwaitingSlotReservation,
            When(SlotReserved)
                .Then(context => context.Instance.ReservedSlotId = context.Data.SlotId)
                .PublishAsync(context => context.Init<SendBlindDateNotifications>(new {
                    context.Instance.InitiatorId,
                    context.Instance.PartnerId,
                    context.Instance.ReservedSlotId
                }))
                .TransitionTo(AwaitingNotifications),

            When(SlotReservationFailed)
                .Then(context => Console.WriteLine("Slot reservation failed, compensating..."))
                .PublishAsync(context => context.Init<MarkMatchAsPending>(new {
                    context.Instance.MatchId
                }))
                .Finalize());

        During(AwaitingNotifications,
            When(NotificationsSent)
                .PublishAsync(context => context.Init<BlindDateConfirmed>(new {
                    context.Instance.BlindDateId,
                    context.Instance.InitiatorId,
                    context.Instance.PartnerId
                }))
                .Finalize());

        SetCompletedWhenFinalized();
    }

    public State AwaitingSlotReservation { get; private set; }
    public State AwaitingNotifications { get; private set; }

    public Event<MatchAccepted> MatchAccepted { get; private set; }
    public Event<SlotReserved> SlotReserved { get; private set; }
    public Event<SlotReservationFailed> SlotReservationFailed { get; private set; }
    public Event<NotificationsSent> NotificationsSent { get; private set; }
}
```

---

## Security & Authentication

### Custom IdentityService (Not Duende IdentityServer)

**Decision**: Build a custom OAuth2/OIDC provider instead of using Duende IdentityServer.

**Why**:
- **Learning**: Build OAuth2 flows from scratch to deeply understand them
- **Cost**: Duende requires commercial license for production ($$$$)
- **Simplicity**: QuietMatch only needs social login (no complex flows like SAML, federation)

**Trade-Offs**:
- **Pro**: Full control, no licensing costs, educational
- **Con**: More work to build, must ensure security correctness
- **Mitigation**: Use well-tested libraries (AspNet.Security.OAuth.Google, System.IdentityModel.Tokens.Jwt), follow OAuth2 RFCs

**Implementation**:
```csharp
// IdentityService handles:
// 1. Social login (Google, Apple Sign-In)
// 2. JWT issuance (access tokens)
// 3. Refresh token issuance
// 4. Token validation endpoint

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    [HttpPost("login/google")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        // 1. Validate Google ID token
        var googleUser = await _googleAuthService.ValidateIdTokenAsync(request.IdToken);

        // 2. Find or create local user
        var user = await _userRepository.FindByExternalIdAsync("Google", googleUser.Sub)
                   ?? await _userRepository.CreateAsync(new User(googleUser.Email, "Google", googleUser.Sub));

        // 3. Generate JWT access token (15 min expiry)
        var accessToken = _jwtGenerator.GenerateAccessToken(user);

        // 4. Generate refresh token (7 days expiry)
        var refreshToken = await _refreshTokenService.CreateAsync(user.Id);

        return Ok(new LoginResponse(accessToken, refreshToken));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var refreshToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken);
        if (refreshToken == null || refreshToken.IsExpired())
            return Unauthorized();

        var user = await _userRepository.GetByIdAsync(refreshToken.UserId);
        var newAccessToken = _jwtGenerator.GenerateAccessToken(user);

        return Ok(new RefreshTokenResponse(newAccessToken));
    }
}
```

### JWT Authentication Across All Services

**Requirement**: Every API endpoint (REST, gRPC, GraphQL) MUST validate JWT.

**Implementation**:
```csharp
// Shared BuildingBlocks library: JwtAuthenticationExtensions.cs
public static class JwtAuthenticationExtensions
{
    public static IServiceCollection AddQuietMatchJwtAuthentication(this IServiceCollection services, IConfiguration config)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = config["Jwt:Issuer"],
                    ValidAudience = config["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:SecretKey"]))
                };
            });

        return services;
    }
}

// Usage in every microservice
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddQuietMatchJwtAuthentication(builder.Configuration); // Shared method

        var app = builder.Build();

        app.UseAuthentication(); // Must come before UseAuthorization
        app.UseAuthorization();

        app.MapControllers();
        app.Run();
    }
}
```

---

## Technology Stack & Alternatives

### Backend Framework

**Choice**: **.NET 8** (LTS)

**Alternatives Considered**:
- **.NET 9**: Latest features, but shorter support (18 months vs. 3 years for .NET 8)
- **.NET 6**: Still supported, but missing newer features (minimal APIs improvements, C# 12)
- **Java Spring Boot**: Mature, but team prefers .NET ecosystem
- **Node.js/NestJS**: Good for microservices, but .NET has better gRPC/performance

**Decision**: .NET 8 for long-term support and team familiarity.

---

### ORM (Object-Relational Mapping)

**Choice**: **Entity Framework Core 9**

**Alternatives Considered**:
- **Dapper**: Faster (micro-ORM), but requires manual SQL (less productive)
- **NHibernate**: Mature, but less .NET Core friendly
- **Raw ADO.NET**: Maximum control, but very verbose

**Decision**: EF Core for productivity, migrations support, and strong typing.

---

### Messaging Library

**Choice**: **MassTransit**

**Alternatives Considered**:
- **NServiceBus**: Excellent, but commercial license
- **Raw RabbitMQ Client**: Full control, but no abstractions (hard to migrate to Azure Service Bus)
- **CAP (DotNetCore.CAP)**: Good for outbox pattern, but less mature than MassTransit

**Decision**: MassTransit for RabbitMQ/Azure Service Bus abstraction and SAGA support.

---

### Frontend

**Choice**: **Blazor Web App (.NET 8)** with SSR + Interactive components

**Alternatives Considered**:
- **React**: More popular, larger ecosystem, but separate tech stack (JavaScript/TypeScript)
- **Blazor WebAssembly**: Fully client-side, but larger download size, poor SEO
- **Vue.js**: Simpler than React, but still JavaScript-based
- **Angular**: Enterprise-grade, but heavier and separate stack

**Decision**: Blazor Web App for:
- **Code sharing**: Same C# code for web and mobile (.NET MAUI Blazor Hybrid)
- **SEO**: Server-side rendering for search engines
- **Performance**: Interactive components only where needed (hybrid rendering)
- **Team skill reuse**: Same language (C#) across full stack

---

## Local vs Azure Deployment

### Local Development Strategy

**Goal**: Run entire system locally with Docker Compose.

**Components**:
```yaml
# docker-compose.yml
version: '3.8'
services:
  postgres:
    image: postgres:16
    ports:
      - "5432:5432"
    environment:
      POSTGRES_USER: admin
      POSTGRES_PASSWORD: SecurePassword123!
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./scripts/init-db.sql:/docker-entrypoint-initdb.d/init-db.sql

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data

  rabbitmq:
    image: rabbitmq:3-management-alpine
    ports:
      - "5672:5672"   # AMQP
      - "15672:15672" # Management UI
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq

  seq:
    image: datalust/seq:latest
    ports:
      - "5341:80"
    environment:
      ACCEPT_EULA: Y

  identity-service:
    build: ./src/Services/Identity
    ports:
      - "5001:80"
    environment:
      - ConnectionStrings__IdentityDb=Host=postgres;Database=identity_db;Username=admin;Password=SecurePassword123!
      - Redis__ConnectionString=redis:6379
      - MessageBroker__Host=rabbitmq
      - Jwt__SecretKey=${JWT_SECRET_KEY}
    depends_on:
      - postgres
      - redis
      - rabbitmq

  # ... other services ...
```

**Pros**:
- Fast development cycle
- No cloud costs during development
- Works offline
- Easy debugging

**Cons**:
- Resource-intensive (multiple containers)
- Doesn't perfectly match cloud environment

---

### Azure Cloud Strategy

**Goal**: Seamless migration from local to Azure with minimal code changes.

**Mapping**:

| Local | Azure | Migration Strategy |
|-------|-------|-------------------|
| Docker Compose | **Azure Container Apps (ACA)** | Deploy containers to ACA, use managed identities |
| RabbitMQ | **Azure Service Bus** | Swap implementation via DI: `services.AddScoped<IMessagePublisher, AzureServiceBusPublisher>()` |
| PostgreSQL (Docker) | **Azure Database for PostgreSQL - Flexible Server** | Update connection string in app config |
| Redis (Docker) | **Azure Cache for Redis** | Update connection string, no code changes |
| Seq | **Application Insights** | Swap logging sink: Serilog → Application Insights sink |
| JWT secret (env var) | **Azure Key Vault** | Use `Azure.Identity` to fetch secrets |
| Volume mounts | **Azure Files** or **Azure Blob Storage** | Update file storage abstraction |

**Infrastructure as Code (Bicep)**:
```bicep
// main.bicep
param location string = resourceGroup().location

// PostgreSQL Flexible Server
resource postgresServer 'Microsoft.DBforPostgreSQL/flexibleServers@2022-12-01' = {
  name: 'quietmatch-postgres'
  location: location
  sku: {
    name: 'Standard_B1ms'
    tier: 'Burstable'
  }
  properties: {
    version: '16'
    administratorLogin: 'adminuser'
    administratorLoginPassword: keyVaultSecretReference
  }
}

// Azure Service Bus
resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-01-01-preview' = {
  name: 'quietmatch-servicebus'
  location: location
  sku: {
    name: 'Standard'
  }
}

// Azure Cache for Redis
resource redisCache 'Microsoft.Cache/redis@2023-04-01' = {
  name: 'quietmatch-redis'
  location: location
  properties: {
    sku: {
      name: 'Basic'
      family: 'C'
      capacity: 0
    }
  }
}

// Azure Container Apps Environment
resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: 'quietmatch-env'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsWorkspace.properties.customerId
        sharedKey: logAnalyticsWorkspace.listKeys().primarySharedKey
      }
    }
  }
}

// Container App (Identity Service)
resource identityServiceApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'identity-service'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      ingress: {
        external: true
        targetPort: 80
      }
      secrets: [
        {
          name: 'jwt-secret'
          keyVaultUrl: keyVault::jwtSecret.properties.secretUri
          identity: 'system'
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'identity-service'
          image: 'quietmatch.azurecr.io/identity-service:latest'
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            {
              name: 'ConnectionStrings__IdentityDb'
              value: 'Host=${postgresServer.properties.fullyQualifiedDomainName};Database=identity_db;...'
            }
            {
              name: 'Redis__ConnectionString'
              value: '${redisCache.properties.hostName}:6380,password=${redisCache.listKeys().primaryKey},ssl=True'
            }
            {
              name: 'MessageBroker__ConnectionString'
              value: serviceBusNamespace.listKeys().primaryConnectionString
            }
            {
              name: 'Jwt__SecretKey'
              secretRef: 'jwt-secret'
            }
          ]
        }
      ]
    }
  }
}
```

**Deployment Command**:
```bash
# Deploy infrastructure
az deployment group create \
  --resource-group quietmatch-rg \
  --template-file main.bicep

# Build and push Docker images
docker build -t quietmatch.azurecr.io/identity-service:latest ./src/Services/Identity
az acr login --name quietmatch
docker push quietmatch.azurecr.io/identity-service:latest

# Update container app with new image
az containerapp update \
  --name identity-service \
  --resource-group quietmatch-rg \
  --image quietmatch.azurecr.io/identity-service:latest
```

---

## Testing Strategy

### Unit Tests

**Framework**: xUnit

**Coverage**: Domain logic, application services (business rules)

**Example**:
```csharp
public class CompatibilityScorerTests
{
    [Fact]
    public void Calculate_WhenValuesAlign_ShouldReturnHighScore()
    {
        // Arrange
        var profile1 = new MemberProfile { Values = new Values { FamilyOriented = 5, CareerDriven = 3 } };
        var profile2 = new MemberProfile { Values = new Values { FamilyOriented = 5, CareerDriven = 4 } };
        var scorer = new CompatibilityScorer();

        // Act
        var score = scorer.Calculate(profile1, profile2);

        // Assert
        Assert.True(score.Overall >= 0.8);
    }
}
```

---

### Integration Tests

**Framework**: xUnit + WebApplicationFactory + Testcontainers

**Coverage**: Database interactions, message publishing, external APIs

**Example**:
```csharp
public class ProfileServiceIntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer _dbContainer;
    private WebApplicationFactory<Program> _factory;

    public async Task InitializeAsync()
    {
        _dbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .Build();
        await _dbContainer.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddDbContext<ProfileDbContext>(options =>
                        options.UseNpgsql(_dbContainer.GetConnectionString()));
                });
            });
    }

    [Fact]
    public async Task CreateProfile_ShouldPersistToDatabase()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new CreateProfileRequest { FullName = "John Doe", ... };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/profiles", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var profileId = await response.Content.ReadFromJsonAsync<Guid>();
        Assert.NotEqual(Guid.Empty, profileId);

        // Verify in database
        var dbContext = _factory.Services.GetRequiredService<ProfileDbContext>();
        var profile = await dbContext.Profiles.FindAsync(profileId);
        Assert.NotNull(profile);
        Assert.Equal("John Doe", profile.FullName);
    }

    public async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
        await _factory.DisposeAsync();
    }
}
```

---

## Observability & Monitoring

### Structured Logging

**Library**: Serilog

**Sinks**:
- **Local**: Seq (web UI for structured logs)
- **Azure**: Application Insights

**Configuration**:
```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.WithProperty("ServiceName", "IdentityService")
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .Enrich.FromLogContext() // Adds correlation IDs
    .WriteTo.Console()
    .WriteTo.Seq("http://seq:5341") // Local
    .WriteTo.ApplicationInsights(telemetryConfiguration, TelemetryConverter.Traces) // Azure
    .CreateLogger();

builder.Host.UseSerilog();
```

**Usage**:
```csharp
public class AuthService
{
    private readonly ILogger<AuthService> _logger;

    public async Task<LoginResult> LoginWithGoogleAsync(string idToken)
    {
        using (_logger.BeginScope(new Dictionary<string, object> { ["IdTokenHash"] = idToken.GetHashCode() }))
        {
            _logger.LogInformation("Starting Google login for user");

            try
            {
                var result = await _googleAuthService.ValidateIdTokenAsync(idToken);
                _logger.LogInformation("Google login successful for {Email}", result.Email);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Google login failed");
                throw;
            }
        }
    }
}
```

---

### Distributed Tracing

**Library**: OpenTelemetry

**Implementation**:
```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddNpgsql() // PostgreSQL instrumentation
            .AddSource("MassTransit") // RabbitMQ/Azure Service Bus
            .AddJaegerExporter(options => // Local: Jaeger
            {
                options.AgentHost = "jaeger";
                options.AgentPort = 6831;
            })
            .AddAzureMonitorTraceExporter(options => // Azure: Application Insights
            {
                options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
            });
    });
```

---

### Health Checks

**Every service MUST expose `/health` endpoint**:

```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("ProfileDb"), name: "postgres")
    .AddRedis(builder.Configuration["Redis:ConnectionString"], name: "redis")
    .AddRabbitMQ(builder.Configuration["MessageBroker:ConnectionString"], name: "rabbitmq");

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse // JSON response
});
```

---

## Adding a New Microservice

### Checklist

- [ ] **1. Choose Architecture Pattern** (Layered, Onion, Hexagonal, CQRS)
- [ ] **2. Create Folder Structure** (see templates in `03_service-templates.md`)
- [ ] **3. Create Database** (`CREATE DATABASE new_service_db;` in `init-db.sql`)
- [ ] **4. Add to Docker Compose** (`docker-compose.yml`)
- [ ] **5. Implement Domain Layer** (entities, value objects, domain services)
- [ ] **6. Implement Application Layer** (use cases, commands/queries if CQRS)
- [ ] **7. Implement Infrastructure Layer** (EF Core DbContext, repositories, messaging)
- [ ] **8. Implement API Layer** (controllers or gRPC services)
- [ ] **9. Add JWT Authentication** (call `AddQuietMatchJwtAuthentication()`)
- [ ] **10. Add Logging** (Serilog with Seq/App Insights sinks)
- [ ] **11. Add Health Checks** (`/health` endpoint)
- [ ] **12. Add OpenTelemetry Tracing**
- [ ] **13. Write Unit Tests** (domain logic, application services)
- [ ] **14. Write Integration Tests** (Testcontainers for real dependencies)
- [ ] **15. Write API Tests** (WebApplicationFactory)
- [ ] **16. Document in `docs/30_microservices/`**
- [ ] **17. Create `PATTERNS.md`** in service folder (explain pattern, why, how, alternatives)

---

## Code Quality Standards

### Naming Conventions

- **Use ubiquitous language**: `BlindDate`, `MatchCandidate`, `ExposureLevel` (not `Appointment`, `Suggestion`, `PrivacyLevel`)
- **PascalCase**: Classes, methods, properties
- **camelCase**: Local variables, parameters
- **SCREAMING_SNAKE_CASE**: Constants

### Comments

**When to comment**:
- Architectural decisions
- Complex business logic
- GDPR/privacy reasoning
- Technology choices
- Non-obvious code

**Example**:
```csharp
// Hexagonal Architecture: IMatchingEngine is a port, allowing swappable adapters.
// Current: RuleBasedMatchingEngine
// Future: EmbeddingBasedMatchingEngine (AI-powered personality matching)
public interface IMatchingEngine
{
    Task<IEnumerable<MatchCandidate>> FindCandidatesAsync(MemberId memberId, int limit);
}

// GDPR Article 17: Right to erasure
// Soft-delete profile but retain anonymized match data for 30 days
// for fraud detection, then hard-delete via background job.
public async Task DeleteProfileAsync(MemberId id)
{
    var profile = await _profileRepo.GetByIdAsync(id);
    profile.SoftDelete(); // Sets DeletedAt timestamp
    await _profileRepo.UpdateAsync(profile);

    // Schedule hard delete after 30 days
    await _scheduler.ScheduleAsync(new HardDeleteProfileJob(id), TimeSpan.FromDays(30));
}
```

### Configuration & Secrets Best Practices

#### 🔒 Secrets Management (CRITICAL)

**NEVER commit the following to source control**:
- ❌ API keys (Google, Stripe, Twilio, etc.)
- ❌ Database passwords
- ❌ JWT secret keys
- ❌ Encryption keys
- ❌ OAuth client secrets
- ❌ Connection strings with credentials
- ❌ Service account credentials

**✅ Correct Approach**:

**Local Development**:
```csharp
// Use .env file (add to .gitignore)
// Load in Program.cs
builder.Configuration.AddEnvironmentVariables();

// Access in code
var jwtSecret = builder.Configuration["Jwt:SecretKey"];
var googleClientId = builder.Configuration["Google:ClientId"];
```

**Azure Production**:
```csharp
// Use Azure Key Vault with Managed Identity
if (!builder.Environment.IsDevelopment())
{
    var keyVaultUrl = builder.Configuration["KeyVault:Url"];
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUrl),
        new DefaultAzureCredential());
}
```

**Provide `.env.example`**:
```bash
# .env.example (commit this to git as template)
GOOGLE_CLIENT_ID=your-client-id-here
GOOGLE_CLIENT_SECRET=your-client-secret-here
JWT_SECRET_KEY=generate-a-256-bit-key
POSTGRES_PASSWORD=your-secure-password
```

---

#### ⚙️ Configuration Best Practices

**All tunable parameters MUST be externalized to configuration**:

**✅ DO externalize**:
- Rate limits (requests per minute)
- Timeout values (HTTP, database, cache)
- Batch sizes (pagination, bulk operations)
- Retry policies (max attempts, backoff intervals)
- Feature flags
- Cache TTL values
- Token expiration times

**Example - appsettings.json**:
```json
{
  "RateLimiting": {
    "LoginEndpoint": {
      "RequestsPerMinute": 5,
      "IpWhitelist": []
    }
  },
  "Jwt": {
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 7
  },
  "Resilience": {
    "HttpTimeoutSeconds": 30,
    "MaxRetryAttempts": 3,
    "RetryDelayMilliseconds": 1000
  },
  "Caching": {
    "DefaultTtlMinutes": 10,
    "ProfileCacheTtlMinutes": 30
  }
}
```

**Load in code**:
```csharp
// Strongly-typed configuration (preferred)
public class RateLimitingOptions
{
    public const string Section = "RateLimiting:LoginEndpoint";
    public int RequestsPerMinute { get; set; } = 5;
    public List<string> IpWhitelist { get; set; } = new();
}

// Register in Program.cs
builder.Services.Configure<RateLimitingOptions>(
    builder.Configuration.GetSection(RateLimitingOptions.Section));

// Inject in service
public class AuthController
{
    private readonly IOptions<RateLimitingOptions> _rateLimitOptions;

    public AuthController(IOptions<RateLimitingOptions> rateLimitOptions)
    {
        _rateLimitOptions = rateLimitOptions;
    }
}
```

**❌ DON'T hardcode**:
```csharp
// BAD: Hardcoded values
const int MAX_LOGIN_ATTEMPTS = 5; // Should be in appsettings
Thread.Sleep(1000); // Should be configurable timeout
var cacheExpiry = TimeSpan.FromMinutes(10); // Should be in appsettings
```

---

#### 🏗️ .NET-Specific Best Practices

**Async/Await**:
```csharp
// ✅ DO: Always pass CancellationToken
public async Task<User> GetUserAsync(Guid id, CancellationToken ct = default)
{
    return await _dbContext.Users.FindAsync(new object[] { id }, ct);
}

// ❌ DON'T: Blocking async calls
var user = GetUserAsync(id).Result; // Causes deadlocks
var user = GetUserAsync(id).GetAwaiter().GetResult(); // Still bad
```

**Dependency Injection Lifetimes**:
```csharp
// Scoped: Per HTTP request (DbContext, repositories)
services.AddScoped<IUserRepository, UserRepository>();
services.AddDbContext<AppDbContext>(options => ...); // Scoped by default

// Singleton: Shared across application (caches, configuration)
services.AddSingleton<IMemoryCache, MemoryCache>();

// Transient: New instance per injection (lightweight services)
services.AddTransient<IEmailService, EmailService>();
```

**Entity Framework Core**:
```csharp
// ✅ DO: Use AsNoTracking for read-only queries
var users = await _dbContext.Users
    .AsNoTracking()
    .Where(u => u.IsActive)
    .ToListAsync(ct);

// ✅ DO: Explicit loading for navigation properties when needed
var user = await _dbContext.Users
    .Include(u => u.RefreshTokens)
    .FirstOrDefaultAsync(u => u.Id == id, ct);

// ❌ DON'T: N+1 query problem
foreach (var user in users)
{
    // This triggers a separate query for each user
    var tokens = await _dbContext.RefreshTokens
        .Where(t => t.UserId == user.Id)
        .ToListAsync(ct);
}
```

**Logging Best Practices**:
```csharp
// ✅ DO: Structured logging with message templates
_logger.LogInformation(
    "User {UserId} logged in from {IpAddress} at {LoginTime}",
    userId, ipAddress, DateTime.UtcNow);

// ❌ DON'T: String interpolation (loses structure)
_logger.LogInformation($"User {userId} logged in from {ipAddress}");

// ✅ DO: Log levels appropriately
_logger.LogDebug("Processing match for user {UserId}", userId); // Development
_logger.LogInformation("Match created {MatchId}", matchId); // Production events
_logger.LogWarning("Google API slow response {Duration}ms", duration);
_logger.LogError(ex, "Failed to validate token for user {UserId}", userId);
```

**Exception Handling**:
```csharp
// ✅ DO: Catch specific exceptions
try
{
    await _googleAuthService.ValidateIdTokenAsync(idToken, ct);
}
catch (GoogleApiException ex)
{
    _logger.LogError(ex, "Google API validation failed");
    throw new AuthenticationException("Invalid ID token", ex);
}

// ❌ DON'T: Catch and swallow
try
{
    await DoSomethingAsync();
}
catch (Exception)
{
    // Silent failure - BAD!
}

// ❌ DON'T: Catch generic exceptions in application code
catch (Exception ex) // Too broad, use specific exceptions
```

**Nullable Reference Types**:
```csharp
// ✅ DO: Enable nullable reference types (already enabled in .csproj)
// <Nullable>enable</Nullable>

// ✅ DO: Use null-forgiving operator only when certain
var user = await _repository.GetByIdAsync(id, ct);
if (user is null)
    throw new NotFoundException($"User {id} not found");

return user!; // Null-forgiving - we checked above

// ✅ DO: Use nullable for optional parameters
public async Task<User?> FindUserAsync(string? email, CancellationToken ct = default)
{
    if (string.IsNullOrEmpty(email))
        return null;

    return await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
}
```

---

#### 🛡️ Security Best Practices

**Input Validation**:
```csharp
// ✅ DO: Use FluentValidation for input validation
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.IdToken)
            .NotEmpty().WithMessage("ID token is required")
            .MaximumLength(2048).WithMessage("ID token too long");
    }
}

// ✅ DO: Validate at API boundary
[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginRequest request)
{
    var validator = new LoginRequestValidator();
    var result = await validator.ValidateAsync(request);

    if (!result.IsValid)
        return BadRequest(result.Errors);

    // Process request...
}
```

**SQL Injection Prevention**:
```csharp
// ✅ DO: Use EF Core (parameterized queries by default)
var user = await _dbContext.Users
    .FirstOrDefaultAsync(u => u.Email == email, ct);

// ❌ DON'T: Use raw SQL with string concatenation
var sql = $"SELECT * FROM Users WHERE Email = '{email}'"; // SQL INJECTION!

// ✅ DO: If raw SQL needed, use parameters
var user = await _dbContext.Users
    .FromSqlInterpolated($"SELECT * FROM Users WHERE Email = {email}")
    .FirstOrDefaultAsync(ct);
```

**Authentication & Authorization**:
```csharp
// ✅ DO: Use [Authorize] attribute on controllers/actions
[Authorize]
[ApiController]
[Route("api/v1/profile")]
public class ProfileController : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProfile(Guid id)
    {
        // User is authenticated, access User.Claims
        var userId = User.FindFirst("sub")?.Value;

        // Check authorization
        if (userId != id.ToString())
            return Forbid(); // 403 Forbidden

        // Process request...
    }
}

// ✅ DO: Validate JWT on every request
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!))
        };
    });
```

---

#### 📊 Rate Limiting & Throttling

**Rate Limiting Standards**:
```csharp
// ✅ DO: Configure rate limiting per endpoint category
services.Configure<IpRateLimitOptions>(options =>
{
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "POST:/api/v1/auth/login/*",
            Period = "1m",
            Limit = 5 // 5 requests per minute per IP
        },
        new RateLimitRule
        {
            Endpoint = "*",
            Period = "1m",
            Limit = 100 // Default: 100 requests per minute
        }
    };
});

// ✅ DO: Apply rate limiting middleware
app.UseIpRateLimiting();
```

**Guidelines**:
- **Authentication endpoints**: 5-10 requests/min per IP (prevent brute force)
- **Read endpoints (GET)**: 100-200 requests/min per user
- **Write endpoints (POST/PUT)**: 20-50 requests/min per user
- **Search/expensive queries**: 10-20 requests/min per user
- **Always configurable**: Load limits from appsettings, not hardcoded

---

#### ✅ Code Review Checklist Extension

**Before committing, verify**:

**Configuration & Secrets**:
- [ ] No hardcoded credentials, API keys, or secrets
- [ ] All secrets loaded from environment variables or Key Vault
- [ ] `.env.example` updated with new configuration keys
- [ ] Configurable parameters (timeouts, limits) in appsettings.json

**.NET Best Practices**:
- [ ] All async methods use `CancellationToken`
- [ ] Proper DI lifetimes (Scoped, Singleton, Transient)
- [ ] EF Core queries use `AsNoTracking()` for read-only
- [ ] No N+1 query problems (use `.Include()` for eager loading)
- [ ] Structured logging with message templates (not string interpolation)
- [ ] Specific exception handling (not generic `catch (Exception)`)

**Security**:
- [ ] Input validation with FluentValidation
- [ ] No SQL injection vulnerabilities (use EF Core or parameterized queries)
- [ ] JWT validation on all protected endpoints
- [ ] Rate limiting configured for authentication endpoints
- [ ] Sensitive data encrypted/hashed (passwords, tokens, PII)

---

**This concludes the Architecture Guidelines. All implementation decisions MUST follow these rules.**

**Next Steps**:
- Review [Service Templates](03_service-templates.md) for folder structure examples
- Read [Security & Auth](05_security-and-auth.md) for authentication details
- Explore [Messaging & Integration](06_messaging-and-integration.md) for event patterns

---

**Last Updated**: 2025-11-21
**Document Owner**: Architecture Team
**Status**: Living Document (updated as patterns evolve)
