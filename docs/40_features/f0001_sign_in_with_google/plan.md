# Implementation Plan - F0001: Sign In with Google

**Status**: 🔴 Not Started
**Feature File**: [f0001_sign_in_with_google.md](./f0001_sign_in_with_google.md)
**Architecture Pattern**: Layered (IdentityService)
**Started**: TBD
**Last Updated**: 2025-11-21
**Estimated Total Time**: 8 hours

---

## Overview

This plan provides a detailed, smart implementation roadmap for F0001 (Sign In with Google), applying all QuietMatch architecture guidelines, patterns, and best practices to this specific feature.

**What this plan contains**:
- Step-by-step tasks organized by architecture layers
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
- [ ] **Read completely**: [Feature Specification](./f0001_sign_in_with_google.md)
- [ ] **Read**: [Architecture Guidelines - Layered Architecture](../../10_architecture/02_architecture-guidelines.md) (Sections on Layered pattern, why it's used for IdentityService)
- [ ] **Read**: [Service Templates - Layered Folder Structure](../../10_architecture/03_service-templates.md) (Section 2: Layered Architecture example)
- [ ] **Read**: [Security & Auth - Custom IdentityService](../../10_architecture/05_security-and-auth.md) (Sections on JWT, Google OAuth, refresh tokens)
- [ ] **Read**: [Messaging & Integration - MassTransit](../../10_architecture/06_messaging-and-integration.md) (Event publishing, RabbitMQ/Azure Service Bus)
- [ ] **Read**: [Ubiquitous Language](../../20_domain/01_domain-ubiquitous-language.md) (Domain terms used in this feature)

### Environment Setup
- [ ] Docker Desktop installed and running
- [ ] Start infrastructure: `docker-compose up -d`
- [ ] Verify PostgreSQL running and accessible:
  - Host: localhost:5432
  - Database: identity_db
  - User: admin
  - Password: QuietMatch_Dev_2025!
- [ ] Verify RabbitMQ running: http://localhost:15672 (guest/guest)
- [ ] Verify Redis running: `redis-cli ping` returns PONG
- [ ] Verify Seq running: http://localhost:5341

---

## Phase 0: Project Setup (30-45 minutes)

### Create Feature Branch
- [ ] Create and checkout feature branch
  - **Branch name**: `feature/f0001-sign-in-with-google`
  - **Why**: Isolate feature development, enable PR workflow

### Create Solution and Projects
- [ ] Navigate to `src/Services/` and create Identity folder
- [ ] Create .NET solution file: `DatingApp.IdentityService.sln`
- [ ] Create projects following Layered architecture:
  - **DatingApp.IdentityService.Domain** (classlib) - Core entities, no dependencies
  - **DatingApp.IdentityService.Application** (classlib) - Business logic
  - **DatingApp.IdentityService.Infrastructure** (classlib) - External dependencies (DB, Google API)
  - **DatingApp.IdentityService.Api** (webapi) - HTTP endpoints
  - **DatingApp.IdentityService.Tests.Unit** (xunit) - Unit tests
  - **DatingApp.IdentityService.Tests.Integration** (xunit) - Integration tests
  - **Reference**: [Service Templates - Layered Project Structure](../../10_architecture/03_service-templates.md)

### Configure Project References
- [ ] Set up dependency chain (Layered pattern - unidirectional dependencies):
  - **API** → Application + Infrastructure
  - **Application** → Domain
  - **Infrastructure** → Domain
  - **Tests.Unit** → Application
  - **Tests.Integration** → API
  - **Why**: Layered architecture enforces dependencies flow downward, Domain has no dependencies

### Install NuGet Packages
- [ ] Domain layer packages:
  - (None - keep domain pure, no external dependencies)

- [ ] Application layer packages:
  - FluentValidation (for input validation)
  - MediatR (if using CQRS commands/queries - optional for this feature)

- [ ] Infrastructure layer packages:
  - Npgsql.EntityFrameworkCore.PostgreSQL (database)
  - Microsoft.EntityFrameworkCore.Design (migrations)
  - Google.Apis.Auth (Google ID token validation)
  - MassTransit.RabbitMQ (messaging)

- [ ] API layer packages:
  - Microsoft.AspNetCore.Authentication.JwtBearer (JWT middleware)
  - Serilog.AspNetCore (structured logging)
  - Serilog.Sinks.Seq (log shipping)
  - AspNetCoreRateLimit (rate limiting)
  - Swashbuckle.AspNetCore (Swagger/OpenAPI)

- [ ] Test packages:
  - Moq (mocking)
  - FluentAssertions (readable assertions)
  - Testcontainers.PostgreSql (real database for integration tests)
  - Testcontainers.RabbitMq (real message broker for integration tests)
  - Microsoft.AspNetCore.Mvc.Testing (API testing)

### Create PATTERNS.md
- [ ] Create `src/Services/Identity/PATTERNS.md` explaining:
  - Why Layered architecture chosen for IdentityService (simple CRUD, clear separation)
  - Folder structure breakdown
  - Layer responsibilities
  - Dependency rules
  - Alternative patterns considered (Onion, Hexagonal) and why not chosen
  - **Reference**: [Architecture Guidelines - Pattern Selection](../../10_architecture/02_architecture-guidelines.md)

### Initial Commit
- [ ] Commit project structure
  - Message: `feat(identity): initial project structure for F0001 (#issue-number)`

---

## Phase 1: Domain Layer (1 hour)

**Reference**: [Architecture Guidelines - Domain Layer](../../10_architecture/02_architecture-guidelines.md)
**Why Domain First**: In Layered architecture, domain is the foundation - no dependencies, pure business logic

### Create AuthProvider Enum
- [ ] Create enum in Domain layer
  - **Namespace**: `DatingApp.IdentityService.Domain.Enums`
  - **Values**: Google, Apple
  - **Why**: Type-safe provider identification, extensible for future providers
  - **Reference**: [Ubiquitous Language - AuthProvider](../../20_domain/01_domain-ubiquitous-language.md)

### Create User Entity
- [ ] Create User entity in Domain layer
  - **Namespace**: `DatingApp.IdentityService.Domain.Entities`
  - **Properties**:
    - `Id` (Guid) - Primary key
    - `Email` (string) - User email from OAuth provider
    - `Provider` (AuthProvider enum) - Google or Apple
    - `ExternalUserId` (string) - Provider's user ID (Google sub claim)
    - `CreatedAt` (DateTime) - Registration timestamp
    - `LastLoginAt` (DateTime?) - Last successful login (nullable)
  - **Business Rules**:
    - Private parameterless constructor for EF Core
    - Static factory method `CreateFromGoogle(string email, string googleUserId)` - returns new User with Provider=Google
    - Instance method `RecordLogin()` - sets LastLoginAt to UtcNow
  - **Why**: Rich domain model encapsulates User creation and login logic
  - **Reference**: [Feature Spec - Database Schema](./f0001_sign_in_with_google.md) (lines 244-257)
  - **Reference**: [Ubiquitous Language - User](../../20_domain/01_domain-ubiquitous-language.md)

### Create RefreshToken Entity
- [ ] Create RefreshToken entity in Domain layer
  - **Namespace**: `DatingApp.IdentityService.Domain.Entities`
  - **Properties**:
    - `Id` (Guid) - Primary key
    - `UserId` (Guid) - Foreign key to User
    - `TokenHash` (string) - SHA-256 hash of refresh token (never store plain text)
    - `ExpiresAt` (DateTime) - Token expiration timestamp
    - `CreatedAt` (DateTime) - Creation timestamp
    - `RevokedAt` (DateTime?) - Revocation timestamp (nullable)
    - `IsRevoked` (bool) - Revocation flag
    - `User` (User) - Navigation property to User
  - **Business Rules**:
    - Private parameterless constructor for EF Core
    - Static factory method `Create(Guid userId, string tokenHash, int validityDays)` - creates token with expiry
    - Instance method `Revoke()` - sets IsRevoked=true, RevokedAt=UtcNow
    - Instance method `IsValid()` - returns false if revoked or expired
  - **Why**: Encapsulates refresh token lifecycle, ensures tokens can't be used past expiry/revocation
  - **Security**: Token hash (not plain text) prevents theft if database compromised
  - **Reference**: [Feature Spec - Database Schema](./f0001_sign_in_with_google.md) (lines 260-274)
  - **Reference**: [Security & Auth - Refresh Tokens](../../10_architecture/05_security-and-auth.md)

### Create Repository Interfaces
- [ ] Create `IUserRepository` interface in Domain layer
  - **Namespace**: `DatingApp.IdentityService.Domain.Repositories`
  - **Methods**:
    - `Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)`
    - `Task<User?> GetByExternalUserIdAsync(AuthProvider provider, string externalUserId, CancellationToken ct = default)`
    - `Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)`
    - `Task AddAsync(User user, CancellationToken ct = default)`
    - `Task UpdateAsync(User user, CancellationToken ct = default)`
  - **Why**: Interface in Domain (dependency inversion), implementation in Infrastructure
  - **Reference**: [Architecture Guidelines - Repository Pattern](../../10_architecture/02_architecture-guidelines.md)

- [ ] Create `IRefreshTokenRepository` interface in Domain layer
  - **Namespace**: `DatingApp.IdentityService.Domain.Repositories`
  - **Methods**:
    - `Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default)`
    - `Task<IEnumerable<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default)`
    - `Task AddAsync(RefreshToken token, CancellationToken ct = default)`
    - `Task UpdateAsync(RefreshToken token, CancellationToken ct = default)`
  - **Why**: Separate repository for token management, support querying active tokens for security audit

### Commit Domain Layer
- [ ] Commit domain entities and interfaces
  - Message: `feat(identity): add User and RefreshToken domain entities (#issue-number)`

---

## Phase 2: Infrastructure Layer - Persistence (1-1.5 hours)

**Reference**: [Service Templates - Infrastructure Layer](../../10_architecture/03_service-templates.md)
**Reference**: [Architecture Guidelines - EF Core Configuration](../../10_architecture/02_architecture-guidelines.md)

### Create DbContext
- [ ] Create `IdentityDbContext` in Infrastructure layer
  - **Namespace**: `DatingApp.IdentityService.Infrastructure.Data`
  - **Inherits**: `DbContext`
  - **DbSets**: `DbSet<User> Users`, `DbSet<RefreshToken> RefreshTokens`
  - **OnModelCreating**: Apply all entity configurations from assembly
  - **Why**: Centralized database configuration, enables EF Core conventions

### Create Entity Configurations
- [ ] Create `UserConfiguration` (IEntityTypeConfiguration<User>)
  - **Namespace**: `DatingApp.IdentityService.Infrastructure.Data.Configurations`
  - **Table name**: `users`
  - **Property configurations**:
    - Id: Primary key
    - Email: Required, MaxLength(255), Index
    - Provider: Required, Convert to string, MaxLength(50)
    - ExternalUserId: Required, MaxLength(255)
    - CreatedAt: Required
    - LastLoginAt: Optional
  - **Constraints**:
    - Unique index on (Provider, ExternalUserId) - prevent duplicate users from same provider
  - **Why**: Fluent API for precise database schema control
  - **Reference**: [Feature Spec - Database Schema DDL](./f0001_sign_in_with_google.md) (lines 244-257)

- [ ] Create `RefreshTokenConfiguration` (IEntityTypeConfiguration<RefreshToken>)
  - **Namespace**: `DatingApp.IdentityService.Infrastructure.Data.Configurations`
  - **Table name**: `refresh_tokens`
  - **Property configurations**:
    - Id: Primary key
    - UserId: Foreign key, Index
    - TokenHash: Required, MaxLength(255), Unique index
    - ExpiresAt: Required, Index (for efficient expiry queries)
    - CreatedAt: Required
    - RevokedAt: Optional
    - IsRevoked: Required, Default false
  - **Relationships**:
    - Many-to-one with User, OnDelete(Cascade) - delete tokens when user deleted
  - **Why**: Indexes optimize lookups, unique constraint prevents token reuse
  - **Reference**: [Feature Spec - Database Schema DDL](./f0001_sign_in_with_google.md) (lines 260-274)

### Implement Repositories
- [ ] Create `UserRepository` implementing `IUserRepository`
  - **Namespace**: `DatingApp.IdentityService.Infrastructure.Repositories`
  - **Constructor**: Inject `IdentityDbContext`
  - **Implement all methods** from interface:
    - Use `FindAsync` for GetByIdAsync (fast primary key lookup)
    - Use `FirstOrDefaultAsync` with predicate for provider/email lookups
    - Use `AddAsync` + `SaveChangesAsync` for Add
    - Use `Update` + `SaveChangesAsync` for Update
  - **Why**: Repository pattern abstracts data access, enables testing with mocks

- [ ] Create `RefreshTokenRepository` implementing `IRefreshTokenRepository`
  - **Namespace**: `DatingApp.IdentityService.Infrastructure.Repositories`
  - **Constructor**: Inject `IdentityDbContext`
  - **GetActiveByUserIdAsync**: Filter where `!IsRevoked && ExpiresAt > UtcNow`
  - **Why**: Encapsulates token queries, business logic stays in domain

### Create EF Core Migration
- [ ] Run `dotnet ef migrations add InitialCreate`
  - From: Infrastructure project
  - Startup project: API project
  - **Migration name**: `InitialCreate`
- [ ] Review generated migration SQL - verify matches feature spec DDL
- [ ] Run `dotnet ef database update` to apply migration to local `identity_db`
- [ ] Verify tables created: Connect to PostgreSQL and check `users` and `refresh_tokens` tables exist
  - **Reference**: [Feature Spec - Database Schema](./f0001_sign_in_with_google.md)

### Commit Persistence Layer
- [ ] Commit DbContext, configurations, repositories, migration
  - Message: `feat(identity): add EF Core persistence layer with migrations (#issue-number)`

---

## Phase 3: Infrastructure Layer - External Services (1-1.5 hours)

**Reference**: [Security & Auth - Google OAuth](../../10_architecture/05_security-and-auth.md)
**Reference**: [Security & Auth - JWT Implementation](../../10_architecture/05_security-and-auth.md)

### Create Google OAuth Service
- [ ] Create `IGoogleAuthService` interface
  - **Namespace**: `DatingApp.IdentityService.Infrastructure.Services`
  - **Method**: `Task<GoogleUserInfo?> ValidateIdTokenAsync(string idToken, CancellationToken ct = default)`
  - **Return type**: `GoogleUserInfo` record with properties: Sub (string), Email (string), Name (string), EmailVerified (bool)
  - **Why**: Abstracts Google API, enables testing with mocks

- [ ] Implement `GoogleAuthService`
  - **Namespace**: `DatingApp.IdentityService.Infrastructure.Services`
  - **Constructor**: Inject `IConfiguration`, `ILogger<GoogleAuthService>`
  - **Configuration**: Read Google:ClientId from appsettings
  - **Implementation**:
    - Use `Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync()`
    - Validate against clientId (audience claim)
    - Return GoogleUserInfo on success
    - Return null or throw on invalid token
    - Log validation attempts
  - **Security**: Server-side validation prevents client token tampering
  - **Reference**: [Feature Spec - AC6](./f0001_sign_in_with_google.md) (Validate ID token with Google API)
  - **Reference**: [Security & Auth - Google OAuth Validation](../../10_architecture/05_security-and-auth.md)

### Create JWT Token Generator
- [ ] Create `IJwtTokenGenerator` interface
  - **Namespace**: `DatingApp.IdentityService.Infrastructure.Services`
  - **Methods**:
    - `string GenerateAccessToken(Guid userId, string email)`
    - `string GenerateRefreshToken()`
    - `string HashToken(string token)` - SHA-256 hash for storage
  - **Why**: Abstracts token generation, supports testing

- [ ] Implement `JwtTokenGenerator`
  - **Namespace**: `DatingApp.IdentityService.Infrastructure.Services`
  - **Constructor**: Inject `IConfiguration`
  - **Configuration**: Read from appsettings:
    - Jwt:SecretKey (256-bit key)
    - Jwt:Issuer (e.g., "https://quietmatch.com")
    - Jwt:Audience (e.g., "https://api.quietmatch.com")
    - Jwt:AccessTokenExpiryMinutes (15 minutes)
  - **GenerateAccessToken**:
    - Claims: sub (userId), email, jti (unique token ID), iat (issued at)
    - Algorithm: HMAC-SHA256
    - Expiry: 15 minutes from now
    - Use `JwtSecurityTokenHandler` to write token
  - **GenerateRefreshToken**:
    - Generate 32 random bytes using `RandomNumberGenerator`
    - Convert to Base64 string
  - **HashToken**:
    - Use `SHA256.ComputeHash()` on token bytes
    - Return Base64-encoded hash
  - **Why**: JWT access token for API auth, refresh token for session renewal
  - **Security**: Refresh tokens hashed before storage (theft mitigation)
  - **Reference**: [Feature Spec - AC9, AC10](./f0001_sign_in_with_google.md)
  - **Reference**: [Security & Auth - JWT Configuration](../../10_architecture/05_security-and-auth.md)

### Commit External Services
- [ ] Commit Google OAuth and JWT services
  - Message: `feat(identity): add Google OAuth and JWT token services (#issue-number)`

---

## Phase 4: Application Layer (1 hour)

**Reference**: [Service Templates - Application Layer](../../10_architecture/03_service-templates.md)
**Reference**: [Feature Spec - Acceptance Criteria](./f0001_sign_in_with_google.md)

### Create DTOs
- [ ] Create `LoginWithGoogleRequest` record
  - **Namespace**: `DatingApp.IdentityService.Application.DTOs`
  - **Properties**: `string IdToken`
  - **Why**: Input DTO for API endpoint

- [ ] Create `LoginResponse` record
  - **Namespace**: `DatingApp.IdentityService.Application.DTOs`
  - **Properties**:
    - `string AccessToken` - JWT for API authentication
    - `string RefreshToken` - For session renewal
    - `int ExpiresIn` - Access token TTL in seconds (900 = 15 min)
    - `string TokenType` - "Bearer"
    - `Guid UserId` - User identifier
    - `bool IsNewUser` - True if first login (triggers profile creation flow)
    - `string Email` - User email
  - **Why**: Structured response matching feature spec API contract
  - **Reference**: [Feature Spec - API Specification](./f0001_sign_in_with_google.md) (lines 136-158)

### Create AuthService
- [ ] Create `AuthService` class
  - **Namespace**: `DatingApp.IdentityService.Application.Services`
  - **Constructor**: Inject:
    - `IGoogleAuthService` - For ID token validation
    - `IJwtTokenGenerator` - For token generation
    - `IUserRepository` - For user CRUD
    - `IRefreshTokenRepository` - For refresh token CRUD
    - `ILogger<AuthService>` - For structured logging
  - **Method**: `Task<LoginResponse?> LoginWithGoogleAsync(string idToken, CancellationToken ct = default)`
  - **Implementation Flow** (matches all acceptance criteria):
    1. Validate ID token with Google (AC6) - call `GoogleAuthService.ValidateIdTokenAsync()`
    2. Return null if invalid
    3. Query user by provider + externalUserId
    4. **If new user** (AC7):
       - Create User using `User.CreateFromGoogle()`
       - Save to database via repository
       - Log registration event
    5. **If existing user** (AC8):
       - Call `user.RecordLogin()` to update LastLoginAt
       - Save to database via repository
       - Log login event
    6. Generate JWT access token (AC9) - call `JwtTokenGenerator.GenerateAccessToken()`
    7. Generate refresh token (AC10):
       - Call `JwtTokenGenerator.GenerateRefreshToken()`
       - Hash token via `JwtTokenGenerator.HashToken()`
       - Create RefreshToken entity with 7-day validity
       - Save to database
    8. Return LoginResponse (AC11) with all required fields
  - **Why**: Application service orchestrates domain logic and infrastructure
  - **Note**: Event publishing (UserRegistered) added in Phase 6
  - **Reference**: [Feature Spec - Acceptance Criteria](./f0001_sign_in_with_google.md) (AC6-AC11)

### Create FluentValidation Validator
- [ ] Create `LoginWithGoogleRequestValidator`
  - **Namespace**: `DatingApp.IdentityService.Application.Validators`
  - **Inherits**: `AbstractValidator<LoginWithGoogleRequest>`
  - **Rules**:
    - IdToken: NotEmpty() with message "ID token is required"
  - **Why**: Input validation before processing, fail fast on bad requests
  - **Reference**: [Architecture Guidelines - Validation Strategy](../../10_architecture/02_architecture-guidelines.md)

### Commit Application Layer
- [ ] Commit DTOs, AuthService, validators
  - Message: `feat(identity): add authentication application service (#issue-number)`

---

## Phase 5: API Layer (1 hour)

**Reference**: [Service Templates - API Layer](../../10_architecture/03_service-templates.md)
**Reference**: [Feature Spec - API Specification](./f0001_sign_in_with_google.md)

### Create AuthController
- [ ] Create `AuthController`
  - **Namespace**: `DatingApp.IdentityService.Api.Controllers`
  - **Attributes**: `[ApiController]`, `[Route("api/v1/auth")]`
  - **Constructor**: Inject `AuthService`, `ILogger<AuthController>`
  - **Endpoint**: `POST /api/v1/auth/login/google`
    - Attribute: `[HttpPost("login/google")]`
    - Parameter: `[FromBody] LoginWithGoogleRequest request`
    - Return types:
      - 200 OK: `LoginResponse`
      - 400 Bad Request: `ProblemDetails` (invalid token)
      - 429 Too Many Requests: `ProblemDetails` (rate limit)
      - 500 Internal Server Error: `ProblemDetails` (server error)
    - Implementation:
      - Call `AuthService.LoginWithGoogleAsync()`
      - Return 200 OK with response if successful
      - Return 400 BadRequest with RFC 7807 Problem Details if validation fails
    - **Why**: HTTP endpoint exposes authentication functionality
    - **Reference**: [Feature Spec - API Specification](./f0001_sign_in_with_google.md) (lines 126-189)

### Configure Program.cs (Dependency Injection)
- [ ] Register services in DI container:
  - **DbContext**: `AddDbContext<IdentityDbContext>()` with connection string from configuration
  - **Repositories** (scoped):
    - `IUserRepository` → `UserRepository`
    - `IRefreshTokenRepository` → `RefreshTokenRepository`
  - **Infrastructure Services** (scoped):
    - `IGoogleAuthService` → `GoogleAuthService`
    - `IJwtTokenGenerator` → `JwtTokenGenerator`
  - **Application Services** (scoped):
    - `AuthService`
  - **Validators**: `AddValidatorsFromAssemblyContaining<LoginWithGoogleRequestValidator>()`
  - **Why**: DI enables loose coupling, testability
  - **Reference**: [Architecture Guidelines - DI Registration](../../10_architecture/02_architecture-guidelines.md)

### Configure JWT Authentication
- [ ] Add JWT Bearer authentication middleware
  - Use `AddAuthentication(JwtBearerDefaults.AuthenticationScheme)`
  - Configure `TokenValidationParameters`:
    - ValidateIssuer: true
    - ValidateAudience: true
    - ValidateLifetime: true
    - ValidateIssuerSigningKey: true
    - ValidIssuer: from appsettings Jwt:Issuer
    - ValidAudience: from appsettings Jwt:Audience
    - IssuerSigningKey: from appsettings Jwt:SecretKey (convert to SymmetricSecurityKey)
  - Add middleware: `app.UseAuthentication()`
  - **Why**: Validates JWT tokens on protected endpoints
  - **Reference**: [Security & Auth - JWT Middleware](../../10_architecture/05_security-and-auth.md)

### Configure Rate Limiting
- [ ] Add rate limiting with AspNetCoreRateLimit
  - Configure `IpRateLimitOptions` from appsettings IpRateLimiting section
  - Add memory cache: `AddMemoryCache()`
  - Add rate limiting services: `AddInMemoryRateLimiting()`
  - Add middleware: `app.UseIpRateLimiting()`
  - **Configuration**: 5 requests per IP per minute for /api/v1/auth/login/* endpoints
  - **Why**: Prevents brute force attacks, DoS protection
  - **Reference**: [Feature Spec - NF3](./f0001_sign_in_with_google.md) (Rate limiting requirement)

### Configure Serilog Logging
- [ ] Configure Serilog in Program.cs
  - Use `UseSerilog()` host builder extension
  - Read configuration from appsettings Serilog section
  - **Sinks**: Console (for development), Seq (for structured logs at http://localhost:5341)
  - Add request logging middleware: `app.UseSerilogRequestLogging()`
  - **Why**: Structured logging enables debugging, correlation IDs for tracing
  - **Reference**: [Architecture Guidelines - Logging Strategy](../../10_architecture/02_architecture-guidelines.md)

### Configure Swagger/OpenAPI
- [ ] Add Swagger services: `AddSwaggerGen()`
- [ ] Add Swagger middleware (development only)
- [ ] **Why**: API documentation, interactive testing

### Create appsettings.Development.json
- [ ] Add configuration sections:
  - **ConnectionStrings**:
    - IdentityDb: "Host=localhost;Database=identity_db;Username=admin;Password=QuietMatch_Dev_2025!"
  - **Google**:
    - ClientId: "your-client-id.apps.googleusercontent.com"
    - ClientSecret: "your-client-secret"
  - **Jwt**:
    - SecretKey: "your-256-bit-secret-key-for-development-only"
    - Issuer: "https://localhost:5001"
    - Audience: "https://localhost:5001"
    - AccessTokenExpiryMinutes: "15"
  - **IpRateLimiting**:
    - General rules: 5 requests per minute for /api/v1/auth/login/*
  - **Serilog**:
    - MinimumLevel: Debug
    - WriteTo: Console, Seq (serverUrl: http://localhost:5341)
  - **Reference**: [Feature Spec - Configuration](./f0001_sign_in_with_google.md) (lines 306-327)

### Commit API Layer
- [ ] Commit controller, Program.cs, appsettings
  - Message: `feat(identity): add auth controller and API configuration (#issue-number)`

---

## Phase 6: Messaging Integration (45 minutes - 1 hour)

**Reference**: [Messaging & Integration - MassTransit](../../10_architecture/06_messaging-and-integration.md)
**Reference**: [Feature Spec - Events Published](./f0001_sign_in_with_google.md)

### Create UserRegistered Event
- [ ] Create `UserRegistered` record (integration event)
  - **Namespace**: `DatingApp.IdentityService.Infrastructure.Events` (or BuildingBlocks.Events if shared)
  - **Properties**:
    - `Guid UserId` - User identifier
    - `string Email` - User email
    - `string Provider` - "Google" or "Apple"
    - `DateTime RegisteredAt` - Registration timestamp
    - `Guid CorrelationId` - For distributed tracing
  - **Why**: Integration event notifies other services of new user registration
  - **Naming**: Past tense (event = fact that happened)
  - **Reference**: [Feature Spec - Events Published](./f0001_sign_in_with_google.md) (lines 280-298)
  - **Reference**: [Messaging Guidelines - Event Design](../../10_architecture/06_messaging-and-integration.md)

### Configure MassTransit
- [ ] Add MassTransit to Program.cs:
  - Register with `AddMassTransit()`
  - **For Development** (local):
    - Use `UsingRabbitMq()` transport
    - Host: "rabbitmq" (Docker service name)
    - Credentials: guest/guest
  - **For Production** (Azure):
    - Use `UsingAzureServiceBus()` transport
    - Connection string from configuration
  - Use environment check: `builder.Environment.IsDevelopment()` to switch transports
  - **Why**: Abstraction enables local dev with RabbitMQ, production with Azure Service Bus
  - **Reference**: [Messaging Guidelines - MassTransit Setup](../../10_architecture/06_messaging-and-integration.md) (lines 272-304)

### Update AuthService to Publish Event
- [ ] Modify AuthService constructor to inject `IPublishEndpoint` (from MassTransit)
- [ ] In LoginWithGoogleAsync method, after creating new user:
  - Publish `UserRegistered` event using `_publishEndpoint.Publish()`
  - Include all required event properties
  - Use `Guid.NewGuid()` for CorrelationId
  - **Only publish for NEW users** (not existing users)
  - **Why**: ProfileService subscribes to this event to create empty profile
  - **Reference**: [Feature Spec - AC7](./f0001_sign_in_with_google.md) (UserRegistered event requirement)

### Commit Messaging Integration
- [ ] Commit event definition, MassTransit configuration, AuthService update
  - Message: `feat(identity): add UserRegistered event publishing with MassTransit (#issue-number)`

---

## Phase 7: Testing (2-2.5 hours)

**Reference**: [Feature Spec - Testing Requirements](./f0001_sign_in_with_google.md)
**Reference**: [Architecture Guidelines - Testing Strategy](../../10_architecture/02_architecture-guidelines.md)

### Unit Tests - Domain Layer
- [ ] Create test class: `UserTests.cs`
  - Test: `CreateFromGoogle_ShouldSetPropertiesCorrectly()` - verify Id, Email, Provider, ExternalUserId, CreatedAt set
  - Test: `RecordLogin_ShouldUpdateLastLoginAt()` - verify timestamp updated
  - **Why**: Domain logic must be tested in isolation

- [ ] Create test class: `RefreshTokenTests.cs`
  - Test: `Create_ShouldSetExpiryCorrectly()` - verify ExpiresAt = CreatedAt + validityDays
  - Test: `Revoke_ShouldMarkAsRevoked()` - verify IsRevoked=true, RevokedAt set
  - Test: `IsValid_WhenExpired_ShouldReturnFalse()` - test expiry check
  - Test: `IsValid_WhenRevoked_ShouldReturnFalse()` - test revocation check
  - **Why**: Token lifecycle critical for security

### Unit Tests - Application Layer
- [ ] Create test class: `AuthServiceTests.cs`
  - **Setup**: Use Moq to mock all dependencies (IGoogleAuthService, IJwtTokenGenerator, repositories)
  - Test: `LoginWithGoogle_ValidToken_NewUser_ShouldCreateUser()` - verify user created, event published
  - Test: `LoginWithGoogle_ValidToken_ExistingUser_ShouldUpdateLastLogin()` - verify LastLoginAt updated
  - Test: `LoginWithGoogle_InvalidToken_ShouldReturnNull()` - verify null return on invalid token
  - Test: `LoginWithGoogle_NewUser_ShouldPublishUserRegisteredEvent()` - verify event published
  - Test: `LoginWithGoogle_ShouldGenerateJwtWithCorrectClaims()` - verify JWT structure
  - Test: `LoginWithGoogle_ShouldHashRefreshToken()` - verify token not stored in plain text
  - **Why**: Application logic orchestration must be tested
  - **Use**: FluentAssertions for readable assertions

### Unit Tests - Infrastructure Layer
- [ ] Create test class: `GoogleAuthServiceTests.cs`
  - Test: `ValidateIdToken_ValidToken_ShouldReturnUserInfo()` - mock Google API response
  - Test: `ValidateIdToken_InvalidToken_ShouldReturnNull()` - test error handling
  - **Why**: External service integration needs testing

- [ ] Create test class: `JwtTokenGeneratorTests.cs`
  - Test: `GenerateAccessToken_ShouldCreateValidJwt()` - verify token can be parsed, claims correct
  - Test: `HashToken_ShouldProduceConsistentHash()` - verify same input = same hash
  - **Why**: Token generation correctness critical for security

### Integration Tests - API Layer
- [ ] Create test class: `AuthControllerTests.cs`
  - **Setup**: Use `WebApplicationFactory<Program>` for in-memory API
  - **Setup**: Use Testcontainers for real PostgreSQL and RabbitMQ
  - Test: `POST_LoginGoogle_NewUser_ShouldReturn200WithIsNewUserTrue()` - verify full flow
  - Test: `POST_LoginGoogle_ExistingUser_ShouldReturn200WithIsNewUserFalse()` - verify existing user flow
  - Test: `POST_LoginGoogle_InvalidToken_ShouldReturn400()` - verify error response
  - Test: `POST_LoginGoogle_RateLimitExceeded_ShouldReturn429()` - verify rate limiting (call 6 times rapidly)
  - **Database Verification**: Query database after API call to verify user/token persisted
  - **Event Verification**: Check RabbitMQ for published UserRegistered event
  - **Why**: End-to-end testing ensures all layers work together
  - **Reference**: [Feature Spec - Integration Testing Requirements](./f0001_sign_in_with_google.md)

### Run All Tests
- [ ] Execute: `dotnet test` from solution root
- [ ] Verify all tests pass
- [ ] Check code coverage (target: >80% for business logic)

### Commit Tests
- [ ] Commit all test files
  - Message: `test(identity): add comprehensive unit and integration tests for F0001 (#issue-number)`

---

## Phase 8: Docker Integration (30-45 minutes)

**Reference**: [Architecture Guidelines - Docker Configuration](../../10_architecture/02_architecture-guidelines.md)

### Create Dockerfile
- [ ] Create `src/Services/Identity/Dockerfile`
  - **Multi-stage build**:
    - Stage 1 (base): `mcr.microsoft.com/dotnet/aspnet:8.0` - runtime
    - Stage 2 (build): `mcr.microsoft.com/dotnet/sdk:8.0` - build + restore
    - Stage 3 (publish): Build Release configuration
    - Stage 4 (final): Copy published output, set entrypoint
  - **Why**: Multi-stage build keeps final image small (only runtime + app)

### Update docker-compose.yml
- [ ] Add IdentityService configuration:
  - Service name: `identity-service`
  - Build context: `./src/Services/Identity`
  - Ports: Map 5001:80
  - Environment variables:
    - ConnectionStrings__IdentityDb: PostgreSQL connection string (use Docker service name "postgres")
    - Google__ClientId: ${GOOGLE_CLIENT_ID} (from .env file)
    - Google__ClientSecret: ${GOOGLE_CLIENT_SECRET}
    - Jwt__SecretKey: ${JWT_SECRET_KEY}
    - Jwt__Issuer, Jwt__Audience
  - Depends on: postgres, rabbitmq
  - **Why**: Enables local development with all dependencies

### Test Docker Build and Run
- [ ] Build Docker image: `docker-compose build identity-service`
- [ ] Run service: `docker-compose up identity-service`
- [ ] Verify service starts without errors in logs
- [ ] Test health check: `curl http://localhost:5001/health` (if health endpoint added)
- [ ] Test Swagger: http://localhost:5001/swagger

### Commit Docker Integration
- [ ] Commit Dockerfile and docker-compose.yml changes
  - Message: `feat(identity): add Docker support for F0001 (#issue-number)`

---

## Phase 9: Manual Testing & Verification (30-45 minutes)

**Reference**: [Feature Spec - Manual Testing Checklist](./f0001_sign_in_with_google.md)

### Manual Testing Checklist
- [ ] **Happy Path - New User**:
  - Use Postman/Insomnia to POST to http://localhost:5001/api/v1/auth/login/google
  - Include valid Google ID token in request body
  - Verify 200 OK response with isNewUser=true
  - Verify accessToken and refreshToken returned
  - Check database: User record created in `users` table
  - Check database: RefreshToken record created in `refresh_tokens` table with hashed token
  - Check RabbitMQ UI (http://localhost:15672): UserRegistered message in queue
  - Check Seq logs (http://localhost:5341): Login event logged

- [ ] **Happy Path - Existing User**:
  - Use same Google account to login again
  - Verify 200 OK response with isNewUser=false
  - Check database: LastLoginAt updated for user

- [ ] **Error Scenario - Invalid Token**:
  - POST with invalid/malformed ID token
  - Verify 400 Bad Request with Problem Details response

- [ ] **Error Scenario - Rate Limiting**:
  - POST 6 times rapidly from same IP
  - Verify 6th request returns 429 Too Many Requests

- [ ] **Token Validation**:
  - Copy accessToken from response
  - Use jwt.io to decode token
  - Verify claims: sub (userId), email, jti, iat, exp
  - Verify exp is ~15 minutes from iat

- [ ] **Logs Verification**:
  - Check Seq (http://localhost:5341) for structured logs
  - Search for correlation IDs
  - Verify no errors/warnings during happy path

---

## Completion Checklist

### Code Quality
- [ ] Follows Layered architecture pattern (Domain → Application → Infrastructure → API)
- [ ] Uses ubiquitous language from domain model (User, RefreshToken, AuthProvider)
- [ ] No hardcoded values (all configuration externalized to appsettings)
- [ ] No commented-out code
- [ ] Meaningful variable and method names
- [ ] Comments explain "why" (architecture decisions), not "what"
- [ ] All async methods use CancellationToken

### Testing
- [ ] Unit tests written for domain entities (User, RefreshToken)
- [ ] Unit tests written for application services (AuthService)
- [ ] Unit tests written for infrastructure services (GoogleAuthService, JwtTokenGenerator)
- [ ] Integration tests written for API endpoints
- [ ] Integration tests written for database operations
- [ ] Integration tests written for messaging
- [ ] All tests passing
- [ ] Code coverage >80% for business logic
- [ ] Manual testing checklist complete

### Security
- [ ] JWT authentication configured and working
- [ ] Refresh tokens hashed before storage (SHA-256)
- [ ] Google ID token validated server-side (audience, issuer, expiry)
- [ ] Rate limiting configured (5 requests/minute per IP)
- [ ] Input validation with FluentValidation
- [ ] No SQL injection vulnerabilities (using EF Core parameterized queries)
- [ ] Sensitive data encrypted/hashed appropriately
- [ ] HTTPS enforced (production)

### Documentation
- [ ] Feature file remains unmodified (immutable specification)
- [ ] Plan.md updated with all progress and notes
- [ ] PATTERNS.md created for IdentityService explaining Layered architecture
- [ ] API documentation available via Swagger
- [ ] README.md updated with setup instructions (if needed)
- [ ] .env.example updated with required environment variables

### Deployment
- [ ] Dockerfile created and builds successfully
- [ ] docker-compose.yml updated with IdentityService configuration
- [ ] Service runs in Docker container
- [ ] All dependencies (PostgreSQL, RabbitMQ, Redis) accessible from container
- [ ] Environment variables configured correctly

### GitHub
- [ ] All commits reference issue number (#issue-number)
- [ ] Commit messages follow conventional commits format
- [ ] Feature branch up to date with main
- [ ] No merge conflicts
- [ ] Ready to create PR

### Final Verification
- [ ] All acceptance criteria from feature spec met (AC1-AC14)
- [ ] All non-functional requirements met (NF1-NF5)
- [ ] All security requirements met (SEC1-SEC7)
- [ ] Feature file status updated to "Complete"
- [ ] Plan.md status updated to "Complete"
- [ ] Total implementation time recorded
- [ ] Lessons learned documented in Notes section below

---

## Blockers / Questions

*Document any issues requiring human approval here*

**Template**:
```
### [Date] - Blocker Title
**Issue**: Describe the blocker
**Impact**: What is blocked?
**Proposed Solution**: Your recommendation
**Status**: Awaiting human approval / Resolved
```

---

## Notes & Decisions

*Document implementation discoveries, decisions, and lessons learned here*

**Template**:
```
### [Date] - Decision/Discovery Title
**Context**: What was the situation?
**Decision**: What did you decide?
**Rationale**: Why this approach?
**Alternatives Considered**: What else was considered?
**Outcome**: How did it work out?
```

---

**Completion Status**: 🔴 Not Started
**Started**: TBD
**Completed**: TBD
**Total Implementation Time**: TBD hours
