# F0002 ProfileService - Architecture Summary

> **⚠️ IMPORTANT: HUMAN USE ONLY**
>
> Quick reference guide for code review and architectural understanding.
> See [architecture-implementation.md](./architecture-implementation.md) for detailed analysis.

---

## 30-Second Overview

**ProfileService** is a DDD-based microservice managing user profiles with personality traits, values, and preferences. Built with Clean Architecture, it enforces business rules in the domain layer, encrypts PII with AES-256, and communicates via event-driven messaging (RabbitMQ).

**Stack**: .NET 8, EF Core, PostgreSQL, MassTransit, RabbitMQ, Docker
**Pattern**: Clean Architecture / Domain-Driven Design
**Tests**: 50/50 passing (100% domain coverage)

---

## Architecture in 5 Points

### 1. Clean Architecture Layers (Dependency Inward)

```
API (Controllers, JWT Auth)
  ↓ depends on
Infrastructure (EF Core, RabbitMQ, Encryption)
  ↓ depends on
Application (Use Cases, DTOs, Orchestration)
  ↓ depends on
Domain (Entities, Value Objects, Events)
  ↓ depends on
NOTHING (pure business logic)
```

**Why Clean Architecture?**
- Rich domain logic (completion rules, privacy, validation)
- 8 value objects with complex validation
- Need to test business rules in isolation
- Business rules change frequently

### 2. Domain-Driven Design Patterns

| Pattern | Implementation | Location |
|---------|----------------|----------|
| **Aggregate Root** | `MemberProfile` - enforces invariants, raises events | `Core/Domain/Entities/` |
| **Value Objects (7)** | `PersonalityProfile`, `Values`, `Lifestyle`, `Location`, etc. | `Core/Domain/ValueObjects/` |
| **Domain Events (3)** | `ProfileCreated`, `ProfileUpdated`, `ProfileCompleted` | `Core/Domain/Events/` |
| **Repository** | `IProfileRepository` (port) → `ProfileRepository` (adapter) | Interface in Domain, impl in Infrastructure |
| **Factory Method** | `MemberProfile.CreateSkeleton()` - ensures valid initial state | `MemberProfile.cs` |

**Key Business Rules**:
- Age must be >= 18 years (validation in domain)
- Profile completion = 20% per section (5 sections = 100%)
- 80% threshold for matching eligibility
- Privacy levels: Public (0) ≤ MatchedOnly (1) ≤ Private (2)

### 3. Microservice Patterns Applied

| Pattern | Status | Implementation |
|---------|--------|----------------|
| **Database per Service** | ✅ Implemented | `profile_db` - exclusive ownership |
| **Event-Driven Communication** | ✅ Implemented | Pub/Sub via RabbitMQ, async messaging |
| **Domain Events** | ✅ Implemented | Aggregate collects events, app publishes after save |
| **Transactional Outbox** | ⏳ Future | MassTransit outbox for at-least-once delivery |
| **SAGA Orchestration** | ⏳ Future | Complex workflows (user onboarding) |
| **Circuit Breaker** | ⏳ Future | Polly for RabbitMQ resilience |

### 4. Security & Infrastructure

**Encryption**:
- **Algorithm**: AES-256-CBC
- **What's Encrypted**: UserId, Email, FullName, DateOfBirth, Gender, Location, all JSON value objects
- **How**: EF Core value converters (transparent at persistence boundary)
- **Key Management**: Environment variable (Azure Key Vault in production)

**Authentication**:
- **JWT Bearer Tokens**: Shared secret with IdentityService
- **Authorization**: User can only access own profile (`sub` claim validation)
- **Pattern**: Claim-based authorization in controller

**Messaging**:
- **Broker**: RabbitMQ 3.13
- **Library**: MassTransit 8.x (transport-agnostic)
- **Exchange**: `profile-events` (topic exchange)
- **Routing Keys**: `profile.created`, `profile.updated`, `profile.completed`

### 5. Event Flow

