# Implementation Plan - F0001: Sign In with Google

**Status**: 🔴 Not Started
**Feature File**: [f0001_sign_in_with_google.md](./f0001_sign_in_with_google.md)
**Architecture Pattern**: Layered (IdentityService)
**Started**: TBD
**Last Updated**: 2025-11-21
**Estimated Total Time**: 8 hours

---

## Prerequisites

### Documentation to Review Before Starting
- [ ] Read [Architecture Guidelines](../../10_architecture/02_architecture-guidelines.md) - Layered Architecture section
- [ ] Read [Service Templates](../../10_architecture/03_service-templates.md) - Layered Architecture folder structure
- [ ] Read [Security & Auth](../../10_architecture/05_security-and-auth.md) - Custom IdentityService design, JWT tokens, Google OAuth
- [ ] Read [Messaging & Integration](../../10_architecture/06_messaging-and-integration.md) - Event publishing, outbox pattern
- [ ] Read [Ubiquitous Language](../../20_domain/01_domain-ubiquitous-language.md) - Domain terminology
- [ ] Read [Feature Specification](./f0001_sign_in_with_google.md) - Complete requirements

### Environment Setup
- [ ] Docker Desktop running
- [ ] Start infrastructure: `docker-compose up -d`
- [ ] Verify PostgreSQL running: `psql -h localhost -U admin -d identity_db`
- [ ] Verify RabbitMQ running: http://localhost:15672
- [ ] Verify Redis running: `redis-cli ping`
- [ ] Verify Seq running: http://localhost:5341

---

## Phase 0: Setup (30 minutes)

