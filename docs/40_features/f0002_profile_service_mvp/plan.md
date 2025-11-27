# Implementation Plan - F0002: ProfileService MVP

**Status**: 🔴 Not Started
**Feature File**: [f0002_profile_service_mvp.md](./f0002_profile_service_mvp.md)
**Architecture Pattern**: Onion Architecture (ProfileService)
**Started**: TBD
**Last Updated**: 2025-11-27
**Estimated Total Time**: 20-24 hours

**🔐 Encryption Update**: Enhanced encryption coverage for GDPR compliance:
- **5 encrypted fields**: FullName, Email, DateOfBirth, LocationLatitude, LocationLongitude
- **Rationale**: All direct PII identifiers must be encrypted at rest per GDPR Article 32
- **Implementation**: EF Core value converters (transparent encryption at persistence layer)

---

## Overview

This plan provides a detailed, smart implementation roadmap for F0002 (ProfileService MVP), applying all QuietMatch architecture guidelines, patterns, and best practices to this specific feature.

**What this plan contains**:
- Step-by-step tasks organized by Onion Architecture layers (Domain → Application → Infrastructure → API)
- Documentation references for each phase
- Architecture reasoning (WHY we do things this way)
- Entity/property specifications (WHAT to create)
- Configuration details
- Testing scenarios