```
┌──────────────────┐                    ┌──────────────────┐
│ IdentityService  │                    │ ProfileService   │
└──────────────────┘                    └──────────────────┘
         │                                       │
         │ UserRegistered event                  │
         ├──────────────────────────────────────>│
         │                                       │
         │                         UserRegisteredConsumer
         │                           creates skeleton profile
         │                                       │
         │                         Publish ProfileCreated
         │<──────────────────────────────────────┤
         │                                       │
         │                         User updates profile
         │                                       │
         │                         Publish ProfileUpdated
         │<──────────────────────────────────────┤
         │                                       │
         │                         Profile reaches 80%
         │                                       │
         │                         Publish ProfileCompleted
         │<──────────────────────────────────────┤
         │                                       │
```

**Events Consumed**: `UserRegistered` (from IdentityService)
**Events Published**: `ProfileCreated`, `ProfileUpdated`, `ProfileCompleted`

---

## Code Structure at a Glance

### Domain Layer (Pure Business Logic)

```csharp
// Aggregate Root
public class MemberProfile
{
    // Factory method - ensures valid state
    public static MemberProfile CreateSkeleton(MemberId userId, string email) { }

    // Business rules enforced
    public void UpdateBasicInfo(string fullName, DateTime dob, string gender, Location location)
    {
        if (CalculateAge(dob) < 18)
            throw new ProfileDomainException("User must be at least 18");
        // ... update state, recalculate completion, raise event
    }

    // Completion logic
    private void RecalculateCompletion()
    {
        CompletionPercentage = filledSections * 20; // 20% per section
        if (CompletionPercentage >= 80 && !IsComplete)
        {
            IsComplete = true;
            RaiseDomainEvent(new ProfileCompleted(UserId));
        }
    }
}

// Value Object (immutable, validates in constructor)
public record PersonalityProfile
{
    public int Openness { get; } // 1-5
    public int Conscientiousness { get; } // 1-5
    // ... validates all scores in constructor, throws if invalid
}
```

### Application Layer (Orchestration)

```csharp
public class ProfileService
{
    public async Task<ProfileResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
    {
        // 1. Load aggregate
        var profile = await _repository.GetByUserIdAsync(new MemberId(userId));

        // 2. Execute domain operations (business logic in domain!)
        if (request.Personality != null)
            profile.UpdatePersonality(MapToValueObject(request.Personality));

        // 3. Persist changes
        await _repository.UpdateAsync(profile);

        // 4. Publish events (after successful save)
        foreach (var evt in profile.DomainEvents)
            await _publisher.PublishAsync(evt);
        profile.ClearDomainEvents();

        // 5. Map to DTO
        return MapToResponse(profile);
    }
}
```

### Infrastructure Layer (External Adapters)

```csharp
// Encryption Service
public class EncryptionService : IEncryptionService
{
    public string Encrypt(string plainText)
    {
        // AES-256-CBC with random IV prepended to ciphertext
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();
        // ... encrypt and prepend IV
    }
}

// Repository (encryption at boundary)
public class ProfileRepository : IProfileRepository
{
    public async Task<MemberProfile?> GetByUserIdAsync(MemberId userId)
    {
        var entity = await _context.MemberProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId.Value && p.DeletedAt == null);

        // Decrypt on load
        return entity?.ToDomainModel(_encryption);
    }

    public async Task UpdateAsync(MemberProfile profile)
    {
        // Encrypt on save
        var entity = MemberProfileEntity.FromDomainModel(profile, _encryption);
        _context.Update(entity);
        await _context.SaveChangesAsync();
    }
}

// Event Consumer
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
        await context.Publish(new ProfileCreated(profile.UserId, profile.Email));
    }
}
```

### API Layer (HTTP Entry Point)

```csharp
[Authorize] // JWT required
[ApiController]
[Route("api/profiles")]
public class ProfileController : ControllerBase
{
    [HttpPut("{userId}")]
    public async Task<ActionResult<ProfileResponse>> UpdateProfile(
        [FromRoute] Guid userId,
        [FromBody] UpdateProfileRequest request)
    {
        // Authorization: User can only access own profile
        var claimUserId = User.FindFirst("sub")?.Value;
        if (claimUserId != userId.ToString())
            return Forbid();

        var result = await _profileService.UpdateProfileAsync(userId, request);
        return Ok(result);
    }
}
```

---

## Future Evolution: SAGA Pattern

### Current State (Event-Driven, No Coordination)

