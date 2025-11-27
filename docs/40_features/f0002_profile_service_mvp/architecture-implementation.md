# F0002 ProfileService - Architecture Implementation Analysis

> **⚠️ IMPORTANT: HUMAN USE ONLY**
>
> This document is intended **exclusively for human code review, learning, and understanding**. It provides detailed architectural analysis and implementation explanations for educational purposes.
>
> **DO NOT USE THIS DOCUMENT FOR:**
> - Future AI-assisted development
> - Code generation or scaffolding
> - Implementation guidance for new features
> - Automated code analysis
>
> **This is a retrospective analysis, not a forward-looking specification.**

---

## Table of Contents

1. [Architecture Pattern: Clean Architecture / DDD](#architecture-pattern-clean-architecture--ddd)
2. [Layer-by-Layer Implementation](#layer-by-layer-implementation)
3. [Microservice Design Patterns](#microservice-design-patterns)
4. [Tools and Technologies](#tools-and-technologies)
5. [Messaging Infrastructure](#messaging-infrastructure)
6. [Future Evolution: SAGA Pattern](#future-evolution-saga-pattern)
7. [Code Examples and Locations](#code-examples-and-locations)

---

## Architecture Pattern: Clean Architecture / DDD

### Why Clean Architecture for ProfileService?

**Decision Rationale**:
- **Rich Domain Logic**: Profile completion calculation, privacy rules, validation invariants
- **Complexity**: 8 value objects, 1 aggregate root, 3 domain events
- **Business Rules**: Age validation, score ranges (1-5), completion thresholds (80%)
- **Testability**: Need to unit test domain logic in isolation
- **Maintainability**: Business rules change frequently (new personality dimensions, privacy levels)

**Alternative Patterns Considered**:
- ❌ **Layered Architecture**: Too simple for rich domain logic, business rules leak into services
- ❌ **Hexagonal Architecture**: Overkill for single microservice, ports/adapters add ceremony
- ✅ **Clean Architecture**: Perfect balance - rich domain, clear boundaries, testable

### The Onion Rings (Dependency Flow)

```
┌─────────────────────────────────────────────────┐
│  API Layer (Controllers, Middleware)            │ ← HTTP Entry Point
│  Dependencies: → Infrastructure, → Application   │
└─────────────────────────────────────────────────┘
         ↓ depends on
┌─────────────────────────────────────────────────┐
│  Infrastructure Layer (EF Core, RabbitMQ, AES)  │ ← External Adapters
│  Dependencies: → Application (ports)             │
└─────────────────────────────────────────────────┘
         ↓ depends on
┌─────────────────────────────────────────────────┐
│  Application Layer (Use Cases, DTOs, Services)  │ ← Orchestration
│  Dependencies: → Domain (entities, interfaces)   │
└─────────────────────────────────────────────────┘
         ↓ depends on
┌─────────────────────────────────────────────────┐
│  Domain Layer (Entities, Value Objects, Events) │ ← Pure Business Logic
│  Dependencies: NONE (only .NET BCL)              │
└─────────────────────────────────────────────────┘
```

**Key Principle**: Dependencies point INWARD. The domain has ZERO external dependencies.

---

## Layer-by-Layer Implementation

### 1. Domain Layer (Core/Domain/)

**Location**: `src/Services/Profile/DatingApp.ProfileService.Core/Domain/`

**Purpose**: Pure business logic, zero infrastructure dependencies

#### Aggregate Root: MemberProfile

**File**: `Core/Domain/Entities/MemberProfile.cs`

**Key Design Decisions**:

1. **Private Constructor**:
   ```csharp
   private MemberProfile() { }  // EF Core needs parameterless constructor
   ```
   - Prevents invalid state creation
   - Forces use of factory method `CreateSkeleton()`
   - EF Core reflection can still instantiate

2. **Factory Method Pattern**:
   ```csharp
   public static MemberProfile CreateSkeleton(MemberId userId, string email)
   {
       var profile = new MemberProfile
       {
           UserId = userId,
           Email = email,
           CompletionPercentage = 0,
           IsComplete = false,
           ExposureLevel = ExposureLevel.Public,
           CreatedAt = DateTime.UtcNow,
           UpdatedAt = DateTime.UtcNow
       };

       profile.RaiseDomainEvent(new ProfileCreated(userId, email));
       return profile;
   }
   ```
   - **Why**: Ensures valid initial state
   - **Why**: Automatically raises `ProfileCreated` event
   - **Pattern**: Named constructor pattern (GoF)

3. **Business Rule Enforcement**:
   ```csharp
   public void UpdateBasicInfo(string fullName, DateTime dateOfBirth, string gender, Location location)
   {
       var age = CalculateAge(dateOfBirth);
       if (age < 18)
           throw new ProfileDomainException("User must be at least 18 years old");

       FullName = fullName;
       DateOfBirth = dateOfBirth;
       Gender = gender;
       Location = location;

       UpdatedAt = DateTime.UtcNow;
       RecalculateCompletion();
       RaiseDomainEvent(new ProfileUpdated(UserId));
   }
   ```
   - **Why**: Age validation in domain (business invariant)
   - **Why**: Automatic completion recalculation
   - **Why**: Event raised after successful state change
   - **Pattern**: Tell, Don't Ask

4. **Completion Calculation Algorithm**:
   ```csharp
   private void RecalculateCompletion()
   {
       int sections = 0;
       if (!string.IsNullOrEmpty(FullName)) sections++; // Basic info = 20%
       if (Personality != null) sections++;             // Personality = 20%
       if (Values != null) sections++;                  // Values = 20%
       if (Lifestyle != null) sections++;               // Lifestyle = 20%
       if (Preferences != null) sections++;             // Preferences = 20%

       var oldPercentage = CompletionPercentage;
       CompletionPercentage = sections * 20;

       var wasNotComplete = !IsComplete;
       IsComplete = CompletionPercentage >= 80;

       // Raise ProfileCompleted event only on transition from incomplete → complete
       if (wasNotComplete && IsComplete)
       {
           RaiseDomainEvent(new ProfileCompleted(UserId));
       }
   }
   ```
   - **Why**: 20% per section (5 sections = 100%)
   - **Why**: 80% threshold for matching eligibility
   - **Why**: Event raised only once (transition detection)
   - **Pattern**: State transition monitoring

5. **Privacy Logic**:
   ```csharp
   public bool CanShareWith(ExposureLevel requesterLevel)
   {
       return ExposureLevel <= requesterLevel;
   }
   ```
   - **Why**: Domain encapsulates privacy rules
   - **How**: Public (0) <= MatchedOnly (1) <= Private (2)
   - **Future**: Can add complex rules (e.g., block list, consent)

#### Value Objects

**Design Principles**:
- Immutable (C# `record` keyword)
- Validate in constructor (fail fast)
- Structural equality (value-based comparison)
- No identity (defined by attributes, not ID)

**Example: PersonalityProfile**

**File**: `Core/Domain/ValueObjects/PersonalityProfile.cs`

```csharp
public record PersonalityProfile
{
    public int Openness { get; }
    public int Conscientiousness { get; }
    public int Extraversion { get; }
    public int Agreeableness { get; }
    public int Neuroticism { get; }
    public string? AboutMe { get; }
    public string? LifePhilosophy { get; }

    public PersonalityProfile(
        int openness,
        int conscientiousness,
        int extraversion,
        int agreeableness,
        int neuroticism,
        string? aboutMe = null,
        string? lifePhilosophy = null)
    {
        // Validation: Fail fast in constructor
        ValidateScore(openness, nameof(Openness));
        ValidateScore(conscientiousness, nameof(Conscientiousness));
        // ... (other validations)

        if (aboutMe?.Length > 500)
            throw new ProfileDomainException("AboutMe cannot exceed 500 characters");

        Openness = openness;
        Conscientiousness = conscientiousness;
        // ... (assignments)
    }

    private static void ValidateScore(int score, string propertyName)
    {
        if (score < 1 || score > 5)
            throw new ProfileDomainException($"{propertyName} must be between 1 and 5");
    }
}
```

**Why Record Type?**:
- Immutability by default (init-only properties)
- Value-based equality (two objects with same scores are equal)
- Concise syntax (no boilerplate)
- C# 9+ feature

**Validation Strategy**:
- Constructor validation (fail fast)
- Private helper methods (DRY principle)
- Domain exception (not generic ArgumentException)

**Example: PreferenceSet with Defensive Copy**

**File**: `Core/Domain/ValueObjects/PreferenceSet.cs`

```csharp
public record PreferenceSet
{
    public AgeRange PreferredAgeRange { get; }
    public int MaxDistanceKm { get; }
    public IReadOnlyList<string> PreferredLanguages { get; }
    public GenderPreference GenderPreference { get; }

    public PreferenceSet(
        AgeRange preferredAgeRange,
        int maxDistanceKm,
        List<string> preferredLanguages,
        GenderPreference genderPreference)
    {
        if (maxDistanceKm < 1 || maxDistanceKm > 500)
            throw new ProfileDomainException("MaxDistanceKm must be between 1 and 500");

        if (preferredLanguages.Count == 0)
            throw new ProfileDomainException("At least one preferred language required");

        PreferredAgeRange = preferredAgeRange;
        MaxDistanceKm = maxDistanceKm;

        // Defensive copy: Prevent external mutation
        PreferredLanguages = preferredLanguages.ToList().AsReadOnly();
        GenderPreference = genderPreference;
    }
}
```

**Why Defensive Copy?**:
- Caller can't mutate list after construction
- Preserves immutability guarantee
- `IReadOnlyList<T>` prevents external changes

#### Domain Events

**File**: `Core/Domain/Events/ProfileCreated.cs`

```csharp
public record ProfileCreated : DomainEvent
{
    public Guid MemberId { get; }
    public string Email { get; }

    public ProfileCreated(Guid memberId, string email)
    {
        MemberId = memberId;
        Email = email;
    }
}
```

**Event Management in Aggregate**:

```csharp
// In MemberProfile.cs
private readonly List<DomainEvent> _domainEvents = new();
public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

protected void RaiseDomainEvent(DomainEvent domainEvent)
{
    _domainEvents.Add(domainEvent);
}

public void ClearDomainEvents()
{
    _domainEvents.Clear();
}
```

**Pattern**: Domain Event Collector
- Aggregate collects events during state changes
- Application layer publishes after successful persistence
- Ensures events only published if transaction succeeds

---

### 2. Application Layer (Core/Application/)

**Location**: `src/Services/Profile/DatingApp.ProfileService.Core/Application/`

**Purpose**: Orchestrate use cases, coordinate domain and infrastructure

#### Application Service: ProfileService

**File**: `Core/Application/Services/ProfileService.cs`

**Key Design Decision**: Thin application layer, logic in domain

```csharp
public class ProfileService
{
    private readonly IProfileRepository _repository;
    private readonly IMessagePublisher _publisher;

    public async Task<ProfileResponse> UpdateProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Load aggregate from repository
        var profile = await _repository.GetByUserIdAsync(new MemberId(userId), cancellationToken);
        if (profile == null)
            throw new ProfileDomainException("Profile not found");

        // 2. Execute domain operations (business logic in domain!)
        if (request.Personality != null)
        {
            var personality = new PersonalityProfile(
                request.Personality.Openness,
                request.Personality.Conscientiousness,
                // ... map DTO to value object
            );
            profile.UpdatePersonality(personality); // Domain method
        }

        // 3. Persist changes
        await _repository.UpdateAsync(profile, cancellationToken);

        // 4. Publish domain events (after successful save)
        foreach (var domainEvent in profile.DomainEvents)
        {
            await _publisher.PublishAsync(domainEvent, cancellationToken);
        }
        profile.ClearDomainEvents();

        // 5. Map to response DTO
        return MapToResponse(profile);
    }
}
```

**Orchestration Steps**:
1. **Load**: Retrieve aggregate from repository
2. **Execute**: Call domain methods (business logic)
3. **Persist**: Save changes via repository
4. **Publish**: Send domain events to message broker
5. **Map**: Convert domain entity to DTO

**Why This Order?**:
- Events published AFTER persistence (transactional consistency)
- If save fails, no events published (atomicity)
- Aggregate clears events after publishing (idempotency)

#### DTOs (Data Transfer Objects)

**File**: `Core/Application/DTOs/UpdateProfileRequest.cs`

```csharp
public class UpdateProfileRequest
{
    public PersonalityDto? Personality { get; set; }
    public ValuesDto? Values { get; set; }
    public LifestyleDto? Lifestyle { get; set; }
    public PreferencesDto? Preferences { get; set; }
    public string? ExposureLevel { get; set; }
}
```

**Why All Nullable?**:
- Supports partial updates (PATCH semantics)
- Null = "don't update this field"
- Avoids sending entire profile for small changes

**Mapping Strategy**:
- Application layer maps DTO → Value Object
- Domain layer doesn't know about DTOs
- Keeps domain pure

---

### 3. Infrastructure Layer (Infrastructure/)

**Location**: `src/Services/Profile/DatingApp.ProfileService.Infrastructure/`

**Purpose**: Implement domain ports, integrate external dependencies

#### Repository Implementation

**File**: `Infrastructure/Persistence/ProfileRepository.cs`

```csharp
public class ProfileRepository : IProfileRepository
{
    private readonly ProfileDbContext _context;
    private readonly IEncryptionService _encryption;

    public async Task<MemberProfile?> GetByUserIdAsync(
        MemberId userId,
        CancellationToken cancellationToken = default)
    {
        // Query encrypted database
        var entity = await _context.MemberProfiles
            .FirstOrDefaultAsync(
                p => p.UserId == userId.Value && p.DeletedAt == null,
                cancellationToken);

        if (entity == null) return null;

        // Decrypt and map to domain entity
        return entity.ToDomainModel(_encryption);
    }

    public async Task UpdateAsync(
        MemberProfile profile,
        CancellationToken cancellationToken = default)
    {
        // Map domain entity to EF entity
        var entity = MemberProfileEntity.FromDomainModel(profile, _encryption);

        _context.MemberProfiles.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

**Key Patterns**:
1. **Encryption at Boundary**: Encrypt on save, decrypt on load
2. **Soft Delete Filter**: `DeletedAt == null` in all queries
3. **Mapping Methods**: `ToDomainModel()` and `FromDomainModel()`

#### Encryption Service

**File**: `Infrastructure/Security/EncryptionService.cs`

```csharp
public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public EncryptionService(IConfiguration configuration)
    {
        var keyBase64 = configuration["Encryption:Key"]
            ?? throw new InvalidOperationException("Encryption key not configured");
        _key = Convert.FromBase64String(keyBase64);
    }

    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV(); // Random IV per encryption

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Prepend IV to ciphertext (IV doesn't need to be secret)
        var result = new byte[aes.IV.Length + encrypted.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encrypted, 0, result, aes.IV.Length, encrypted.Length);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string cipherText)
    {
        var cipherBytes = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = _key;

        // Extract IV from prepended bytes
        var iv = new byte[aes.IV.Length];
        Buffer.BlockCopy(cipherBytes, 0, iv, 0, iv.Length);
        aes.IV = iv;

        var encrypted = new byte[cipherBytes.Length - iv.Length];
        Buffer.BlockCopy(cipherBytes, iv.Length, encrypted, 0, encrypted.Length);

        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);

        return Encoding.UTF8.GetString(plainBytes);
    }
}
```

**Security Design**:
- **Algorithm**: AES-256-CBC (industry standard)
- **IV Management**: Random IV per encryption, prepended to ciphertext
- **Key Source**: Environment variable (Azure Key Vault in production)
- **Why Prepend IV?**: IV doesn't need to be secret, simplifies storage

#### EF Core Configuration

**File**: `Infrastructure/Persistence/Configurations/MemberProfileConfiguration.cs`

```csharp
public class MemberProfileConfiguration : IEntityTypeConfiguration<MemberProfileEntity>
{
    private readonly IEncryptionService _encryption;

    public void Configure(EntityTypeBuilder<MemberProfileEntity> builder)
    {
        builder.ToTable("member_profiles");
        builder.HasKey(p => p.Id);

        // Encrypted fields via value converter
        builder.Property(p => p.UserId)
            .HasConversion(
                v => _encryption.Encrypt(v.ToString()),    // To DB: Encrypt
                v => Guid.Parse(_encryption.Decrypt(v))     // From DB: Decrypt
            )
            .HasColumnName("user_id");

        // JSON column for PersonalityProfile (encrypted)
        builder.Property(p => p.PersonalityJson)
            .HasConversion(
                v => v == null ? null : _encryption.Encrypt(JsonSerializer.Serialize(v)),
                v => v == null ? null : JsonSerializer.Deserialize<PersonalityProfile>(_encryption.Decrypt(v))
            )
            .HasColumnName("personality_json");

        // Index for queries
        builder.HasIndex(p => p.UserId)
            .HasDatabaseName("idx_member_profiles_user_id");

        // Soft delete filter
        builder.HasIndex(p => p.DeletedAt)
            .HasDatabaseName("idx_member_profiles_deleted_at");
    }
}
```

**Pattern**: Value Converter for Transparent Encryption
- EF Core automatically encrypts/decrypts on save/load
- Domain layer never sees ciphertext
- Infrastructure concern (encryption) isolated from domain

---

### 4. API Layer (Api/)

**Location**: `src/Services/Profile/DatingApp.ProfileService.Api/`

**Purpose**: HTTP entry point, authentication, validation

#### Controller

**File**: `Api/Controllers/ProfileController.cs`

```csharp
[Authorize] // JWT required for all endpoints
[ApiController]
[Route("api/profiles")]
public class ProfileController : ControllerBase
{
    private readonly ProfileService _profileService;

    [HttpGet("{userId}")]
    public async Task<ActionResult<ProfileResponse>> GetProfile(
        [FromRoute] Guid userId,
        CancellationToken cancellationToken)
    {
        // Extract userId from JWT claims
        var claimUserId = User.FindFirst("sub")?.Value;
        if (claimUserId == null || claimUserId != userId.ToString())
        {
            return Forbid(); // User can only access own profile
        }

        var profile = await _profileService.GetProfileAsync(userId, cancellationToken);
        if (profile == null)
        {
            return NotFound();
        }

        return Ok(profile);
    }

    [HttpPut("{userId}")]
    public async Task<ActionResult<ProfileResponse>> UpdateProfile(
        [FromRoute] Guid userId,
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        // Authorization check
        var claimUserId = User.FindFirst("sub")?.Value;
        if (claimUserId == null || claimUserId != userId.ToString())
        {
            return Forbid();
        }

        try
        {
            var result = await _profileService.UpdateProfileAsync(userId, request, cancellationToken);
            return Ok(result);
        }
        catch (ProfileDomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
```

**Security Pattern**: Claim-Based Authorization
- JWT contains `sub` claim with userId
- Controller verifies `sub == userId` in route
- Prevents user A from accessing user B's profile

#### Dependency Injection

**File**: `Api/Program.cs`

```csharp
// Domain services (no external dependencies)
// None needed - domain is pure!

// Application services
builder.Services.AddScoped<ProfileService>();

// Infrastructure services
builder.Services.AddScoped<IProfileRepository, ProfileRepository>();
builder.Services.AddSingleton<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IMessagePublisher, RabbitMqPublisher>();

// EF Core
builder.Services.AddDbContext<ProfileDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ProfileDb")));

// JWT Authentication
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
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]))
        };
    });
```

**Lifetime Choices**:
- **Scoped**: Repository, ProfileService (per HTTP request)
- **Singleton**: EncryptionService (stateless, thread-safe)
- **Transient**: (none in this service)

---

## Microservice Design Patterns

### 1. Database per Service

**Implementation**:
- ProfileService owns `profile_db` database exclusively
- No other service can directly query this database
- Data shared via domain events (eventual consistency)

**Why**:
- Service autonomy (can change schema without coordination)
- Scalability (independent database scaling)
- Resilience (database failure isolated to one service)

**Code Evidence**:
- Connection string: `ConnectionStrings__ProfileDb`
- Migration: `Initial_MemberProfiles_Table`
- No foreign keys to other service databases

### 2. Event-Driven Communication

**Pattern**: Publish-Subscribe with Domain Events

**Publishers** (ProfileService sends):
- `ProfileCreated` → when skeleton created from UserRegistered
- `ProfileUpdated` → when any profile section updated
- `ProfileCompleted` → when profile reaches 80% completion

**Consumers** (ProfileService receives):
- `UserRegistered` → creates skeleton profile

**Why**:
- Loose coupling (services don't call each other's APIs)
- Async communication (fire-and-forget, non-blocking)
- Temporal decoupling (services don't need to be online simultaneously)

**Code Example**:

```csharp
// Consumer: Infrastructure/Messaging/Consumers/UserRegisteredConsumer.cs
public class UserRegisteredConsumer : IConsumer<UserRegistered>
{
    private readonly IProfileRepository _repository;

    public async Task Consume(ConsumeContext<UserRegistered> context)
    {
        var @event = context.Message;

        // Create skeleton profile from event
        var profile = MemberProfile.CreateSkeleton(
            new MemberId(@event.UserId),
            @event.Email);

        await _repository.AddAsync(profile);

        // Publish ProfileCreated event
        foreach (var domainEvent in profile.DomainEvents)
        {
            await context.Publish(domainEvent);
        }
        profile.ClearDomainEvents();
    }
}
```

### 3. API Gateway Pattern (Future)

**Current**: Direct client → ProfileService calls
**Future**: Client → API Gateway → ProfileService

**Why**:
- Single entry point for all services
- Authentication/authorization at gateway
- Rate limiting, caching, request aggregation
- BFF (Backend for Frontend) pattern

**Not Implemented Yet**: MVP has direct service access

### 4. Transactional Outbox Pattern (Implicit)

**Problem**: How to atomically update DB + publish event?

**Current Solution**: In-memory event collection
```csharp
// 1. Domain operation (adds event to list)
profile.UpdatePersonality(personality);

// 2. Save to database
await _repository.UpdateAsync(profile);

// 3. Publish events AFTER successful save
foreach (var domainEvent in profile.DomainEvents)
{
    await _publisher.PublishAsync(domainEvent);
}
```

**Issue**: If publish fails, event lost!

**Future**: MassTransit Outbox Pattern
```csharp
// Configure in Program.cs (future enhancement)
builder.Services.AddDbContext<ProfileDbContext>(options =>
{
    options.UseNpgsql(connectionString);
    options.AddMassTransitOutbox(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
    });
});
```

**How Outbox Works**:
1. Save entity + event in same DB transaction
2. Background worker polls outbox table
3. Publishes events, marks as sent
4. **Guarantee**: At-least-once delivery

**Why Not Now?**: MVP complexity trade-off, added later

### 5. Circuit Breaker (Future)

**Problem**: What if RabbitMQ is down?

**Solution**: Polly circuit breaker policy
```csharp
// Future enhancement in Program.cs
builder.Services.AddHttpClient<IMessagePublisher, RabbitMqPublisher>()
    .AddTransientHttpErrorPolicy(p => p.CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 3,
        durationOfBreak: TimeSpan.FromSeconds(30)
    ));
```

**Not Implemented**: MVP assumes RabbitMQ always available

---

## Tools and Technologies

### Core Framework
- **.NET 8.0**: Latest LTS, performance improvements, minimal APIs
- **ASP.NET Core**: Web framework, dependency injection, middleware

### Domain Layer
- **C# Records**: Immutable value objects, structural equality
- **Custom Exceptions**: `ProfileDomainException` for business rule violations

### Application Layer
- **No External Libraries**: Pure C# orchestration

### Infrastructure Layer

#### Persistence
- **Entity Framework Core 8.0**: ORM for PostgreSQL
  - **Why**: LINQ queries, migrations, change tracking
  - **Location**: `Infrastructure/Persistence/`

- **Npgsql.EntityFrameworkCore.PostgreSQL**: EF Core provider
  - **Why**: Best PostgreSQL support, native types

- **PostgreSQL 16**: Relational database
  - **Why**: ACID compliance, jsonb support, mature

#### Messaging
- **MassTransit 8.x**: Messaging abstraction library
  - **Why**: Transport-agnostic (RabbitMQ dev, Azure Service Bus prod)
  - **Location**: `Infrastructure/Messaging/`

- **MassTransit.RabbitMQ**: RabbitMQ transport
  - **Why**: Local development, widely used

- **RabbitMQ 3.13**: Message broker
  - **Why**: Reliable, easy local dev, management UI

**MassTransit Configuration**:
```csharp
// Program.cs
builder.Services.AddMassTransit(x =>
{
    // Register consumers
    x.AddConsumer<UserRegisteredConsumer>();

    // Configure RabbitMQ transport
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"], "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"]);
            h.Password(builder.Configuration["RabbitMQ:Password"]);
        });

        cfg.ConfigureEndpoints(context);
    });
});
```

**Exchange/Queue Setup**:
- **Exchange**: `profile-events` (topic exchange)
- **Routing Keys**:
  - `profile.created`
  - `profile.updated`
  - `profile.completed`
- **Queue**: `profile-service.user-registered` (consumes UserRegistered)

#### Security
- **System.Security.Cryptography**: Built-in AES encryption
  - **Why**: No external dependencies, battle-tested
  - **Algorithm**: AES-256-CBC

- **Microsoft.AspNetCore.Authentication.JwtBearer**: JWT validation
  - **Why**: Standard ASP.NET Core middleware
  - **Configuration**: Shared secret with IdentityService

#### Testing
- **xUnit**: Test framework
  - **Why**: .NET standard, clean syntax

- **Moq**: Mocking library
  - **Why**: Easy interface mocking for repositories

- **FluentAssertions**: Assertion library
  - **Why**: Readable test assertions (`result.Should().Be(expected)`)

#### Infrastructure as Code
- **Docker**: Containerization
  - **Multi-stage build**: Build → Publish → Runtime
  - **Base images**: `mcr.microsoft.com/dotnet/aspnet:8.0`

- **Docker Compose**: Local orchestration
  - **Services**: postgres, rabbitmq, redis, seq, profile-service

---

## Messaging Infrastructure

### Event Flow Diagram

```
┌──────────────────┐                    ┌──────────────────┐
│ IdentityService  │                    │ ProfileService   │
└──────────────────┘                    └──────────────────┘
         │                                       │
         │ 1. User registers                     │
         │                                       │
         │ 2. Publish UserRegistered             │
         ├──────────────────────────────────────>│
         │       (RabbitMQ)                      │
         │                                       │
         │                              3. UserRegisteredConsumer
         │                                 creates skeleton profile
         │                                       │
         │                              4. Publish ProfileCreated
         │<──────────────────────────────────────┤
         │       (RabbitMQ)                      │
         │                                       │
         │                              5. User updates profile
         │                                       │
         │                              6. Publish ProfileUpdated
         │<──────────────────────────────────────┤
         │                                       │
         │                              7. Profile reaches 80%
         │                                       │
         │                              8. Publish ProfileCompleted
         │<──────────────────────────────────────┤
         │                                       │
         v                                       v
  (NotificationService                    (MatchingService
   sends welcome email)                    enables matching)
