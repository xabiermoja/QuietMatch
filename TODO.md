# QuietMatch - Project TODO List

> **Purpose**: This file tracks technical debt, future enhancements, and follow-up work that emerged from feature implementations but are not currently in scope.
>
> **Format**: Each TODO includes priority, source feature, and detailed description.

---

## How to Use This TODO List

### Adding New TODOs

When implementing a feature and you discover work that should be done later:

1. Add an entry to the appropriate priority section below
2. Include the **source feature ID** (e.g., F0001)
3. Write a **clear, actionable title**
4. Provide **detailed description** of:
   - What needs to be done
   - Why it's important
   - What the current limitation is
   - Acceptance criteria for completion
   - Related documentation or references

### Priority Levels

- **P0 - Critical**: Security vulnerabilities, data loss risks, production blockers
- **P1 - High**: Important for production readiness, significant user impact
- **P2 - Medium**: Quality improvements, nice-to-have features, technical debt
- **P3 - Low**: Future enhancements, optimizations, exploratory work

### TODO States

- 🔴 **Blocked**: Cannot proceed due to external dependency
- 🟡 **Ready**: Can be implemented when capacity available
- 🟢 **In Progress**: Currently being worked on
- ✅ **Done**: Completed (move to archive section at bottom)

---

## P0 - Critical

> Production blockers, security vulnerabilities, data loss risks

*No critical TODOs at this time*

---

## P1 - High Priority

> Important for production readiness, significant impact

### 🟡 T0001: Production Security Hardening for IdentityService

**Source**: [F0001 - Sign In with Google](docs/40_features/f0001_sign_in_with_google/f0001_sign_in_with_google.md)
**Status**: 🟡 Ready
**Effort**: 8-12 hours
**Created**: 2025-11-24

#### Description

The IdentityService is currently configured for development and local testing. Before deploying to production, several security hardening measures must be implemented.

#### Current Limitations

- ✅ JWT tokens are signed with HMAC-SHA256 (secure)
- ✅ Refresh tokens are hashed with SHA-256 (secure)
- ✅ No secrets in source code (secure)
- ❌ HTTPS not enforced (development only)
- ❌ No rate limiting beyond basic IP throttling
- ❌ Secrets stored in appsettings.json (not Azure Key Vault)
- ❌ No geo-based blocking or suspicious login detection
- ❌ No CORS policy configured for production domains

#### What Needs to Be Done

**1. HTTPS Enforcement**
- Configure HTTPS redirection middleware
- Set up HSTS (HTTP Strict Transport Security) headers
- Configure proper SSL/TLS certificates in Azure Container Apps
- Test certificate renewal process

**2. Advanced Rate Limiting**
- Implement sliding window rate limiting (not just fixed window)
- Add per-user rate limiting (in addition to per-IP)
- Configure different limits for authenticated vs. unauthenticated users
- Add rate limit headers to responses (X-RateLimit-Limit, X-RateLimit-Remaining)
- **Recommended**: 5 login attempts per IP per 5 minutes, 10 per user per hour

**3. Azure Key Vault Integration**
- Migrate all secrets from appsettings.json to Azure Key Vault
  - `Jwt:SecretKey`
  - `Google:ClientId`
  - `Google:ClientSecret`
  - `ConnectionStrings:IdentityDb`
  - `MessageBroker:RabbitMQ:Password` (or Azure Service Bus connection string)
- Configure Managed Identity for IdentityService in Azure Container Apps
- Update DI configuration to read from Key Vault
- Document Key Vault setup in deployment guide

**4. CORS Configuration**
- Define allowed origins for production (web app domain, mobile app domains)
- Restrict CORS headers to minimum required
- Configure credentials policy (allow or disallow)
- Add CORS policy to GraphQL Gateway as well

**5. Security Headers**
- Add security headers middleware:
  - `X-Content-Type-Options: nosniff`
  - `X-Frame-Options: DENY`
  - `X-XSS-Protection: 1; mode=block`
  - `Content-Security-Policy` (appropriate policy)
  - `Referrer-Policy: strict-origin-when-cross-origin`

**6. Logging & Monitoring**
- Configure Application Insights for security events
- Set up alerts for:
  - High rate of 401 Unauthorized responses
  - High rate of 429 Rate Limit Exceeded
  - Unusual geographic login patterns (if implemented)
- Log failed login attempts with IP address and user agent

#### Acceptance Criteria