**Problem**: When profile completes, we fire `ProfileCompleted` event and hope MatchingService processes it. No guarantee, no compensation if it fails.

```csharp
// Current: Fire-and-forget
profile.UpdateLifestyle(lifestyle); // Reaches 80%
await _repository.UpdateAsync(profile);

// Publish event, no coordination
await _publisher.PublishAsync(new ProfileCompleted(userId));
// Hope MatchingService generates matches!
```

### Future State (Orchestration SAGA)

**Solution**: Central coordinator manages multi-step workflow with compensation.

```csharp
// Future: SAGA State Machine
public class UserOnboardingSaga : MassTransitStateMachine<UserOnboardingState>
{
    public UserOnboardingSaga()
    {
        // Step 1: Profile completed
        Initially(
            When(ProfileCompletedEvent)
                .Publish(context => new GenerateMatchesCommand(userId))
        );

        // Step 2: Matches generated
        During(ProfileCompleted,
            When(MatchesGeneratedEvent)
                .Publish(context => new SendWelcomeEmailCommand(userId))
        );

        // Compensation: If matching fails, rollback
        During(ProfileCompleted,
            When(MatchGenerationFailed)
                .Publish(context => new CompensateProfileCompletionCommand(userId))
        );
    }
}
```

**SAGA State Table** (stored in DB):

| SagaId | UserId | CurrentState | ProfileComplete | MatchesGenerated | EmailSent |
|--------|--------|--------------|-----------------|------------------|-----------|
| 001    | abc    | Completed    | ✅              | ✅               | ✅        |
| 002    | def    | Failed       | ✅              | ❌               | ❌        |

**Benefits**:
- **Guaranteed Completion**: Multi-step workflow reliable
- **Compensation**: Rollback on failure (e.g., mark profile incomplete if matching fails)
- **Monitoring**: Dashboard showing where users get stuck
- **Debugging**: Query SAGA state to troubleshoot

**When to Introduce**:
- Onboarding becomes multi-step (3+ services)
- Need strong consistency guarantees
- Too many incomplete workflows observed

**Effort**: 3-5 hours (MassTransit.StateMachine setup)

---

## Testing Strategy

### Unit Tests (50/50 passing)

**Domain Layer (35 tests)**:
- `MemberProfile` entity: 17 tests
  - Factory method creates valid skeleton
  - Age validation (< 18 throws exception)
  - Completion calculation (20% per section)
  - `ProfileCompleted` event raised at 80%
  - Privacy logic (`CanShareWith()`)

- Value objects: 18 tests
  - `PersonalityProfile` validates 1-5 scores
  - `AgeRange` validates min <= max, min >= 18
  - `PreferenceSet` validates distance 1-500km
  - Defensive copy of language list

**Application Layer (15 tests)**:
- `ProfileService` orchestration:
  - Create, update, retrieve, delete operations
  - Domain events published after save
  - `ProfileCompleted` event when reaching 80%
  - Mocks: `IProfileRepository`, `IMessagePublisher`, `IEncryptionService` (Moq)

**Tools**: xUnit, Moq, FluentAssertions

### Integration Tests (Future)

- Repository with real PostgreSQL (Testcontainers)
- Event consumer with real RabbitMQ
- API endpoints with WebApplicationFactory

---

## Key Design Decisions

| Decision | Rationale | Alternative Considered |
|----------|-----------|------------------------|
| **Clean Architecture** | Rich domain logic, testability | ❌ Layered (too simple), ❌ Hexagonal (overkill) |
| **C# Records for Value Objects** | Immutability, value equality | ❌ Classes with readonly properties |
| **AES-256-CBC Encryption** | Industry standard, built-in .NET | ❌ External library (unnecessary dependency) |
| **MassTransit** | Transport-agnostic (RabbitMQ → Azure Service Bus) | ❌ Raw RabbitMQ client (tight coupling) |
| **Private Constructor + Factory** | Prevent invalid state, force factory use | ❌ Public constructor (allows invalid objects) |
| **Events After Save** | Transactional consistency (no events if save fails) | ❌ Events before save (risk of orphan events) |
| **Completion = 20% per section** | Simple, predictable (5 sections = 100%) | ❌ Weighted (complex, hard to explain) |
| **80% Matching Threshold** | Allows some missing sections, encourages completion | ❌ 100% (too strict), ❌ 50% (too lenient) |

