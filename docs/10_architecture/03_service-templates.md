# Service Templates - Folder Structures for Each Architecture Pattern

> **Concrete examples of how to structure each microservice**
>
> This document provides the exact folder structure for each architecture pattern used in QuietMatch. Copy these templates when implementing a new service.

---

## Table of Contents

- [Overview](#overview)
- [Layered Architecture Template](#layered-architecture-template)
- [Onion Architecture Template](#onion-architecture-template)
- [Hexagonal Architecture Template](#hexagonal-architecture-template)
- [CQRS Template](#cqrs-template)
- [Shared BuildingBlocks](#shared-buildingblocks)
- [Testing Structure](#testing-structure)

---

## Overview

Each microservice in QuietMatch follows one of four architecture patterns. This document shows the **exact folder structure and key files** for each pattern.

### Pattern Assignment Reference

| Service | Pattern | Template Section |
|---------|---------|------------------|
| IdentityService | Layered | [Link](#layered-architecture-template) |
| ProfileService | Onion | [Link](#onion-architecture-template) |
| MatchingService | Hexagonal | [Link](#hexagonal-architecture-template) |
| SchedulingService | Layered + CQRS | [Link](#cqrs-template) |
| NotificationService | Hexagonal | [Link](#hexagonal-architecture-template) |
| VerificationService | Hexagonal | [Link](#hexagonal-architecture-template) |
| PaymentService | Hexagonal | [Link](#hexagonal-architecture-template) |
| RealTimeService | Layered | [Link](#layered-architecture-template) |
| GraphQLGateway | Layered | [Link](#layered-architecture-template) |

---

## Layered Architecture Template

**Used by**: IdentityService, RealTimeService, GraphQLGateway

**Principle**: Dependencies flow downward: Presentation → Application → Domain → Infrastructure

### Folder Structure

```
src/Services/Identity/
├── DatingApp.IdentityService.sln
├── Dockerfile
├── README.md
├── PATTERNS.md                              # Explains Layered architecture, why, how, alternatives
│
├── DatingApp.IdentityService.Api/           # ⬇️ Layer 1: Presentation (Entry point)
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── Controllers/
│   │   ├── AuthController.cs               # POST /api/v1/auth/login/google
│   │   └── TokenController.cs              # POST /api/v1/auth/refresh
│   ├── Models/                             # DTOs (Data Transfer Objects)
│   │   ├── Requests/
│   │   │   ├── GoogleLoginRequest.cs
│   │   │   └── RefreshTokenRequest.cs
│   │   └── Responses/
│   │       ├── LoginResponse.cs
│   │       └── RefreshTokenResponse.cs
│   ├── Filters/                            # Exception filters, action filters
│   │   └── GlobalExceptionFilter.cs
│   ├── Middleware/
│   │   └── RequestLoggingMiddleware.cs
│   └── Extensions/
│       └── ServiceCollectionExtensions.cs  # DI registration
│
├── DatingApp.IdentityService.Application/   # ⬇️ Layer 2: Application (Business logic)
│   ├── Services/
│   │   ├── AuthService.cs                  # Orchestrates login flow
│   │   ├── TokenService.cs                 # JWT generation
│   │   └── RefreshTokenService.cs
│   ├── Interfaces/
│   │   ├── IAuthService.cs
│   │   ├── ITokenService.cs
│   │   └── IRefreshTokenService.cs
│   └── DTOs/                               # Internal DTOs (different from API DTOs)
│       └── UserDto.cs
│
├── DatingApp.IdentityService.Domain/        # ⬇️ Layer 3: Domain (Core entities)
│   ├── Entities/
│   │   ├── User.cs                         # Aggregate root
│   │   └── RefreshToken.cs
│   ├── ValueObjects/
│   │   ├── UserId.cs                       # Strongly-typed ID
│   │   ├── Email.cs
│   │   └── AuthProvider.cs                 # Enum: Google, Apple
│   ├── Events/                             # Domain events
│   │   ├── UserRegistered.cs
│   │   └── RefreshTokenRevoked.cs
│   ├── Exceptions/
│   │   └── DomainException.cs
│   └── Interfaces/                         # Ports (implemented by Infrastructure)
│       ├── IUserRepository.cs
│       └── IRefreshTokenRepository.cs
│
└── DatingApp.IdentityService.Infrastructure/ # ⬅️ Layer 4: Infrastructure (External dependencies)
    ├── Data/
    │   ├── IdentityDbContext.cs            # EF Core DbContext
    │   ├── Configurations/
    │   │   ├── UserConfiguration.cs        # EF entity configuration
    │   │   └── RefreshTokenConfiguration.cs
    │   └── Migrations/                     # EF Core migrations (auto-generated)
    │       └── 20250120_InitialCreate.cs
    ├── Repositories/
    │   ├── UserRepository.cs               # Implements IUserRepository
    │   └── RefreshTokenRepository.cs
    ├── ExternalServices/
    │   ├── GoogleAuthService.cs            # Calls Google API
    │   └── AppleAuthService.cs
    ├── Messaging/
    │   ├── MassTransitConfiguration.cs
    │   └── EventPublisher.cs               # Implements IMessagePublisher (from BuildingBlocks)
    └── Security/
        └── JwtTokenGenerator.cs
```

### Key Files Examples

**Program.cs** (Entry point):
```csharp
var builder = WebApplication.CreateBuilder(args);

// Layer registration (top to bottom)
builder.Services.AddControllers(); // Presentation
builder.Services.AddApplicationServices(); // Application
builder.Services.AddInfrastructureServices(builder.Configuration); // Infrastructure

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

**AuthController.cs** (Presentation):
```csharp
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService; // Depends on Application layer

    [HttpPost("login/google")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        var result = await _authService.LoginWithGoogleAsync(request.IdToken);
        return Ok(new LoginResponse(result.AccessToken, result.RefreshToken));
    }
}
```

**AuthService.cs** (Application):
```csharp
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo; // Depends on Domain interface (implemented by Infrastructure)
    private readonly ITokenService _tokenService;

    public async Task<LoginResult> LoginWithGoogleAsync(string idToken)
    {
        // Orchestrates: Validate token → Find/create user → Generate JWT
        var googleUser = await _googleAuthService.ValidateIdTokenAsync(idToken);
        var user = await _userRepo.FindByExternalIdAsync("Google", googleUser.Sub)
                   ?? await CreateNewUserAsync(googleUser);

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = await _refreshTokenService.CreateAsync(user.Id);

        return new LoginResult(accessToken, refreshToken);
    }
}
```

**User.cs** (Domain):
```csharp
public class User
{
    public UserId Id { get; private set; }
    public Email Email { get; private set; }
    public AuthProvider Provider { get; private set; }
    public string ExternalUserId { get; private set; }

    // Domain logic: Validation in constructor
    public User(string email, string provider, string externalUserId)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email is required");

        Id = UserId.New();
        Email = new Email(email);
        Provider = Enum.Parse<AuthProvider>(provider);
        ExternalUserId = externalUserId;
    }
}
```

---

## Onion Architecture Template

**Used by**: ProfileService

**Principle**: Dependencies point **inward** to the Core. Domain has zero external dependencies.

### Folder Structure

```
src/Services/Profile/
├── DatingApp.ProfileService.sln
├── Dockerfile
├── README.md
├── PATTERNS.md                              # Explains Onion architecture
│
├── DatingApp.ProfileService.Api/            # 🔴 Outer Layer: Entry point
│   ├── Program.cs
│   ├── appsettings.json
│   ├── Controllers/
│   │   └── ProfilesController.cs           # GET /api/v1/profiles/{id}, PUT /api/v1/profiles/{id}
│   └── Extensions/
│       └── ServiceCollectionExtensions.cs
│
├── DatingApp.ProfileService.Core/           # 🟡 Core (Domain + Application, NO external dependencies)
│   ├── Domain/                             # 🔵 Inner-most: Pure domain logic
│   │   ├── Entities/
│   │   │   └── MemberProfile.cs            # Aggregate root with rich behavior
│   │   ├── ValueObjects/
│   │   │   ├── MemberId.cs
│   │   │   ├── PersonalityProfile.cs
│   │   │   ├── Values.cs
│   │   │   ├── Lifestyle.cs
│   │   │   ├── PreferenceSet.cs
│   │   │   ├── DealBreakerSet.cs
│   │   │   └── ExposureLevel.cs
│   │   ├── Events/
│   │   │   ├── ProfileCreated.cs
│   │   │   ├── ProfileUpdated.cs
│   │   │   └── PreferencesUpdated.cs
│   │   ├── Exceptions/
│   │   │   └── ProfileDomainException.cs
│   │   └── Interfaces/                     # Ports (no implementations here)
│   │       ├── IProfileRepository.cs
│   │       ├── IEncryptionService.cs       # For field-level encryption
│   │       └── IMessagePublisher.cs
│   │
│   └── Application/                        # 🟠 Application logic (uses Domain)
│       ├── UseCases/
│       │   ├── CreateProfileUseCase.cs
│       │   ├── UpdateProfileUseCase.cs
│       │   └── UpdatePreferencesUseCase.cs
│       ├── DTOs/
│       │   ├── CreateProfileDto.cs
│       │   └── ProfileDto.cs
│       └── Services/
│           └── ProfileApplicationService.cs
│
└── DatingApp.ProfileService.Infrastructure/ # 🔴 Outer Layer: Adapters (implements Core interfaces)
    ├── Data/
    │   ├── ProfileDbContext.cs
    │   ├── Configurations/
    │   │   └── MemberProfileConfiguration.cs
    │   └── Migrations/
    ├── Repositories/
    │   └── ProfileRepository.cs            # Implements IProfileRepository (from Core)
    ├── Security/
    │   └── AesEncryptionService.cs         # Implements IEncryptionService (from Core)
    └── Messaging/
        └── MassTransitEventPublisher.cs    # Implements IMessagePublisher (from Core)
```

### Key Files Examples

**MemberProfile.cs** (Core/Domain):
```csharp
// Rich domain model with behavior (not anemic)
public class MemberProfile : AggregateRoot
{
    public MemberId MemberId { get; private set; }
    public string FullName { get; private set; } // Encrypted via EF converter
    public PersonalityProfile Personality { get; private set; }
    public Values Values { get; private set; }
    public PreferenceSet Preferences { get; private set; }
    public ExposureLevel ExposureLevel { get; private set; }

    // Domain logic: Privacy rules
    public bool CanShareWith(MemberId otherId, MatchStatus matchStatus)
    {
        // Business rule: Exposure level determines data sharing
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
            throw new ProfileDomainException("Invalid preferences");

        Preferences = newPreferences;
        RaiseDomainEvent(new PreferencesUpdated(MemberId, newPreferences));
    }
}
```

**UpdatePreferencesUseCase.cs** (Core/Application):
```csharp
public class UpdatePreferencesUseCase
{
    private readonly IProfileRepository _profileRepo; // Port (interface from Core/Domain)
    private readonly IMessagePublisher _publisher;     // Port (interface from Core/Domain)

    public async Task ExecuteAsync(MemberId id, PreferenceSet newPreferences)
    {
        var profile = await _profileRepo.GetByIdAsync(id);
        profile.UpdatePreferences(newPreferences); // Domain method
        await _profileRepo.UpdateAsync(profile);

        // Publish domain events
        foreach (var evt in profile.DomainEvents)
            await _publisher.PublishAsync(evt);
    }
}
```

**ProfileRepository.cs** (Infrastructure):
```csharp
// Adapter: Implements port defined in Core
public class ProfileRepository : IProfileRepository
{
    private readonly ProfileDbContext _dbContext;

    public async Task<MemberProfile> GetByIdAsync(MemberId id)
    {
        return await _dbContext.Profiles
            .Include(p => p.Personality)
            .Include(p => p.Values)
            .FirstOrDefaultAsync(p => p.MemberId == id);
    }
}
```

**Dependency Flow**: API → Infrastructure → Core (all point inward to Core)

---

## Hexagonal Architecture Template

**Used by**: MatchingService, NotificationService, VerificationService, PaymentService

**Principle**: Domain core surrounded by **Ports** (interfaces) and **Adapters** (implementations). Maximum testability and flexibility.

### Folder Structure

```
src/Services/Matching/
├── DatingApp.MatchingService.sln
├── Dockerfile
├── README.md
├── PATTERNS.md                              # Explains Hexagonal architecture
│
├── DatingApp.MatchingService.Domain/        # ⚪ Center: Core business logic (no dependencies)
│   ├── Entities/
│   │   ├── Match.cs                        # Aggregate root
│   │   └── MatchCandidate.cs
│   ├── ValueObjects/
│   │   ├── MatchId.cs
│   │   ├── MemberId.cs
│   │   ├── CompatibilityScore.cs
│   │   └── MatchStatus.cs
│   ├── Events/
│   │   ├── MatchProposed.cs
│   │   ├── MatchAccepted.cs
│   │   └── MatchDeclined.cs
│   └── Services/
│       └── MatchingDomainService.cs        # Domain logic (e.g., filtering deal-breakers)
│
├── DatingApp.MatchingService.Ports/         # 🔵 Ports: Interfaces (contracts)
│   ├── Inbound/                            # Driving ports (use cases)
│   │   └── IMatchingService.cs             # "I want to generate matches"
│   │
│   └── Outbound/                           # Driven ports (dependencies)
│       ├── IMatchingEngine.cs              # "How to find compatible members"
│       ├── IMatchRepository.cs             # "How to persist matches"
│       ├── IProfileClient.cs               # "How to fetch member profiles"
│       └── IMessagePublisher.cs            # "How to publish events"
│
├── DatingApp.MatchingService.Application/   # 🟢 Application: Implements inbound ports
│   └── Services/
│       └── MatchingService.cs              # Implements IMatchingService (uses outbound ports)
│
└── DatingApp.MatchingService.Adapters/      # 🟠 Adapters: Implement outbound ports
    ├── Inbound/                            # Driving adapters (controllers, gRPC)
    │   ├── Rest/
    │   │   └── MatchesController.cs        # REST API
    │   └── Grpc/
    │       └── MatchingGrpcService.cs      # gRPC service
    │
    ├── Outbound/                           # Driven adapters (implementations)
    │   ├── MatchingEngines/
    │   │   ├── RuleBasedMatchingEngine.cs  # Implements IMatchingEngine
    │   │   └── EmbeddingBasedMatchingEngine.cs (future)
    │   ├── Data/
    │   │   ├── MatchingDbContext.cs
    │   │   └── MatchRepository.cs          # Implements IMatchRepository
    │   ├── Clients/
    │   │   └── ProfileGrpcClient.cs        # Implements IProfileClient
    │   └── Messaging/
    │       └── EventPublisher.cs           # Implements IMessagePublisher
    │
    └── Api/                                # Entry point
        ├── Program.cs
        └── appsettings.json
```

### Key Files Examples

**IMatchingEngine.cs** (Ports/Outbound):
```csharp
// Port: Contract for matching algorithms
public interface IMatchingEngine
{
    Task<IEnumerable<MatchCandidate>> FindCandidatesAsync(MemberId memberId, int limit);
    CompatibilityScore CalculateCompatibility(MemberProfile profile1, MemberProfile profile2);
}
```

**RuleBasedMatchingEngine.cs** (Adapters/Outbound):
```csharp
// Adapter: Implements IMatchingEngine with rule-based logic
public class RuleBasedMatchingEngine : IMatchingEngine
{
    public async Task<IEnumerable<MatchCandidate>> FindCandidatesAsync(MemberId memberId, int limit)
    {
        // Rule-based logic: Filter by age, location, deal-breakers
        // Calculate compatibility score by values, lifestyle, etc.
        // Return top N candidates
    }
}

// Future adapter: EmbeddingBasedMatchingEngine.cs (swappable!)
public class EmbeddingBasedMatchingEngine : IMatchingEngine
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorSearchRepository _vectorRepo;

    public async Task<IEnumerable<MatchCandidate>> FindCandidatesAsync(MemberId memberId, int limit)
    {
        // AI-powered: Generate embeddings, vector similarity search
    }
}
```

**MatchingService.cs** (Application):
```csharp
// Application service: Implements inbound port, uses outbound ports
public class MatchingService : IMatchingService
{
    private readonly IMatchingEngine _matchingEngine;       // Outbound port (injected)
    private readonly IMatchRepository _matchRepo;           // Outbound port
    private readonly IProfileClient _profileClient;         // Outbound port
    private readonly IMessagePublisher _publisher;          // Outbound port

    public async Task<IEnumerable<MatchSuggestion>> GenerateMatchesAsync(MemberId memberId)
    {
        var profile = await _profileClient.GetProfileAsync(memberId);
        var candidates = await _matchingEngine.FindCandidatesAsync(memberId, 10);

        // Domain logic: Filter, rank, create match suggestions
        var matches = candidates
            .Where(c => MeetsDealBreakers(profile, c))
            .OrderByDescending(c => c.CompatibilityScore.Overall)
            .Take(3);

        foreach (var candidate in matches)
        {
            var match = Match.Propose(memberId, candidate.CandidateId, candidate.CompatibilityScore);
            await _matchRepo.AddAsync(match);
            await _publisher.PublishAsync(new MatchProposed(match.Id, memberId, candidate.CandidateId));
        }

        return matches.Select(c => new MatchSuggestion(c));
    }
}
```

**Program.cs** (Adapters/Api):
```csharp
var builder = WebApplication.CreateBuilder(args);

// Register ports and adapters
builder.Services.AddScoped<IMatchingEngine, RuleBasedMatchingEngine>(); // Easy to swap!
builder.Services.AddScoped<IMatchRepository, MatchRepository>();
builder.Services.AddScoped<IProfileClient, ProfileGrpcClient>();
builder.Services.AddScoped<IMessagePublisher, MassTransitEventPublisher>();

// Register application service
builder.Services.AddScoped<IMatchingService, MatchingService>();

var app = builder.Build();
app.Run();
```

**Key Benefit**: Swapping adapters is trivial (change DI registration, no code changes):
```csharp
// Switch from rule-based to AI-powered matching in 1 line!
builder.Services.AddScoped<IMatchingEngine, EmbeddingBasedMatchingEngine>();
```

---

## CQRS Template

**Used by**: SchedulingService (Layered + CQRS)

**Principle**: Separate **Command** (write) and **Query** (read) models for optimization.

### Folder Structure

```
src/Services/Scheduling/
├── DatingApp.SchedulingService.sln
├── Dockerfile
├── README.md
├── PATTERNS.md
│
├── DatingApp.SchedulingService.Api/
│   ├── Program.cs
│   ├── Controllers/
│   │   └── AvailabilityController.cs       # Routes to commands/queries
│   └── appsettings.json
│
├── DatingApp.SchedulingService.Application/
│   ├── Commands/                           # Write operations
│   │   ├── CreateAvailabilitySlot/
│   │   │   ├── CreateAvailabilitySlotCommand.cs
│   │   │   ├── CreateAvailabilitySlotHandler.cs
│   │   │   └── CreateAvailabilitySlotValidator.cs (FluentValidation)
│   │   └── ScheduleBlindDate/
│   │       ├── ScheduleBlindDateCommand.cs
│   │       └── ScheduleBlindDateHandler.cs
│   │
│   ├── Queries/                            # Read operations
│   │   ├── GetAvailableSlots/
│   │   │   ├── GetAvailableSlotsQuery.cs
│   │   │   ├── GetAvailableSlotsHandler.cs
│   │   │   └── AvailabilitySlotDto.cs
│   │   └── GetUpcomingDates/
│   │       ├── GetUpcomingDatesQuery.cs
│   │       └── GetUpcomingDatesHandler.cs
│   │
│   └── Sagas/                              # SAGA orchestrators
│       └── BlindDateCreationSaga.cs
│
├── DatingApp.SchedulingService.Domain/
│   ├── Entities/
│   │   ├── BlindDate.cs
│   │   └── AvailabilitySlot.cs
│   ├── ValueObjects/
│   │   └── VenueType.cs
│   └── Events/
│       ├── BlindDateScheduled.cs
│       └── BlindDateCancelled.cs
│
└── DatingApp.SchedulingService.Infrastructure/
    ├── Data/
    │   ├── WriteModel/                     # Optimized for writes
    │   │   ├── SchedulingDbContext.cs
    │   │   └── Configurations/
    │   └── ReadModel/                      # Optimized for reads
    │       ├── AvailabilityReadRepository.cs
    │       └── MaterializedViews/          # Denormalized views
    ├── Repositories/
    │   └── BlindDateRepository.cs
    └── Messaging/
        └── EventPublisher.cs
```

### Key Files Examples

**CreateAvailabilitySlotCommand.cs** (Application/Commands):
```csharp
// Command: Write operation
public record CreateAvailabilitySlotCommand(
    MemberId MemberId,
    DateTime StartTime,
    DateTime EndTime,
    VenueType PreferredVenueType) : IRequest<AvailabilitySlotId>;
```

**CreateAvailabilitySlotHandler.cs** (Application/Commands):
```csharp
// Command handler: Processes write operations
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
```

**GetAvailableSlotsQuery.cs** (Application/Queries):
```csharp
// Query: Read operation
public record GetAvailableSlotsQuery(
    MemberId MemberId,
    DateTime StartDate,
    DateTime EndDate) : IRequest<IEnumerable<AvailabilitySlotDto>>;
```

**GetAvailableSlotsHandler.cs** (Application/Queries):
```csharp
// Query handler: Processes read operations
public class GetAvailableSlotsHandler : IRequestHandler<GetAvailableSlotsQuery, IEnumerable<AvailabilitySlotDto>>
{
    private readonly IAvailabilityReadRepository _readRepository; // Separate read repo (could use materialized view)

    public async Task<IEnumerable<AvailabilitySlotDto>> Handle(GetAvailableSlotsQuery query, CancellationToken ct)
    {
        // Query optimized read model (could be cached in Redis)
        return await _readRepository.GetAvailableSlotsAsync(query.MemberId, query.StartDate, query.EndDate);
    }
}
```

**AvailabilityController.cs** (Api):
```csharp
[ApiController]
[Route("api/v1/availability")]
public class AvailabilityController : ControllerBase
{
    private readonly IMediator _mediator; // MediatR dispatches commands/queries

    [HttpPost] // Command
    public async Task<IActionResult> CreateSlot([FromBody] CreateAvailabilitySlotRequest request)
    {
        var command = new CreateAvailabilitySlotCommand(request.MemberId, request.StartTime, request.EndTime, request.VenueType);
        var slotId = await _mediator.Send(command); // Dispatch to handler
        return Ok(slotId);
    }

    [HttpGet] // Query
    public async Task<IActionResult> GetAvailableSlots([FromQuery] GetAvailableSlotsRequest request)
    {
        var query = new GetAvailableSlotsQuery(request.MemberId, request.StartDate, request.EndDate);
        var slots = await _mediator.Send(query); // Dispatch to handler
        return Ok(slots);
    }
}
```

---

## Shared BuildingBlocks

All microservices share common libraries in `src/BuildingBlocks/`:

```
src/BuildingBlocks/
├── DatingApp.BuildingBlocks.Common/
│   ├── AggregateRoot.cs
│   ├── ValueObject.cs
│   ├── DomainException.cs
│   └── Result.cs (Result<T> pattern)
│
├── DatingApp.BuildingBlocks.Messaging/
│   ├── IMessagePublisher.cs
│   ├── IMessageConsumer.cs
│   ├── MassTransitExtensions.cs
│   └── OutboxPattern/
│       ├── IOutbox.cs
│       └── OutboxMessage.cs
│
├── DatingApp.BuildingBlocks.EventBus/
│   ├── IntegrationEvent.cs
│   └── EventBusSubscriptionsManager.cs
│
├── DatingApp.BuildingBlocks.Caching/
│   ├── IDistributedCacheService.cs
│   └── RedisCacheService.cs
│
├── DatingApp.BuildingBlocks.Security/
│   ├── JwtAuthenticationExtensions.cs
│   ├── IEncryptionService.cs
│   └── AesEncryptionService.cs
│
└── DatingApp.BuildingBlocks.Observability/
    ├── SerilogConfiguration.cs
    ├── OpenTelemetryExtensions.cs
    └── HealthCheckExtensions.cs
```

---

## Testing Structure

Each microservice has a corresponding test project:

```
tests/
├── DatingApp.IdentityService.Tests/
│   ├── Unit/
│   │   ├── Domain/
│   │   │   └── UserTests.cs
│   │   └── Application/
│   │       └── AuthServiceTests.cs
│   ├── Integration/
│   │   ├── Repositories/
│   │   │   └── UserRepositoryTests.cs
│   │   └── ExternalServices/
│   │       └── GoogleAuthServiceTests.cs
│   └── Api/
│       └── AuthControllerTests.cs
│
├── DatingApp.ProfileService.Tests/
│   ├── Unit/
│   ├── Integration/
│   └── Api/
│
└── Integration/                            # Cross-service tests
    └── EndToEndTests/
        └── SignupToMatchFlowTests.cs
```

**Test Naming Convention**:
```csharp
{MethodUnderTest}_{Scenario}_{ExpectedBehavior}

// Examples:
CreateUser_WhenEmailIsValid_ShouldCreateUserSuccessfully()
CreateUser_WhenEmailIsInvalid_ShouldThrowDomainException()
```

---

## Summary

### When to Use Which Pattern

| Pattern | When to Use | Example Service |
|---------|-------------|-----------------|
| **Layered** | Simple CRUD, clear layers, no swappable adapters | IdentityService |
| **Onion** | Rich domain logic, domain should be isolated | ProfileService |
| **Hexagonal** | Multiple implementations (swappable adapters) | MatchingService (RuleEngine ↔ AI Engine) |
| **CQRS** | Different optimization needs for reads/writes | SchedulingService |

### Key Principles

1. **Follow the template exactly** when creating a new service
2. **Create PATTERNS.md** in each service folder explaining the pattern
3. **Use shared BuildingBlocks** for common functionality
4. **Write tests** for each layer (unit, integration, API)
5. **Document architectural decisions** with comments

---

**Next Steps**:
- Choose a service to implement
- Copy the appropriate template
- Create `PATTERNS.md` explaining pattern choice
- Start coding following the folder structure

---

**Last Updated**: 2025-11-20
**Document Owner**: Architecture Team
**Status**: Living Document (evolve as patterns are refined)
