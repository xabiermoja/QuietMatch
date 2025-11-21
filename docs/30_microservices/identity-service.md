# IdentityService - Authentication & Authorization

> **Custom OAuth2/OIDC provider for QuietMatch**
>
> Handles social login, JWT issuance, token refresh, and service-to-service authentication.

---

## Table of Contents

- [Overview](#overview)
- [Architecture Pattern](#architecture-pattern)
- [Responsibilities](#responsibilities)
- [API Endpoints](#api-endpoints)
- [Authentication Flows](#authentication-flows)
- [Database Schema](#database-schema)
- [Events Published](#events-published)
- [Configuration](#configuration)
- [Security Considerations](#security-considerations)
- [Testing Strategy](#testing-strategy)
- [Deployment](#deployment)

---

## Overview

**IdentityService** is the authentication and authorization gateway for QuietMatch. It provides:

- **Social Login**: Google Sign-In, Apple Sign-In (no passwords stored locally)
- **JWT Token Issuance**: Short-lived access tokens (15 min)
- **Refresh Tokens**: Long-lived tokens for seamless re-authentication (7 days)
- **Service-to-Service Auth**: Client credentials flow for M2M communication
- **Token Validation**: Endpoint for other services to validate JWTs

**Why a Custom Implementation?**

Instead of using Duende IdentityServer, we build our own OAuth2 provider because:
1. **Learning**: Understand OAuth2/OIDC flows deeply
2. **Cost**: No commercial licensing fees
3. **Simplicity**: QuietMatch only needs social login (no complex federation)
4. **Control**: Full control over token structure and claims

**Trade-Offs**:
- **Pro**: Educational, cost-effective, tailored to our needs
- **Con**: More implementation work, must ensure security correctness
- **Mitigation**: Use battle-tested libraries (ASP.NET Identity, JWT libraries), follow OAuth2 RFCs

---

## Architecture Pattern

**Pattern**: **Layered Architecture**

**Why Layered for IdentityService?**
- Simple CRUD operations (create user, issue token, refresh token)
- Clear separation of concerns (API → Business Logic → Data Access)
- No need for swappable adapters (Hexagonal) or rich domain logic (Onion)
- Industry-standard pattern for authentication services

**Layers**:
1. **Presentation**: Controllers, DTOs, API models
2. **Application**: Business logic, token generation, OAuth flows
3. **Domain**: User entity, token entities, domain rules
4. **Infrastructure**: Database access, external API calls (Google, Apple)

**See PATTERNS.md** in this service's source folder for detailed explanation of Layered Architecture.

---

## Responsibilities

### Core Responsibilities

1. **User Registration**: Create user account from social login
2. **Authentication**: Validate external OAuth tokens (Google, Apple)
3. **Token Issuance**:
   - Generate JWT access tokens with claims
   - Generate refresh tokens with secure storage
4. **Token Refresh**: Exchange refresh token for new access token
5. **Token Revocation**: Revoke refresh tokens (logout)
6. **Service-to-Service Auth**: Issue short-lived tokens for microservices (M2M)

### Non-Responsibilities

- ❌ Profile management (handled by ProfileService)
- ❌ User preferences (handled by ProfileService)
- ❌ Password management (no passwords in QuietMatch)
- ❌ Email verification (handled by VerificationService)
- ❌ Two-factor authentication (future enhancement)

---

## API Endpoints

### REST API

Base URL: `/api/v1/auth`

#### `POST /api/v1/auth/login/google`
**Description**: Authenticate user with Google Sign-In

**Request**:
```json
{
  "idToken": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

**Response** (200 OK):
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "550e8400-e29b-41d4-a716-446655440000",
  "expiresIn": 900,
  "tokenType": "Bearer",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "isNewUser": true
}
```

**Errors**:
- `400 Bad Request`: Invalid or expired Google ID token
- `500 Internal Server Error`: Token validation failed

---

#### `POST /api/v1/auth/login/apple`
**Description**: Authenticate user with Apple Sign-In

**Request**:
```json
{
  "identityToken": "eyJraWQiOiI4NkQ4OEtmIiwiYWxnIjoiUlMyNTYifQ...",
  "authorizationCode": "c1234567890abcdef"
}
```

**Response**: Same as Google login

---

#### `POST /api/v1/auth/refresh`
**Description**: Refresh access token using refresh token

**Request**:
```json
{
  "refreshToken": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Response** (200 OK):
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 900,
  "tokenType": "Bearer"
}
```

**Errors**:
- `401 Unauthorized`: Invalid or expired refresh token
- `403 Forbidden`: Refresh token revoked

---

#### `POST /api/v1/auth/revoke`
**Description**: Revoke refresh token (logout)

**Request**:
```json
{
  "refreshToken": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Response** (204 No Content)

**Errors**:
- `404 Not Found`: Refresh token not found

---

#### `POST /api/v1/auth/token/m2m`
**Description**: Service-to-service authentication (client credentials flow)

**Request**:
```json
{
  "clientId": "matching-service",
  "clientSecret": "super-secret-key"
}
```

**Response** (200 OK):
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 300,
  "tokenType": "Bearer"
}
```

**Use Case**: MatchingService calls ProfileService internally to fetch member profiles

---

#### `GET /api/v1/auth/validate`
**Description**: Validate JWT token (for other services to verify tokens)

**Headers**:
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response** (200 OK):
```json
{
  "isValid": true,
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "user@example.com",
  "claims": {
    "sub": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "user@example.com",
    "role": "Member"
  }
}
```

**Errors**:
- `401 Unauthorized`: Token invalid or expired

---

## Authentication Flows

### Google Sign-In Flow

```mermaid
sequenceDiagram
    participant Client as Web/Mobile App
    participant Google as Google OAuth
    participant Identity as IdentityService
    participant DB as PostgreSQL

    Client->>Google: 1. Click "Sign in with Google"
    Google->>Google: 2. User authenticates
    Google->>Client: 3. Return ID token
    Client->>Identity: 4. POST /auth/login/google {idToken}
    Identity->>Google: 5. Validate ID token
    Google->>Identity: 6. Return user info (email, sub)
    Identity->>DB: 7. Find user by externalUserId
    alt User exists
        Identity->>DB: 8. Update last login
    else New user
        Identity->>DB: 8. Create new user
        Identity->>Identity: 9. Publish UserRegistered event
    end
    Identity->>Identity: 10. Generate JWT access token
    Identity->>DB: 11. Create refresh token
    Identity->>Client: 12. Return {accessToken, refreshToken}
```

### Token Refresh Flow

```mermaid
sequenceDiagram
    participant Client as Web/Mobile App
    participant Identity as IdentityService
    participant DB as PostgreSQL

    Client->>Identity: 1. POST /auth/refresh {refreshToken}
    Identity->>DB: 2. Query refresh token
    DB->>Identity: 3. Return token (if valid)
    alt Token valid
        Identity->>Identity: 4. Generate new JWT access token
        Identity->>Client: 5. Return {accessToken}
    else Token invalid/expired
        Identity->>Client: 5. 401 Unauthorized
    end
```

### Service-to-Service Flow

```mermaid
sequenceDiagram
    participant Matching as MatchingService
    participant Identity as IdentityService
    participant Profile as ProfileService

    Matching->>Identity: 1. POST /auth/token/m2m {clientId, clientSecret}
    Identity->>Identity: 2. Validate client credentials
    Identity->>Matching: 3. Return {accessToken} (5 min expiry)
    Matching->>Profile: 4. GET /profiles/{id} (with Bearer token)
    Profile->>Identity: 5. Validate JWT (internal call or middleware)
    Identity->>Profile: 6. Token valid
    Profile->>Matching: 7. Return profile data
```

---

## Database Schema

**Database**: `identity_db`

### Table: `users`

```sql
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) NOT NULL,
    provider VARCHAR(50) NOT NULL, -- 'Google', 'Apple'
    external_user_id VARCHAR(255) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    last_login_at TIMESTAMP,
    CONSTRAINT unique_provider_user UNIQUE (provider, external_user_id)
);

CREATE INDEX idx_users_email ON users(email);
```

### Table: `refresh_tokens`

```sql
CREATE TABLE refresh_tokens (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token_hash VARCHAR(255) NOT NULL, -- SHA256 hash of token
    expires_at TIMESTAMP NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    revoked_at TIMESTAMP NULL,
    is_revoked BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT unique_token_hash UNIQUE (token_hash)
);

CREATE INDEX idx_refresh_tokens_user_id ON refresh_tokens(user_id);
CREATE INDEX idx_refresh_tokens_expires_at ON refresh_tokens(expires_at);
```

### Table: `service_clients` (M2M)

```sql
CREATE TABLE service_clients (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id VARCHAR(100) NOT NULL UNIQUE, -- 'matching-service'
    client_secret_hash VARCHAR(255) NOT NULL, -- bcrypt hash
    service_name VARCHAR(100) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Seed data (insert during deployment)
INSERT INTO service_clients (client_id, client_secret_hash, service_name)
VALUES
    ('matching-service', '$2a$...', 'MatchingService'),
    ('profile-service', '$2a$...', 'ProfileService'),
    ('scheduling-service', '$2a$...', 'SchedulingService');
```

---

## Events Published

### `UserRegistered`

**When**: New user signs in for the first time

**Payload**:
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "user@example.com",
  "provider": "Google",
  "registeredAt": "2025-11-20T10:30:00Z"
}
```

**Subscribers**:
- **ProfileService**: Create empty profile for new user
- **NotificationService**: Send welcome email
- **AnalyticsService**: Track new user registration

---

### `RefreshTokenRevoked`

**When**: User logs out or token is manually revoked

**Payload**:
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "revokedAt": "2025-11-20T10:35:00Z"
}
```

**Subscribers**:
- **AnalyticsService**: Track logout events

---

## Configuration

### Environment Variables

```bash
# Database
ConnectionStrings__IdentityDb=Host=postgres;Database=identity_db;Username=admin;Password=***

# JWT Settings
Jwt__Issuer=https://quietmatch.com
Jwt__Audience=https://api.quietmatch.com
Jwt__SecretKey=*** # 256-bit key, stored in Azure Key Vault in production
Jwt__AccessTokenExpiryMinutes=15
Jwt__RefreshTokenExpiryDays=7

# Google OAuth
Google__ClientId=***
Google__ClientSecret=***

# Apple Sign-In
Apple__ClientId=***
Apple__TeamId=***
Apple__KeyId=***
Apple__PrivateKey=*** # P8 file content

# Redis (for distributed caching, optional)
Redis__ConnectionString=redis:6379

# Message Broker
MessageBroker__Host=rabbitmq
MessageBroker__Username=guest
MessageBroker__Password=guest

# Logging
Serilog__SeqUrl=http://seq:5341
ApplicationInsights__ConnectionString=*** # Azure only
```

---

## Security Considerations

### 1. **Refresh Token Security**

**Threat**: Refresh token theft → long-term account access

**Mitigations**:
- Store hashed tokens (SHA-256) in database, never plaintext
- Set expiry (7 days)
- Revoke on logout
- Implement token rotation (issue new refresh token on each refresh)
- Rate limiting on refresh endpoint (10 requests/hour per user)

**Implementation**:
```csharp
public class RefreshTokenService
{
    public async Task<RefreshToken> CreateAsync(UserId userId)
    {
        var tokenValue = GenerateSecureRandomString(64); // 64-byte random
        var tokenHash = SHA256.HashData(Encoding.UTF8.GetBytes(tokenValue));

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId.Value,
            TokenHash = Convert.ToBase64String(tokenHash),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.RefreshTokens.AddAsync(refreshToken);
        await _dbContext.SaveChangesAsync();

        return refreshToken with { Token = tokenValue }; // Return unhashed token to client ONCE
    }
}
```

---

### 2. **JWT Secret Management**

**Threat**: JWT secret leaked → ability to forge tokens

**Mitigations**:
- **Local Dev**: Environment variable (never commit to git)
- **Azure**: Store in Azure Key Vault, fetch on startup
- Rotate secrets periodically (every 90 days)
- Use asymmetric keys (RS256) for better security (future enhancement)

---

### 3. **Client Credentials Security (M2M)**

**Threat**: Service client secret leaked → unauthorized service access

**Mitigations**:
- Store bcrypt-hashed secrets in database
- Use Azure Managed Identity in production (no secrets needed)
- Short token expiry (5 minutes)
- Audit logging of all M2M token issuances

---

### 4. **Google/Apple Token Validation**

**Threat**: Fake Google ID tokens → unauthorized access

**Mitigations**:
- Always validate tokens server-side (never trust client)
- Use Google's official libraries (`Google.Apis.Auth`)
- Verify `aud` (audience) claim matches our client ID
- Verify `iss` (issuer) is Google
- Check token expiry (`exp` claim)

---

## Testing Strategy

### Unit Tests

**Coverage**: Token generation, validation logic, OAuth flow logic

**Example**:
```csharp
public class JwtTokenGeneratorTests
{
    [Fact]
    public void GenerateAccessToken_ShouldIncludeCorrectClaims()
    {
        // Arrange
        var user = new User("test@example.com", "Google", "google-123");
        var generator = new JwtTokenGenerator(_config);

        // Act
        var token = generator.GenerateAccessToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.Equal(user.Id.ToString(), jwtToken.Claims.First(c => c.Type == "sub").Value);
        Assert.Equal(user.Email, jwtToken.Claims.First(c => c.Type == "email").Value);
        Assert.True(jwtToken.ValidTo > DateTime.UtcNow); // Not expired
    }
}
```

---

### Integration Tests

**Coverage**: Database operations, external OAuth validation

**Example**:
```csharp
public class AuthServiceIntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer _dbContainer;
    private WebApplicationFactory<Program> _factory;

    [Fact]
    public async Task LoginWithGoogle_WhenNewUser_ShouldCreateUserAndReturnTokens()
    {
        // Arrange
        var client = _factory.CreateClient();
        var mockGoogleToken = CreateMockGoogleIdToken(); // Test helper

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/login/google", new { idToken = mockGoogleToken });

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(result.AccessToken);
        Assert.NotNull(result.RefreshToken);
        Assert.True(result.IsNewUser);

        // Verify user in database
        var dbContext = _factory.Services.GetRequiredService<IdentityDbContext>();
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
        Assert.NotNull(user);
    }
}
```

---

## Deployment

### Local (Docker Compose)

```yaml
# docker-compose.yml
identity-service:
  build: ./src/Services/Identity
  ports:
    - "5001:80"
  environment:
    - ConnectionStrings__IdentityDb=Host=postgres;Database=identity_db;...
    - Jwt__SecretKey=${JWT_SECRET_KEY}
    - Google__ClientId=${GOOGLE_CLIENT_ID}
  depends_on:
    - postgres
    - rabbitmq
```

**Run**:
```bash
docker-compose up identity-service
```

---

### Azure (Container Apps)

```bicep
resource identityServiceApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'identity-service'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      secrets: [
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
          name: 'identity-service'
          image: 'quietmatch.azurecr.io/identity-service:latest'
          env: [
            {
              name: 'Jwt__SecretKey'
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
- Implement IdentityService following Layered architecture
- Read `src/Services/Identity/PATTERNS.md` for folder structure and pattern details
- Review [Security & Auth](../10_architecture/05_security-and-auth.md) for broader security context

---

**Last Updated**: 2025-11-20
**Document Owner**: IdentityService Team
**Status**: Ready for implementation
