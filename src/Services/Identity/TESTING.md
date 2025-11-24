# IdentityService - Manual Testing Guide

This guide provides comprehensive manual testing procedures for the IdentityService (Feature F0001: Sign In with Google).

## Table of Contents
1. [Prerequisites](#prerequisites)
2. [Environment Setup](#environment-setup)
3. [Test Scenarios](#test-scenarios)
4. [API Testing](#api-testing)
5. [Integration Testing](#integration-testing)
6. [Verification Checklist](#verification-checklist)

## Prerequisites

Before testing, ensure you have:
- [ ] .NET 8.0 SDK installed
- [ ] PostgreSQL running (local or Docker)
- [ ] RabbitMQ running (local or Docker)
- [ ] Google OAuth credentials (Client ID and Secret)
- [ ] API testing tool (Postman, Insomnia, or curl)
- [ ] Valid Google ID token for testing

## Environment Setup

### Option 1: Local Development

1. **Configure appsettings.Development.json**
```json
{
  "ConnectionStrings": {
    "IdentityDb": "Host=localhost;Database=identity_db;Username=admin;Password=your-password"
  },
  "Google": {
    "ClientId": "your-client-id.apps.googleusercontent.com",
    "ClientSecret": "your-client-secret"
  },
  "Jwt": {
    "SecretKey": "your-256-bit-secret-key-at-least-32-characters-long",
    "Issuer": "https://quietmatch.com",
    "Audience": "https://api.quietmatch.com",
    "AccessTokenExpiryMinutes": "15"
  },
  "RabbitMq": {
    "Host": "localhost",
    "Username": "guest",
    "Password": "guest"
  }
}
```

2. **Apply Database Migrations**
```bash
cd src/Services/Identity/DatingApp.IdentityService.Infrastructure
dotnet ef database update --startup-project ../DatingApp.IdentityService.Api
```

3. **Start the API**
```bash
cd src/Services/Identity/DatingApp.IdentityService.Api
dotnet run
```

### Option 2: Docker

```bash
cd src/Services/Identity
docker-compose up --build
```

## Test Scenarios

### Scenario 1: New User Sign-In with Google

**Objective**: Verify that a new user can sign in with Google and receive JWT tokens.

**Prerequisites**:
- Valid Google ID token
- User does not exist in database

**Steps**:
1. Obtain a Google ID token from Google OAuth 2.0
2. Send POST request to `/api/v1/auth/login/google`
3. Verify response contains access token, refresh token, and user info

**Expected Result**:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "base64-encoded-random-string",
  "expiresIn": 900,
  "tokenType": "Bearer",
  "userId": "guid",
  "isNewUser": true,
  "email": "user@example.com"
}
```

**Verification**:
- [ ] Response status: 200 OK
- [ ] `isNewUser` is `true`
- [ ] `accessToken` is valid JWT with correct claims
- [ ] `refreshToken` is 44-character Base64 string
- [ ] `expiresIn` is 900 (15 minutes)
- [ ] User created in `users` table
- [ ] Refresh token hash stored in `refresh_tokens` table
- [ ] `UserRegistered` event published to RabbitMQ

### Scenario 2: Existing User Sign-In with Google

**Objective**: Verify that an existing user can sign in and receive new tokens.

**Prerequisites**:
- Valid Google ID token
- User already exists in database

**Steps**:
1. Use same Google ID token from Scenario 1
2. Send POST request to `/api/v1/auth/login/google`
3. Verify response contains tokens

**Expected Result**:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "different-base64-encoded-string",
  "expiresIn": 900,
  "tokenType": "Bearer",
  "userId": "same-guid-as-scenario-1",
  "isNewUser": false,
  "email": "user@example.com"
}
```

**Verification**:
- [ ] Response status: 200 OK
- [ ] `isNewUser` is `false`
- [ ] `userId` matches the user created in Scenario 1
- [ ] New tokens are different from Scenario 1
- [ ] `last_login_at` updated in database
- [ ] No new user created (user count unchanged)
- [ ] New refresh token created in database
- [ ] No `UserRegistered` event published

### Scenario 3: Invalid Google ID Token

**Objective**: Verify proper error handling for invalid tokens.

**Steps**:
1. Send POST request with invalid or expired Google ID token
2. Verify error response

**Expected Result**:
- Response status: 401 Unauthorized
- Error message indicating invalid authentication

**Verification**:
- [ ] Response status: 401 Unauthorized
- [ ] No user created in database
- [ ] No tokens generated
- [ ] Error logged in Serilog/Seq

### Scenario 4: Empty or Malformed Request

**Objective**: Verify input validation works correctly.

**Steps**:
1. Send POST request with empty `idToken`
2. Send POST request with null `idToken`
3. Send POST request with invalid JSON

**Expected Result**:
- Response status: 400 Bad Request
- Validation error message

**Verification**:
- [ ] Response status: 400 Bad Request
- [ ] FluentValidation error message returned
- [ ] RFC 7807 ProblemDetails format
- [ ] No database operations performed

### Scenario 5: JWT Token Claims Verification

**Objective**: Verify JWT access tokens contain correct claims.

**Steps**:
1. Sign in with Google (Scenario 1 or 2)
2. Decode the access token using jwt.io or similar tool
3. Verify claims

**Expected Claims**:
```json
{
  "sub": "user-guid",
  "email": "user@example.com",
  "jti": "unique-token-id",
  "iat": 1234567890,
  "exp": 1234568790,
  "iss": "https://quietmatch.com",
  "aud": "https://api.quietmatch.com"
}
```

**Verification**:
- [ ] `sub` claim contains user GUID
- [ ] `email` claim contains user email
- [ ] `jti` claim is unique per token
- [ ] `iat` claim is current timestamp
- [ ] `exp` claim is 15 minutes after `iat`
- [ ] `iss` matches configured issuer
- [ ] `aud` matches configured audience
- [ ] Token signature is valid (HMAC-SHA256)

### Scenario 6: Database Persistence

**Objective**: Verify data is correctly persisted to PostgreSQL.

**Steps**:
1. Complete Scenario 1 (new user sign-in)
2. Query database to verify data

**Database Queries**:
```sql
-- Verify user created
SELECT * FROM users WHERE email = 'user@example.com';

-- Verify refresh token stored
SELECT * FROM refresh_tokens WHERE user_id = 'user-guid';

-- Verify token is not revoked
SELECT is_revoked FROM refresh_tokens WHERE user_id = 'user-guid';
```

**Verification**:
- [ ] User exists with correct `provider` (Google)
- [ ] `external_user_id` matches Google sub
- [ ] `created_at` is recent timestamp
- [ ] `last_login_at` is NULL for new users
- [ ] Refresh token exists with correct `user_id`
- [ ] Token hash is 44-character Base64 string
- [ ] `expires_at` is 7 days from `created_at`
- [ ] `is_revoked` is FALSE
- [ ] `revoked_at` is NULL

### Scenario 7: RabbitMQ Event Publishing

**Objective**: Verify `UserRegistered` event is published for new users.

**Steps**:
1. Complete Scenario 1 (new user sign-in)
2. Check RabbitMQ Management UI (http://localhost:15672)

**Verification**:
- [ ] Event published to exchange
- [ ] Event contains correct user data:
  - `UserId`: user GUID
  - `Email`: user email
  - `Provider`: "Google"
  - `RegisteredAt`: timestamp
  - `CorrelationId`: unique GUID
- [ ] Event published only for new users (not existing users)

### Scenario 8: Logging and Observability

**Objective**: Verify structured logging works correctly.

**Steps**:
1. Complete various test scenarios
2. Check Seq UI (http://localhost:5341) or console logs

**Verification**:
- [ ] Successful sign-ins logged at Information level
- [ ] Failed sign-ins logged at Warning level
- [ ] Token validation logged
- [ ] User creation logged with user ID
- [ ] Event publishing logged with correlation ID
- [ ] All logs contain structured data (not just strings)
- [ ] Request/response logging via Serilog middleware

## API Testing

### Using curl

**New User Sign-In**:
```bash
curl -X POST http://localhost:8080/api/v1/auth/login/google \
  -H "Content-Type: application/json" \
  -d '{
    "idToken": "your-google-id-token"
  }'
```

**Expected Response**:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "abcd1234...",
  "expiresIn": 900,
  "tokenType": "Bearer",
  "userId": "12345678-1234-1234-1234-123456789abc",
  "isNewUser": true,
  "email": "user@example.com"
}
```

### Using Postman

1. **Create New Request**
   - Method: POST
   - URL: `http://localhost:8080/api/v1/auth/login/google`
   - Headers:
     - `Content-Type: application/json`
   - Body (raw JSON):
     ```json
     {
       "idToken": "your-google-id-token"
     }
     ```

2. **Send Request**
3. **Verify Response**
   - Status: 200 OK
   - Body contains all required fields

### Obtaining a Google ID Token for Testing

**Method 1: OAuth 2.0 Playground**
1. Go to https://developers.google.com/oauthplayground/
2. Select "Google OAuth2 API v2" → "userinfo.email"
3. Click "Authorize APIs"
4. Sign in with Google
5. Click "Exchange authorization code for tokens"
6. Copy the `id_token` value

**Method 2: Client Application**
- Use the frontend application with Google Sign-In button
- Intercept the ID token from the callback
- Use for API testing

## Integration Testing

### Database Integration
```bash
# Run migrations
dotnet ef database update --startup-project DatingApp.IdentityService.Api

# Verify tables created
psql -U admin -d identity_db -c "\dt"

# Expected tables:
# - users
# - refresh_tokens
# - __EFMigrationsHistory
```

### Message Broker Integration
```bash
# Check RabbitMQ is running
curl http://localhost:15672/api/overview -u guest:guest

# Verify exchanges and queues exist
# (Open Management UI: http://localhost:15672)
```

### Docker Integration
```bash
# Start all services
docker-compose up

# Verify all containers healthy
docker-compose ps

# Check logs
docker-compose logs identity-service

# Test API endpoint
curl http://localhost:8080/api/v1/auth/login/google
```

## Verification Checklist

### Code Quality
- [ ] All unit tests pass (55 tests)
- [ ] No compiler warnings
- [ ] Code follows C# conventions
- [ ] Proper error handling throughout
- [ ] Security best practices followed

### API Functionality
- [ ] Login endpoint accepts POST requests
- [ ] Request validation works (FluentValidation)
- [ ] Google ID token validation works
- [ ] JWT tokens generated correctly
- [ ] Refresh tokens hashed before storage
- [ ] Proper HTTP status codes returned

### Database
- [ ] Migrations apply successfully
- [ ] Tables created with correct schema
- [ ] Indexes created for performance
- [ ] Foreign keys enforced
- [ ] Unique constraints work
- [ ] Data persisted correctly

### Messaging
- [ ] RabbitMQ connection established
- [ ] UserRegistered event published for new users
- [ ] No events for existing users
- [ ] Event structure correct

### Logging
- [ ] Console logging works
- [ ] Seq logging works (if enabled)
- [ ] Structured logging format
- [ ] Log levels appropriate
- [ ] Sensitive data not logged

### Docker
- [ ] Docker image builds successfully
- [ ] docker-compose starts all services
- [ ] Health checks pass
- [ ] Services communicate correctly
- [ ] Volumes persist data

### Security
- [ ] Tokens hashed before database storage
- [ ] JWT signed with HMAC-SHA256
- [ ] Non-root user in Docker container
- [ ] No secrets in source code
- [ ] HTTPS recommended for production

### Performance
- [ ] Response time < 500ms for login
- [ ] Database queries optimized (indexes)
- [ ] Token generation efficient
- [ ] No N+1 queries

## Known Limitations

1. **Google OAuth Testing**
   - Requires real Google OAuth credentials
   - ID tokens expire after 1 hour
   - Cannot easily mock for integration tests

2. **Refresh Token Flow**
   - Refresh token endpoint not yet implemented
   - Token revocation not yet implemented
   - Will be added in future feature

3. **Apple Sign-In**
   - Domain entities support Apple
   - Implementation not yet added
   - Planned for future feature

4. **Rate Limiting**
   - Not yet implemented
   - Recommended for production
   - Should limit login attempts per IP

5. **HTTPS**
   - Local development uses HTTP
   - Production must use HTTPS
   - Certificate configuration needed

## Troubleshooting

### "Failed to validate Google ID token"
- Ensure Client ID is correct in configuration
- Verify ID token is not expired
- Check Google Console API is enabled

### "Database connection failed"
- Verify PostgreSQL is running
- Check connection string is correct
- Ensure database exists

### "RabbitMQ connection refused"
- Verify RabbitMQ is running
- Check host/port configuration
- Ensure credentials are correct

### "JWT signature invalid"
- Verify secret key is at least 256 bits (32 characters)
- Check key matches between token generation and validation
- Ensure key is not changed after tokens issued

### Docker build fails
- Check .dockerignore is correct
- Ensure all project files are copied
- Verify NuGet restore succeeds

## Test Report Template

```markdown
# IdentityService Test Report

**Date**: YYYY-MM-DD
**Tester**: Your Name
**Environment**: Local / Docker / Staging
**Version**: Feature branch name

## Summary
- Total scenarios tested: X
- Passed: X
- Failed: X
- Blocked: X

## Test Results

### Scenario 1: New User Sign-In
- Status: PASS / FAIL / BLOCKED
- Notes: ...

### Scenario 2: Existing User Sign-In
- Status: PASS / FAIL / BLOCKED
- Notes: ...

[Continue for all scenarios...]

## Issues Found
1. Issue description
   - Severity: Critical / High / Medium / Low
   - Steps to reproduce:
   - Expected:
   - Actual:

## Recommendations
- ...

## Sign-Off
- [ ] All critical scenarios passed
- [ ] No blocking issues
- [ ] Ready for deployment
```

## Additional Resources

- [Feature Specification](../../docs/03_features/F0001_sign_in_with_google.md)
- [API Documentation](API.md) (if exists)
- [Architecture Guidelines](../../docs/10_architecture/02_architecture-guidelines.md)
- [Docker Setup](DOCKER.md)
