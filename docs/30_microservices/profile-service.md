# ProfileService - User Profile Management

> **DDD-based profile management for QuietMatch**
>
> Handles user profiles, personality traits, values, lifestyle preferences, and matching criteria.

---

## Table of Contents

- [Overview](#overview)
- [Architecture Pattern](#architecture-pattern)
- [Responsibilities](#responsibilities)
- [API Endpoints](#api-endpoints)
- [Domain Model](#domain-model)
- [Database Schema](#database-schema)
- [Events Published](#events-published)
- [Configuration](#configuration)
- [Security Considerations](#security-considerations)
- [Testing Strategy](#testing-strategy)
- [Deployment](#deployment)

---

## Overview

**ProfileService** manages all user profile data for QuietMatch members. It provides:

- **Profile Creation**: Initialize user profiles from skeleton to completion
- **Profile Sections**: Basic info, personality, values, lifestyle, preferences
- **Completion Tracking**: Calculate profile completion percentage (0-100%)
- **Privacy Control**: Manage profile visibility (Public, MatchedOnly, Private)
- **Event Publishing**: Notify other services of profile changes

**Why Domain-Driven Design?**

ProfileService uses DDD (Clean Architecture) because:
1. **Rich Domain Logic**: Profile completion rules, validation, privacy logic
2. **Business Invariants**: Age >= 18, completion percentage calculation
3. **Aggregate Root**: `MemberProfile` enforces consistency boundaries
4. **Ubiquitous Language**: Personality, Values, Lifestyle as first-class concepts
5. **Testability**: Domain logic isolated from infrastructure

**Trade-Offs**:
- **Pro**: Maintainable, testable, scalable domain logic
- **Con**: More complex than simple CRUD (Layered architecture)
- **Mitigation**: Clear layer separation, comprehensive documentation

---

## Architecture Pattern

**Pattern**: **Clean Architecture (DDD)**

**Why Clean Architecture for ProfileService?**
- Complex business rules (profile completion, validation)
- Rich domain model (8 value objects, 1 aggregate root)
- Need for testability and maintainability
- Clear separation of concerns (domain vs. infrastructure)

**Layers**:
1. **Domain**: Entities, Value Objects, Domain Events, Business Rules
2. **Application**: Use Cases, DTOs, Service Orchestration
3. **Infrastructure**: EF Core, Repositories, Encryption, RabbitMQ
4. **API**: Controllers, Middleware, API Models

**See PATTERNS.md** in this service's source folder for detailed DDD pattern explanations.

---

## Responsibilities

### Core Responsibilities

1. **Profile Management**:
   - Create skeleton profiles from `UserRegistered` events
   - Update profile sections (basic info, personality, values, lifestyle, preferences)
   - Calculate completion percentage
   - Soft delete profiles

2. **Validation**:
   - Enforce business rules (age >= 18, score ranges 1-5)
   - Validate value object constraints
   - Ensure data consistency

3. **Privacy**:
   - Manage exposure levels (Public, MatchedOnly, Private)
   - Control profile visibility for matching

4. **Events**:
   - Publish `ProfileCreated`, `ProfileUpdated`, `ProfileCompleted` events
   - Notify matching service when profile reaches 80% completion

### Non-Responsibilities

- ❌ User authentication (handled by IdentityService)
- ❌ Profile photos/media (handled by MediaService - future)
- ❌ Matching algorithm (handled by MatchingService)
- ❌ User notifications (handled by NotificationService)

---

## API Endpoints

### REST API

Base URL: `/api/profiles`

#### `GET /api/profiles/{userId}`
**Description**: Get user profile by userId

**Headers**:
```
Authorization: Bearer <access-token>
```

**Response** (200 OK):
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "user@example.com",
  "fullName": "John Doe",
  "age": 28,
  "gender": "Male",
  "location": {
    "city": "New York",
    "country": "USA",
    "latitude": 40.7128,
    "longitude": -74.0060
  },
  "personality": {
    "openness": 4,
    "conscientiousness": 3,
    "extraversion": 5,
    "agreeableness": 4,
    "neuroticism": 2,
    "aboutMe": "I love coding and hiking",
    "lifePhilosophy": "Live and let live"
  },
  "values": {
    "familyOrientation": 5,
    "careerAmbition": 4,
    "spirituality": 3,
    "adventure": 5,
    "intellectualCuriosity": 4,
    "socialJustice": 3,
    "financialSecurity": 4,
    "environmentalism": 5
  },
  "lifestyle": {
    "exerciseFrequency": "Regularly",
    "dietType": "Vegetarian",
    "smokingStatus": "Never",
    "drinkingFrequency": "Socially",
    "hasPets": true,
    "wantsChildren": "Maybe"
  },
  "preferences": {
    "preferredAgeRange": {
      "min": 25,
      "max": 35
    },
    "maxDistanceKm": 50,
    "preferredLanguages": ["English", "Spanish"],
    "genderPreference": "Women"
  },
  "completionPercentage": 100,
  "isComplete": true,
  "exposureLevel": "MatchedOnly"
}
```

**Errors**:
- `401 Unauthorized`: Invalid or missing JWT token
- `403 Forbidden`: userId in token doesn't match path parameter
- `404 Not Found`: Profile not found

---

#### `POST /api/profiles/{userId}/basic`
**Description**: Create basic profile information

**Headers**:
```
Authorization: Bearer <access-token>
Content-Type: application/json
```

**Request**:
```json
{
  "fullName": "John Doe",
  "dateOfBirth": "1995-05-15T00:00:00Z",
  "gender": "Male",
  "location": {
    "city": "New York",
    "country": "USA",
    "latitude": 40.7128,
    "longitude": -74.0060
  }
}
```

**Response** (201 Created):
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fullName": "John Doe",
  "age": 28,
  "completionPercentage": 20
}
```

**Business Rules**:
- Age must be >= 18 years
- Location is optional but recommended
- GPS coordinates are optional

**Errors**:
- `400 Bad Request`: Validation errors (age < 18, invalid data)
- `401 Unauthorized`: Invalid JWT
- `404 Not Found`: Skeleton profile not found

---

#### `PUT /api/profiles/{userId}`
**Description**: Update profile sections (partial updates supported)

**Headers**:
```
Authorization: Bearer <access-token>
Content-Type: application/json
```

**Request** (all fields optional):
```json
{
  "personality": {
    "openness": 4,
    "conscientiousness": 3,
    "extraversion": 5,
    "agreeableness": 4,
    "neuroticism": 2,
    "aboutMe": "I love coding and hiking",
    "lifePhilosophy": "Live and let live"
  },
  "values": {
    "familyOrientation": 5,
    "careerAmbition": 4,
    "spirituality": 3,
    "adventure": 5,
    "intellectualCuriosity": 4,
    "socialJustice": 3,
    "financialSecurity": 4,
    "environmentalism": 5
  },
  "lifestyle": {
    "exerciseFrequency": "Regularly",
    "dietType": "Vegetarian",
    "smokingStatus": "Never",
    "drinkingFrequency": "Socially",
    "hasPets": true,
    "wantsChildren": "Maybe"
  },
  "preferences": {
    "preferredAgeRange": {
      "min": 25,
      "max": 35
    },
    "maxDistanceKm": 50,
    "preferredLanguages": ["English", "Spanish"],
    "genderPreference": "Women"
  },
  "exposureLevel": "MatchedOnly"
}
```

**Response** (200 OK): Full profile DTO

**Business Rules**:
- Personality scores: 1-5 (required if personality provided)
- Value scores: 1-5 (required if values provided)
- Age range: Min >= 18, Max <= 100, Min <= Max
- Max distance: 1-500 km
- At least one preferred language required

**Errors**:
- `400 Bad Request`: Validation errors
- `401 Unauthorized`: Invalid JWT
- `404 Not Found`: Profile not found

---

#### `DELETE /api/profiles/{userId}`
**Description**: Soft delete user profile

**Headers**:
```
Authorization: Bearer <access-token>
```

**Response** (204 No Content)

**Note**: This is a soft delete. Profile is marked as deleted but not removed from database.

**Errors**:
- `401 Unauthorized`: Invalid JWT
- `404 Not Found`: Profile not found

---

## Domain Model

### MemberProfile Aggregate

**Location**: `DatingApp.ProfileService.Core/Domain/Entities/MemberProfile.cs`

The `MemberProfile` is the aggregate root managing all profile data.

**Completion Model**:
- Basic Info: 20%
- Personality: 20%
- Values: 20%
- Lifestyle: 20%
- Preferences: 20%
- **Total**: 100% possible, **80% required** for matching eligibility

**Privacy Levels**:
```csharp
public enum ExposureLevel
{
    Public = 0,      // Visible to all matched users
    MatchedOnly = 1, // Visible only to mutual matches
    Private = 2      // Not shared in matching
}
```

### Value Objects

1. **PersonalityProfile**: Big Five traits (OCEAN) + text fields
2. **Values**: 8 value dimensions (1-5 scale)
3. **Location**: City, country, GPS coordinates (optional)
4. **AgeRange**: Min/max age for matching
5. **PreferenceSet**: Age, distance, languages, gender preference
6. **Lifestyle**: Exercise, diet, smoking, drinking, pets, children
7. **MemberId**: Strongly-typed ID (wraps Guid)

**See PATTERNS.md** for detailed value object documentation.

---

## Database Schema

**Database**: `profile_db`

### Table: `member_profiles`

```sql
CREATE TABLE member_profiles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id TEXT NOT NULL, -- Encrypted
    email TEXT NOT NULL, -- Encrypted
    full_name TEXT, -- Encrypted
    date_of_birth TEXT, -- Encrypted
    gender TEXT, -- Encrypted
    location_city TEXT, -- Encrypted
    location_country TEXT, -- Encrypted
    location_latitude DECIMAL(10,7),
    location_longitude DECIMAL(10,7),
    personality_json TEXT, -- Encrypted JSON
    values_json TEXT, -- Encrypted JSON
    lifestyle_json TEXT, -- Encrypted JSON
    preferences_json TEXT, -- Encrypted JSON
    completion_percentage INT NOT NULL DEFAULT 0,
    exposure_level INT NOT NULL DEFAULT 0,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    deleted_at TIMESTAMP NULL,
    CONSTRAINT check_completion CHECK (completion_percentage >= 0 AND completion_percentage <= 100)
);

CREATE INDEX idx_member_profiles_user_id ON member_profiles(user_id);
CREATE INDEX idx_member_profiles_deleted_at ON member_profiles(deleted_at);
```

**Encryption**:
- All PII fields encrypted with AES-256
- Encryption key stored in Azure Key Vault (production)
- IV prepended to ciphertext
- JSON value objects encrypted as serialized JSON strings

---

## Events Published

### `ProfileCreated`

**When**: Skeleton profile created from `UserRegistered` event

**Payload**:
```json
{
  "eventId": "550e8400-e29b-41d4-a716-446655440000",
  "occurredAt": "2025-11-27T10:00:00Z",
  "memberId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "user@example.com"
}
```

**Exchange**: `profile-events`
**Routing Key**: `profile.created`

**Subscribers**:
- **NotificationService**: Send profile setup instructions
- **AnalyticsService**: Track profile creation rate

---

### `ProfileUpdated`

**When**: Any profile section is updated

**Payload**:
```json
{
  "eventId": "550e8400-e29b-41d4-a716-446655440000",
  "occurredAt": "2025-11-27T10:05:00Z",
  "memberId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Exchange**: `profile-events`
**Routing Key**: `profile.updated`

**Subscribers**:
- **MatchingService**: Invalidate matching cache
- **SearchService**: Update search index (future)

---

### `ProfileCompleted`

**When**: Profile completion reaches 80%

**Payload**:
```json
{
  "eventId": "550e8400-e29b-41d4-a716-446655440000",
  "occurredAt": "2025-11-27T10:10:00Z",
  "memberId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Exchange**: `profile-events`
**Routing Key**: `profile.completed`

**Subscribers**:
- **MatchingService**: Enable matching for this user
- **NotificationService**: Send congratulations message
- **AnalyticsService**: Track completion funnel

---

## Configuration

### Environment Variables

```bash
# Database
ConnectionStrings__ProfileDb=Host=postgres;Port=5432;Database=profile_db;Username=admin;Password=***

# RabbitMQ
RabbitMQ__Host=rabbitmq
RabbitMQ__Username=guest
RabbitMQ__Password=guest

# JWT Authentication
Jwt__Issuer=DatingApp.IdentityService
Jwt__Audience=DatingApp.ProfileService
Jwt__Secret=*** # Min 32 bytes, same as IdentityService

# Encryption (Base64-encoded 32-byte key)
Encryption__Key=***

# Logging
Seq__Url=http://seq:80
ApplicationInsights__ConnectionString=*** # Azure only
```

---

## Security Considerations

### 1. **Encryption at Rest**

**Threat**: Database breach exposes PII

**Mitigation**:
- All PII encrypted with AES-256 before storage
- Encryption key stored in Azure Key Vault (production)
- IV randomized per encryption operation
- Decryption only in application layer (never in database queries)

**Implementation**:
```csharp
public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        var encrypted = aes.CreateEncryptor()
            .TransformFinalBlock(Encoding.UTF8.GetBytes(plainText), 0, plainText.Length);

        // Prepend IV to ciphertext
        var result = new byte[aes.IV.Length + encrypted.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encrypted, 0, result, aes.IV.Length, encrypted.Length);

        return Convert.ToBase64String(result);
    }
}
```

---

### 2. **JWT Authorization**

**Threat**: Unauthorized profile access

**Mitigation**:
- All endpoints require JWT bearer token
- Verify `userId` claim matches route parameter
- Reject expired or invalid signatures
- Use same JWT secret as IdentityService

**Implementation**:
```csharp
[Authorize]
[HttpGet("{userId}")]
public async Task<IActionResult> GetProfile([FromRoute] Guid userId)
{
    var userIdClaim = User.FindFirst("sub")?.Value;
    if (userIdClaim != userId.ToString())
    {
        return Forbid(); // userId in token doesn't match route
    }

    // Proceed with profile retrieval
}
```

---

### 3. **Privacy Controls**

**Threat**: Profile oversharing to unmatched users

**Mitigation**:
- `ExposureLevel` enum controls visibility
- `CanShareWith()` method on aggregate enforces access control
- MatchingService checks exposure level before sharing profiles

**Business Logic**:
```csharp
public bool CanShareWith(ExposureLevel requesterLevel)
{
    return ExposureLevel <= requesterLevel;
}
```

---

### 4. **Event Publishing Security**

**Threat**: Sensitive data leaked in events

**Mitigation**:
- Events contain only IDs, no PII
- Email included in `ProfileCreated` only (one-time setup)
- RabbitMQ requires authentication (guest/guest in dev, managed identity in prod)
- TLS encryption for message broker connections in production

---

## Testing Strategy

### Unit Tests

**Coverage**: Domain logic, value objects, application services

**Location**: `DatingApp.ProfileService.Tests.Unit/`

**Examples**:
```csharp
// Value object validation
[Fact]
public void PersonalityProfile_WithInvalidScore_ShouldThrowException()
{
    var act = () => new PersonalityProfile(0, 3, 3, 3, 3); // Invalid: 0 < 1
    act.Should().Throw<ProfileDomainException>()
        .WithMessage("*must be between 1 and 5*");
}

// Aggregate business rules
[Fact]
public void UpdateBasicInfo_WithAgeLessThan18_ShouldThrowException()
{
    var profile = MemberProfile.CreateSkeleton(memberId, "test@example.com");
    var location = new Location("NYC", "USA");

    var act = () => profile.UpdateBasicInfo("John", DateTime.UtcNow.AddYears(-17), "Male", location);
    act.Should().Throw<ProfileDomainException>()
        .WithMessage("*at least 18*");
}

// Completion percentage
[Fact]
public void CompletionPercentage_WithAllSections_ShouldBe100()
{
    var profile = MemberProfile.CreateSkeleton(memberId, "test@example.com");
    profile.UpdateBasicInfo("John", DateTime.UtcNow.AddYears(-25), "Male", location);
    profile.UpdatePersonality(personality);
    profile.UpdateValues(values);
    profile.UpdateLifestyle(lifestyle);
    profile.UpdatePreferences(preferences);

    profile.CompletionPercentage.Should().Be(100);
}
```

**Test Results**: 50/50 passing (100% coverage)

---

### Integration Tests

**Coverage**: Database persistence, encryption, event publishing

**Future Work**:
- API endpoint tests with TestContainers (PostgreSQL)
- RabbitMQ integration tests
- End-to-end profile creation flow

---

## Deployment

### Local (Docker Compose)

```yaml
# docker-compose.yml
profile-service:
  build:
    context: .
    dockerfile: src/Services/Profile/Dockerfile
  ports:
    - "5002:80"
  environment:
    - ConnectionStrings__ProfileDb=Host=postgres;Port=5432;Database=profile_db;...
    - Encryption__Key=${ENCRYPTION_KEY}
    - Jwt__Secret=${JWT_SECRET}
  depends_on:
    postgres:
      condition: service_healthy
    rabbitmq:
      condition: service_healthy
```

**Run**:
```bash
docker-compose up profile-service
```

---

### Azure (Container Apps)

```bicep
resource profileServiceApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'profile-service'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      secrets: [
        {
          name: 'encryption-key'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/profile-encryption-key'
          identity: 'system'
        }
        {
          name: 'jwt-secret'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/jwt-secret'
          identity: 'system'
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'profile-service'
          image: 'quietmatch.azurecr.io/profile-service:latest'
          env: [
            {
              name: 'Encryption__Key'
              secretRef: 'encryption-key'
            }
            {
              name: 'Jwt__Secret'
              secretRef: 'jwt-secret'
            }
          ]
        }
      ]
    }
  }
}
```

---

**Next Steps**:
- Read `src/Services/Profile/README.md` for detailed service documentation
- Read `src/Services/Profile/PATTERNS.md` for DDD pattern details
- Review [Architecture Guidelines](../10_architecture/02_architecture-guidelines.md) for broader context

---

**Last Updated**: 2025-11-27
**Document Owner**: ProfileService Team
**Status**: ✅ Implemented (F0002 - MVP Complete)