**What this plan does NOT contain**:
- Complete class implementations (you'll write the code)
- Full method bodies
- Copy-paste bash commands (except key references)

---

## Prerequisites

### Documentation to Review Before Starting

- [ ] **Read completely**: [Feature Specification](./f0002_profile_service_mvp.md)
- [ ] **Read**: [Architecture Guidelines - Onion Architecture](../../10_architecture/02_architecture-guidelines.md) (Sections on Onion pattern, why it's used for ProfileService)
- [ ] **Read**: [Service Templates - Onion Folder Structure](../../10_architecture/03_service-templates.md) (Section 3: Onion Architecture example)
- [ ] **Read**: [Security & Auth - Field-Level Encryption](../../10_architecture/05_security-and-auth.md) (Sections on AES-256 encryption, EF Core value converters)
- [ ] **Read**: [Messaging & Integration - Event Consumers](../../10_architecture/06_messaging-and-integration.md) (MassTransit consumers, UserRegistered event)
- [ ] **Read**: [Ubiquitous Language - ProfileService Terms](../../20_domain/01_domain-ubiquitous-language.md) (MemberProfile, PersonalityProfile, Values, etc.)

### Environment Setup

- [ ] Docker Desktop installed and running
- [ ] Start infrastructure: `docker-compose up -d`
- [ ] Verify PostgreSQL running and accessible:
  - Host: localhost:5432
  - Database: profile_db (will be created)
  - User: admin
  - Password: QuietMatch_Dev_2025!
- [ ] Verify RabbitMQ running: http://localhost:15672 (guest/guest)
- [ ] Verify IdentityService running and publishing `UserRegistered` events

---

## Phase 0: Project Setup (45-60 minutes)

### Create Feature Branch

- [ ] Create and checkout feature branch
  - **Branch name**: `feature/f0002-profile-service-mvp`
  - **Why**: Isolate feature development, enable PR workflow
  - **Reference**: [Feature Workflow - Git Branching](../../60_operations/feature-workflow.md)

### Create Solution and Projects

- [ ] Navigate to `src/Services/` and create Profile folder
- [ ] Create .NET solution file: `DatingApp.ProfileService.sln`
- [ ] Create projects following **Onion Architecture**:
  - **DatingApp.ProfileService.Core** (classlib) - Domain + Application (inner layers)
    - Contains: Domain/, Application/ folders
    - **Why**: Onion combines Domain and Application in one "Core" project
  - **DatingApp.ProfileService.Infrastructure** (classlib) - External dependencies (DB, messaging, encryption)
  - **DatingApp.ProfileService.Api** (webapi) - HTTP endpoints (outermost layer)
  - **DatingApp.ProfileService.Tests.Unit** (xunit) - Unit tests
  - **DatingApp.ProfileService.Tests.Integration** (xunit) - Integration tests
  - **Reference**: [Service Templates - Onion Project Structure](../../10_architecture/03_service-templates.md)

### Configure Project References

- [ ] Set up dependency chain (**Onion pattern** - dependencies point inward):
  - **API** → Infrastructure + Core
  - **Infrastructure** → Core (ONLY)
  - **Core** → (NO external dependencies - pure domain logic)
  - **Tests.Unit** → Core
  - **Tests.Integration** → API
  - **Why**: Onion architecture enforces dependencies flow inward toward domain core

### Install NuGet Packages

- [ ] Core layer packages:
  - (None - keep core pure, no external dependencies except base class library)
  - **Why**: Domain logic must not depend on frameworks

- [ ] Infrastructure layer packages:
  - Npgsql.EntityFrameworkCore.PostgreSQL (database)
  - Microsoft.EntityFrameworkCore.Design (migrations)
  - MassTransit.RabbitMQ (messaging)
  - MassTransit.EntityFrameworkCore (outbox pattern)
  - **Why**: Infrastructure adapters implement domain ports

- [ ] API layer packages:
  - Microsoft.AspNetCore.Authentication.JwtBearer (JWT middleware)
  - Serilog.AspNetCore (structured logging)
  - Serilog.Sinks.Seq (log shipping)
  - Swashbuckle.AspNetCore (Swagger/OpenAPI)
  - AspNetCore.HealthChecks.NpgSql (health checks)
  - AspNetCore.HealthChecks.Rabbitmq

- [ ] Test packages:
  - Moq (mocking)
  - FluentAssertions (readable assertions)
  - Testcontainers.PostgreSql (real database for integration tests)
  - Testcontainers.RabbitMq (real message broker for integration tests)
  - Microsoft.AspNetCore.Mvc.Testing (API testing)

### Create Folder Structure (Onion Architecture)

- [ ] Create folder structure inside `Core` project:
  ```
  Core/
  ├── Domain/
  │   ├── Entities/
  │   ├── ValueObjects/
  │   ├── Events/
  │   ├── Exceptions/
  │   └── Interfaces/  (ports - repository interfaces)
  └── Application/
      ├── UseCases/
      ├── DTOs/
      └── Services/
  ```

- [ ] Create folder structure inside `Infrastructure` project:
  ```
  Infrastructure/
  ├── Data/
  │   ├── ProfileDbContext.cs
  │   └── Configurations/
  ├── Repositories/
  ├── Security/
  │   └── EncryptionService.cs
  └── Messaging/
      └── Consumers/
  ```

### Create PATTERNS.md

- [ ] Create `src/Services/Profile/PATTERNS.md` explaining:
  - Why Onion architecture chosen for ProfileService (rich domain logic, privacy rules)
  - Folder structure breakdown (Core vs Infrastructure vs API)
  - Layer responsibilities and dependency flow
  - Domain-centric design principles
  - Alternative patterns considered (Layered, Hexagonal) and why Onion chosen
  - **Reference**: [Architecture Guidelines - Pattern Selection](../../10_architecture/02_architecture-guidelines.md)

### Create README.md

- [ ] Create `src/Services/Profile/README.md` with:
  - Service overview
  - Architecture: Onion
  - How to run locally
  - API endpoints summary
  - Environment variables needed

### Update Database Initialization Script

- [ ] Add profile_db to `infrastructure/docker/init-db.sql`:
  ```sql
  CREATE DATABASE profile_db;
  ```

### Update docker-compose.yml

- [ ] Add ProfileService to docker-compose.yml:
  - Service name: `profile-service`
  - Port mapping: `5001:8080`
  - Environment variables: connection strings, messaging, JWT config
  - Depends on: postgres, rabbitmq

### Initial Commit

- [ ] Commit project structure
  - Message: `feat(profile): initial project structure for F0002 (#issue-number)`

---

## Phase 1: Domain Layer (3-4 hours)

**Reference**: [Architecture Guidelines - Domain Layer](../../10_architecture/02_architecture-guidelines.md)
**Why Domain First**: In Onion architecture, domain is the heart - richest business logic, zero dependencies

### Create Domain Exceptions

- [ ] Create `ProfileDomainException` in `Core/Domain/Exceptions/`
  - Base exception for all profile domain violations
  - **Why**: Distinguish domain rule violations from infrastructure errors

### Create Value Objects

- [ ] Create `MemberId` value object
  - **Location**: `Core/Domain/ValueObjects/MemberId.cs`
  - **Properties**: `Value` (Guid)
  - **Why**: Type-safe user ID, prevents primitive obsession
  - **Reference**: [Ubiquitous Language - MemberId](../../20_domain/01_domain-ubiquitous-language.md)

- [ ] Create `PersonalityProfile` value object
  - **Location**: `Core/Domain/ValueObjects/PersonalityProfile.cs`
  - **Properties**:
    - Openness (int, 1-5)
    - Conscientiousness (int, 1-5)
    - Extraversion (int, 1-5)
    - Agreeableness (int, 1-5)
    - Neuroticism (int, 1-5)
    - AboutMe (string, max 500 chars)
    - LifePhilosophy (string, max 500 chars)
  - **Business Rules**:
    - Validate all trait scores 1-5 in constructor
    - Throw `ProfileDomainException` if invalid
  - **Why**: Encapsulates Big Five personality model, enforces invariants
  - **Reference**: [Feature Spec - AC11-14](./f0002_profile_service_mvp.md)
  - **Reference**: [Ubiquitous Language - PersonalityProfile](../../20_domain/01_domain-ubiquitous-language.md)

- [ ] Create `Values` value object
  - **Location**: `Core/Domain/ValueObjects/Values.cs`
  - **Properties**:
    - FamilyOrientation (int, 1-5)
    - CareerAmbition (int, 1-5)
    - Spirituality (int, 1-5)
    - Adventure (int, 1-5)
    - IntellectualCuriosity (int, 1-5)
    - SocialJustice (int, 1-5)
    - FinancialSecurity (int, 1-5)
    - Environmentalism (int, 1-5)
  - **Business Rules**: Validate all scores 1-5
  - **Reference**: [Feature Spec - AC15-17](./f0002_profile_service_mvp.md)

- [ ] Create `Lifestyle` value object
  - **Location**: `Core/Domain/ValueObjects/Lifestyle.cs`
  - **Properties**:
    - ExerciseFrequency (enum: Never, Occasionally, Regularly, Daily)
    - DietType (enum: Omnivore, Vegetarian, Vegan, Pescatarian, Other)
    - SmokingStatus (enum: Never, Occasionally, Regularly, Trying to Quit)
    - DrinkingFrequency (enum: Never, Occasionally, Socially, Regularly)
    - HasPets (bool)
    - WantsChildren (enum: Yes, No, Maybe, Has Children)
  - **Reference**: [Feature Spec - AC18-20](./f0002_profile_service_mvp.md)

- [ ] Create `AgeRange` value object
  - **Location**: `Core/Domain/ValueObjects/AgeRange.cs`
  - **Properties**: Min (int), Max (int)
  - **Business Rules**:
    - Min >= 18
    - Max <= 100
    - Min <= Max
  - **Why**: Encapsulates age preference validation

- [ ] Create `PreferenceSet` value object
  - **Location**: `Core/Domain/ValueObjects/PreferenceSet.cs`
  - **Properties**:
    - PreferredAgeRange (AgeRange)
    - MaxDistanceKm (int, 1-500)
    - PreferredLanguages (List<string>)
    - GenderPreference (enum: Men, Women, NonBinary, NoPreference)
  - **Business Rules**: Validate distance range
  - **Reference**: [Feature Spec - AC21-24](./f0002_profile_service_mvp.md)

- [ ] Create `Location` value object
  - **Location**: `Core/Domain/ValueObjects/Location.cs`
  - **Properties**: City, Country, Latitude, Longitude
  - **Why**: Geo data belongs together as a value object

- [ ] Create `ExposureLevel` enum
  - **Location**: `Core/Domain/ValueObjects/ExposureLevel.cs`
  - **Values**: MatchedOnly, AllMatches, Public
  - **Reference**: [Feature Spec - AC25-28](./f0002_profile_service_mvp.md)
  - **Reference**: [Ubiquitous Language - ExposureLevel](../../20_domain/01_domain-ubiquitous-language.md)

### Create Domain Events

- [ ] Create `ProfileCreated` domain event
  - **Location**: `Core/Domain/Events/ProfileCreated.cs`
  - **Properties**: UserId, Email, CreatedAt, CorrelationId
  - **Reference**: [Feature Spec - Events Published](./f0002_profile_service_mvp.md)

- [ ] Create `ProfileUpdated` domain event
  - **Properties**: UserId, UpdatedFields (List<string>), UpdatedAt, CorrelationId

- [ ] Create `ProfileCompleted` domain event
  - **Properties**: UserId, CompletedAt, CorrelationId
  - **When**: Profile reaches 100% completion for first time

### Create MemberProfile Entity (Aggregate Root)

- [ ] Create `MemberProfile` entity
  - **Location**: `Core/Domain/Entities/MemberProfile.cs`
  - **Properties**:
    - UserId (MemberId) - Primary key
    - Email (string) - Will be encrypted via Infrastructure (from UserRegistered event)
    - FullName (string) - Will be encrypted via Infrastructure
    - DateOfBirth (DateTime) - Will be encrypted via Infrastructure
    - Gender (string)
    - Location (Location value object) - Latitude/Longitude will be encrypted
    - Personality (PersonalityProfile value object)
    - Values (Values value object)
    - Lifestyle (Lifestyle value object)
    - Preferences (PreferenceSet value object)
    - ExposureLevel (ExposureLevel enum)
    - CompletionPercentage (int, 0-100)
    - IsComplete (bool)
    - CreatedAt (DateTime)
    - UpdatedAt (DateTime)
    - DeletedAt (DateTime?)
  - **Business Rules** (methods to implement):
    - `static CreateSkeleton(MemberId userId, string email)` - factory for skeleton profile
    - `UpdatePersonality(PersonalityProfile personality)` - update personality, recalc completion
    - `UpdateValues(Values values)` - update values, recalc completion
    - `UpdateLifestyle(Lifestyle lifestyle)` - update lifestyle, recalc completion
    - `UpdatePreferences(PreferenceSet preferences)` - update preferences, recalc completion
    - `UpdateExposureLevel(ExposureLevel level)` - change privacy
    - `SoftDelete()` - mark as deleted (GDPR)
    - `CalculateCompletion()` - private method: return percentage based on filled fields
    - `CheckIfComplete()` - private method: mark IsComplete=true if >= 80%
    - `CanShareWith(MemberId otherId, MatchStatus matchStatus)` - privacy logic
  - **Domain Events**:
    - Raise `ProfileCreated` on creation
    - Raise `ProfileUpdated` on any update
    - Raise `ProfileCompleted` when IsComplete changes from false → true
  - **Why**: Rich aggregate root with business logic, enforces invariants
  - **Reference**: [Feature Spec - AC5-10, AC25-31](./f0002_profile_service_mvp.md)
  - **Reference**: [Ubiquitous Language - MemberProfile](../../20_domain/01_domain-ubiquitous-language.md)

### Create Repository Interfaces (Ports)

- [ ] Create `IProfileRepository` interface
  - **Location**: `Core/Domain/Interfaces/IProfileRepository.cs`
  - **Methods**:
    - `Task<MemberProfile?> GetByUserIdAsync(MemberId userId, CancellationToken ct = default)`
    - `Task AddAsync(MemberProfile profile, CancellationToken ct = default)`
    - `Task UpdateAsync(MemberProfile profile, CancellationToken ct = default)`
    - `Task<bool> ExistsAsync(MemberId userId, CancellationToken ct = default)`
  - **Why**: Port defined in domain, adapter implemented in Infrastructure (Onion pattern)
  - **Reference**: [Architecture Guidelines - Ports and Adapters](../../10_architecture/02_architecture-guidelines.md)

### Create Encryption Service Interface (Port)

- [ ] Create `IEncryptionService` interface
  - **Location**: `Core/Domain/Interfaces/IEncryptionService.cs`
  - **Methods**:
    - `string Encrypt(string plainText)`
    - `string Decrypt(string cipherText)`
  - **Why**: Domain defines contract, Infrastructure provides AES-256 implementation

### Create Message Publisher Interface (Port)

- [ ] Create `IMessagePublisher` interface
  - **Location**: `Core/Domain/Interfaces/IMessagePublisher.cs`
  - **Methods**:
    - `Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class`
  - **Why**: Domain publishes events via port, Infrastructure adapts to RabbitMQ/Azure Service Bus

---

## Phase 2: Application Layer (2-3 hours)

**Reference**: [Architecture Guidelines - Application Layer](../../10_architecture/02_architecture-guidelines.md)
**Why Application Layer**: Orchestrates domain logic, handles use cases, no business rules (those are in Domain)

### Create DTOs

- [ ] Create `CreateProfileRequest` DTO
  - **Location**: `Core/Application/DTOs/CreateProfileRequest.cs`
  - **Properties**: Match all fields from API spec
  - **Why**: Input model for profile creation

- [ ] Create `UpdateProfileRequest` DTO
  - **Location**: `Core/Application/DTOs/UpdateProfileRequest.cs`
  - **Properties**: All fields nullable (partial updates)
  - **Why**: PATCH semantics

- [ ] Create `ProfileResponse` DTO
  - **Location**: `Core/Application/DTOs/ProfileResponse.cs`
  - **Properties**: Match API spec response format
  - **Why**: Output model for API responses

### Create FluentValidation Validators

- [ ] Create `CreateProfileRequestValidator`
  - **Location**: `Core/Application/Validators/CreateProfileRequestValidator.cs`
  - **Rules**:
    - FullName required, max 100 chars
    - DateOfBirth required, age >= 18
    - Gender required
    - Location required
  - **Reference**: [Architecture Guidelines - Validation](../../10_architecture/02_architecture-guidelines.md)

- [ ] Create `UpdateProfileRequestValidator`
  - **Location**: `Core/Application/Validators/UpdateProfileRequestValidator.cs`
  - **Rules**: Validate non-null fields only (partial updates)

### Create Application Services

- [ ] Create `ProfileService` application service
  - **Location**: `Core/Application/Services/ProfileService.cs`
  - **Dependencies**: IProfileRepository, IMessagePublisher, IEncryptionService
  - **Methods**:
    - `Task<ProfileResponse> CreateProfileAsync(CreateProfileRequest request, MemberId userId, CancellationToken ct)`
      - Logic: Create MemberProfile entity, encrypt FullName, save via repository, publish ProfileCreated event
    - `Task<ProfileResponse> GetProfileAsync(MemberId userId, CancellationToken ct)`
      - Logic: Retrieve from repository, decrypt FullName, map to DTO
    - `Task<ProfileResponse> UpdateProfileAsync(UpdateProfileRequest request, MemberId userId, CancellationToken ct)`
      - Logic: Load entity, update fields, recalc completion, save, publish ProfileUpdated, check ProfileCompleted
    - `Task DeleteProfileAsync(MemberId userId, CancellationToken ct)`
      - Logic: Soft delete (GDPR compliance)
  - **Why**: Orchestrates use cases, coordinates domain and infrastructure
  - **Reference**: [Feature Spec - AC5-9](./f0002_profile_service_mvp.md)

---

## Phase 3: Infrastructure Layer (4-5 hours)

**Reference**: [Architecture Guidelines - Infrastructure Layer](../../10_architecture/02_architecture-guidelines.md)
**Why Infrastructure**: Implements ports defined in Domain, handles external dependencies

### Create ProfileDbContext

- [ ] Create `ProfileDbContext`
  - **Location**: `Infrastructure/Data/ProfileDbContext.cs`
  - **DbSets**: `DbSet<MemberProfile> Profiles`
  - **Override OnModelCreating**: Apply entity configurations
  - **Why**: EF Core context for ProfileService database
  - **Reference**: [Service Templates - Infrastructure DbContext](../../10_architecture/03_service-templates.md)

### Create Entity Configurations

- [ ] Create `MemberProfileConfiguration`
  - **Location**: `Infrastructure/Data/Configurations/MemberProfileConfiguration.cs`
  - **Configuration**:
    - Table name: `member_profiles`
    - Primary key: UserId (Guid)
    - **Encrypted fields** (via value converters):
      - FullName: Use `EncryptedStringConverter`
      - Email: Use `EncryptedStringConverter`
      - DateOfBirth: Use `EncryptedDateTimeConverter`
      - LocationLatitude: Use `EncryptedDecimalConverter`
      - LocationLongitude: Use `EncryptedDecimalConverter`
    - PersonalityProfile: Owned entity (same table)
    - Values: Owned entity (same table)
    - Lifestyle: Owned entity (same table)
    - Preferences: Owned entity (same table)
    - PreferredLanguages: JSON column
    - Indexes: DeletedAt (filtered), IsComplete
    - Check constraints: Age range validation
  - **Why**: Configure EF Core mapping, apply encryption converters for GDPR-sensitive PII
  - **Reference**: [Feature Spec - Database Schema](./f0002_profile_service_mvp.md)
  - **Reference**: [Security & Auth - Field-Level Encryption](../../10_architecture/05_security-and-auth.md)

### Create Encryption Service (Adapter)

- [ ] Create `EncryptionService` implementation
  - **Location**: `Infrastructure/Security/EncryptionService.cs`
  - **Implements**: `IEncryptionService` (domain port)
  - **Algorithm**: AES-256-CBC
  - **Key Source**: Read from environment variable (ENCRYPTION_KEY)
  - **Why**: Provides field-level encryption for PII (FullName, Email, DateOfBirth, Latitude, Longitude)
  - **Security**: Key stored in Azure Key Vault in production
  - **Reference**: [Security & Auth - Encryption](../../10_architecture/05_security-and-auth.md)

- [ ] Create EF Core value converters for transparent encryption
  - **EncryptedStringConverter** (`Infrastructure/Security/EncryptedStringConverter.cs`)
    - For: FullName, Email
  - **EncryptedDateTimeConverter** (`Infrastructure/Security/EncryptedDateTimeConverter.cs`)
    - For: DateOfBirth
  - **EncryptedDecimalConverter** (`Infrastructure/Security/EncryptedDecimalConverter.cs`)
    - For: LocationLatitude, LocationLongitude
  - **Purpose**: Transparently encrypt/decrypt during save/load
  - **Usage**: Apply in `MemberProfileConfiguration`
  - **Why**: Encryption happens automatically at persistence layer, domain stays pure

### Create Repository Implementations (Adapters)

- [ ] Create `ProfileRepository` implementation
  - **Location**: `Infrastructure/Repositories/ProfileRepository.cs`
  - **Implements**: `IProfileRepository` (domain port)
  - **Dependencies**: ProfileDbContext
  - **Methods**: Implement all interface methods using EF Core
  - **Why**: Adapter implementing domain port, uses EF Core for persistence
  - **Reference**: [Repository Pattern guidelines](../../10_architecture/02_architecture-guidelines.md)

### Create Message Consumers (Event Handlers)

- [ ] Create `UserRegisteredConsumer` (MassTransit consumer)
  - **Location**: `Infrastructure/Messaging/Consumers/UserRegisteredConsumer.cs`
  - **Consumes**: `UserRegistered` event from IdentityService
  - **Logic**:
    1. Receive event with UserId and Email
    2. Create MemberProfile skeleton via `MemberProfile.CreateSkeleton(userId, email)`
    3. Save via IProfileRepository
    4. Publish ProfileCreated event
  - **Why**: Automatically create profile when user registers
  - **Reference**: [Feature Spec - AC1-4](./f0002_profile_service_mvp.md)
  - **Reference**: [Messaging & Integration - Consumers](../../10_architecture/06_messaging-and-integration.md)

### Create EF Core Migration

- [ ] Create and apply EF Core migration
  - **Migration Name**: `Initial_MemberProfiles_Table`
  - **Command**: `dotnet ef migrations add Initial_MemberProfiles_Table --project Infrastructure --startup-project Api`
  - **Verify**: Migration SQL matches feature spec DDL
  - **Apply Locally**: `dotnet ef database update --project Infrastructure --startup-project Api`
  - **Why**: Schema versioning, repeatable deployments

---

## Phase 4: API Layer (3-4 hours)

**Reference**: [Architecture Guidelines - API Layer](../../10_architecture/02_architecture-guidelines.md)
**Why API Layer**: Outermost Onion layer, handles HTTP concerns only

### Create ProfileController

- [ ] Create `ProfileController`
  - **Location**: `Api/Controllers/ProfileController.cs`
  - **Route**: `/api/v1/profiles`
  - **Dependencies**: ProfileService (application service)
  - **Endpoints**:
    - `POST /api/v1/profiles` - Create profile
    - `GET /api/v1/profiles/{userId}` - Get profile
    - `PATCH /api/v1/profiles/{userId}` - Update profile
    - `DELETE /api/v1/profiles/{userId}` - Delete profile
  - **Authorization**: Require JWT, extract UserId from claims, validate user can only access own profile
  - **Error Handling**: Return RFC 7807 Problem Details
  - **Reference**: [Feature Spec - API Specification](./f0002_profile_service_mvp.md)

### Configure Dependency Injection (Program.cs)

- [ ] Register DbContext with connection string
  - Read from `appsettings.json` or environment variable
  - Use Npgsql provider

- [ ] Register Core services:
  - `IProfileRepository` → `ProfileRepository` (scoped)
  - `IEncryptionService` → `EncryptionService` (singleton)

- [ ] Register Application services:
  - `ProfileService` (scoped)

- [ ] Register validators:
  - FluentValidation validators (scoped)

- [ ] **Reference**: [Architecture Guidelines - DI Patterns](../../10_architecture/02_architecture-guidelines.md)

### Configure MassTransit

- [ ] Configure MassTransit in Program.cs
  - Register UserRegisteredConsumer
  - Configure RabbitMQ for local development
  - Configure Azure Service Bus for production (environment-based)
  - **Reference**: [Messaging Guidelines - MassTransit Setup](../../10_architecture/06_messaging-and-integration.md)

- [ ] Configure message routing:
  - Listen for `UserRegistered` event on `identity.user-registered` queue
  - Publish ProfileCreated, ProfileUpdated, ProfileCompleted events

### Configure JWT Authentication

- [ ] Add JWT Bearer authentication
  - **Reference**: [Security & Auth - JWT Middleware](../../10_architecture/05_security-and-auth.md)
  - Validate issuer, audience, signing key
  - Read JWT settings from appsettings.json

### Configure Logging

- [ ] Add Serilog with:
  - Console sink (development)
  - Seq sink (structured logging)
  - Include correlation IDs in all log entries
  - **Reference**: [Observability Patterns - Logging](../../10_architecture/07_observability-patterns.md)

### Configure Health Checks

- [ ] Add health check endpoints:
  - `/health` - Overall health
  - `/health/ready` - Readiness (DB + RabbitMQ)
  - `/health/live` - Liveness
  - **Reference**: [Architecture Guidelines - Health Checks](../../10_architecture/02_architecture-guidelines.md)

### Create appsettings.json

- [ ] Configure:
  - ConnectionStrings:ProfileDb
  - Jwt:SecretKey, Issuer, Audience
  - EncryptionKey (256-bit key for AES)
  - RabbitMq:Host, Username, Password
  - Serilog configuration
  - CORS policy

### Create appsettings.Development.json

- [ ] Override settings for local development

---

## Phase 5: Testing (4-5 hours)

**Reference**: [Architecture Guidelines - Testing Strategy](../../10_architecture/02_architecture-guidelines.md)
**Why Tests**: Verify business logic, integration, and API contracts

### Unit Tests - Domain Layer

- [ ] Test MemberProfile entity:
  - `CreateSkeleton()` creates profile with correct defaults
  - `UpdatePersonality()` updates personality and recalculates completion
  - `CalculateCompletion()` returns correct percentage based on filled fields
  - `CheckIfComplete()` marks IsComplete when >= 80%
  - `CanShareWith()` enforces privacy rules based on ExposureLevel
  - Domain events raised correctly (ProfileCreated, ProfileUpdated, ProfileCompleted)

- [ ] Test Value Objects:
  - PersonalityProfile validates trait scores 1-5
  - Values validates scores 1-5
  - AgeRange validates min <= max, min >= 18, max <= 100
  - PreferenceSet validates distance 1-500

- [ ] **Tool**: xUnit, FluentAssertions
- [ ] **Reference**: [Feature Spec - Testing Strategy](./f0002_profile_service_mvp.md)

### Unit Tests - Application Layer

- [ ] Test ProfileService:
  - CreateProfileAsync creates profile and publishes event
  - GetProfileAsync retrieves and decrypts FullName
  - UpdateProfileAsync updates fields and recalculates completion
  - UpdateProfileAsync publishes ProfileCompleted when reaching 100%
  - DeleteProfileAsync soft-deletes profile

- [ ] **Mocking**: Mock IProfileRepository, IEncryptionService, IMessagePublisher using Moq

### Integration Tests - Infrastructure Layer

- [ ] Test ProfileRepository:
  - AddAsync persists to real PostgreSQL
  - GetByUserIdAsync retrieves from database
  - UpdateAsync saves changes
  - **Encryption/decryption works correctly for all 5 encrypted fields**:
    - FullName encrypted/decrypted
    - Email encrypted/decrypted
    - DateOfBirth encrypted/decrypted
    - LocationLatitude encrypted/decrypted
    - LocationLongitude encrypted/decrypted
  - Verify encrypted values in DB are unreadable (inspect raw DB)

- [ ] Test UserRegisteredConsumer:
  - Consumes UserRegistered event and creates profile skeleton
  - Email field populated and encrypted
  - Publishes ProfileCreated event

- [ ] **Tool**: Testcontainers.PostgreSql, Testcontainers.RabbitMq
- [ ] **Reference**: [Testing with Testcontainers](../../10_architecture/02_architecture-guidelines.md)

### API Tests (WebApplicationFactory)

- [ ] Test ProfileController endpoints:
  - `POST /api/v1/profiles` with valid data returns 201
  - `POST /api/v1/profiles` with invalid data returns 400
  - `POST /api/v1/profiles` without JWT returns 401
  - `GET /api/v1/profiles/{userId}` returns user's profile (200)
  - `GET /api/v1/profiles/{userId}` for other user returns 403
  - `PATCH /api/v1/profiles/{userId}` updates profile (200)
  - `DELETE /api/v1/profiles/{userId}` soft-deletes (204)

- [ ] **Test Setup**: Use WebApplicationFactory with Testcontainers for real DB + RabbitMQ
- [ ] **Reference**: [Feature Spec - Testing Strategy](./f0002_profile_service_mvp.md)

### Manual Testing Checklist

- [ ] Use Postman/Insomnia to test full CRUD flow
- [ ] **Verify encryption in database** (inspect via psql):
  - FullName is encrypted (unreadable ciphertext)
  - Email is encrypted
  - DateOfBirth is encrypted
  - LocationLatitude is encrypted
  - LocationLongitude is encrypted
- [ ] Verify API responses show decrypted values correctly
- [ ] Verify UserRegistered event triggers profile creation with encrypted email
- [ ] Verify ProfileCompleted event published when reaching 100%
- [ ] Check RabbitMQ management UI for events
- [ ] Check Seq logs for correlation IDs

---

## Phase 6: Docker Integration (1-2 hours)

### Create Dockerfile

- [ ] Create `src/Services/Profile/Dockerfile`
  - Multi-stage build (build → publish → runtime)
  - Base: mcr.microsoft.com/dotnet/aspnet:8.0
  - SDK: mcr.microsoft.com/dotnet/sdk:8.0
  - **Reference**: [Deployment & DevOps - Docker](../../10_architecture/07_deployment-and-devops.md)

### Update docker-compose.yml

- [ ] Add profile-service configuration:
  - Build context and Dockerfile path
  - Port mapping (5001:8080)
  - Environment variables (DB connection, messaging, JWT, encryption key)
  - Depends on: postgres, rabbitmq
  - Networks: quietmatch-network

- [ ] Test Docker build and run locally:
  - `docker-compose up -d profile-service`
  - Verify health checks pass
  - Verify can create profile via API

---

## Phase 7: Documentation (1 hour)

### Update Service Documentation

- [ ] Create `docs/30_microservices/profile-service.md`:
  - Service overview
  - Architecture: Onion
  - Responsibilities
  - API endpoints
  - Events consumed/published
  - Technology stack
  - How to run locally

### Update PATTERNS.md

- [ ] Finalize `src/Services/Profile/PATTERNS.md`:
  - Detailed explanation of Onion Architecture implementation
  - Why Onion chosen for ProfileService (rich domain logic, privacy rules)
  - Folder structure walkthrough
  - Code examples showing domain-centric design
  - Trade-offs and alternatives

### Update PROGRESS.md

- [ ] Check off F0002 in project PROGRESS.md:
  - Mark ProfileService as complete
  - Link to feature spec and PR

---

## Completion Checklist

Before marking feature as complete:

- [ ] All acceptance criteria from feature spec met (AC1-AC31, NF1-6, SEC1-7, GDPR1-6)
- [ ] All tests passing (unit + integration + API)
- [ ] Manual testing complete
- [ ] Docker container runs successfully
- [ ] Code follows Onion Architecture pattern
- [ ] Uses ubiquitous language (MemberProfile, PersonalityProfile, Values, etc.)
- [ ] No hardcoded values (all in config)
- [ ] Security requirements met (JWT, encryption, authorization)
- [ ] GDPR requirements met (soft delete, right to access/rectification/erasure)
- [ ] **All 5 PII fields encrypted at rest**:
  - [ ] FullName encrypted
  - [ ] Email encrypted
  - [ ] DateOfBirth encrypted
  - [ ] LocationLatitude encrypted
  - [ ] LocationLongitude encrypted
- [ ] UserRegistered event consumed correctly
- [ ] ProfileCreated, ProfileUpdated, ProfileCompleted events published
- [ ] Health checks working
- [ ] Logging with correlation IDs
- [ ] Documentation updated (PATTERNS.md, README.md, microservices doc)
- [ ] Ready for PR

---

## Blockers / Questions

*Document any issues requiring human approval*

---

## Notes & Decisions

*Document implementation discoveries and decisions*

---

**Last Updated**: 2025-11-26
**Plan Status**: Ready for Implementation
**Next Step**: Begin Phase 0 - Project Setup
