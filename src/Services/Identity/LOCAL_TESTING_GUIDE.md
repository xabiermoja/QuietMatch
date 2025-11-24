# Local Testing Guide - IdentityService

> Complete guide to run and test the IdentityService on your local machine

## Prerequisites ✅

You already have everything installed:
- ✅ Docker (27.5.1)
- ✅ Docker Compose (v2.31.0)
- ✅ .NET SDK (8.0.405)
- ✅ curl (for testing)

---

## Quick Start (3 Steps)

### Option A: Run with Docker Compose

```bash
# 1. Navigate to Identity service
cd /Users/xabiermoja/code/QuietMatch/src/Services/Identity

# 2. Start infrastructure services (skip identity-service for now due to Seq issue)
docker compose up -d postgres rabbitmq

# 3. Wait for services to be healthy (~30 seconds)
docker compose ps

# Expected output:
# identity-postgres    Up (healthy)
# identity-rabbitmq    Up (healthy)
```

**Note**: We're skipping the `seq` service because it has storage issues on macOS. Logging will go to console instead.

### Option B: Run .NET API Directly (Recommended)

This is faster for development and avoids Docker issues:

```bash
# 1. Start only infrastructure
docker compose up -d postgres rabbitmq

# 2. Run the API directly
cd DatingApp.IdentityService.Api
dotnet run

# API will start on http://localhost:5000
```

---

## Port Conflict Resolution

If you see `Bind for 0.0.0.0:5432 failed: port is already allocated`, you have PostgreSQL running on port 5432.

**Solution**: Use a different port for the Identity database:

1. Edit `docker-compose.yml`:
   ```yaml
   postgres:
     ports:
       - "5433:5432"  # Changed from 5432:5432
   ```

2. Update `appsettings.json`:
   ```json
   "ConnectionStrings": {
     "IdentityDb": "Host=localhost;Port=5433;Database=identity_db;Username=admin;Password=devpassword123"
   }
   ```

3. Restart:
   ```bash
   docker compose down
   docker compose up -d postgres rabbitmq
   ```

---

## Step-by-Step Testing

### 1. Verify Infrastructure is Running

```bash
# Check container status
docker compose ps

# Should show:
# NAME                 STATUS
# identity-postgres    Up (healthy)
# identity-rabbitmq    Up (healthy)

# Test PostgreSQL connection
docker exec -it identity-postgres psql -U admin -d identity_db -c "SELECT version();"

# Test RabbitMQ (open in browser)
open http://localhost:15672
# Login: guest / guest
```

### 2. Run Database Migrations

```bash
cd DatingApp.IdentityService.Api

# Apply migrations
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

**Option 1: Using dotnet run (Recommended for Development)**

```bash
cd DatingApp.IdentityService.Api
dotnet run

# Output:
# info: Microsoft.Hosting.Lifetime[14]
#       Now listening on: http://localhost:5000
# info: Microsoft.Hosting.Lifetime[0]
#       Application started. Press Ctrl+C to shut down.
```

**Option 2: Using Docker**

```bash
# From Identity directory
docker compose up identity-service

# Watch logs
docker compose logs -f identity-service
```

### 4. Test the API

**Check Health** (if you added health endpoints):
```bash
curl http://localhost:5000/health
```

**Test Google Login Endpoint** (will fail without valid Google token):
```bash
curl -X POST http://localhost:5000/api/v1/auth/login/google \
  -H "Content-Type: application/json" \
  -d '{"idToken": "fake-token-for-testing"}'

# Expected: 400 Bad Request (invalid token)
```

**View Swagger UI** (if enabled):
```bash
open http://localhost:5000/swagger
```

---

## Running Unit Tests

```bash
# From Identity root directory
cd /Users/xabiermoja/code/QuietMatch/src/Services/Identity

# Run all 55 unit tests
dotnet test DatingApp.IdentityService.Tests.Unit/DatingApp.IdentityService.Tests.Unit.csproj

# Expected output:
# Passed!  - Failed:     0, Passed:    55, Skipped:     0, Total:    55

# Run with detailed output
dotnet test DatingApp.IdentityService.Tests.Unit/DatingApp.IdentityService.Tests.Unit.csproj --logger "console;verbosity=detailed"

# Run specific test
dotnet test --filter "FullyQualifiedName~LoginWithGoogleAsync_WithValidTokenAndNewUser"

# Generate coverage report
dotnet test DatingApp.IdentityService.Tests.Unit/DatingApp.IdentityService.Tests.Unit.csproj \
  --collect:"XPlat Code Coverage" \
  --results-directory ./TestResults
```

---

## Manual API Testing with Real Google Token

### Get a Real Google ID Token

1. **Option A: Use Google OAuth Playground**
   - Go to: https://developers.google.com/oauthplayground/
   - Select "Google OAuth2 API v2"
   - Check "https://www.googleapis.com/auth/userinfo.email"
   - Click "Authorize APIs"
   - Exchange authorization code for tokens
   - Copy the `id_token` from the response

2. **Option B: Create a Test Client**
   ```html
   <!-- save as test.html and open in browser -->
   <!DOCTYPE html>
   <html>
   <head>
       <script src="https://accounts.google.com/gsi/client" async></script>
   </head>
   <body>
       <div id="g_id_onload"
            data-client_id="YOUR_GOOGLE_CLIENT_ID"
            data-callback="handleCredentialResponse">
       </div>
       <div class="g_id_signin" data-type="standard"></div>
       <script>
           function handleCredentialResponse(response) {
               console.log("ID Token: " + response.credential);
               alert("Check console for ID token");
           }
       </script>
   </body>
   </html>
   ```

### Test with Real Token

```bash
# Replace YOUR_REAL_ID_TOKEN with actual token from above
curl -X POST http://localhost:5000/api/v1/auth/login/google \
  -H "Content-Type: application/json" \
  -d '{"idToken": "YOUR_REAL_ID_TOKEN"}'

