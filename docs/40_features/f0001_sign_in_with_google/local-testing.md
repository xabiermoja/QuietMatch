# F0001 Local Testing Guide - Sign In with Google

> Complete guide to run and test F0001 (IdentityService) on your local machine

---

## Table of Contents

- [Prerequisites](#prerequisites)
- [Quick Start](#quick-start)
- [Port Conflict Resolution](#port-conflict-resolution)
- [Step-by-Step Testing](#step-by-step-testing)
- [Running Tests](#running-tests)
- [Manual API Testing](#manual-api-testing)
- [Testing Scenarios](#testing-scenarios)
- [Troubleshooting](#troubleshooting)
- [Clean Up](#clean-up)

---

## Prerequisites

Before starting, ensure you have:

- ✅ **Docker** (27.5.1+) - Container runtime
- ✅ **Docker Compose** (v2.31.0+) - Multi-container orchestration
- ✅ **.NET SDK** (8.0.405+) - To build and run the API
- ✅ **curl** - For API testing (or use Postman/Insomnia)

**Verify installations:**
```bash
docker --version
docker compose version
dotnet --version
curl --version
```

---

## Quick Start

### Option A: Run API with dotnet (Recommended for Development)

```bash
# 1. Navigate to Identity service
cd /Users/xabiermoja/code/QuietMatch/src/Services/Identity

# 2. Start infrastructure (PostgreSQL + RabbitMQ)
docker compose up -d postgres rabbitmq

# 3. Wait for services to be healthy (~15 seconds)
docker compose ps

# 4. Run the API
cd DatingApp.IdentityService.Api
dotnet run --urls "http://localhost:5000"

# API starts on http://localhost:5000
```

### Option B: Run Everything with Docker Compose

```bash
# Navigate to Identity service
cd /Users/xabiermoja/code/QuietMatch/src/Services/Identity

# Start all services (postgres, rabbitmq, identity-service)
docker compose up -d

# Note: seq service has issues on macOS, it's safe to skip it
# Logs will go to console instead
```

---

## Port Conflict Resolution

### Common Issue: Port 5432 Already in Use

If you see: `Bind for 0.0.0.0:5432 failed: port is already allocated`

**Solution**: The docker-compose.yml is already configured to use port **5433** instead of 5432.

**Verify current port configuration:**
```bash
# Check docker-compose.yml
grep -A 2 "ports:" docker-compose.yml | grep 5433

# Should show:
#   - "5433:5432"
```

**Verify appsettings.json:**
```bash
# Check connection string
grep "IdentityDb" DatingApp.IdentityService.Api/appsettings.json

# Should show Port=5433:
# "IdentityDb": "Host=localhost;Port=5433;Database=identity_db;Username=admin;Password=devpassword123"
```

**If ports are not configured correctly:**

1. Edit `docker-compose.yml`:
   ```yaml
   postgres:
     ports:
       - "5433:5432"  # Use 5433 on host, 5432 in container
   ```

2. Edit `DatingApp.IdentityService.Api/appsettings.json`:
   ```json
   "ConnectionStrings": {
     "IdentityDb": "Host=localhost;Port=5433;Database=identity_db;Username=admin;Password=devpassword123"
   }
   ```

3. Restart services:
   ```bash
   docker compose down
   docker compose up -d postgres rabbitmq
   ```

---

## Step-by-Step Testing

### 1. Verify Infrastructure is Running

```bash
cd /Users/xabiermoja/code/QuietMatch/src/Services/Identity

# Check container status
docker compose ps

# Expected output:
# NAME                 STATUS
# identity-postgres    Up (healthy)
# identity-rabbitmq    Up (healthy)
```

**Test PostgreSQL connection:**
```bash
docker exec -it identity-postgres psql -U admin -d identity_db -c "SELECT version();"

# Should show PostgreSQL 16.x version
```

**Test RabbitMQ Management UI:**
```bash
open http://localhost:15672
# Login: guest / guest
```

### 2. Apply Database Migrations

```bash
cd DatingApp.IdentityService.Api

# Apply EF Core migrations
dotnet ef database update --project ../DatingApp.IdentityService.Infrastructure

# Verify tables created
docker exec -it identity-postgres psql -U admin -d identity_db -c "\dt"

# Expected output:
#  Schema |     Name      | Type  | Owner
# --------+---------------+-------+-------
#  public | RefreshTokens | table | admin
#  public | Users         | table | admin
```

### 3. Run the IdentityService API

**Option 1: Using dotnet run (Best for Development)**

```bash
cd DatingApp.IdentityService.Api
dotnet run --urls "http://localhost:5000"

# Expected output:
# [17:52:16 INF] Starting IdentityService API
# [17:52:16 INF] Bus started: rabbitmq://localhost/
# [17:52:16 INF] Now listening on: http://localhost:5000
```

**Option 2: Using Docker**

```bash
# From Identity directory
docker compose up identity-service

# Watch logs
docker compose logs -f identity-service
```

### 4. Test the API

**Basic health check:**
```bash
curl http://localhost:5000/
# Expected: 301 or 404 (means API is responding)
```

**Test Google login endpoint (will fail without valid token):**
```bash
curl -X POST http://localhost:5000/api/v1/auth/login/google \
  -H "Content-Type: application/json" \
  -d '{"idToken": "test-invalid-token"}'

# Expected: 400 Bad Request
# {
#   "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
#   "title": "Invalid ID Token",
#   "status": 400,
#   "detail": "The provided ID token is invalid or expired.",
#   "instance": "/api/v1/auth/login/google"
# }
```

---

## Running Tests

### Run Unit Tests (55 Tests)

```bash
cd /Users/xabiermoja/code/QuietMatch/src/Services/Identity

# Run all unit tests
dotnet test DatingApp.IdentityService.Tests.Unit/DatingApp.IdentityService.Tests.Unit.csproj

# Expected output:
# Passed!  - Failed: 0, Passed: 55, Skipped: 0, Total: 55
```

**Run with detailed output:**
```bash
dotnet test DatingApp.IdentityService.Tests.Unit/DatingApp.IdentityService.Tests.Unit.csproj \
  --logger "console;verbosity=detailed"
```

**Run specific test:**
```bash
dotnet test --filter "FullyQualifiedName~LoginWithGoogleAsync_WithValidTokenAndNewUser"
```

**Generate coverage report:**
```bash
dotnet test DatingApp.IdentityService.Tests.Unit/DatingApp.IdentityService.Tests.Unit.csproj \
  --collect:"XPlat Code Coverage" \
  --results-directory ./TestResults
```

---

## Manual API Testing

### Get a Real Google ID Token

To test with actual Google authentication, you need a real ID token:

**Option A: Google OAuth Playground**

1. Go to: https://developers.google.com/oauthplayground/
2. In the left panel, expand "Google OAuth2 API v2"
3. Select scope: `https://www.googleapis.com/auth/userinfo.email`
4. Click "Authorize APIs"
5. Sign in with your Google account
6. Click "Exchange authorization code for tokens"
7. Copy the `id_token` from the JSON response

**Option B: Create Test HTML Page**

```html
<!-- Save as test-google-login.html and open in browser -->
<!DOCTYPE html>
<html>
<head>
    <title>Test Google Sign In</title>
    <script src="https://accounts.google.com/gsi/client" async></script>
</head>
<body>
    <h1>Test Google Sign In for QuietMatch</h1>

    <div id="g_id_onload"
         data-client_id="YOUR_GOOGLE_CLIENT_ID"
         data-callback="handleCredentialResponse">
    </div>
    <div class="g_id_signin" data-type="standard"></div>

    <div id="result" style="margin-top: 20px;"></div>

    <script>
        function handleCredentialResponse(response) {
            console.log("ID Token:", response.credential);
            document.getElementById('result').innerHTML =
                '<strong>ID Token (check browser console):</strong><br>' +
                '<textarea style="width:100%;height:100px;">' +
                response.credential + '</textarea>';
        }
    </script>
</body>
</html>
```

### Test with Real Token

```bash
# Replace YOUR_REAL_ID_TOKEN with token from above
curl -X POST http://localhost:5000/api/v1/auth/login/google \
  -H "Content-Type: application/json" \
  -d '{"idToken": "YOUR_REAL_ID_TOKEN"}'

# Expected successful response:
# {
#   "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
#   "refreshToken": "abc123defhijklmnop...",
#   "expiresIn": 900,
#   "tokenType": "Bearer",
#   "userId": "550e8400-e29b-41d4-a716-446655440000",
#   "email": "your.email@gmail.com",
#   "isNewUser": true
# }
```

### Verify Data in Database

```bash
# Check created user
docker exec -it identity-postgres psql -U admin -d identity_db -c \
  "SELECT \"Id\", \"Email\", \"Provider\", \"CreatedAt\", \"LastLoginAt\" FROM \"Users\";"

# Check refresh token (stored as hash)
docker exec -it identity-postgres psql -U admin -d identity_db -c \
  "SELECT \"Id\", \"UserId\", \"ExpiresAt\", \"IsRevoked\", \"CreatedAt\" FROM \"RefreshTokens\";"
```

### Verify Event Published to RabbitMQ

1. Open RabbitMQ Management UI: http://localhost:15672
   - Login: `guest` / `guest`

2. Go to **"Queues"** tab

3. Look for a queue (it may be auto-created by MassTransit)

4. Click on the queue name → **"Get messages"** button

5. You should see `UserRegistered` event with payload:
   ```json
   {
     "userId": "550e8400-e29b-41d4-a716-446655440000",
     "email": "user@example.com",
     "provider": "Google",
     "registeredAt": "2025-11-24T17:00:00Z",
     "correlationId": "..."
   }
   ```

---

## Testing Scenarios

### Scenario 1: New User Registration

**Test Flow:**
1. User signs in with Google for the first time
2. System creates new user record
3. System publishes `UserRegistered` event
4. Returns tokens with `isNewUser: true`

```bash
# 1. Login with Google (use real token from OAuth Playground)
curl -X POST http://localhost:5000/api/v1/auth/login/google \
  -H "Content-Type: application/json" \
  -d '{"idToken": "YOUR_REAL_TOKEN"}'

# 2. Verify response
# {
#   "isNewUser": true,
#   "userId": "...",
#   "email": "...",
#   "accessToken": "...",
#   "refreshToken": "..."
# }

# 3. Check database - user exists
docker exec -it identity-postgres psql -U admin -d identity_db -c \
  "SELECT \"Email\", \"Provider\" FROM \"Users\";"

# 4. Check RabbitMQ - UserRegistered event published
# Open http://localhost:15672 and check queues
```

### Scenario 2: Existing User Login

**Test Flow:**
1. Same user signs in again
2. System updates `LastLoginAt` timestamp
3. No `UserRegistered` event published (user already exists)
4. Returns tokens with `isNewUser: false`

```bash
# 1. Login with same Google account again
curl -X POST http://localhost:5000/api/v1/auth/login/google \
  -H "Content-Type: application/json" \
  -d '{"idToken": "SAME_TOKEN_AS_BEFORE"}'

# 2. Verify response
# {
#   "isNewUser": false,  # ← Key difference
#   "userId": "...",
#   "email": "...",
#   "accessToken": "...",
#   "refreshToken": "..."
# }

# 3. Check database - LastLoginAt updated
docker exec -it identity-postgres psql -U admin -d identity_db -c \
  "SELECT \"Email\", \"LastLoginAt\" FROM \"Users\";"

# 4. Check RabbitMQ - NO new UserRegistered event
```

### Scenario 3: Invalid Token

**Test Flow:**
1. Attempt login with malformed or expired token
2. System validates token with Google
3. Returns 400 Bad Request

```bash
curl -X POST http://localhost:5000/api/v1/auth/login/google \
  -H "Content-Type: application/json" \
  -d '{"idToken": "invalid-or-expired-token"}'

# Expected: 400 Bad Request
# {
#   "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
#   "title": "Invalid ID Token",
#   "status": 400,
#   "detail": "The provided ID token is invalid or expired.",
#   "instance": "/api/v1/auth/login/google"
# }
```

### Scenario 4: Decode JWT Token

Verify the access token structure:

```bash
# Copy the accessToken from login response
# Go to https://jwt.io
# Paste the token

# You should see decoded payload:
# {
#   "sub": "550e8400-e29b-41d4-a716-446655440000",  # User ID
#   "email": "user@example.com",
#   "jti": "unique-jwt-id",
#   "exp": 1700000000,  # Expiration timestamp
#   "iss": "https://quietmatch.com",
#   "aud": "https://api.quietmatch.com"
# }
```

---

## Troubleshooting

### Problem: "Connection refused" to PostgreSQL

```bash
# Check if PostgreSQL is running
docker compose ps postgres

# If not running, start it
docker compose up -d postgres

# Wait for healthy status
sleep 10 && docker compose ps postgres

# Check logs if still failing
docker compose logs postgres
```

### Problem: "Connection refused" to RabbitMQ

```bash
# Check if RabbitMQ is running
docker compose ps rabbitmq

# Start if needed
docker compose up -d rabbitmq

# Verify management UI is accessible
open http://localhost:15672

# Check logs
docker compose logs rabbitmq
```

### Problem: Migrations Fail

```bash
# Reset database (WARNING: Deletes all data)
docker compose down -v
docker compose up -d postgres

# Wait for PostgreSQL to be ready
sleep 15

# Run migrations
cd DatingApp.IdentityService.Api
dotnet ef database update --project ../DatingApp.IdentityService.Infrastructure

# Verify tables
docker exec -it identity-postgres psql -U admin -d identity_db -c "\dt"
```

### Problem: "Google:ClientId is not configured"

The app needs a real Google OAuth Client ID for actual authentication.

**For local development testing:**

1. Go to: https://console.cloud.google.com/
2. Create a new project or select existing
3. Enable "Google+ API" (or newer equivalent)
4. Go to "Credentials" → "Create Credentials" → "OAuth 2.0 Client ID"
5. Select "Web application"
6. Add authorized redirect URI: `http://localhost:5000`
7. Copy the Client ID
8. Update `appsettings.json`:
   ```json
   "Google": {
     "ClientId": "123456789-abc.apps.googleusercontent.com"
   }
   ```

**Note**: For unit tests, you don't need a real Client ID (tests use mocks).

### Problem: Port 5000 Already in Use

```bash
# Find what's using port 5000
lsof -i :5000

# Kill the process
kill -9 <PID>

# Or run API on different port
dotnet run --urls "http://localhost:5001"
```

### Problem: Test Failures

```bash
# Run tests with detailed output to see which test fails
dotnet test DatingApp.IdentityService.Tests.Unit/DatingApp.IdentityService.Tests.Unit.csproj \
  --logger "console;verbosity=detailed"

# Run specific failing test
dotnet test --filter "FullyQualifiedName~TestName"

# Clean and rebuild
dotnet clean
dotnet build
dotnet test
```

### Problem: seq Service Won't Start (macOS)

**Issue**: The `seq` service has storage permission issues on macOS.

**Solution**: Skip the `seq` service for local development:

```bash
# Start only postgres and rabbitmq
docker compose up -d postgres rabbitmq

# Logs will go to console instead
# This is fine for local testing
```

---

## Clean Up

### Stop Services (Keep Data)

```bash
cd /Users/xabiermoja/code/QuietMatch/src/Services/Identity

# Stop all services
docker compose down

# Services stopped, volumes preserved
```

### Stop and Remove All Data

```bash
# Stop and remove volumes (deletes database data!)
docker compose down -v

# Remove images too
docker compose down -v --rmi all
```

### Stop Only API (Keep Infrastructure Running)

```bash
# If running with dotnet run, press Ctrl+C

# If running in Docker
docker compose stop identity-service
```

---

## Quick Reference

### Service URLs

| Service | Port | Credentials | URL |
|---------|------|-------------|-----|
| IdentityService API | 5000 | N/A | http://localhost:5000 |
| PostgreSQL | 5433* | admin / devpassword123 | localhost:5433 |
| RabbitMQ AMQP | 5672 | guest / guest | localhost:5672 |
| RabbitMQ Management | 15672 | guest / guest | http://localhost:15672 |

*Port 5433 to avoid conflict with other PostgreSQL instances

### API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/v1/auth/login/google` | POST | Authenticate with Google OAuth |

### Common Commands

```bash
# Start infrastructure
docker compose up -d postgres rabbitmq

# Run API
cd DatingApp.IdentityService.Api && dotnet run

# Run tests
dotnet test DatingApp.IdentityService.Tests.Unit/DatingApp.IdentityService.Tests.Unit.csproj

# Check containers
docker compose ps

# View logs
docker compose logs -f

# Stop everything
docker compose down
```

---

## Performance Checklist

Before considering the local setup complete, verify:

- ✅ **All tests passing**: 55/55 unit tests green
- ✅ **Database accessible**: Can query Users and RefreshTokens tables
- ✅ **RabbitMQ accessible**: Management UI loads at http://localhost:15672
- ✅ **API responding**: Can call POST /api/v1/auth/login/google
- ✅ **Invalid token handling**: Returns 400 with RFC 7807 format
- ✅ **Valid token handling**: Creates user and returns tokens (with real Google token)
- ✅ **JWT tokens valid**: Can decode accessToken at https://jwt.io
- ✅ **Events published**: UserRegistered events appear in RabbitMQ for new users

---

## Next Steps

**After local testing works:**

1. **Integration Tests**: Run integration tests with Testcontainers (when implemented)
2. **Load Testing**: Use k6 or Apache Bench to test performance
3. **Security Testing**: Test with malformed, expired, and revoked tokens
4. **Docker Build**: Verify the service runs correctly in Docker
5. **Production Config**: Review appsettings for production deployment

**See also:**
- Feature Specification: `f0001_sign_in_with_google.md`
- Implementation Summary: `../../Services/Identity/IMPLEMENTATION_SUMMARY.md`
- Testing Guide: `../../Services/Identity/TESTING.md`
- API Documentation: `../../Services/Identity/API.md`

---

**Last Updated**: 2025-11-24
**Feature**: F0001 - Sign In with Google
**Maintained By**: QuietMatch Engineering Team