```

### Event Schemas

#### UserRegistered (Consumed)

**Publisher**: IdentityService
**Consumer**: ProfileService
**Purpose**: Create skeleton profile for new user

```json
{
  "eventId": "550e8400-e29b-41d4-a716-446655440000",
  "occurredAt": "2025-11-27T10:00:00Z",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "user@example.com",
  "provider": "Google",
  "registeredAt": "2025-11-27T10:00:00Z"
}
```

**Handler Code**:
```csharp
public class UserRegisteredConsumer : IConsumer<UserRegistered>
{
    public async Task Consume(ConsumeContext<UserRegistered> context)
    {
        var profile = MemberProfile.CreateSkeleton(
            new MemberId(context.Message.UserId),
            context.Message.Email
        );

        await _repository.AddAsync(profile);

        // Publish ProfileCreated
        await context.Publish(new ProfileCreated(
            profile.UserId.Value,
            profile.Email
        ));
    }
}
```

#### ProfileCreated (Published)

**Publisher**: ProfileService
**Consumers**: NotificationService, AnalyticsService

```json
{
  "eventId": "550e8400-e29b-41d4-a716-446655440000",
  "occurredAt": "2025-11-27T10:00:01Z",
  "memberId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "user@example.com"
}
```

**Use Cases**:
- NotificationService: Send "complete your profile" email
- AnalyticsService: Track registration funnel

#### ProfileUpdated (Published)

**Publisher**: ProfileService
**Consumers**: MatchingService, SearchService (future)

```json
{
  "eventId": "550e8400-e29b-41d4-a716-446655440000",
  "occurredAt": "2025-11-27T10:05:00Z",
  "memberId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Use Cases**:
- MatchingService: Invalidate cached match scores
- SearchService: Re-index profile for search

#### ProfileCompleted (Published)

**Publisher**: ProfileService
**Consumers**: MatchingService, NotificationService

```json
{
  "eventId": "550e8400-e29b-41d4-a716-446655440000",
  "occurredAt": "2025-11-27T10:10:00Z",
  "memberId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Use Cases**:
- MatchingService: Enable user for matching algorithm
- NotificationService: Send "congratulations" message

### Message Reliability

**Current Guarantees**:
- **At-Most-Once**: If publish fails after DB save, event lost
- **No Retries**: MassTransit default (0 retries)
- **No Dead Letter Queue**: Failed messages disappear

**Future Enhancements**:
1. **Outbox Pattern**: At-least-once delivery
2. **Retry Policy**: 3 retries with exponential backoff
3. **DLQ**: Dead letter queue for failed messages
4. **Idempotent Consumers**: Handle duplicate events gracefully

---

## Future Evolution: SAGA Pattern

### What is SAGA?

**Definition**: A sequence of local transactions coordinated across multiple services

**Example Scenario**: User Profile Completion Flow

```
┌─────────────────────────────────────────────────────────────┐
│ SAGA: CompleteUserOnboarding                                │
│                                                              │
│ Steps:                                                       │
│  1. ProfileService: Mark profile complete                   │
│  2. MatchingService: Generate initial matches               │
│  3. NotificationService: Send welcome email                 │
│  4. AnalyticsService: Track completion event                │
│                                                              │
│ If Step 3 fails → Compensate Steps 2, 1                     │
└─────────────────────────────────────────────────────────────┘
```

### Current vs. Future State

#### Current Implementation (Event-Driven, No Coordination)

**Code**: ProfileService publishes `ProfileCompleted`, hopes for the best

```csharp
// Current: Fire-and-forget
profile.UpdateLifestyle(lifestyle); // Reaches 80%
await _repository.UpdateAsync(profile);

// Publish event, no coordination
foreach (var domainEvent in profile.DomainEvents)
{
    await _publisher.PublishAsync(domainEvent); // Hope MatchingService processes it!
}
```

**Problems**:
- No guarantee MatchingService generated matches
- If MatchingService fails, user sees "profile complete" but has no matches
- No compensation if downstream services fail

#### Future Implementation (Orchestration SAGA)

**Pattern**: Orchestration (Central Coordinator)

**Code Sketch** (NOT IMPLEMENTED):

```csharp
// Future: SAGA Orchestrator
public class UserOnboardingSaga : MassTransitStateMachine<UserOnboardingState>
{
    public State ProfileCompleted { get; private set; }
    public State MatchesGenerated { get; private set; }
    public State EmailSent { get; private set; }
    public State Completed { get; private set; }

    public Event<ProfileCompletedEvent> ProfileCompletedEvent { get; private set; }
    public Event<MatchesGeneratedEvent> MatchesGeneratedEvent { get; private set; }
    public Event<EmailSentEvent> EmailSentEvent { get; private set; }

    public UserOnboardingSaga()
    {
        InstanceState(x => x.CurrentState);

        // Step 1: Profile completed
        Initially(
            When(ProfileCompletedEvent)
                .TransitionTo(ProfileCompleted)
                .Publish(context => new GenerateMatchesCommand(context.Data.UserId))
        );

        // Step 2: Matches generated
        During(ProfileCompleted,
            When(MatchesGeneratedEvent)
                .TransitionTo(MatchesGenerated)
                .Publish(context => new SendWelcomeEmailCommand(context.Data.UserId))
        );

        // Step 3: Email sent
        During(MatchesGenerated,
            When(EmailSentEvent)
                .TransitionTo(Completed)
                .Finalize()
        );

        // Compensation: If match generation fails
        During(ProfileCompleted,
            When(MatchGenerationFailed)
                .Publish(context => new CompensateProfileCompletionCommand(context.Data.UserId))
                .TransitionTo(Failed)
        );
    }
}
```

**Orchestration State Table** (Stored in DB):

| SagaId | UserId | CurrentState | ProfileComplete | MatchesGenerated | EmailSent |
|--------|--------|--------------|-----------------|------------------|-----------|
| 001    | abc    | Completed    | true            | true             | true      |
| 002    | def    | MatchesGenerated | true        | true             | false     |
| 003    | ghi    | Failed       | true            | false            | false     |

**Why Orchestration?**:
- **Central Visibility**: See all onboarding flows in one place
- **Compensation**: Automatically rollback on failure
- **Monitoring**: Track which step failed, how many retries
- **Debugging**: Replay failed SAGAs

### Choreography SAGA (Alternative)

**Pattern**: Services react to events, no central coordinator

**How It Would Work**:

```
ProfileService                    MatchingService              NotificationService
      │                                 │                            │
      │ ProfileCompleted                │                            │
      ├────────────────────────────────>│                            │
      │                                 │                            │
      │                          Generate Matches                    │
      │                                 │                            │
      │                                 │ MatchesGenerated           │
      │                                 ├───────────────────────────>│
      │                                 │                            │
      │                                 │                     Send Welcome Email
      │                                 │                            │
      │<────────────────────────────────┼────────────────────────────┤
      │         EmailSent (no one cares)                             │
```

**Problems with Choreography**:
- No central view of workflow state
- Hard to debug (events scattered across services)
- Difficult to add new steps (touch multiple services)
- Compensation logic duplicated

**When to Use Choreography**:
- Simple workflows (2-3 steps)
- Services truly independent
- No compensation needed

### How Current Code Evolves to SAGA

#### Step 1: Install MassTransit.StateMachine

```bash
dotnet add package MassTransit.EntityFrameworkCore
```

#### Step 2: Create SAGA State Entity

```csharp
// Infrastructure/Sagas/UserOnboardingState.cs
public class UserOnboardingState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; }

    public Guid UserId { get; set; }
    public bool ProfileCompleted { get; set; }
    public bool MatchesGenerated { get; set; }
    public bool EmailSent { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
```

#### Step 3: Define SAGA State Machine

```csharp
// Application/Sagas/UserOnboardingSaga.cs
public class UserOnboardingSaga : MassTransitStateMachine<UserOnboardingState>
{
    // (See orchestration code above)
}
```

#### Step 4: Configure MassTransit

```csharp
// Program.cs
builder.Services.AddMassTransit(x =>
{
    x.AddSagaStateMachine<UserOnboardingSaga, UserOnboardingState>()
        .EntityFrameworkRepository(r =>
        {
            r.ExistingDbContext<ProfileDbContext>();
            r.UsePostgres();
        });

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});
```

#### Step 5: Trigger SAGA from Domain Event

```csharp
// Application/Services/ProfileService.cs (modified)
public async Task<ProfileResponse> UpdateProfileAsync(...)
{
    // ... existing code ...

    foreach (var domainEvent in profile.DomainEvents)
    {
        if (domainEvent is ProfileCompleted completed)
        {
            // Start SAGA instead of just publishing event
            await _publisher.PublishAsync(new StartUserOnboardingSagaCommand
            {
                UserId = completed.MemberId,
                ProfileCompletedAt = completed.OccurredAt
            });
        }
        else
        {
            await _publisher.PublishAsync(domainEvent);
        }
    }
}
```

### SAGA Benefits for ProfileService

1. **Guaranteed Onboarding**: User completion triggers reliable multi-step workflow
2. **Compensation**: If matching fails, can undo "profile complete" status
3. **Monitoring**: Dashboard showing onboarding funnel drop-off points
4. **Resilience**: Automatic retries if downstream services temporarily unavailable
5. **Debugging**: Query SAGA state table to see where user is stuck

### When to Introduce SAGA?

**Not in MVP Because**:
- Adds complexity (state machine, compensation logic)
- Requires additional database table for state
- Profile completion is non-critical (eventual consistency acceptable)

**Introduce SAGA When**:
- Onboarding flow becomes multi-step (email verification + profile + preferences)
- Need strong consistency guarantees (e.g., payment processing)
- Monitoring shows too many incomplete workflows
- Customer complaints about stuck onboarding

**Estimated Future Work**: 3-5 hours to add SAGA orchestration

---

## Code Examples and Locations

### Complete File Tree

```
src/Services/Profile/
├── DatingApp.ProfileService.Core/
│   ├── Domain/
│   │   ├── Entities/
│   │   │   └── MemberProfile.cs              ← Aggregate root
│   │   ├── ValueObjects/
│   │   │   ├── MemberId.cs                   ← Strongly-typed ID
│   │   │   ├── PersonalityProfile.cs         ← Big Five traits
│   │   │   ├── Values.cs                     ← 8 value dimensions
│   │   │   ├── Lifestyle.cs                  ← Lifestyle preferences
│   │   │   ├── Location.cs                   ← Geo data
│   │   │   ├── AgeRange.cs                   ← Age preferences
│   │   │   └── PreferenceSet.cs              ← Matching criteria
│   │   ├── Events/
│   │   │   ├── DomainEvent.cs                ← Base event class
│   │   │   ├── ProfileCreated.cs             ← Event: skeleton created
│   │   │   ├── ProfileUpdated.cs             ← Event: profile changed
│   │   │   └── ProfileCompleted.cs           ← Event: 80% reached
│   │   ├── Exceptions/
│   │   │   └── ProfileDomainException.cs     ← Business rule violations
│   │   └── Interfaces/
│   │       ├── IProfileRepository.cs         ← Repository port
│   │       ├── IEncryptionService.cs         ← Encryption port
│   │       └── IMessagePublisher.cs          ← Messaging port
│   └── Application/
│       ├── Services/
│       │   └── ProfileService.cs             ← Use case orchestration
│       └── DTOs/
│           ├── CreateProfileRequest.cs       ← Input DTO
│           ├── UpdateProfileRequest.cs       ← Input DTO
│           └── ProfileResponse.cs            ← Output DTO
│
├── DatingApp.ProfileService.Infrastructure/
│   ├── Persistence/
│   │   ├── ProfileDbContext.cs               ← EF Core context
│   │   ├── ProfileRepository.cs              ← Repository adapter
│   │   ├── Entities/
│   │   │   └── MemberProfileEntity.cs        ← EF entity (mapping)
│   │   └── Configurations/
│   │       └── MemberProfileConfiguration.cs ← EF configuration
│   ├── Security/
│   │   └── EncryptionService.cs              ← AES-256 encryption
│   └── Messaging/
│       ├── RabbitMqPublisher.cs              ← Message publisher adapter
│       └── Consumers/
│           └── UserRegisteredConsumer.cs     ← Event consumer
│
├── DatingApp.ProfileService.Api/
│   ├── Controllers/
│   │   └── ProfileController.cs              ← HTTP endpoints
│   ├── Program.cs                            ← DI configuration
│   ├── appsettings.json                      ← Configuration
│   └── appsettings.Development.json
│
├── DatingApp.ProfileService.Tests.Unit/
│   ├── Domain/
│   │   ├── MemberProfileTests.cs             ← 17 entity tests
│   │   └── ValueObjectTests.cs               ← 18 value object tests
│   └── Application/
│       └── ProfileServiceTests.cs            ← 15 service tests
│
├── Dockerfile                                ← Multi-stage build
└── README.md                                 ← Service documentation
```

### Key Classes and Responsibilities

| Class | Responsibility | Pattern |
|-------|----------------|---------|
| `MemberProfile` | Enforce business invariants, raise events | Aggregate Root |
| `PersonalityProfile` | Validate Big Five scores (1-5) | Value Object |
| `ProfileService` | Orchestrate use cases | Application Service |
| `ProfileRepository` | Persist/retrieve aggregates | Repository |
| `EncryptionService` | Encrypt/decrypt PII | Adapter |
| `UserRegisteredConsumer` | React to external events | Event Consumer |
| `ProfileController` | Handle HTTP requests | Controller |

### Dependency Flow Example

**Scenario**: User updates personality

```
HTTP Request
    ↓
ProfileController.UpdateProfile()
    ↓ calls
ProfileService.UpdateProfileAsync()
    ↓ loads from
IProfileRepository.GetByUserIdAsync()
    ↓ implemented by
ProfileRepository.GetByUserIdAsync()
    ↓ queries
ProfileDbContext (EF Core)
    ↓ decrypts via
IEncryptionService.Decrypt()
    ↓ implemented by
EncryptionService.Decrypt()
    ↓ returns
MemberProfile (domain entity)
    ↓ calls business method
profile.UpdatePersonality(personality)
    ↓ raises
ProfileUpdated (domain event)
    ↓ published via
IMessagePublisher.PublishAsync()
    ↓ implemented by
RabbitMqPublisher.PublishAsync()
    ↓ sends to
RabbitMQ exchange: profile-events
```

**Dependency Direction**: Always inward toward domain
- API → Application ✅
- Infrastructure → Domain (via interfaces) ✅
- Domain → Infrastructure ❌ (NEVER)

---

## Summary

### Architectural Highlights

1. **Clean Architecture**: Domain is pure, zero external dependencies
2. **DDD Tactical Patterns**: Aggregate, value objects, domain events, repository
3. **Event-Driven**: Loosely coupled services via async messaging
4. **Security by Design**: Field-level encryption, JWT authentication
5. **Testability**: 50 unit tests with 100% domain coverage

### Microservice Maturity Level

**Current State**: Level 2 (Event-Driven)
- ✅ Database per service
- ✅ Async communication
- ✅ Domain events
- ❌ SAGA orchestration (future)
- ❌ Circuit breakers (future)
- ❌ Distributed tracing (future)

**Future Evolution Path**:
1. Add Transactional Outbox (reliability)
2. Implement SAGA for complex workflows
3. Add circuit breakers (Polly)
4. Distributed tracing (OpenTelemetry)
5. Service mesh (Linkerd/Istio)

### Key Takeaways for Code Review

1. **Domain Logic Lives in Domain**: Check that `MemberProfile` enforces all rules
2. **Application Layer is Thin**: `ProfileService` only orchestrates, no business logic
3. **Encryption at Boundary**: Infrastructure encrypts/decrypts, domain never sees ciphertext
4. **Events After Persistence**: Verify events published only after successful DB save
5. **Immutable Value Objects**: All value objects use C# `record` for immutability
6. **Authorization in Controller**: User ownership check happens in API layer, not domain

---

**Document Version**: 1.0
**Last Updated**: 2025-11-27
**Author**: Implementation Team
**Purpose**: Code review and architectural understanding
