# IdentityService - Architecture Pattern

**Pattern**: Layered Architecture
**Service**: IdentityService
**Bounded Context**: Authentication & Authorization

---

## Why Layered Architecture?

The IdentityService uses **Layered Architecture** (also known as N-Tier Architecture) because:

### ✅ Perfect for This Service
1. **Simple CRUD Operations**: User and token management are straightforward database operations
2. **Clear Separation**: Authentication logic naturally separates into layers (API → Business → Data)
3. **Sequential Flow**: Requests flow top-down (Presentation → Application → Domain → Infrastructure)
4. **Easy to Understand**: Most developers are familiar with layered architecture
5. **Low Complexity**: No need for the sophisticated patterns like Hexagonal (which we use for MatchingService)

### 🎯 Not Needed Here
- **Hexagonal Architecture**: Overkill - we don't need multiple adapters (only one auth provider at a time)
- **Onion Architecture**: Unnecessary complexity - domain logic is simple (create user, validate token)
- **CQRS**: Not beneficial - read/write operations are balanced and simple

---

## Layer Breakdown

### 1. **API Layer** (`DatingApp.IdentityService.Api`)
**Responsibility**: HTTP endpoints, request/response handling

**Contains**:
- Controllers (`AuthController`, `TokenController`)
- DTOs for requests/responses
- Middleware (authentication, rate limiting, error handling)
- `Program.cs` (DI container, middleware pipeline)
- `appsettings.json` (configuration)

**Dependencies**: → Application, Infrastructure

**Example**:
```csharp
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    [HttpPost("login/google")]
    public async Task<IActionResult> LoginWithGoogle([FromBody] LoginWithGoogleRequest request)
    {
        var response = await _authService.LoginWithGoogleAsync(request.IdToken);
        return Ok(response);
    }
}
```

---

### 2. **Application Layer** (`DatingApp.IdentityService.Application`)
**Responsibility**: Business logic, orchestration, use cases

**Contains**:
- Application services (`AuthService`, `TokenService`)
- DTOs (data transfer objects)
- Validators (FluentValidation)
- Application exceptions

**Dependencies**: → Domain

**Example**:
```csharp
public class AuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IGoogleAuthService _googleAuth;
    private readonly IJwtTokenGenerator _jwtGenerator;

    public async Task<LoginResponse> LoginWithGoogleAsync(string idToken, CancellationToken ct = default)
    {
        // 1. Validate Google ID token (Infrastructure)
        var googleUser = await _googleAuth.ValidateIdTokenAsync(idToken, ct);

        // 2. Find or create user (Domain + Infrastructure)
        var user = await _userRepo.GetByExternalUserIdAsync(AuthProvider.Google, googleUser.Sub, ct);
        if (user is null)
        {
            user = User.CreateFromGoogle(googleUser.Email, googleUser.Sub);
            await _userRepo.AddAsync(user, ct);
        }
        else
        {
            user.RecordLogin();
            await _userRepo.UpdateAsync(user, ct);
        }

        // 3. Generate tokens (Infrastructure)
        var accessToken = _jwtGenerator.GenerateAccessToken(user.Id, user.Email);
        var refreshToken = _jwtGenerator.GenerateRefreshToken();

        // 4. Return response
        return new LoginResponse(accessToken, refreshToken, user.Id, user.Email, user.IsNewUser);
    }
}
```

**Why This Layer Is Important**:
- Orchestrates multiple operations (validate token → find/create user → generate tokens)
- Keeps API layer thin (controllers just call services)
- Testable (can mock repositories and infrastructure services)

---

### 3. **Domain Layer** (`DatingApp.IdentityService.Domain`)
**Responsibility**: Core business entities, domain logic, repository interfaces

**Contains**:
- Entities (`User`, `RefreshToken`)
- Enums (`AuthProvider`)
- Repository interfaces (`IUserRepository`, `IRefreshTokenRepository`)
- Domain exceptions (`InvalidTokenException`, `UserNotFoundException`)

**Dependencies**: ❌ **NONE** - Domain is the innermost layer

**Example**:
```csharp
public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; }
    public AuthProvider Provider { get; private set; }
    public string ExternalUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    // Private constructor for EF Core
    private User() { }

    // Factory method - encapsulates creation logic
    public static User CreateFromGoogle(string email, string googleUserId)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Provider = AuthProvider.Google,
            ExternalUserId = googleUserId,
            CreatedAt = DateTime.UtcNow
        };
    }

    // Business logic method
    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }
}
```

**Why Pure Domain**:
- No dependencies = easy to test
- Rich domain model (entities have behavior, not just data)
- Encapsulation (private setters, factory methods)

---

### 4. **Infrastructure Layer** (`DatingApp.IdentityService.Infrastructure`)
**Responsibility**: External dependencies (database, Google API, messaging)

**Contains**:
- Repository implementations (`UserRepository`, `RefreshTokenRepository`)
- DbContext (`IdentityDbContext`)
- EF Core configurations (`UserConfiguration`, `RefreshTokenConfiguration`)
- External service implementations (`GoogleAuthService`, `JwtTokenGenerator`)
- Event publishers (MassTransit)

**Dependencies**: → Domain

**Example**:
```csharp
// Repository implementation
public class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _dbContext;

    public async Task<User?> GetByExternalUserIdAsync(
        AuthProvider provider,
        string externalUserId,
        CancellationToken ct = default)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Provider == provider && u.ExternalUserId == externalUserId, ct);
    }
}

// External service implementation
public class GoogleAuthService : IGoogleAuthService
{
    public async Task<GoogleUserInfo?> ValidateIdTokenAsync(string idToken, CancellationToken ct = default)
    {
        var payload = await GoogleJsonWebSignature.ValidateAsync(idToken);
        return new GoogleUserInfo(payload.Subject, payload.Email, payload.Name, payload.EmailVerified);
    }
}
```