- [ ] HTTPS enforced on all endpoints (HTTP redirects to HTTPS)
- [ ] HSTS header present with max-age >= 1 year
- [ ] All secrets stored in Azure Key Vault (none in config files)
- [ ] Managed Identity authentication to Key Vault working
- [ ] Advanced rate limiting active (sliding window, per-user + per-IP)
- [ ] CORS policy configured for production domains only
- [ ] All security headers present in responses
- [ ] Application Insights capturing security events
- [ ] Alerts configured for suspicious activity
- [ ] Deployment guide updated with Key Vault setup
- [ ] Manual security testing completed (pen test checklist)

#### References

- Feature Spec: `docs/40_features/f0001_sign_in_with_google/f0001_sign_in_with_google.md`
- Implementation: `src/Services/Identity/`
- Azure Key Vault Docs: https://learn.microsoft.com/en-us/azure/key-vault/
- ASP.NET Core Security: https://learn.microsoft.com/en-us/aspnet/core/security/

---

### 🟢 T0002: Implement Refresh Token Endpoint ✅

**Source**: [F0001 - Sign In with Google](docs/40_features/f0001_sign_in_with_google/f0001_sign_in_with_google.md)
**Status**: 🟢 In Progress (PR #4 - Ready to Merge)
**Effort**: 4-6 hours
**Created**: 2025-11-24
**Completed**: 2025-11-25
**Pull Request**: [#4](https://github.com/xabiermoja/QuietMatch/pull/4)

#### Description

The IdentityService generates and stores refresh tokens, but there is currently no endpoint to use them. Clients cannot refresh expired access tokens without requiring the user to re-authenticate with Google.

#### Current Limitations

- ✅ Refresh tokens generated and stored securely (SHA-256 hashed)
- ✅ Refresh tokens have 7-day expiry
- ✅ `RefreshToken` entity includes `IsRevoked`, `RevokedAt` fields
- ❌ No `POST /api/v1/auth/refresh` endpoint
- ❌ Clients must re-authenticate when access token expires (poor UX)

#### What Needs to Be Done

**1. Create RefreshTokenRequest DTO**
```csharp
// Application Layer
public record RefreshTokenRequest(string RefreshToken);
```

**2. Create RefreshTokenResponse DTO**
```csharp
// Application Layer
public record RefreshTokenResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType
);
```

**3. Add RefreshTokenRequestValidator**
```csharp
// Application Layer - FluentValidation
public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("Refresh token is required");
    }
}
```

**4. Implement RefreshTokenAsync in AuthService**
```csharp
// Application Layer
public async Task<RefreshTokenResponse?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
{
    // 1. Hash the incoming refresh token
    // 2. Look up in database by token hash
    // 3. Validate: not expired, not revoked
    // 4. Get associated user
    // 5. Generate new access token
    // 6. Optionally: Rotate refresh token (generate new one, revoke old one)
    // 7. Return response
}
```

**5. Add POST /api/v1/auth/refresh Endpoint**
```csharp
// API Layer - AuthController
[HttpPost("refresh")]
[AllowAnonymous]
public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
{
    var response = await _authService.RefreshTokenAsync(request.RefreshToken);

    if (response == null)
        return Unauthorized(new ProblemDetails { Title = "Invalid or expired refresh token" });

    return Ok(response);
}
```

**6. Write Unit Tests**
- Test valid refresh token returns new access token
- Test expired refresh token returns 401
- Test revoked refresh token returns 401
- Test invalid (non-existent) refresh token returns 401
- Test token rotation (if implemented)

**7. Update API Documentation**
- Add endpoint to `src/Services/Identity/API.md`
- Add request/response examples
- Document error responses (400, 401, 500)

**8. Update Manual Testing Guide**
- Add refresh token flow to `src/Services/Identity/TESTING.md`
- Include curl examples and expected responses

#### Acceptance Criteria

- [ ] `POST /api/v1/auth/refresh` endpoint implemented
- [ ] Valid refresh token returns new access token (200)
- [ ] Expired refresh token returns 401 Unauthorized
- [ ] Revoked refresh token returns 401 Unauthorized
- [ ] Invalid refresh token returns 401 Unauthorized
- [ ] RFC 7807 Problem Details format for errors
- [ ] Unit tests passing (minimum 4 tests)
- [ ] API documentation updated
- [ ] Manual testing guide updated
- [ ] Endpoint secured with rate limiting
- [ ] Decision made: Token rotation enabled or disabled (document in code comments)

#### References

- Feature Spec: `docs/40_features/f0001_sign_in_with_google/f0001_sign_in_with_google.md`
- Implementation: `src/Services/Identity/`
- OAuth 2.0 Refresh Token Flow: https://www.rfc-editor.org/rfc/rfc6749#section-6

---

### 🟡 T0003: Implement Token Revocation Endpoint

**Source**: [F0001 - Sign In with Google](docs/40_features/f0001_sign_in_with_google/f0001_sign_in_with_google.md)
**Status**: 🟡 Ready
**Effort**: 2-4 hours
**Created**: 2025-11-24

#### Description

Users need the ability to explicitly revoke their refresh tokens (e.g., when logging out from a device or revoking access). This is critical for security best practices and GDPR compliance (user control over their data/sessions).

#### Current Limitations

- ✅ `RefreshToken` entity has `Revoke()` method
- ✅ `IsRevoked` and `RevokedAt` fields present
- ❌ No endpoint to trigger revocation
- ❌ Users cannot log out properly (tokens remain valid until expiry)
- ❌ No way to revoke all sessions for a user

#### What Needs to Be Done

**1. Create RevokeTokenRequest DTO**
```csharp
// Application Layer
public record RevokeTokenRequest(string RefreshToken);
```

**2. Implement RevokeTokenAsync in AuthService**
```csharp
// Application Layer
public async Task<bool> RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
{
    // 1. Hash the refresh token
    // 2. Find token in database
    // 3. If not found, return false (idempotent)
    // 4. If already revoked, return true (idempotent)
    // 5. Call token.Revoke()
    // 6. Update in repository
    // 7. Return true
}
```

**3. Implement RevokeAllTokensForUserAsync in AuthService**
```csharp
// Application Layer
public async Task<int> RevokeAllTokensForUserAsync(Guid userId, CancellationToken cancellationToken = default)
{
    // 1. Get all active (non-revoked, non-expired) tokens for user
    // 2. Revoke each one
    // 3. Update in repository
    // 4. Return count of revoked tokens
}
```

**4. Add POST /api/v1/auth/revoke Endpoint**
```csharp
// API Layer - AuthController
[HttpPost("revoke")]
[AllowAnonymous]
public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequest request)
{
    await _authService.RevokeTokenAsync(request.RefreshToken);
    return NoContent(); // 204 No Content (idempotent)
}
```

**5. Add POST /api/v1/auth/logout Endpoint**
```csharp
// API Layer - AuthController
[HttpPost("logout")]
[Authorize] // Requires valid access token
public async Task<IActionResult> Logout()
{
    var userId = User.GetUserId(); // Extension method to extract from JWT claims
    var revokedCount = await _authService.RevokeAllTokensForUserAsync(userId);
    return Ok(new { message = "Logged out successfully", tokensRevoked = revokedCount });
}
```

**6. Write Unit Tests**
- Test revoking valid token succeeds
- Test revoking non-existent token is idempotent (no error)
- Test revoking already-revoked token is idempotent
- Test logout revokes all user tokens

**7. Update API Documentation**
- Document both endpoints in `API.md`
- Include request/response examples
- Document 204 vs 200 response codes

**8. Security Considerations**
- Revoke endpoint should be idempotent (safe to call multiple times)
- Logout endpoint requires authentication (valid access token)
- Consider adding optional "revoke_all_devices" parameter to logout

#### Acceptance Criteria

- [ ] `POST /api/v1/auth/revoke` endpoint implemented (204 No Content)
- [ ] `POST /api/v1/auth/logout` endpoint implemented (200 OK) with JWT required
- [ ] Revoke endpoint is idempotent (calling multiple times is safe)
- [ ] Logout revokes all tokens for authenticated user
- [ ] Unit tests passing (minimum 4 tests)
- [ ] API documentation updated
- [ ] Manual testing guide updated with logout flow

#### References

- Feature Spec: `docs/40_features/f0001_sign_in_with_google/f0001_sign_in_with_google.md`
- Implementation: `src/Services/Identity/`
- RFC 7009 - OAuth Token Revocation: https://www.rfc-editor.org/rfc/rfc7009.html

---

## P2 - Medium Priority

> Quality improvements, technical debt, nice-to-have features

### 🟡 T0004: Add Health Check Endpoints to IdentityService

**Source**: [F0001 - Sign In with Google](docs/40_features/f0001_sign_in_with_google/f0001_sign_in_with_google.md)
**Status**: 🟡 Ready
**Effort**: 2-3 hours
**Created**: 2025-11-24

#### Description

Production-grade microservices should expose health check endpoints for monitoring and orchestration (Kubernetes readiness/liveness probes, Azure Container Apps health probes).

#### Current Limitations

- ❌ No health check endpoints
- ❌ Cannot monitor service health programmatically
- ❌ Azure Container Apps cannot detect unhealthy instances

#### What Needs to Be Done

**1. Install NuGet Packages**
```bash
dotnet add package Microsoft.Extensions.Diagnostics.HealthChecks
dotnet add package AspNetCore.HealthChecks.NpgSql
dotnet add package AspNetCore.HealthChecks.Rabbitmq
```

**2. Configure Health Checks in Program.cs**
```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres")
    .AddRabbitMQ(rabbitMqConnection, name: "rabbitmq");
```

**3. Add Health Check Endpoints**
```csharp
app.MapHealthChecks("/health"); // Simple 200 OK
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => true, // All checks
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse // Detailed JSON
});
```

**4. Update Azure Container Apps Configuration**
- Add health probe configuration to Bicep/ARM template or Portal
- Configure readiness probe: `GET /health/ready`
- Configure liveness probe: `GET /health/live`

**5. Document Endpoints**
- Add to `API.md`
- Include sample responses (healthy, unhealthy, degraded)

#### Acceptance Criteria

- [ ] `/health` endpoint returns 200 OK (simple check)
- [ ] `/health/ready` returns 200 if database + message broker available
- [ ] `/health/live` returns 200 if service is running (detailed JSON response)
- [ ] Unhealthy dependencies cause 503 Service Unavailable response
- [ ] Azure Container Apps configured with health probes
- [ ] API documentation updated

#### References

- ASP.NET Core Health Checks: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks
- Azure Container Apps Health Probes: https://learn.microsoft.com/en-us/azure/container-apps/health-probes

---

### 🟡 T0005: Fix Integration Test Infrastructure for IdentityService

**Source**: [T0002 - Implement Refresh Token Endpoint](TODO.md)
**Status**: 🟡 Ready
**Effort**: 2-4 hours
**Created**: 2025-11-25

#### Description

The IdentityService integration tests are failing due to a `ServiceProvider` disposal issue during `WebApplicationFactory` initialization. This prevents integration tests from running and verifying end-to-end API functionality.

#### Current Limitations

- ✅ Unit tests working perfectly (61/61 passing)
- ✅ Application builds and runs successfully
- ✅ API endpoints work correctly in manual testing
- ❌ Integration tests fail during test setup (not during actual tests)
- ❌ Cannot verify end-to-end flows automatically
- ❌ CI/CD pipeline will fail on integration test step

#### Error Details

```
System.ObjectDisposedException: Cannot access a disposed object.
Object name: 'IServiceProvider'.
  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory`1.ConfigureHostBuilder(IHostBuilder hostBuilder)
  at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory`1.EnsureServer()
```

**Affected Tests**:
- All 7 integration tests in `AuthControllerIntegrationTests.cs`
- Error occurs during `InitializeAsync()` when creating HTTP client
- Tests fail before actual API calls are made

#### Root Cause Analysis

The issue appears to be related to:
1. **Testcontainers lifecycle**: PostgreSQL and RabbitMQ containers may be disposing the ServiceProvider prematurely
2. **WebApplicationFactory configuration**: Custom service registration in test setup may conflict with application's DI container
3. **Async initialization**: Race condition between container startup and service provider initialization

#### What Needs to Be Done

**1. Review WebApplicationFactory Configuration**
- Examine `AuthControllerIntegrationTests.cs:95` (where error occurs)
- Check `CustomWebApplicationFactory` or test base class configuration
- Verify service registration order and lifecycle

**2. Fix Testcontainers Integration**
- Ensure PostgreSQL container is fully started before WebApplicationFactory initialization
- Verify RabbitMQ container startup doesn't interfere with DI container
- Consider using `IAsyncLifetime` pattern for proper async initialization

**3. Update Service Registration in Tests**
- Review how mock services are registered in test configuration
- Ensure test services don't dispose the main ServiceProvider
- Use `ConfigureTestServices()` instead of `ConfigureServices()` if applicable

**4. Add Diagnostics**
- Add logging to test initialization to identify exact disposal point
- Verify container health before running tests
- Add retry logic for container startup if needed

**5. Verify Fix**
- All 7 integration tests should pass
- Tests should run reliably (no flakiness)
- CI/CD pipeline should pass integration test step

#### Acceptance Criteria

- [ ] All integration tests pass (7/7)
- [ ] Tests run reliably without ServiceProvider disposal errors
- [ ] WebApplicationFactory initializes successfully
- [ ] Testcontainers (PostgreSQL, RabbitMQ) start and connect properly
- [ ] CI/CD pipeline integration test step passes
- [ ] Documentation updated with any test setup changes

#### References

- Error Location: `src/Services/Identity/DatingApp.IdentityService.Tests.Integration/AuthControllerIntegrationTests.cs:95`
- Related: WebApplicationFactory docs - https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests
- Related: Testcontainers .NET - https://dotnet.testcontainers.org/
- Related PR: #4 (T0002 implementation found this issue)

---

## P3 - Low Priority

> Future enhancements, optimizations, exploratory work

### 🟡 T0006: Implement Sign In with Apple

**Source**: [F0001 - Sign In with Google](docs/40_features/f0001_sign_in_with_google/f0001_sign_in_with_google.md)
**Status**: 🟡 Ready
**Effort**: 6-8 hours
**Created**: 2025-11-24

#### Description

The domain model already supports Apple as an `AuthProvider`, but the implementation is not complete. Adding Sign In with Apple is important for iOS users and App Store requirements.

#### Current Limitations

- ✅ Domain model supports `AuthProvider.Apple`
- ✅ `User.CreateFromApple()` factory method exists
- ❌ No Apple OAuth validation service
- ❌ No Apple-specific controller endpoint
- ❌ Apple requires different configuration than Google (team ID, key ID, private key)

#### What Needs to Be Done

**1. Apple Developer Account Setup**
- Create App ID for QuietMatch
- Enable "Sign in with Apple" capability
- Create Service ID (OAuth client ID)
- Generate private key for JWT signing
- Download key and note Key ID, Team ID

**2. Create AppleAuthService**
```csharp
// Infrastructure Layer
public interface IAppleAuthService
{
    Task<AppleUserInfo?> ValidateIdTokenAsync(string idToken, CancellationToken cancellationToken = default);
}

public class AppleAuthService : IAppleAuthService
{
    // Validate Apple ID token using public keys from Apple
    // https://appleid.apple.com/auth/keys
    // Verify iss, aud, exp claims similar to Google
}
```

**3. Configuration**
```json
// appsettings.json
{
  "Apple": {
    "TeamId": "ABC123DEF4",
    "ClientId": "com.quietmatch.service",
    "KeyId": "XYZ987ABC1",
    "PrivateKey": "[Store in Azure Key Vault for production]"
  }
}
```

**4. Add POST /api/v1/auth/login/apple Endpoint**
```csharp
[HttpPost("login/apple")]
public async Task<IActionResult> LoginWithApple([FromBody] LoginWithAppleRequest request)
{
    var response = await _authService.LoginWithAppleAsync(request.IdToken);
    if (response == null)
        return BadRequest(new ProblemDetails { Title = "Invalid Apple ID token" });

    return Ok(response);
}
```

**5. Implement LoginWithAppleAsync in AuthService**
- Similar flow to Google login
- Use `User.CreateFromApple()` instead of `User.CreateFromGoogle()`
- Publish `UserRegistered` event for new users

**6. Testing Considerations**
- Apple Sign In requires real device or simulator for testing
- Use Apple's sandbox environment for development
- Document test Apple ID creation process

**7. Documentation**
- Add Apple setup guide to `IMPLEMENTATION_SUMMARY.md`
- Document Apple-specific configuration in `API.md`
- Add manual testing steps to `TESTING.md`

#### Acceptance Criteria

- [ ] Apple Developer Account configured with Service ID and key
- [ ] `IAppleAuthService` implemented and validated
- [ ] `POST /api/v1/auth/login/apple` endpoint working
- [ ] New users created with `AuthProvider.Apple`
- [ ] Existing Apple users can log in (lastLoginAt updated)
- [ ] `UserRegistered` event published for new users
- [ ] Unit tests passing
- [ ] Manual testing completed with real Apple ID
- [ ] API documentation updated
- [ ] Apple private key stored in Azure Key Vault (production)

#### References

- Sign in with Apple Docs: https://developer.apple.com/sign-in-with-apple/
- Apple REST API: https://developer.apple.com/documentation/sign_in_with_apple/sign_in_with_apple_rest_api
- Apple Public Keys: https://appleid.apple.com/auth/keys

---

## Archive - Completed TODOs

> TODOs that have been completed. Kept for historical reference.

*No completed TODOs yet*

---

**Last Updated**: 2025-11-24
**Maintained By**: QuietMatch Engineering Team