### Create Project Structure
**Reference**: [Service Templates - Layered Architecture](../../10_architecture/03_service-templates.md#layered-architecture)

- [ ] Create feature branch
  ```bash
  git checkout -b feature/f0001-sign-in-with-google
  ```

- [ ] Create solution and projects
  ```bash
  cd src/Services
  mkdir Identity
  cd Identity

  # Create solution
  dotnet new sln -n DatingApp.IdentityService

  # Create projects (Layered architecture - top to bottom)
  dotnet new webapi -n DatingApp.IdentityService.Api
  dotnet new classlib -n DatingApp.IdentityService.Application
  dotnet new classlib -n DatingApp.IdentityService.Domain
  dotnet new classlib -n DatingApp.IdentityService.Infrastructure

  # Create test projects
  dotnet new xunit -n DatingApp.IdentityService.Tests.Unit
  dotnet new xunit -n DatingApp.IdentityService.Tests.Integration

  # Add projects to solution
  dotnet sln add **/*.csproj
  ```

- [ ] Set up project references (Layered dependencies)
  ```bash
  # API depends on Application
  dotnet add DatingApp.IdentityService.Api/DatingApp.IdentityService.Api.csproj reference DatingApp.IdentityService.Application/DatingApp.IdentityService.Application.csproj

  # Application depends on Domain
  dotnet add DatingApp.IdentityService.Application/DatingApp.IdentityService.Application.csproj reference DatingApp.IdentityService.Domain/DatingApp.IdentityService.Domain.csproj

  # Infrastructure depends on Domain (implements interfaces)
  dotnet add DatingApp.IdentityService.Infrastructure/DatingApp.IdentityService.Infrastructure.csproj reference DatingApp.IdentityService.Domain/DatingApp.IdentityService.Domain.csproj

  # API depends on Infrastructure (DI registration)
  dotnet add DatingApp.IdentityService.Api/DatingApp.IdentityService.Api.csproj reference DatingApp.IdentityService.Infrastructure/DatingApp.IdentityService.Infrastructure.csproj

  # Test projects
  dotnet add DatingApp.IdentityService.Tests.Unit/*.csproj reference DatingApp.IdentityService.Application/*.csproj
  dotnet add DatingApp.IdentityService.Tests.Integration/*.csproj reference DatingApp.IdentityService.Api/*.csproj
  ```

- [ ] Install NuGet packages
  ```bash
  # Domain (minimal, no dependencies)
  # (No external packages needed)

  # Infrastructure
  cd DatingApp.IdentityService.Infrastructure
  dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
  dotnet add package Microsoft.EntityFrameworkCore.Design
  dotnet add package Google.Apis.Auth --version 1.68.0
  dotnet add package MassTransit.RabbitMQ

  # Application
  cd ../DatingApp.IdentityService.Application
  dotnet add package FluentValidation
  dotnet add package MediatR

  # API
  cd ../DatingApp.IdentityService.Api
  dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
  dotnet add package Serilog.AspNetCore
  dotnet add package Serilog.Sinks.Seq
  dotnet add package Swashbuckle.AspNetCore
  dotnet add package AspNetCoreRateLimit

  # Tests
  cd ../DatingApp.IdentityService.Tests.Unit
  dotnet add package Moq
  dotnet add package FluentAssertions

  cd ../DatingApp.IdentityService.Tests.Integration
  dotnet add package Microsoft.AspNetCore.Mvc.Testing
  dotnet add package Testcontainers.PostgreSql
  dotnet add package Testcontainers.RabbitMq
  ```

- [ ] Create PATTERNS.md in Identity folder
  - Explain Layered architecture choice
  - Document folder structure
  - Reference architecture guidelines

- [ ] Commit setup
  ```bash
  git add src/Services/Identity/
  git commit -m "feat(identity): initial project structure for F0001 (#issue-number)"
  ```

---

## Phase 1: Domain Layer (1 hour)

**Reference**: [Service Templates - Domain Layer](../../10_architecture/03_service-templates.md#domain-layer)
**Why Domain First**: Layered architecture starts with core entities (no dependencies)

### Create User Entity
**Reference**: [Feature Spec - Database Schema](./f0001_sign_in_with_google.md#database-changes) (lines 244-257)

- [ ] Create `Entities/User.cs`
  ```csharp
  namespace DatingApp.IdentityService.Domain.Entities;

  public class User
  {
      public Guid Id { get; private set; }
      public string Email { get; private set; }
      public AuthProvider Provider { get; private set; }
      public string ExternalUserId { get; private set; }
      public DateTime CreatedAt { get; private set; }
      public DateTime? LastLoginAt { get; private set; }

      private User() { } // EF Core

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

      public void RecordLogin()
      {
          LastLoginAt = DateTime.UtcNow;
      }
  }
  ```

- [ ] Create `Enums/AuthProvider.cs`
  ```csharp
  namespace DatingApp.IdentityService.Domain.Enums;

  public enum AuthProvider
  {
      Google,
      Apple
  }
  ```

### Create RefreshToken Entity
**Reference**: [Feature Spec - Database Schema](./f0001_sign_in_with_google.md#database-changes) (lines 260-274)

- [ ] Create `Entities/RefreshToken.cs`
  ```csharp
  namespace DatingApp.IdentityService.Domain.Entities;

  public class RefreshToken
  {
      public Guid Id { get; private set; }
      public Guid UserId { get; private set; }
      public string TokenHash { get; private set; }
      public DateTime ExpiresAt { get; private set; }
      public DateTime CreatedAt { get; private set; }
      public DateTime? RevokedAt { get; private set; }
      public bool IsRevoked { get; private set; }

      // Navigation
      public User User { get; private set; }

      private RefreshToken() { } // EF Core

      public static RefreshToken Create(Guid userId, string tokenHash, int validityDays)
      {
          return new RefreshToken
          {
              Id = Guid.NewGuid(),
              UserId = userId,
              TokenHash = tokenHash,
              ExpiresAt = DateTime.UtcNow.AddDays(validityDays),
              CreatedAt = DateTime.UtcNow,
              IsRevoked = false
          };
      }

      public void Revoke()
      {
          IsRevoked = true;
          RevokedAt = DateTime.UtcNow;
      }

      public bool IsValid() => !IsRevoked && DateTime.UtcNow < ExpiresAt;
  }
  ```

### Create Repository Interfaces
**Reference**: [Architecture Guidelines - Repository Pattern](../../10_architecture/02_architecture-guidelines.md#repository-pattern)
**Why in Domain**: Interfaces defined in domain, implemented in infrastructure (Layered pattern)

- [ ] Create `Repositories/IUserRepository.cs`
  ```csharp
  namespace DatingApp.IdentityService.Domain.Repositories;

  public interface IUserRepository
  {
      Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
      Task<User?> GetByExternalUserIdAsync(AuthProvider provider, string externalUserId, CancellationToken ct = default);
      Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
      Task AddAsync(User user, CancellationToken ct = default);
      Task UpdateAsync(User user, CancellationToken ct = default);
  }
  ```

- [ ] Create `Repositories/IRefreshTokenRepository.cs`
  ```csharp
  namespace DatingApp.IdentityService.Domain.Repositories;

  public interface IRefreshTokenRepository
  {
      Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);
      Task<IEnumerable<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default);
      Task AddAsync(RefreshToken token, CancellationToken ct = default);
      Task UpdateAsync(RefreshToken token, CancellationToken ct = default);
  }
  ```

- [ ] Commit domain layer
  ```bash
  git add src/Services/Identity/DatingApp.IdentityService.Domain/
  git commit -m "feat(identity): add User and RefreshToken domain entities (#issue-number)"
  ```

---

## Phase 2: Infrastructure Layer - Persistence (1 hour)

**Reference**: [Service Templates - Infrastructure Layer](../../10_architecture/03_service-templates.md#infrastructure-layer)

### Set Up Database Context
**Reference**: [Architecture Guidelines - EF Core Setup](../../10_architecture/02_architecture-guidelines.md#entity-framework-core)

- [ ] Create `Data/IdentityDbContext.cs`
  ```csharp
  using Microsoft.EntityFrameworkCore;
  using DatingApp.IdentityService.Domain.Entities;

  namespace DatingApp.IdentityService.Infrastructure.Data;

  public class IdentityDbContext : DbContext
  {
      public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
          : base(options)
      {
      }

      public DbSet<User> Users => Set<User>();
      public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
          base.OnModelCreating(modelBuilder);

          // Apply configurations
          modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
      }
  }
  ```

- [ ] Create `Data/Configurations/UserConfiguration.cs`
  ```csharp
  using Microsoft.EntityFrameworkCore;
  using Microsoft.EntityFrameworkCore.Metadata.Builders;
  using DatingApp.IdentityService.Domain.Entities;

  namespace DatingApp.IdentityService.Infrastructure.Data.Configurations;

  public class UserConfiguration : IEntityTypeConfiguration<User>
  {
      public void Configure(EntityTypeBuilder<User> builder)
      {
          builder.ToTable("users");

          builder.HasKey(u => u.Id);

          builder.Property(u => u.Email)
              .IsRequired()
              .HasMaxLength(255);

          builder.Property(u => u.Provider)
              .IsRequired()
              .HasConversion<string>()
              .HasMaxLength(50);

          builder.Property(u => u.ExternalUserId)
              .IsRequired()
              .HasMaxLength(255);

          builder.Property(u => u.CreatedAt)
              .IsRequired();

          // Unique constraint
          builder.HasIndex(u => new { u.Provider, u.ExternalUserId })
              .IsUnique()
              .HasDatabaseName("unique_provider_user");

          builder.HasIndex(u => u.Email)
              .HasDatabaseName("idx_users_email");
      }
  }
  ```

- [ ] Create `Data/Configurations/RefreshTokenConfiguration.cs`
  ```csharp
  using Microsoft.EntityFrameworkCore;
  using Microsoft.EntityFrameworkCore.Metadata.Builders;
  using DatingApp.IdentityService.Domain.Entities;

  namespace DatingApp.IdentityService.Infrastructure.Data.Configurations;

  public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
  {
      public void Configure(EntityTypeBuilder<RefreshToken> builder)
      {
          builder.ToTable("refresh_tokens");

          builder.HasKey(rt => rt.Id);

          builder.Property(rt => rt.TokenHash)
              .IsRequired()
              .HasMaxLength(255);

          builder.Property(rt => rt.ExpiresAt)
              .IsRequired();

          builder.Property(rt => rt.CreatedAt)
              .IsRequired();

          builder.Property(rt => rt.IsRevoked)
              .IsRequired();

          // Unique constraint
          builder.HasIndex(rt => rt.TokenHash)
              .IsUnique()
              .HasDatabaseName("unique_token_hash");

          builder.HasIndex(rt => rt.UserId)
              .HasDatabaseName("idx_refresh_tokens_user_id");

          builder.HasIndex(rt => rt.ExpiresAt)
              .HasDatabaseName("idx_refresh_tokens_expires_at");

          // Relationship
          builder.HasOne(rt => rt.User)
              .WithMany()
              .HasForeignKey(rt => rt.UserId)
              .OnDelete(DeleteBehavior.Cascade);
      }
  }
  ```

### Implement Repositories
**Reference**: [Architecture Guidelines - Repository Implementation](../../10_architecture/02_architecture-guidelines.md#repository-implementation)

- [ ] Create `Repositories/UserRepository.cs`
  ```csharp
  using Microsoft.EntityFrameworkCore;
  using DatingApp.IdentityService.Domain.Entities;
  using DatingApp.IdentityService.Domain.Repositories;
  using DatingApp.IdentityService.Infrastructure.Data;

  namespace DatingApp.IdentityService.Infrastructure.Repositories;

  public class UserRepository : IUserRepository
  {
      private readonly IdentityDbContext _context;

      public UserRepository(IdentityDbContext context)
      {
          _context = context;
      }

      public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
      {
          return await _context.Users.FindAsync(new object[] { id }, ct);
      }

      public async Task<User?> GetByExternalUserIdAsync(AuthProvider provider, string externalUserId, CancellationToken ct = default)
      {
          return await _context.Users
              .FirstOrDefaultAsync(u => u.Provider == provider && u.ExternalUserId == externalUserId, ct);
      }

      public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
      {
          return await _context.Users
              .FirstOrDefaultAsync(u => u.Email == email, ct);
      }

      public async Task AddAsync(User user, CancellationToken ct = default)
      {
          await _context.Users.AddAsync(user, ct);
          await _context.SaveChangesAsync(ct);
      }

      public async Task UpdateAsync(User user, CancellationToken ct = default)
      {
          _context.Users.Update(user);
          await _context.SaveChangesAsync(ct);
      }
  }
  ```

- [ ] Create `Repositories/RefreshTokenRepository.cs`
  ```csharp
  using Microsoft.EntityFrameworkCore;
  using DatingApp.IdentityService.Domain.Entities;
  using DatingApp.IdentityService.Domain.Repositories;
  using DatingApp.IdentityService.Infrastructure.Data;

  namespace DatingApp.IdentityService.Infrastructure.Repositories;

  public class RefreshTokenRepository : IRefreshTokenRepository
  {
      private readonly IdentityDbContext _context;

      public RefreshTokenRepository(IdentityDbContext context)
      {
          _context = context;
      }

      public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default)
      {
          return await _context.RefreshTokens
              .Include(rt => rt.User)
              .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct);
      }

      public async Task<IEnumerable<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default)
      {
          return await _context.RefreshTokens
              .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
              .ToListAsync(ct);
      }

      public async Task AddAsync(RefreshToken token, CancellationToken ct = default)
      {
          await _context.RefreshTokens.AddAsync(token, ct);
          await _context.SaveChangesAsync(ct);
      }

      public async Task UpdateAsync(RefreshToken token, CancellationToken ct = default)
      {
          _context.RefreshTokens.Update(token);
          await _context.SaveChangesAsync(ct);
      }
  }
  ```

### Create Database Migration
**Reference**: [Feature Spec - Database Schema](./f0001_sign_in_with_google.md#database-changes)

- [ ] Create migration
  ```bash
  cd src/Services/Identity/DatingApp.IdentityService.Infrastructure
  dotnet ef migrations add InitialCreate \
      --startup-project ../DatingApp.IdentityService.Api/DatingApp.IdentityService.Api.csproj \
      --context IdentityDbContext
  ```

- [ ] Review generated migration SQL
- [ ] Apply migration to local database
  ```bash
  dotnet ef database update \
      --startup-project ../DatingApp.IdentityService.Api/DatingApp.IdentityService.Api.csproj \
      --context IdentityDbContext
  ```

- [ ] Verify tables created
  ```bash
  psql -h localhost -U admin -d identity_db -c "\dt"
  ```

- [ ] Commit persistence layer
  ```bash
  git add src/Services/Identity/DatingApp.IdentityService.Infrastructure/
  git commit -m "feat(identity): add EF Core database context and repositories (#issue-number)"
  ```

---

## Phase 3: Infrastructure Layer - External Services (1.5 hours)

**Reference**: [Security & Auth - Google OAuth](../../10_architecture/05_security-and-auth.md#google-oauth)

### Implement Google Authentication Service
**Reference**: [Feature Spec - API Specification](./f0001_sign_in_with_google.md#api-specification)

- [ ] Create `Services/IGoogleAuthService.cs` interface
  ```csharp
  namespace DatingApp.IdentityService.Infrastructure.Services;

  public interface IGoogleAuthService
  {
      Task<GoogleUserInfo?> ValidateIdTokenAsync(string idToken, CancellationToken ct = default);
  }

  public record GoogleUserInfo(
      string Sub,        // Google User ID
      string Email,
      string Name,
      bool EmailVerified
  );
  ```

- [ ] Create `Services/GoogleAuthService.cs` implementation
  ```csharp
  using Google.Apis.Auth;
  using Microsoft.Extensions.Configuration;
  using Microsoft.Extensions.Logging;

  namespace DatingApp.IdentityService.Infrastructure.Services;

  public class GoogleAuthService : IGoogleAuthService
  {
      private readonly string _clientId;
      private readonly ILogger<GoogleAuthService> _logger;

      public GoogleAuthService(IConfiguration config, ILogger<GoogleAuthService> logger)
      {
          _clientId = config["Google:ClientId"]
              ?? throw new InvalidOperationException("Google:ClientId not configured");
          _logger = logger;
      }

      public async Task<GoogleUserInfo?> ValidateIdTokenAsync(string idToken, CancellationToken ct = default)
      {
          try
          {
              var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
              {
                  Audience = new[] { _clientId }
              });

              return new GoogleUserInfo(
                  payload.Subject,
                  payload.Email,
                  payload.Name,
                  payload.EmailVerified
              );
          }
          catch (InvalidJwtException ex)
          {
              _logger.LogWarning(ex, "Invalid Google ID token");
              return null;
          }
      }
  }
  ```

### Implement JWT Token Generator
**Reference**: [Security & Auth - JWT Implementation](../../10_architecture/05_security-and-auth.md#jwt-tokens)

- [ ] Create `Services/IJwtTokenGenerator.cs` interface
  ```csharp
  namespace DatingApp.IdentityService.Infrastructure.Services;

  public interface IJwtTokenGenerator
  {
      string GenerateAccessToken(Guid userId, string email);
      string GenerateRefreshToken();
      string HashToken(string token);
  }
  ```

- [ ] Create `Services/JwtTokenGenerator.cs` implementation
  ```csharp
  using System.IdentityModel.Tokens.Jwt;
  using System.Security.Claims;
  using System.Security.Cryptography;
  using Microsoft.Extensions.Configuration;
  using Microsoft.IdentityModel.Tokens;
  using System.Text;

  namespace DatingApp.IdentityService.Infrastructure.Services;

  public class JwtTokenGenerator : IJwtTokenGenerator
  {
      private readonly string _secretKey;
      private readonly string _issuer;
      private readonly string _audience;
      private readonly int _accessTokenExpiryMinutes;

      public JwtTokenGenerator(IConfiguration config)
      {
          _secretKey = config["Jwt:SecretKey"]
              ?? throw new InvalidOperationException("Jwt:SecretKey not configured");
          _issuer = config["Jwt:Issuer"] ?? "https://quietmatch.com";
          _audience = config["Jwt:Audience"] ?? "https://api.quietmatch.com";
          _accessTokenExpiryMinutes = int.Parse(config["Jwt:AccessTokenExpiryMinutes"] ?? "15");
      }

      public string GenerateAccessToken(Guid userId, string email)
      {
          var claims = new[]
          {
              new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
              new Claim(JwtRegisteredClaimNames.Email, email),
              new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
              new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
          };

          var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
          var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

          var token = new JwtSecurityToken(
              issuer: _issuer,
              audience: _audience,
              claims: claims,
              expires: DateTime.UtcNow.AddMinutes(_accessTokenExpiryMinutes),
              signingCredentials: creds
          );

          return new JwtSecurityTokenHandler().WriteToken(token);
      }

      public string GenerateRefreshToken()
      {
          var randomBytes = new byte[32];
          using var rng = RandomNumberGenerator.Create();
          rng.GetBytes(randomBytes);
          return Convert.ToBase64String(randomBytes);
      }

      public string HashToken(string token)
      {
          using var sha256 = SHA256.Create();
          var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
          return Convert.ToBase64String(hashBytes);
      }
  }
  ```

- [ ] Commit external services
  ```bash
  git add src/Services/Identity/DatingApp.IdentityService.Infrastructure/
  git commit -m "feat(identity): add Google OAuth and JWT token services (#issue-number)"
  ```

---

## Phase 4: Application Layer (1 hour)

**Reference**: [Service Templates - Application Layer](../../10_architecture/03_service-templates.md#application-layer)

### Create DTOs
- [ ] Create `DTOs/LoginWithGoogleRequest.cs`
  ```csharp
  namespace DatingApp.IdentityService.Application.DTOs;

  public record LoginWithGoogleRequest(string IdToken);
  ```

- [ ] Create `DTOs/LoginResponse.cs`
  ```csharp
  namespace DatingApp.IdentityService.Application.DTOs;

  public record LoginResponse(
      string AccessToken,
      string RefreshToken,
      int ExpiresIn,
      string TokenType,
      Guid UserId,
      bool IsNewUser,
      string Email
  );
  ```

### Create Application Service
**Reference**: [Feature Spec - Acceptance Criteria](./f0001_sign_in_with_google.md#acceptance-criteria)

- [ ] Create `Services/AuthService.cs`
  ```csharp
  using DatingApp.IdentityService.Application.DTOs;
  using DatingApp.IdentityService.Domain.Entities;
  using DatingApp.IdentityService.Domain.Enums;
  using DatingApp.IdentityService.Domain.Repositories;
  using DatingApp.IdentityService.Infrastructure.Services;
  using Microsoft.Extensions.Logging;

  namespace DatingApp.IdentityService.Application.Services;

  public class AuthService
  {
      private readonly IGoogleAuthService _googleAuthService;
      private readonly IJwtTokenGenerator _jwtTokenGenerator;
      private readonly IUserRepository _userRepository;
      private readonly IRefreshTokenRepository _refreshTokenRepository;
      private readonly ILogger<AuthService> _logger;

      public AuthService(
          IGoogleAuthService googleAuthService,
          IJwtTokenGenerator jwtTokenGenerator,
          IUserRepository userRepository,
          IRefreshTokenRepository refreshTokenRepository,
          ILogger<AuthService> logger)
      {
          _googleAuthService = googleAuthService;
          _jwtTokenGenerator = jwtTokenGenerator;
          _userRepository = userRepository;
          _refreshTokenRepository = refreshTokenRepository;
          _logger = logger;
      }

      public async Task<LoginResponse?> LoginWithGoogleAsync(string idToken, CancellationToken ct = default)
      {
          // AC6: Validate ID token with Google API
          var googleUser = await _googleAuthService.ValidateIdTokenAsync(idToken, ct);
          if (googleUser == null)
          {
              _logger.LogWarning("Invalid Google ID token");
              return null;
          }

          // Check if user exists
          var user = await _userRepository.GetByExternalUserIdAsync(AuthProvider.Google, googleUser.Sub, ct);

          bool isNewUser = user == null;

          if (isNewUser)
          {
              // AC7: Create new user
              user = User.CreateFromGoogle(googleUser.Email, googleUser.Sub);
              await _userRepository.AddAsync(user, ct);

              _logger.LogInformation("New user registered: {UserId}", user.Id);
          }
          else
          {
              // AC8: Update last login
              user.RecordLogin();
              await _userRepository.UpdateAsync(user, ct);

              _logger.LogInformation("Existing user logged in: {UserId}", user.Id);
          }

          // AC9: Generate JWT access token
          var accessToken = _jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email);

          // AC10: Create refresh token
          var refreshTokenValue = _jwtTokenGenerator.GenerateRefreshToken();
          var refreshTokenHash = _jwtTokenGenerator.HashToken(refreshTokenValue);

          var refreshToken = RefreshToken.Create(user.Id, refreshTokenHash, validityDays: 7);
          await _refreshTokenRepository.AddAsync(refreshToken, ct);

          // AC11: Return response
          return new LoginResponse(
              AccessToken: accessToken,
              RefreshToken: refreshTokenValue,
              ExpiresIn: 900, // 15 minutes
              TokenType: "Bearer",
              UserId: user.Id,
              IsNewUser: isNewUser,
              Email: user.Email
          );
      }
  }
  ```

### Create FluentValidation Validators
**Reference**: [Architecture Guidelines - Validation](../../10_architecture/02_architecture-guidelines.md#validation)

- [ ] Create `Validators/LoginWithGoogleRequestValidator.cs`
  ```csharp
  using FluentValidation;
  using DatingApp.IdentityService.Application.DTOs;

  namespace DatingApp.IdentityService.Application.Validators;

  public class LoginWithGoogleRequestValidator : AbstractValidator<LoginWithGoogleRequest>
  {
      public LoginWithGoogleRequestValidator()
      {
          RuleFor(x => x.IdToken)
              .NotEmpty().WithMessage("ID token is required");
      }
  }
  ```

- [ ] Commit application layer
  ```bash
  git add src/Services/Identity/DatingApp.IdentityService.Application/
  git commit -m "feat(identity): add authentication application service (#issue-number)"
  ```

---

## Phase 5: API Layer (1 hour)

**Reference**: [Service Templates - API Layer](../../10_architecture/03_service-templates.md#api-layer)

### Create Auth Controller
**Reference**: [Feature Spec - API Specification](./f0001_sign_in_with_google.md#api-specification)

- [ ] Create `Controllers/AuthController.cs`
  ```csharp
  using Microsoft.AspNetCore.Mvc;
  using DatingApp.IdentityService.Application.DTOs;
  using DatingApp.IdentityService.Application.Services;

  namespace DatingApp.IdentityService.Api.Controllers;

  [ApiController]
  [Route("api/v1/auth")]
  public class AuthController : ControllerBase
  {
      private readonly AuthService _authService;
      private readonly ILogger<AuthController> _logger;

      public AuthController(AuthService authService, ILogger<AuthController> logger)
      {
          _authService = authService;
          _logger = logger;
      }

      /// <summary>
      /// Sign in with Google OAuth
      /// </summary>
      [HttpPost("login/google")]
      [ProducesResponseType(typeof(LoginResponse), 200)]
      [ProducesResponseType(typeof(ProblemDetails), 400)]
      [ProducesResponseType(typeof(ProblemDetails), 429)]
      [ProducesResponseType(typeof(ProblemDetails), 500)]
      public async Task<IActionResult> LoginWithGoogle([FromBody] LoginWithGoogleRequest request, CancellationToken ct)
      {
          var result = await _authService.LoginWithGoogleAsync(request.IdToken, ct);

          if (result == null)
          {
              return BadRequest(new ProblemDetails
              {
                  Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                  Title = "Invalid ID Token",
                  Status = 400,
                  Detail = "The provided ID token is invalid or expired."
              });
          }

          return Ok(result);
      }
  }
  ```

### Configure Dependency Injection
**Reference**: [Architecture Guidelines - DI Registration](../../10_architecture/02_architecture-guidelines.md#dependency-injection)

- [ ] Update `Program.cs` with service registrations
  ```csharp
  using DatingApp.IdentityService.Infrastructure.Data;
  using DatingApp.IdentityService.Infrastructure.Services;
  using DatingApp.IdentityService.Infrastructure.Repositories;
  using DatingApp.IdentityService.Domain.Repositories;
  using DatingApp.IdentityService.Application.Services;
  using Microsoft.EntityFrameworkCore;
  using Microsoft.AspNetCore.Authentication.JwtBearer;
  using Microsoft.IdentityModel.Tokens;
  using System.Text;
  using Serilog;
  using FluentValidation;
  using AspNetCoreRateLimit;

  var builder = WebApplication.CreateBuilder(args);

  // Serilog
  builder.Host.UseSerilog((context, config) =>
  {
      config.ReadFrom.Configuration(context.Configuration);
  });

  // Database
  builder.Services.AddDbContext<IdentityDbContext>(options =>
      options.UseNpgsql(builder.Configuration.GetConnectionString("IdentityDb")));

  // Repositories
  builder.Services.AddScoped<IUserRepository, UserRepository>();
  builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

  // Services
  builder.Services.AddScoped<IGoogleAuthService, GoogleAuthService>();
  builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
  builder.Services.AddScoped<AuthService>();

  // Validators
  builder.Services.AddValidatorsFromAssemblyContaining<LoginWithGoogleRequestValidator>();

  // Rate Limiting
  builder.Services.AddMemoryCache();
  builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
  builder.Services.AddInMemoryRateLimiting();
  builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

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
                  Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!))
          };
      });

  builder.Services.AddControllers();
  builder.Services.AddEndpointsApiExplorer();
  builder.Services.AddSwaggerGen();

  var app = builder.Build();

  if (app.Environment.IsDevelopment())
  {
      app.UseSwagger();
      app.UseSwaggerUI();
  }

  app.UseSerilogRequestLogging();
  app.UseIpRateLimiting();
  app.UseAuthentication();
  app.UseAuthorization();
  app.MapControllers();

  app.Run();
  ```

### Configure appsettings.json
**Reference**: [Feature Spec - Configuration](./f0001_sign_in_with_google.md#configuration)

- [ ] Update `appsettings.Development.json`
  ```json
  {
    "ConnectionStrings": {
      "IdentityDb": "Host=localhost;Database=identity_db;Username=admin;Password=QuietMatch_Dev_2025!"
    },
    "Google": {
      "ClientId": "your-client-id.apps.googleusercontent.com",
      "ClientSecret": "your-client-secret"
    },
    "Jwt": {
      "SecretKey": "your-256-bit-secret-key-for-development-only",
      "Issuer": "https://localhost:5001",
      "Audience": "https://localhost:5001",
      "AccessTokenExpiryMinutes": "15"
    },
    "IpRateLimiting": {
      "EnableEndpointRateLimiting": true,
      "StackBlockedRequests": false,
      "RealIpHeader": "X-Real-IP",
      "ClientIdHeader": "X-ClientId",
      "HttpStatusCode": 429,
      "GeneralRules": [
        {
          "Endpoint": "*/api/v1/auth/login/*",
          "Period": "1m",
          "Limit": 5
        }
      ]
    },
    "Serilog": {
      "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.Seq" ],
      "MinimumLevel": "Debug",
      "WriteTo": [
        { "Name": "Console" },
        {
          "Name": "Seq",
          "Args": { "serverUrl": "http://localhost:5341" }
        }
      ]
    }
  }
  ```

- [ ] Commit API layer
  ```bash
  git add src/Services/Identity/DatingApp.IdentityService.Api/
  git commit -m "feat(identity): add auth controller and DI configuration (#issue-number)"
  ```

---

## Phase 6: Messaging Integration (1 hour)

**Reference**: [Messaging & Integration - MassTransit Setup](../../10_architecture/06_messaging-and-integration.md)

### Create Integration Event
**Reference**: [Feature Spec - Events Published](./f0001_sign_in_with_google.md#events-published)

- [ ] Create `Events/UserRegistered.cs` in Infrastructure
  ```csharp
  namespace DatingApp.IdentityService.Infrastructure.Events;

  public record UserRegistered
  {
      public Guid UserId { get; init; }
      public string Email { get; init; }
      public string Provider { get; init; }
      public DateTime RegisteredAt { get; init; }
      public Guid CorrelationId { get; init; }
  }
  ```

### Configure MassTransit
**Reference**: [Messaging & Integration - MassTransit Configuration](../../10_architecture/06_messaging-and-integration.md#masstransit-implementation)

- [ ] Update `Program.cs` with MassTransit
  ```csharp
  using MassTransit;

  // Add after other service registrations
  builder.Services.AddMassTransit(config =>
  {
      if (builder.Environment.IsDevelopment())
      {
          // RabbitMQ (local)
          config.UsingRabbitMq((context, cfg) =>
          {
              cfg.Host("rabbitmq", "/", h =>
              {
                  h.Username("guest");
                  h.Password("guest");
              });

              cfg.ConfigureEndpoints(context);
          });
      }
      else
      {
          // Azure Service Bus (production)
          config.UsingAzureServiceBus((context, cfg) =>
          {
              cfg.Host(builder.Configuration["AzureServiceBus:ConnectionString"]);
              cfg.ConfigureEndpoints(context);
          });
      }
  });
  ```

### Update AuthService to Publish Event
**Reference**: [Feature Spec - Acceptance Criteria AC7](./f0001_sign_in_with_google.md#acceptance-criteria)

- [ ] Update `AuthService.cs` to publish UserRegistered event
  ```csharp
  // Add constructor parameter
  private readonly IPublishEndpoint _publishEndpoint;

  // In LoginWithGoogleAsync, after creating new user:
  if (isNewUser)
  {
      user = User.CreateFromGoogle(googleUser.Email, googleUser.Sub);
      await _userRepository.AddAsync(user, ct);

      // Publish UserRegistered event
      await _publishEndpoint.Publish(new UserRegistered
      {
          UserId = user.Id,
          Email = user.Email,
          Provider = "Google",
          RegisteredAt = user.CreatedAt,
          CorrelationId = Guid.NewGuid()
      }, ct);

      _logger.LogInformation("New user registered and event published: {UserId}", user.Id);
  }
  ```

- [ ] Commit messaging integration
  ```bash
  git add src/Services/Identity/
  git commit -m "feat(identity): add MassTransit and UserRegistered event (#issue-number)"
  ```

---

## Phase 7: Testing (2 hours)

**Reference**: [Feature Spec - Testing Strategy](./f0001_sign_in_with_google.md#testing-strategy)

### Unit Tests

- [ ] Create `Unit/Application/AuthServiceTests.cs`
  - Test: LoginWithGoogle_WhenTokenValid_ShouldReturnAccessToken
  - Test: LoginWithGoogle_WhenTokenInvalid_ShouldThrowException
  - Test: LoginWithGoogle_WhenNewUser_ShouldCreateUser
  - Test: LoginWithGoogle_WhenExistingUser_ShouldUpdateLastLogin
  - **Reference**: [Feature Spec - Unit Tests](./f0001_sign_in_with_google.md#unit-tests) (lines 334-368)

- [ ] Create `Unit/Domain/UserTests.cs`
  - Test: CreateFromGoogle_ShouldSetProperties
  - Test: RecordLogin_ShouldUpdateLastLoginAt

- [ ] Create `Unit/Domain/RefreshTokenTests.cs`
  - Test: Create_ShouldSetExpiryDate
  - Test: Revoke_ShouldMarkAsRevoked
  - Test: IsValid_WhenNotExpired_ShouldReturnTrue
  - Test: IsValid_WhenExpired_ShouldReturnFalse

- [ ] Run unit tests
  ```bash
  cd src/Services/Identity/DatingApp.IdentityService.Tests.Unit
  dotnet test
  ```

### Integration Tests

- [ ] Create `Integration/AuthControllerTests.cs`
  - Test: GoogleLogin_WhenNewUser_ShouldCreateUserAndReturnTokens
  - Test: GoogleLogin_WhenExistingUser_ShouldReturnTokens
  - Test: GoogleLogin_WhenInvalidToken_ShouldReturn400
  - Test: GoogleLogin_WhenRateLimitExceeded_ShouldReturn429
  - **Reference**: [Feature Spec - Integration Tests](./f0001_sign_in_with_google.md#integration-tests) (lines 373-397)

- [ ] Set up Testcontainers for PostgreSQL
  ```csharp
  using Testcontainers.PostgreSql;

  public class IntegrationTestBase : IAsyncLifetime
  {
      private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
          .WithImage("postgres:16-alpine")
          .Build();

      public async Task InitializeAsync()
      {
          await _dbContainer.StartAsync();
      }

      public async Task DisposeAsync()
      {
          await _dbContainer.DisposeAsync();
      }
  }
  ```

- [ ] Run integration tests
  ```bash
  cd src/Services/Identity/DatingApp.IdentityService.Tests.Integration
  dotnet test
  ```

- [ ] Commit tests
  ```bash
  git add src/Services/Identity/*.Tests.*
  git commit -m "test(identity): add unit and integration tests for F0001 (#issue-number)"
  ```

---

## Phase 8: Docker Integration (30 minutes)

**Reference**: [Architecture Guidelines - Docker](../../10_architecture/02_architecture-guidelines.md#docker)

### Create Dockerfile

- [ ] Create `src/Services/Identity/Dockerfile`
  ```dockerfile
  FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
  WORKDIR /app
  EXPOSE 80
  EXPOSE 443

  FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
  WORKDIR /src
  COPY ["DatingApp.IdentityService.Api/DatingApp.IdentityService.Api.csproj", "DatingApp.IdentityService.Api/"]
  COPY ["DatingApp.IdentityService.Application/DatingApp.IdentityService.Application.csproj", "DatingApp.IdentityService.Application/"]
  COPY ["DatingApp.IdentityService.Domain/DatingApp.IdentityService.Domain.csproj", "DatingApp.IdentityService.Domain/"]
  COPY ["DatingApp.IdentityService.Infrastructure/DatingApp.IdentityService.Infrastructure.csproj", "DatingApp.IdentityService.Infrastructure/"]
  RUN dotnet restore "DatingApp.IdentityService.Api/DatingApp.IdentityService.Api.csproj"

  COPY . .
  WORKDIR "/src/DatingApp.IdentityService.Api"
  RUN dotnet build "DatingApp.IdentityService.Api.csproj" -c Release -o /app/build

  FROM build AS publish
  RUN dotnet publish "DatingApp.IdentityService.Api.csproj" -c Release -o /app/publish

  FROM base AS final
  WORKDIR /app
  COPY --from=publish /app/publish .
  ENTRYPOINT ["dotnet", "DatingApp.IdentityService.Api.dll"]
  ```

### Update docker-compose.yml

- [ ] Uncomment IdentityService in `docker-compose.yml`
  ```yaml
  identity-service:
    build:
      context: ./src/Services/Identity
      dockerfile: Dockerfile
    ports:
      - "5001:80"
    environment:
      - ConnectionStrings__IdentityDb=Host=postgres;Database=identity_db;Username=admin;Password=QuietMatch_Dev_2025!
      - Google__ClientId=${GOOGLE_CLIENT_ID}
      - Google__ClientSecret=${GOOGLE_CLIENT_SECRET}
      - Jwt__SecretKey=${JWT_SECRET_KEY}
      - Jwt__Issuer=https://quietmatch.com
      - Jwt__Audience=https://api.quietmatch.com
    depends_on:
      - postgres
      - rabbitmq
  ```

- [ ] Test Docker build
  ```bash
  docker-compose build identity-service
  ```

- [ ] Test Docker run
  ```bash
  docker-compose up identity-service
  ```

- [ ] Verify service health
  ```bash
  curl http://localhost:5001/api/v1/auth/login/google
  ```

- [ ] Commit Docker integration
  ```bash
  git add docker-compose.yml src/Services/Identity/Dockerfile
  git commit -m "feat(identity): add Docker support for F0001 (#issue-number)"
  ```

---

## Phase 9: Manual Testing & Verification (30 minutes)

**Reference**: [Feature Spec - Manual Testing Checklist](./f0001_sign_in_with_google.md#manual-testing-checklist)

### Manual Testing Checklist

- [ ] Sign in with new Google account works
- [ ] Sign in with existing Google account works
- [ ] Invalid ID token returns 400 error
- [ ] Expired ID token returns 400 error
- [ ] Rate limiting blocks after 5 attempts
- [ ] UserRegistered event published for new users (check RabbitMQ UI: http://localhost:15672)
- [ ] Tokens stored correctly (verify in database)
- [ ] Logs appear in Seq (http://localhost:5341)
- [ ] Swagger UI accessible (http://localhost:5001/swagger)

### Postman/cURL Testing

- [ ] Test valid Google login
  ```bash
  curl -X POST http://localhost:5001/api/v1/auth/login/google \
    -H "Content-Type: application/json" \
    -d '{"idToken": "your-valid-google-id-token"}'
  ```

- [ ] Test invalid token
  ```bash
  curl -X POST http://localhost:5001/api/v1/auth/login/google \
    -H "Content-Type: application/json" \
    -d '{"idToken": "invalid-token"}'
  ```

- [ ] Test rate limiting (run 6 times quickly)
  ```bash
  for i in {1..6}; do curl -X POST http://localhost:5001/api/v1/auth/login/google; done
  ```

---

## Completion Checklist

**Reference**: [Feature Workflow - Completion Checklist](../../60_operations/feature-workflow.md#completion-checklist)

### Code Quality
- [ ] Follows Layered architecture pattern
- [ ] Uses ubiquitous language from domain model
- [ ] No hardcoded values (all in configuration)
- [ ] No commented-out code
- [ ] Meaningful variable/method names
- [ ] Comments explain "why", not "what"

### Testing
- [ ] Unit tests written (80%+ coverage for business logic)
- [ ] Integration tests written (database, messaging)
- [ ] API tests written (endpoints)
- [ ] All tests passing
- [ ] Manual testing complete

### Security
- [ ] JWT authentication configured
- [ ] Input validation (FluentValidation)
- [ ] No SQL injection vulnerabilities (EF Core)
- [ ] Refresh token hashed (SHA-256)
- [ ] HTTPS required for production

### Documentation
- [ ] Feature file remains unmodified (immutable input)
- [ ] Plan.md updated with all progress
- [ ] PATTERNS.md created for IdentityService
- [ ] API documentation via Swagger
- [ ] README.md updated (if needed)

### Final Steps
- [ ] All commits reference issue number
- [ ] All tests passing locally
- [ ] Docker build successful
- [ ] Service runs in docker-compose
- [ ] Ready to create PR

---

## Blockers / Questions

*Document any blockers or questions that require human approval here*

---

## Notes & Decisions

*Document implementation decisions and discoveries here*

---

**Completion Status**: 🔴 Not Started
**Total Implementation Time**: TBD
**Lessons Learned**: TBD