**Why Separate Infrastructure**:
- Isolates external dependencies (Google, PostgreSQL, RabbitMQ)
- Implementations can be swapped (e.g., Google → Apple, RabbitMQ → Azure Service Bus)
- Easy to mock in tests

---

## Dependency Flow

```
┌─────────────────────────┐
│      API Layer          │  Controllers, DTOs, Middleware
│  (Presentation)         │
└───────────┬─────────────┘
            │ depends on
            ▼
┌─────────────────────────┐
│  Application Layer      │  Services, Orchestration, Use Cases
│  (Business Logic)       │
└───────────┬─────────────┘
            │ depends on
            ▼
┌─────────────────────────┐
│    Domain Layer         │  Entities, Repository Interfaces
│  (Core Business)        │  ❌ NO DEPENDENCIES
└─────────────────────────┘
            ▲
            │ implements interfaces
            │
┌─────────────────────────┐
│ Infrastructure Layer    │  DbContext, Repositories, External APIs
│  (Data & External)      │
└─────────────────────────┘
```

**Key Rules**:
1. ✅ API can depend on Application and Infrastructure
2. ✅ Application can depend on Domain
3. ✅ Infrastructure can depend on Domain (to implement interfaces)
4. ❌ Domain cannot depend on anything
5. ❌ Application cannot depend on Infrastructure directly (uses interfaces)

---

## Alternative Patterns Considered

### Onion Architecture
**Why Not**:
- Too complex for simple CRUD operations
- Domain logic in IdentityService is straightforward (no rich business rules)
- Dependency inversion at every layer is overkill

**When To Use**: ProfileService (rich domain logic for privacy rules, exposure levels)

---

### Hexagonal Architecture (Ports & Adapters)
**Why Not**:
- We don't need multiple adapters for the same port
- Only one auth provider used at a time (Google OR Apple, not both simultaneously)
- Adds unnecessary abstraction layers

**When To Use**: MatchingService (swappable matching engines: rule-based, AI-based)

---

### CQRS (Command Query Responsibility Segregation)
**Why Not**:
- Read/write operations are balanced (not read-heavy or write-heavy)
- No need for separate read models
- Simple queries (no complex joins or aggregations)

**When To Use**: SchedulingService (complex availability queries, write-heavy slot reservations)

---

## Testing Strategy

### Unit Tests
- **Domain Layer**: Test entity factory methods, business logic (User.CreateFromGoogle, RefreshToken.IsValid)
- **Application Layer**: Test service orchestration with mocked dependencies
- **Infrastructure Layer**: Test token generation logic

### Integration Tests
- **Database**: Use Testcontainers for real PostgreSQL, test repositories
- **API**: Use WebApplicationFactory, test endpoints end-to-end
- **Messaging**: Use Testcontainers for RabbitMQ, verify events published

---

## Folder Structure

```
DatingApp.IdentityService/
├── DatingApp.IdentityService.Api/
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   └── TokenController.cs
│   ├── Middleware/
│   │   └── ErrorHandlingMiddleware.cs
│   ├── Program.cs
│   └── appsettings.json
├── DatingApp.IdentityService.Application/
│   ├── DTOs/
│   │   ├── LoginWithGoogleRequest.cs
│   │   └── LoginResponse.cs
│   ├── Services/
│   │   └── AuthService.cs
│   └── Validators/
│       └── LoginWithGoogleRequestValidator.cs
├── DatingApp.IdentityService.Domain/
│   ├── Entities/
│   │   ├── User.cs
│   │   └── RefreshToken.cs
│   ├── Enums/
│   │   └── AuthProvider.cs
│   ├── Repositories/
│   │   ├── IUserRepository.cs
│   │   └── IRefreshTokenRepository.cs
│   └── Exceptions/
│       └── InvalidTokenException.cs
├── DatingApp.IdentityService.Infrastructure/
│   ├── Data/
│   │   ├── IdentityDbContext.cs
│   │   ├── Configurations/
│   │   │   ├── UserConfiguration.cs
│   │   │   └── RefreshTokenConfiguration.cs
│   │   └── Migrations/
│   ├── Repositories/
│   │   ├── UserRepository.cs
│   │   └── RefreshTokenRepository.cs
│   └── Services/
│       ├── GoogleAuthService.cs
│       └── JwtTokenGenerator.cs
├── DatingApp.IdentityService.Tests.Unit/
│   ├── Domain/
│   │   ├── UserTests.cs
│   │   └── RefreshTokenTests.cs
│   └── Application/
│       └── AuthServiceTests.cs
└── DatingApp.IdentityService.Tests.Integration/
    ├── API/
    │   └── AuthControllerTests.cs
    └── Infrastructure/
        └── UserRepositoryTests.cs
```

---

## Key Takeaways

1. **Layered Architecture = Simple & Familiar**: Best choice for straightforward CRUD services like IdentityService
2. **Domain Has No Dependencies**: Pure business logic, easy to test, portable
3. **Infrastructure Implements Domain Interfaces**: Dependency inversion for testability
4. **Application Orchestrates**: Coordinates domain entities and infrastructure services
5. **API Is Thin**: Controllers just delegate to application services

---

**When in doubt**: Keep it simple. Layered architecture is the right choice when your service doesn't need the complexity of Hexagonal or Onion patterns.

---

**Last Updated**: 2025-11-21
**Related Feature**: F0001 - Sign In with Google
**Related Docs**: [Architecture Guidelines](../../../docs/10_architecture/02_architecture-guidelines.md)
