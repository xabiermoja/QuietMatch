# Domain-Driven Design Patterns

This document describes the DDD patterns and tactical design decisions used in the Profile Service.

## Table of Contents

1. [Aggregate Pattern](#aggregate-pattern)
2. [Value Objects](#value-objects)
3. [Domain Events](#domain-events)
4. [Repository Pattern](#repository-pattern)
5. [Application Services](#application-services)
6. [Data Transfer Objects (DTOs)](#data-transfer-objects-dtos)
7. [Encryption Strategy](#encryption-strategy)

## Aggregate Pattern

### MemberProfile Aggregate Root

**Location**: `DatingApp.ProfileService.Core/Domain/Entities/MemberProfile.cs`

The `MemberProfile` is the aggregate root that enforces all business invariants for user profiles.

#### Responsibilities

1. **Invariant Enforcement**
   - Age >= 18 years (validation on `UpdateBasicInfo`)
   - Completion percentage calculation (0-100%)
   - Profile completion threshold (80% minimum)

2. **Encapsulation**
   - All state changes go through public methods (no setters)
   - Private constructor prevents invalid state
   - Factory method `CreateSkeleton()` for initial creation

3. **Domain Event Management**
   - Raises `ProfileCreated` on skeleton creation
   - Raises `ProfileUpdated` on any profile update
   - Raises `ProfileCompleted` when reaching 80% completion
   - Events stored in `_domainEvents` list for publishing

#### Key Methods

```csharp
// Factory method - creates minimal valid profile
public static MemberProfile CreateSkeleton(MemberId userId, string email)

// Business operations - enforce invariants
public void UpdateBasicInfo(string fullName, DateTime dateOfBirth, string gender, Location location)
public void UpdatePersonality(PersonalityProfile personality)
public void UpdateValues(Values values)
public void UpdateLifestyle(Lifestyle lifestyle)
public void UpdatePreferences(PreferenceSet preferences)

// Privacy control
public bool CanShareWith(ExposureLevel requesterLevel)

// Soft delete
public void MarkAsDeleted()
```

#### Completion Calculation

Each section contributes 20% to completion:
- Basic Info: 20%
- Personality: 20%
- Values: 20%
- Lifestyle: 20%
- Preferences: 20%

**Total**: 100% possible, 80% required for `IsComplete = true`

#### Privacy Model

```csharp
public enum ExposureLevel
{
    Public = 0,      // Visible to all matched users
    MatchedOnly = 1, // Visible only to mutual matches
    Private = 2      // Not shared in matching
}
```

**Access Logic**:
```csharp
public bool CanShareWith(ExposureLevel requesterLevel)
{
    return ExposureLevel <= requesterLevel;
}
```

## Value Objects

Value objects are immutable objects defined by their attributes, not identity.

### PersonalityProfile

**Location**: `DatingApp.ProfileService.Core/Domain/ValueObjects/PersonalityProfile.cs`

Big Five personality traits (OCEAN model).

**Validation Rules**:
- All scores: 1-5 (inclusive)
- AboutMe: max 500 characters
- LifePhilosophy: max 500 characters

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
}
```

### Values

**Location**: `DatingApp.ProfileService.Core/Domain/ValueObjects/Values.cs`

Eight core value dimensions for compatibility matching.

**Validation Rules**:
- All scores: 1-5 (inclusive)

```csharp
public record Values
{
    public int FamilyOrientation { get; }
    public int CareerAmbition { get; }
    public int Spirituality { get; }
    public int Adventure { get; }
    public int IntellectualCuriosity { get; }
    public int SocialJustice { get; }
    public int FinancialSecurity { get; }
    public int Environmentalism { get; }
}
```

### Location

**Location**: `DatingApp.ProfileService.Core/Domain/ValueObjects/Location.cs`

Geographic location with optional GPS coordinates.

**Validation Rules**:
- City: required, not empty
- Country: required, not empty
- Latitude: -90 to 90 (optional)
- Longitude: -180 to 180 (optional)

```csharp
public record Location
{
    public string City { get; }
    public string Country { get; }
    public decimal? Latitude { get; }
    public decimal? Longitude { get; }
}
```

### AgeRange

**Location**: `DatingApp.ProfileService.Core/Domain/ValueObjects/AgeRange.cs`

Age preference range for matching.

**Validation Rules**:
- Min: >= 18
- Max: <= 100
- Min <= Max

```csharp
public record AgeRange
{
    public int Min { get; }
    public int Max { get; }
}
```

### PreferenceSet

**Location**: `DatingApp.ProfileService.Core/Domain/ValueObjects/PreferenceSet.cs`

Complete matching preferences.

**Validation Rules**:
- MaxDistanceKm: 1-500
- PreferredLanguages: at least 1 language
- Defensive copy of language list (immutability)

```csharp
public record PreferenceSet
{
    public AgeRange PreferredAgeRange { get; }
    public int MaxDistanceKm { get; }
    public IReadOnlyList<string> PreferredLanguages { get; }
    public GenderPreference GenderPreference { get; }
}
```

### Lifestyle

**Location**: `DatingApp.ProfileService.Core/Domain/ValueObjects/Lifestyle.cs`

Lifestyle preferences using type-safe enums.

```csharp
public record Lifestyle
{
    public ExerciseFrequency ExerciseFrequency { get; }
    public DietType DietType { get; }
    public SmokingStatus SmokingStatus { get; }
    public DrinkingFrequency DrinkingFrequency { get; }
    public bool HasPets { get; }
    public ChildrenPreference WantsChildren { get; }
}
```

### MemberId

**Location**: `DatingApp.ProfileService.Core/Domain/ValueObjects/MemberId.cs`

Strongly-typed ID for members (wraps Guid).

**Features**:
- Implicit conversion to/from Guid
- Type safety (prevents mixing with other IDs)

```csharp
public record MemberId
{
    public Guid Value { get; }

    public static implicit operator Guid(MemberId id) => id.Value;
    public static implicit operator MemberId(Guid value) => new(value);
}
```

### Why Value Objects?

1. **Validation**: Centralized validation in constructor
2. **Immutability**: Cannot be modified after creation (thread-safe)
3. **Equality**: Compared by value, not reference
4. **Ubiquitous Language**: Domain concepts as first-class types
5. **Type Safety**: Compile-time prevention of invalid states

## Domain Events

Domain events capture important state changes in the aggregate.

### Event Base Class

**Location**: `DatingApp.ProfileService.Core/Domain/Events/DomainEvent.cs`

```csharp
public abstract record DomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
```

### ProfileCreated

**Location**: `DatingApp.ProfileService.Core/Domain/Events/ProfileCreated.cs`

Raised when a skeleton profile is first created.

```csharp
public record ProfileCreated : DomainEvent
{
    public Guid MemberId { get; }
    public string Email { get; }
}
```

**Usage**: Notify other services that a new member exists.

### ProfileUpdated

**Location**: `DatingApp.ProfileService.Core/Domain/Events/ProfileUpdated.cs`

Raised when any profile section is updated.

```csharp
public record ProfileUpdated : DomainEvent
{
    public Guid MemberId { get; }
}
```

**Usage**: Invalidate caches, sync search indexes.

### ProfileCompleted

**Location**: `DatingApp.ProfileService.Core/Domain/Events/ProfileCompleted.cs`

Raised when profile completion reaches 80%.

```csharp
public record ProfileCompleted : DomainEvent
{
    public Guid MemberId { get; }
}
```

**Usage**: Enable matching, send congratulations notification.

### Event Management Pattern

```csharp
// In aggregate root
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

// In application service
var profile = await _repository.GetByUserIdAsync(userId);
profile.UpdatePersonality(personality);
await _repository.UpdateAsync(profile);

// Publish all domain events
foreach (var domainEvent in profile.DomainEvents)
{
    await _messagePublisher.PublishAsync(domainEvent);
}
profile.ClearDomainEvents();
```

## Repository Pattern

### IProfileRepository

**Location**: `DatingApp.ProfileService.Core/Domain/Interfaces/IProfileRepository.cs`

Repository abstraction (depends on nothing).

```csharp
public interface IProfileRepository
{
    Task<MemberProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MemberProfile?> GetByUserIdAsync(MemberId userId, CancellationToken cancellationToken = default);
    Task AddAsync(MemberProfile profile, CancellationToken cancellationToken = default);
    Task UpdateAsync(MemberProfile profile, CancellationToken cancellationToken = default);
}
```

**Design Decisions**:
- Returns domain entities (not DTOs)
- Nullable return types for not-found scenarios
- No IQueryable leaking (prevents N+1 queries)
- CancellationToken support for async operations

### ProfileRepository Implementation

**Location**: `DatingApp.ProfileService.Infrastructure/Persistence/ProfileRepository.cs`

EF Core implementation with encryption.

```csharp
public class ProfileRepository : IProfileRepository
{
    private readonly ProfileDbContext _context;
    private readonly IEncryptionService _encryption;

    public async Task<MemberProfile?> GetByUserIdAsync(MemberId userId, ...)
    {
        var entity = await _context.MemberProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId.Value && p.DeletedAt == null, ...);

        if (entity == null) return null;

        // Decrypt before returning domain entity
        return entity.ToDomainModel(_encryption);
    }

    public async Task UpdateAsync(MemberProfile profile, ...)
    {
        var entity = MemberProfileEntity.FromDomainModel(profile, _encryption);
        _context.MemberProfiles.Update(entity);
        await _context.SaveChangesAsync(...);
    }
}
```

**Key Features**:
- Automatic encryption/decryption
- Filters out soft-deleted profiles
- Maps between domain models and EF entities

## Application Services

### ProfileService

**Location**: `DatingApp.ProfileService.Core/Application/Services/ProfileService.cs`

Application service orchestrates use cases.

**Responsibilities**:
1. Orchestrate domain operations
2. Coordinate repository and message publisher
3. Map between DTOs and domain models
4. Publish domain events
5. NO business logic (that's in the domain layer)

**Example Use Case**:

```csharp
public async Task<ProfileResponse> CreateBasicProfileAsync(Guid userId, CreateProfileRequest request, ...)
{
    // 1. Retrieve aggregate
    var profile = await _repository.GetByUserIdAsync(new MemberId(userId), ...);
    if (profile == null) throw new ProfileDomainException("Profile not found");

    // 2. Map DTO to value object
    var location = new Location(
        request.Location.City,
        request.Location.Country,
        request.Location.Latitude,
        request.Location.Longitude
    );

    // 3. Execute domain operation
    profile.UpdateBasicInfo(
        request.FullName,
        request.DateOfBirth,
        request.Gender,
        location
    );

    // 4. Persist changes
    await _repository.UpdateAsync(profile, ...);

    // 5. Publish domain events
    foreach (var domainEvent in profile.DomainEvents)
    {
        await _messagePublisher.PublishAsync(domainEvent, ...);
    }
    profile.ClearDomainEvents();

    // 6. Map to response DTO
    return MapToResponse(profile);
}
```

**Pattern Benefits**:
- Thin application layer (business logic in domain)
- Transactional consistency (single SaveChanges call)
- Event publishing after successful persistence
- Clear separation of concerns

## Data Transfer Objects (DTOs)

**Location**: `DatingApp.ProfileService.Core/Application/DTOs/`

DTOs decouple API contracts from domain models.

### Request DTOs

```csharp
public class CreateProfileRequest
{
    public string FullName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public LocationDto Location { get; set; } = null!;
}

public class UpdateProfileRequest
{
    public PersonalityDto? Personality { get; set; }
    public ValuesDto? Values { get; set; }
    public LifestyleDto? Lifestyle { get; set; }
    public PreferencesDto? Preferences { get; set; }
    public string? ExposureLevel { get; set; }
}
```

### Response DTOs

```csharp
public class ProfileResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public int? Age { get; set; }
    public PersonalityDto? Personality { get; set; }
    // ... other fields
    public int CompletionPercentage { get; set; }
    public bool IsComplete { get; set; }
}
```

### Why DTOs?

1. **Stability**: API contract doesn't change when domain changes
2. **Security**: Don't expose internal domain structure
3. **Flexibility**: Different representations for different clients
4. **Validation**: DataAnnotations for API-level validation

## Encryption Strategy

**Location**: `DatingApp.ProfileService.Infrastructure/Security/EncryptionService.cs`

### Encrypted Fields

All PII is encrypted at rest:
- UserId, Email, FullName, DateOfBirth, Gender
- Location (City, Country)
- All JSON value objects (Personality, Values, Lifestyle, Preferences)

### Implementation

```csharp
public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}

public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var encrypted = encryptor.TransformFinalBlock(
            Encoding.UTF8.GetBytes(plainText), 0, plainText.Length);

        // Prepend IV to ciphertext
        var result = new byte[aes.IV.Length + encrypted.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encrypted, 0, result, aes.IV.Length, encrypted.Length);

        return Convert.ToBase64String(result);
    }
}
```

### Entity Mapping with Encryption

```csharp
public class MemberProfileEntity
{
    // Encrypted fields
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PersonalityJson { get; set; }

    public static MemberProfileEntity FromDomainModel(
        MemberProfile profile,
        IEncryptionService encryption)
    {
        return new MemberProfileEntity
        {
            UserId = encryption.Encrypt(profile.UserId.Value.ToString()),
            Email = encryption.Encrypt(profile.Email),
            PersonalityJson = profile.Personality == null ? null :
                encryption.Encrypt(JsonSerializer.Serialize(profile.Personality)),
            // ...
        };
    }

    public MemberProfile ToDomainModel(IEncryptionService encryption)
    {
        var userId = new MemberId(Guid.Parse(encryption.Decrypt(UserId)));
        var profile = MemberProfile.CreateSkeleton(userId, encryption.Decrypt(Email));

        if (PersonalityJson != null)
        {
            var personality = JsonSerializer.Deserialize<PersonalityProfile>(
                encryption.Decrypt(PersonalityJson));
            profile.UpdatePersonality(personality!);
        }

        return profile;
    }
}
```

### Key Management

- Key provided via `Encryption__Key` environment variable
- Base64-encoded, minimum 32 bytes (256-bit AES)
- Same key must be used across all service instances
- Key rotation requires data re-encryption (not implemented in MVP)

## Summary

### Pattern Hierarchy

```
Controllers (API Layer)
    ↓
Application Services (Use Cases)
    ↓
Domain Model (Aggregates + Value Objects)
    ↓
Repository Interface (Abstraction)
    ↓
Repository Implementation (Infrastructure)
    ↓
Database (Encrypted Persistence)
```

### Key Principles

1. **Dependency Inversion**: Core depends on nothing, Infrastructure depends on Core
2. **Encapsulation**: Aggregate enforces invariants, no public setters
3. **Immutability**: Value objects are immutable records
4. **Event-Driven**: Domain events for inter-service communication
5. **Security by Design**: Encryption at the infrastructure boundary
6. **Testability**: Mocked repositories and publishers in unit tests

### References

- [Domain-Driven Design by Eric Evans](https://www.domainlanguage.com/ddd/)
- [Implementing Domain-Driven Design by Vaughn Vernon](https://vaughnvernon.com/)
- [Microsoft .NET Microservices Architecture](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/)
- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