# Expected successful response:
# {
#   "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
#   "refreshToken": "abc123...",
#   "expiresIn": 900,
#   "tokenType": "Bearer",
#   "userId": "550e8400-e29b-41d4-a716-446655440000",
#   "email": "user@example.com",
#   "isNewUser": true
# }
```

### Verify Data in Database

```bash
# Check created user
docker exec -it identity-postgres psql -U admin -d identity_db -c \
  "SELECT \"Id\", \"Email\", \"Provider\", \"CreatedAt\" FROM \"Users\";"

# Check refresh token (hashed)
docker exec -it identity-postgres psql -U admin -d identity_db -c \
  "SELECT \"Id\", \"UserId\", \"ExpiresAt\", \"IsRevoked\" FROM \"RefreshTokens\";"
```

### Verify Event Published to RabbitMQ

1. Open RabbitMQ Management UI: http://localhost:15672 (guest/guest)
2. Go to "Queues" tab
3. Look for queue with `UserRegistered` event
4. Click on queue name → "Get messages" to see event payload

---

## Testing Scenarios

### Scenario 1: New User Registration

```bash
# 1. Login with Google (use real token)
curl -X POST http://localhost:5000/api/v1/auth/login/google \
  -H "Content-Type: application/json" \
  -d '{"idToken": "YOUR_TOKEN"}'

# 2. Verify response has isNewUser: true
# 3. Check database has new user record
# 4. Check RabbitMQ has UserRegistered event
```

### Scenario 2: Existing User Login

```bash
# 1. Login with same Google account again
curl -X POST http://localhost:5000/api/v1/auth/login/google \
  -H "Content-Type: application/json" \
  -d '{"idToken": "SAME_TOKEN"}'

# 2. Verify response has isNewUser: false
# 3. Check database user's LastLoginAt updated
# 4. Check RabbitMQ has NO new UserRegistered event
```

### Scenario 3: Invalid Token

```bash
# Try with fake token
curl -X POST http://localhost:5000/api/v1/auth/login/google \
  -H "Content-Type: application/json" \
  -d '{"idToken": "invalid-token"}'

# Expected: 400 Bad Request
# {
#   "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
#   "title": "Bad Request",
#   "status": 400,
#   "detail": "Invalid Google ID token"
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

# Check logs
docker compose logs postgres
```

### Problem: "Connection refused" to RabbitMQ

```bash
# Check if RabbitMQ is running
docker compose ps rabbitmq

# Start if needed
docker compose up -d rabbitmq

# Verify management UI
open http://localhost:15672
```

### Problem: Migrations fail

```bash
# Reset database
docker compose down -v  # WARNING: Deletes all data
docker compose up -d postgres

# Wait for PostgreSQL to be ready
sleep 5

# Run migrations again
cd DatingApp.IdentityService.Api
dotnet ef database update --project ../DatingApp.IdentityService.Infrastructure
```

### Problem: "Google:ClientId is not configured"

The app needs a real Google OAuth Client ID for production. For local testing:

**Option 1: Skip validation (development only)**
- Mock the GoogleAuthService in appsettings.Development.json

**Option 2: Create Google OAuth credentials**
1. Go to https://console.cloud.google.com/
2. Create new project or select existing
3. Enable "Google+ API"
4. Create OAuth 2.0 Client ID (Web application)
5. Add http://localhost:5000 to authorized origins
6. Copy Client ID to appsettings.json:
   ```json
   "Google": {
     "ClientId": "YOUR_CLIENT_ID.apps.googleusercontent.com"
   }
   ```

### Problem: Port already in use

```bash
# Find what's using the port
lsof -i :5000

# Kill the process
kill -9 <PID>

# Or use a different port
dotnet run --urls "http://localhost:5001"
```

---

## Clean Up

```bash
# Stop all services
docker compose down

# Stop and remove volumes (deletes all data)
docker compose down -v

# Remove images
docker compose down --rmi all
```

---

## Performance Checklist

✅ **All tests passing**: 55/55 unit tests
✅ **Database accessible**: Can query Users and RefreshTokens tables
✅ **RabbitMQ accessible**: Management UI at http://localhost:15672
✅ **API responding**: GET /health returns 200 OK
✅ **Google login works**: POST /api/v1/auth/login/google with real token succeeds
✅ **JWT tokens valid**: Can decode accessToken at https://jwt.io
✅ **Events published**: UserRegistered events appear in RabbitMQ

---

## Next Steps

- **Integration Tests**: Run integration tests with Testcontainers (if implemented)
- **Load Testing**: Use tools like k6 or Apache Bench
- **Security Testing**: Test with invalid/expired/malformed tokens
- **Monitoring**: Set up Application Insights (Azure) or Grafana (local)

---

## Quick Reference

| Service | Port | Credentials | URL |
|---------|------|-------------|-----|
| IdentityService API | 5000 | N/A | http://localhost:5000 |
| PostgreSQL | 5432* | admin / devpassword123 | localhost:5432 |
| RabbitMQ AMQP | 5672 | guest / guest | localhost:5672 |
| RabbitMQ Management | 15672 | guest / guest | http://localhost:15672 |
| Swagger UI | 5000 | N/A | http://localhost:5000/swagger |

*If port 5432 conflicts, use 5433

---

**Last Updated**: 2025-11-24
**Maintained By**: QuietMatch Engineering Team