---

## File Locations Quick Reference

| What | Where |
|------|-------|
| **Aggregate Root** | `Core/Domain/Entities/MemberProfile.cs` |
| **Value Objects** | `Core/Domain/ValueObjects/*.cs` (7 files) |
| **Domain Events** | `Core/Domain/Events/*.cs` (3 files) |
| **Application Service** | `Core/Application/Services/ProfileService.cs` |
| **Repository (Interface)** | `Core/Domain/Interfaces/IProfileRepository.cs` |
| **Repository (Implementation)** | `Infrastructure/Persistence/ProfileRepository.cs` |
| **Encryption Service** | `Infrastructure/Security/EncryptionService.cs` |
| **Event Consumer** | `Infrastructure/Messaging/Consumers/UserRegisteredConsumer.cs` |
| **Controller** | `Api/Controllers/ProfileController.cs` |
| **DI Configuration** | `Api/Program.cs` |
| **Unit Tests** | `Tests.Unit/Domain/*.cs`, `Tests.Unit/Application/*.cs` |

---

## Microservice Maturity Level

**Current**: Level 2 (Event-Driven)

| Capability | Status | Notes |
|------------|--------|-------|
| Database per Service | ✅ Implemented | `profile_db` exclusive |
| Async Messaging | ✅ Implemented | RabbitMQ pub/sub |
| Domain Events | ✅ Implemented | 3 events published |
| Field-Level Encryption | ✅ Implemented | AES-256 for PII |
| Unit Tests | ✅ Implemented | 50/50 passing |
| Docker Container | ✅ Implemented | Multi-stage build |
| JWT Authentication | ✅ Implemented | Shared secret |
| Health Checks | ✅ Implemented | `/health`, `/health/ready` |
| Transactional Outbox | ⏳ Future | At-least-once delivery |
| SAGA Orchestration | ⏳ Future | Complex workflows |
| Circuit Breaker | ⏳ Future | Polly policies |
| Distributed Tracing | ⏳ Future | OpenTelemetry |
| API Gateway | ⏳ Future | Single entry point |

---

## Critical Code Review Checklist

When reviewing this service, verify:

✅ **Domain Logic in Domain**: `MemberProfile` enforces all business rules
✅ **Thin Application Layer**: `ProfileService` only orchestrates, no business logic
✅ **Encryption at Boundary**: Infrastructure encrypts/decrypts, domain unaware
✅ **Events After Save**: Published only after successful DB transaction
✅ **Immutable Value Objects**: All use C# `record` keyword
✅ **Private Constructor**: Aggregate uses factory method
✅ **Authorization in Controller**: User ownership checked in API layer
✅ **No Domain Dependencies**: Domain layer has ZERO external references
✅ **Defensive Copies**: `PreferredLanguages` list copied to prevent mutation
✅ **Test Coverage**: All domain rules have corresponding unit tests

---

## Quick Stats

- **Lines of Code**: ~3,500 (excluding tests)
- **Unit Tests**: 50 (100% passing)
- **Value Objects**: 7
- **Domain Events**: 3
- **API Endpoints**: 4 (GET, POST, PUT, DELETE)
- **Database Tables**: 1 (`member_profiles`)
- **Encrypted Fields**: 9 (UserId, Email, FullName, + 5 JSON columns)
- **Dependencies**: 12 NuGet packages
- **Docker Image Size**: ~210 MB (multi-stage optimized)

---

## References

- **Detailed Analysis**: [architecture-implementation.md](./architecture-implementation.md)
- **Service Documentation**: [../../src/Services/Profile/README.md](../../../src/Services/Profile/README.md)
- **DDD Patterns**: [../../src/Services/Profile/PATTERNS.md](../../../src/Services/Profile/PATTERNS.md)
- **Feature Spec**: [f0002_profile_service_mvp.md](./f0002_profile_service_mvp.md)
- **Implementation Plan**: [plan.md](./plan.md)

---

**Document Version**: 1.0
**Last Updated**: 2025-11-27
**Purpose**: Quick reference for code review and learning
