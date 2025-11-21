# Security & Authentication - QuietMatch

> **Comprehensive security strategy for privacy-first dating platform**
>
> This document defines authentication, authorization, encryption, and GDPR security measures for QuietMatch.

---

## Table of Contents

- [Security Principles](#security-principles)
- [Authentication Architecture](#authentication-architecture)
- [Authorization Model](#authorization-model)
- [Data Encryption](#data-encryption)
- [API Security](#api-security)
- [GDPR Security Requirements](#gdpr-security-requirements)
- [Secrets Management](#secrets-management)
- [Security Testing](#security-testing)
- [Threat Model](#threat-model)

---

## Security Principles

### 1. Zero Trust Architecture
- **Never trust, always verify**: Every request authenticated, even internal
- **Least privilege**: Services access only what they need
- **Defense in depth**: Multiple security layers

### 2. Privacy by Design
- **Data minimization**: Collect only necessary data
- **Field-level encryption**: PII encrypted at rest
- **Pseudonymization**: Separate identity from behavioral data where possible

### 3. Secure by Default
- **TLS everywhere**: All communication encrypted
- **No plaintext secrets**: All secrets in Key Vault (Azure) or environment variables (local)
- **Strong defaults**: Secure configuration out-of-the-box

---

## Authentication Architecture

### Custom IdentityService (Not Duende IdentityServer)

**Decision**: Build custom OAuth2/OIDC provider

**Why**:
- Learning: Understand OAuth2 flows deeply
- Cost: No commercial licensing fees
- Simplicity: QuietMatch only needs social login
- Control: Full control over token structure

**Trade-Offs**:
- ✅ Pro: Educational, cost-effective, tailored
- ❌ Con: More implementation work, security responsibility
- ✅ Mitigation: Use battle-tested libraries, follow RFCs, security audits

---

### Social Login Only (No Passwords)

**Supported Providers**:
1. **Google Sign-In** (MVP)
2. **Apple Sign-In** (MVP for iOS users)
3. **Microsoft** (future)

**Why No Passwords?**:
- ✅ Security: No password database to breach
- ✅ UX: Faster onboarding
- ✅ GDPR: Less PII stored
- ❌ Con: Dependency on OAuth providers (mitigated by supporting multiple)

---

### Authentication Flow (Google Sign-In)

```mermaid
sequenceDiagram
    participant User as User (Browser/App)
    participant Frontend as QuietMatch Frontend
    participant Google as Google OAuth
    participant Identity as IdentityService
    participant DB as PostgreSQL

    User->>Frontend: 1. Click "Sign in with Google"
    Frontend->>Google: 2. Redirect to Google consent screen
    Google->>Google: 3. User authenticates with Google
    Google->>Frontend: 4. Redirect back with authorization code
    Frontend->>Google: 5. Exchange code for ID token
    Google->>Frontend: 6. Return ID token (JWT)
    Frontend->>Identity: 7. POST /api/v1/auth/login/google {idToken}
    Identity->>Google: 8. Validate ID token (call Google API)
    Google->>Identity: 9. Return user info (email, sub, name)
    Identity->>DB: 10. Find user by externalUserId (Google sub)
    alt User exists
        Identity->>DB: 11. Update lastLoginAt
    else New user
        Identity->>DB: 11. INSERT INTO users (email, provider, external_user_id)
        Identity->>Identity: 12. Publish UserRegistered event
    end
    Identity->>Identity: 13. Generate JWT access token (15 min)
    Identity->>DB: 14. INSERT INTO refresh_tokens (hashed)
    Identity->>Frontend: 15. Return {accessToken, refreshToken, userId}
    Frontend->>Frontend: 16. Store tokens (localStorage/secure storage)
```

**Security Notes**:
- ID token validated server-side (never trust client)
- Verify `aud` (audience) matches our Google Client ID
- Verify `iss` (issuer) is `https://accounts.google.com`
- Check `exp` (expiry) to prevent replay attacks

---

### Token Types

#### 1. Access Token (JWT)

**Purpose**: Authenticate API requests

**Format**: JSON Web Token (JWT)

**Lifetime**: 15 minutes (short-lived)

**Structure**:
```json
{
  "header": {
    "alg": "HS256",
    "typ": "JWT"
  },
  "payload": {
    "sub": "3fa85f64-5717-4562-b3fc-2c963f66afa6",  // UserId
    "email": "user@example.com",
    "role": "Member",
    "iss": "https://quietmatch.com",
    "aud": "https://api.quietmatch.com",
    "exp": 1732187400,  // Unix timestamp
    "iat": 1732186500,
    "jti": "unique-token-id"  // Prevents replay
  },
  "signature": "HMACSHA256(base64(header) + '.' + base64(payload), secretKey)"
}
```

**Claims**:
- `sub`: User ID (subject)
- `email`: User email
- `role`: User role (Member, Admin)
- `iss`: Issuer (QuietMatch)
- `aud`: Audience (QuietMatch API)
- `exp`: Expiry timestamp
- `iat`: Issued at timestamp
- `jti`: JWT ID (unique token identifier)

**Usage**:
```http
GET /api/v1/profiles/me
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Why Short-Lived (15 min)?**
- Limits impact of token theft
- Forces periodic refresh (validates user still active)
- Balances security vs. UX (not too frequent)

---

#### 2. Refresh Token

**Purpose**: Obtain new access token without re-authentication

**Format**: Opaque token (UUID v4), NOT a JWT

**Lifetime**: 7 days

**Storage**:
- **Database**: Hashed (SHA-256) in `refresh_tokens` table
- **Client**: Secure storage (HttpOnly cookie or secure mobile storage)

**Security Measures**:
- ✅ Hashed before storage (SHA-256)
- ✅ One-time use (revoked after refresh)
- ✅ Tied to user + device (future: device fingerprint)
- ✅ Revoked on logout
- ✅ Revoked on password change (N/A for social login, but on account deletion)

**Token Rotation**:
```
1. Client sends refresh token
2. Server validates and revokes old token
3. Server issues new access token + new refresh token
4. Old refresh token unusable (mitigates theft)
```

**Refresh Flow**:
```mermaid
sequenceDiagram
    participant Client
    participant Identity as IdentityService
    participant DB

    Client->>Identity: POST /api/v1/auth/refresh {refreshToken}
    Identity->>DB: SELECT * FROM refresh_tokens WHERE token_hash = SHA256(refreshToken)
    alt Token valid
        Identity->>DB: UPDATE refresh_tokens SET is_revoked = true (revoke old)
        Identity->>Identity: Generate new access token
        Identity->>DB: INSERT new refresh token
        Identity->>Client: {accessToken, refreshToken (new)}
    else Token invalid/expired
        Identity->>Client: 401 Unauthorized
        Note over Client: Redirect to login
    end
```

---

#### 3. Service-to-Service Tokens (M2M)

**Purpose**: Microservice authentication (e.g., MatchingService calls ProfileService)

**Flow**: OAuth2 Client Credentials

**Lifetime**: 5 minutes (very short-lived)

**How It Works**:
```mermaid
sequenceDiagram
    participant Matching as MatchingService
    participant Identity as IdentityService
    participant Profile as ProfileService

    Matching->>Identity: POST /api/v1/auth/token/m2m<br/>{clientId: "matching-service", clientSecret: "***"}
    Identity->>Identity: Validate client credentials (bcrypt)
    Identity->>Matching: {accessToken (JWT), expiresIn: 300}
    Matching->>Profile: GET /api/v1/profiles/{id}<br/>Authorization: Bearer {accessToken}
    Profile->>Profile: Validate JWT
    Profile->>Matching: {profile data}
```

**Client Credentials Storage**:
```sql
-- service_clients table
CREATE TABLE service_clients (
    client_id VARCHAR(100) PRIMARY KEY,
    client_secret_hash VARCHAR(255),  -- bcrypt hash
    service_name VARCHAR(100),
    is_active BOOLEAN DEFAULT true
);

-- Seed data
INSERT INTO service_clients VALUES
    ('matching-service', '$2a$12$hashed...', 'MatchingService', true),
    ('profile-service', '$2a$12$hashed...', 'ProfileService', true);
```

**Why Short-Lived (5 min)?**
- Services should not cache tokens long-term
- Forces re-authentication frequently
- Limits impact if token leaked

**Future: Azure Managed Identity**
In Azure, replace client credentials with Managed Identity (no secrets needed).

---

## Authorization Model

### Role-Based Access Control (RBAC)

**Roles**:
1. **Member**: Regular user (can create profile, accept matches)
2. **PremiumMember**: Paid subscriber (extra features)
3. **Admin**: Platform administrator (support, moderation)

**Implementation**:
```csharp
[Authorize(Roles = "Member")]
[HttpGet("profiles/me")]
public async Task<IActionResult> GetMyProfile() { ... }

[Authorize(Roles = "Admin")]
[HttpDelete("users/{id}")]
public async Task<IActionResult> DeleteUser(Guid id) { ... }
```

### Resource-Based Authorization

For fine-grained permissions (e.g., "Can user edit this profile?"):

```csharp
// Check if user owns the resource
public class ProfileAuthorizationHandler : AuthorizationHandler<OperationAuthorizationRequirement, MemberProfile>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        MemberProfile profile)
    {
        var userId = context.User.FindFirst("sub")?.Value;

        if (requirement.Name == "Edit" && profile.MemberId.ToString() == userId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

// Usage in controller
[HttpPut("profiles/{id}")]
public async Task<IActionResult> UpdateProfile(Guid id, UpdateProfileRequest request)
{
    var profile = await _profileService.GetByIdAsync(id);

    var authResult = await _authorizationService.AuthorizeAsync(User, profile, "Edit");
    if (!authResult.Succeeded)
        return Forbid();

    // Update profile...
}
```

---

## Data Encryption

### 1. Encryption at Rest (Field-Level)

**What to Encrypt**:
- Full name
- Email (debatable, needed for login)
- Date of birth
- Location (city-level)
- Free-text personality responses

**Algorithm**: AES-256-GCM

**Implementation** (EF Core Value Converter):
```csharp
public class EncryptedStringConverter : ValueConverter<string, string>
{
    public EncryptedStringConverter(IEncryptionService encryptionService)
        : base(
            plainText => encryptionService.Encrypt(plainText),
            cipherText => encryptionService.Decrypt(cipherText))
    {
    }
}

// Usage in EF configuration
public class MemberProfileConfiguration : IEntityTypeConfiguration<MemberProfile>
{
    public void Configure(EntityTypeBuilder<MemberProfile> builder)
    {
        builder.Property(p => p.FullName)
            .HasConversion(new EncryptedStringConverter(_encryptionService))
            .HasMaxLength(500); // Encrypted data larger than plaintext
    }
}
```

**Encryption Service**:
```csharp
public class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key; // 256-bit key from config

    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Prepend IV to ciphertext (IV not secret, but unique per encryption)
        var result = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string cipherText)
    {
        var cipherBytes = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = _key;

        // Extract IV from ciphertext
        var iv = new byte[aes.IV.Length];
        Buffer.BlockCopy(cipherBytes, 0, iv, 0, iv.Length);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, iv.Length, cipherBytes.Length - iv.Length);

        return Encoding.UTF8.GetString(plainBytes);
    }
}
```

**Key Management**:
- **Local**: Environment variable (32-byte base64)
- **Azure**: Azure Key Vault
- **Key Rotation**: Manual process (future: automate with Key Vault versioning)

---

### 2. Encryption in Transit (TLS)

**Requirements**:
- **TLS 1.3** (minimum TLS 1.2)
- **HTTPS everywhere** (no HTTP in production)
- **Certificate**: Let's Encrypt (free) or Azure-managed

**Configuration** (ASP.NET Core):
```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureHttpsDefaults(httpsOptions =>
    {
        httpsOptions.SslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12;
    });
});
```

---

## API Security

### 1. Rate Limiting

**Purpose**: Prevent brute-force, DoS attacks

**Implementation** (ASP.NET Core Rate Limiting):
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            });
    });

    // More restrictive for auth endpoints
    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1)
            }));
});

// Usage
[EnableRateLimiting("auth")]
[HttpPost("login/google")]
public async Task<IActionResult> GoogleLogin(...) { ... }
```

---

### 2. CORS (Cross-Origin Resource Sharing)

**Purpose**: Control which domains can call QuietMatch APIs

**Configuration**:
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("QuietMatchWeb", policy =>
    {
        policy.WithOrigins("https://quietmatch.com", "https://www.quietmatch.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // For cookies
    });

    // Development
    if (builder.Environment.IsDevelopment())
    {
        options.AddPolicy("LocalDev", policy =>
        {
            policy.WithOrigins("http://localhost:3000", "http://localhost:5173") // React dev servers
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
    }
});

app.UseCors(builder.Environment.IsDevelopment() ? "LocalDev" : "QuietMatchWeb");
```

---

### 3. Input Validation

**Always validate**:
- DTOs with FluentValidation
- Command/Query parameters
- Database constraints

**Example**:
```csharp
public class GoogleLoginRequestValidator : AbstractValidator<GoogleLoginRequest>
{
    public GoogleLoginRequestValidator()
    {
        RuleFor(x => x.IdToken)
            .NotEmpty().WithMessage("ID token is required")
            .MinimumLength(100).WithMessage("Invalid token format");
    }
}
```

---

## GDPR Security Requirements

### 1. Data Access Logging

**Requirement**: Log all access to PII (GDPR Article 30)

**Implementation**:
```csharp
public class AuditLoggingMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var userId = context.User.FindFirst("sub")?.Value;
        var endpoint = context.Request.Path;
        var method = context.Request.Method;

        // Log access to PII endpoints
        if (endpoint.Contains("/profiles/") || endpoint.Contains("/personal-data"))
        {
            _logger.LogInformation("User {UserId} accessed {Method} {Endpoint} at {Timestamp}",
                userId, method, endpoint, DateTime.UtcNow);

            // Store in audit log table
            await _auditLogRepo.AddAsync(new AuditLog
            {
                UserId = userId,
                Action = $"{method} {endpoint}",
                Timestamp = DateTime.UtcNow,
                IpAddress = context.Connection.RemoteIpAddress?.ToString()
            });
        }

        await _next(context);
    }
}
```

---

### 2. Secure Data Deletion

**Requirement**: Securely delete data (GDPR Article 17)

**Process**:
1. **Soft Delete**: Mark as deleted, keep encrypted for 30 days
2. **Anonymization**: After 30 days, anonymize (keep aggregate data for analytics)
3. **Hard Delete**: After retention, physically delete

```csharp
public async Task DeleteUserDataAsync(MemberId memberId)
{
    // Step 1: Soft delete (immediate)
    await _profileRepo.SoftDeleteAsync(memberId);

    // Step 2: Schedule hard delete (after 30 days)
    await _backgroundJobScheduler.ScheduleAsync(
        new HardDeleteProfileJob(memberId),
        delay: TimeSpan.FromDays(30));
}

// Background job (runs after 30 days)
public class HardDeleteProfileJob
{
    public async Task ExecuteAsync(MemberId memberId)
    {
        // Anonymize: Replace PII with placeholders
        await _profileRepo.AnonymizeAsync(memberId);

        // Hard delete: Remove from database
        await _profileRepo.HardDeleteAsync(memberId);

        _logger.LogInformation("User {MemberId} data permanently deleted", memberId);
    }
}
```

---

## Secrets Management

### Local Development

**Method**: Environment variables via `.env` file

**Example**:
```bash
# .env (never commit to git!)
GOOGLE_CLIENT_ID=your-client-id
GOOGLE_CLIENT_SECRET=your-client-secret
JWT_SECRET_KEY=your-256-bit-key-base64-encoded
ENCRYPTION_KEY=your-256-bit-encryption-key-base64-encoded
```

**Load in Program.cs**:
```csharp
builder.Configuration.AddEnvironmentVariables();
```

---

### Azure Production

**Method**: Azure Key Vault

**Setup**:
```csharp
if (!builder.Environment.IsDevelopment())
{
    var keyVaultUrl = builder.Configuration["KeyVault:Url"];
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUrl),
        new DefaultAzureCredential()); // Uses Managed Identity
}
```

**Key Vault Secrets**:
- `Jwt--SecretKey`
- `EncryptionKey`
- `Google--ClientSecret`
- `Stripe--SecretKey`

**Access**:
```csharp
var jwtSecret = builder.Configuration["Jwt:SecretKey"]; // Fetches from Key Vault
```

---

## Security Testing

### 1. Unit Tests
- Token generation/validation
- Encryption/decryption
- Authorization logic

### 2. Integration Tests
- End-to-end auth flows
- Token refresh
- RBAC enforcement

### 3. Security Tests
- **OWASP Top 10** automated scans (OWASP ZAP)
- **Penetration testing** (annual)
- **Dependency scanning** (Dependabot, Snyk)

---

## Threat Model

| Threat | Mitigation |
|--------|-----------|
| **Token Theft** | Short-lived access tokens (15 min), refresh token rotation |
| **MITM Attack** | TLS 1.3, HSTS headers |
| **XSS** | HttpOnly cookies (refresh token), CSP headers |
| **CSRF** | SameSite cookies, anti-CSRF tokens |
| **SQL Injection** | EF Core parameterized queries |
| **Brute Force** | Rate limiting, account lockout |
| **Replay Attack** | `jti` claim (JWT ID), expiry checks |

---

**Last Updated**: 2025-11-20
**Document Owner**: Security Team
