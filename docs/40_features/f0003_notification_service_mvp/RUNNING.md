# Running the NotificationService - Complete Guide

**Quick Start:** For development, you need NO external accounts! The service uses console adapters by default.

## 📋 Table of Contents

1. [Prerequisites](#prerequisites)
2. [Development Mode (No External Accounts)](#development-mode-no-external-accounts)
3. [Production Mode (SendGrid Required)](#production-mode-sendgrid-required)
4. [Running Locally](#running-locally)
5. [Running with Docker](#running-with-docker)
6. [Testing & Monitoring](#testing--monitoring)
7. [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Required (All Modes)

- **.NET 8 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
  ```bash
  dotnet --version  # Should be 8.0.x or higher
  ```

- **Git** - For cloning the repository
  ```bash
  git --version
  ```

### Optional (Development with Events)

- **Docker Desktop** - For RabbitMQ message broker
  ```bash
  docker --version
  docker-compose --version
  ```

### Optional (Production Email)

- **SendGrid Account** - Only needed for production email sending
  - Free tier: 100 emails/day forever
  - Sign up: https://signup.sendgrid.com/

---

## Development Mode (No External Accounts)

**Perfect for learning and testing!** No SendGrid, Twilio, or any external services required.

### What You Get

- **Console Email Adapter** - Emails displayed in terminal with nice formatting
- **Console SMS Adapter** - SMS displayed in terminal
- **File Template Provider** - Templates loaded from local files
- **Full functionality** - All features work, just output to console instead of real delivery

### Quick Start (Standalone)

```bash
# 1. Navigate to the API project
cd ~/code/QuietMatch/src/Services/Notification/DatingApp.NotificationService.Api

# 2. Run the service
dotnet run --urls "http://localhost:5003"
```

**That's it!** Service is now running at http://localhost:5003

You'll see:
```
✅ Email Provider: Console (development mode)
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5003
```

### Test It Out

**Option 1: Swagger UI (Easiest)**

1. Open http://localhost:5003/swagger in your browser
2. Try the `/api/notifications/welcome` endpoint
3. Click "Try it out"
4. Fill in the request body:
   ```json
   {
     "userId": "123e4567-e89b-12d3-a456-426614174000",
     "email": "john@example.com",
     "name": "John Doe"
   }
   ```
5. Click "Execute"
6. **Check your terminal** - you'll see a beautifully formatted email!

**Option 2: cURL (Command Line)**

```bash
# Send welcome email
curl -X POST http://localhost:5003/api/notifications/welcome \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "123e4567-e89b-12d3-a456-426614174000",
    "email": "john@example.com",
    "name": "John"
  }'

# Send profile completed email
curl -X POST http://localhost:5003/api/notifications/profile-completed \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "123e4567-e89b-12d3-a456-426614174000",
    "email": "john@example.com",
    "name": "John"
  }'

# Send SMS
curl -X POST http://localhost:5003/api/notifications/sms \
  -H "Content-Type: application/json" \
  -d '{
    "phoneNumber": "+12025551234",
    "message": "Your verification code is: 123456"
  }'
```

**Expected Output (in your terminal):**

```
╔════════════════════════════════════════════════════════════════╗
║                      📧 EMAIL MESSAGE                          ║
╠════════════════════════════════════════════════════════════════╣
║ To:      John <john@example.com>
║ Subject: Welcome to QuietMatch! 🎉
╠════════════════════════════════════════════════════════════════╣
║ BODY:
╠════════════════════════════════════════════════════════════════╣
║ <html><body>Hello John!</body></html>
╚════════════════════════════════════════════════════════════════╝
```

---

## Production Mode (SendGrid Required)

### 1. Get SendGrid API Key

**Create a SendGrid account** (if you don't have one):
1. Go to https://signup.sendgrid.com/
2. Choose **Free Plan** (100 emails/day forever)
3. Verify your email address
4. Complete sender verification (required by SendGrid)

**Create an API Key:**
1. Log in to SendGrid dashboard
2. Go to **Settings** → **API Keys**
3. Click **Create API Key**
4. Choose **Restricted Access**
5. Enable **Mail Send** → **Full Access**
6. Click **Create & View**
7. **Copy the API key** (you won't see it again!)

Example key format: `SG.aBcDeFgHiJkLmNoPqRsTuVwXyZ.1234567890abcdefghijklmnopqrstuvwxyz`

### 2. Configure SendGrid

**Option A: Environment Variables (Recommended for Security)**

```bash
# Set environment variables
export Email__Provider="SendGrid"
export Email__SendGrid__ApiKey="SG.your_actual_api_key_here"
export Email__SendGrid__FromEmail="noreply@yourdomain.com"
export Email__SendGrid__FromName="QuietMatch"

# Run the service
cd ~/code/QuietMatch/src/Services/Notification/DatingApp.NotificationService.Api
dotnet run --urls "http://localhost:5003"
```

**Option B: appsettings.Development.json (Local Testing)**

Create `appsettings.Development.json` (this file is git-ignored):

```json
{
  "Email": {
    "Provider": "SendGrid",
    "SendGrid": {
      "ApiKey": "SG.your_actual_api_key_here",
      "FromEmail": "noreply@yourdomain.com",
      "FromName": "QuietMatch"
    }
  }
}
```

**Option C: appsettings.json (Not Recommended - API Key Visible)**

⚠️ Only use for testing! Never commit API keys to git!

```json
{
  "Email": {
    "Provider": "SendGrid",
    "SendGrid": {
      "ApiKey": "SG.your_actual_api_key_here",
      "FromEmail": "noreply@yourdomain.com",
      "FromName": "QuietMatch"
    }
  }
}
```

### 3. Verify SendGrid Integration

When you run the service, you should see:

```
✅ Email Provider: SendGrid (from: noreply@yourdomain.com)
```

Now when you send an email via the API, it will **actually send** through SendGrid!

**Test with cURL:**

```bash
curl -X POST http://localhost:5003/api/notifications/welcome \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "123e4567-e89b-12d3-a456-426614174000",
    "email": "your-real-email@gmail.com",
    "name": "Test User"
  }'
```

Check your inbox! You should receive a real email.

**Important SendGrid Notes:**

1. **Sender Verification Required** - SendGrid requires you to verify your sender email address or domain
2. **Free Tier Limits** - 100 emails/day on free plan
3. **Spam Filters** - Test emails might go to spam initially
4. **Rate Limiting** - SendGrid has rate limits (40,000 requests/month on free tier)

---

## Running Locally

### Standalone (Just NotificationService)

**No event consumers, just API endpoints:**

```bash
cd ~/code/QuietMatch/src/Services/Notification/DatingApp.NotificationService.Api
dotnet run --urls "http://localhost:5003"
```

- Service URL: http://localhost:5003
- Swagger UI: http://localhost:5003/swagger
- Health Check: http://localhost:5003/health

**Pros:** Simple, fast startup
**Cons:** No event consumption from other services

### Full Stack (With RabbitMQ for Events)

**Requires Docker for RabbitMQ:**

```bash
# 1. Start RabbitMQ
cd ~/code/QuietMatch
docker-compose up -d rabbitmq

# 2. Wait for RabbitMQ to be ready (10-15 seconds)
docker-compose logs -f rabbitmq
# Wait until you see: "Server startup complete"
# Press Ctrl+C to exit logs

# 3. Start NotificationService
cd src/Services/Notification/DatingApp.NotificationService.Api
dotnet run --urls "http://localhost:5003"
```

**RabbitMQ Management UI:** http://localhost:15672
- Username: `guest`
- Password: `guest`

**Now the service will:**
- Listen for `UserRegistered` events → Send welcome email
- Listen for `ProfileCompleted` events → Send profile completed email
- Still accept direct API calls

### Run Unit Tests

```bash
cd ~/code/QuietMatch/src/Services/Notification/DatingApp.NotificationService.Tests.Unit
dotnet test --verbosity normal
```

Expected output:
```
Passed!  - Failed:     0, Passed:    36, Skipped:     0, Total:    36, Duration: < 50ms
```

---

## Running with Docker

### Build the Docker Image

```bash
cd ~/code/QuietMatch/src/Services/Notification
docker build -t quietmatch-notification:latest .
```

Build should complete in ~20-30 seconds.

### Run Standalone Container

```bash
docker run -d \
  --name notification-service \
  -p 5003:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e Email__Provider=Console \
  quietmatch-notification:latest
```

Access at: http://localhost:5003/health

**View logs:**
```bash
docker logs -f notification-service
```

**Stop and remove:**
```bash
docker stop notification-service
docker rm notification-service
```

### Run with Docker Compose (Full Infrastructure)

**Start everything:**

```bash
cd ~/code/QuietMatch
docker-compose up -d notification-service
```

This starts:
- RabbitMQ (message broker)
- NotificationService (with RabbitMQ connection)

**Check status:**
```bash
docker-compose ps
```

**View logs:**
```bash
docker-compose logs -f notification-service
```

**Stop everything:**
```bash
docker-compose down
```

### Docker with SendGrid

Add SendGrid API key to docker-compose:

```bash
# Set environment variable on host
export SENDGRID_API_KEY="SG.your_actual_api_key_here"

# Update docker-compose.yml (uncomment SendGrid env vars)
docker-compose up -d notification-service
```

Or create `.env` file in project root:

```bash
# .env
SENDGRID_API_KEY=SG.your_actual_api_key_here
```

Then update `docker-compose.yml` notification-service section:

```yaml
environment:
  - Email__Provider=SendGrid
  - Email__SendGrid__ApiKey=${SENDGRID_API_KEY}
  - Email__SendGrid__FromEmail=noreply@quietmatch.com
  - Email__SendGrid__FromName=QuietMatch
```

---

## Testing & Monitoring

### Health Check

```bash
curl http://localhost:5003/health
```

Expected response:
```json
{
  "status": "healthy",
  "timestamp": "2025-11-29T12:34:56.789Z"
}
```

### Service Info

```bash
curl http://localhost:5003/
```

Response shows available endpoints and architecture pattern.

### Send Test Notifications

**Welcome Email:**
```bash
curl -X POST http://localhost:5003/api/notifications/welcome \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "550e8400-e29b-41d4-a716-446655440000",
    "email": "test@example.com",
    "name": "Alice"
  }'
```

**Profile Completed Email:**
```bash
curl -X POST http://localhost:5003/api/notifications/profile-completed \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "550e8400-e29b-41d4-a716-446655440000",
    "email": "test@example.com",
    "name": "Alice"
  }'
```

**SMS:**
```bash
curl -X POST http://localhost:5003/api/notifications/sms \
  -H "Content-Type: application/json" \
  -d '{
    "phoneNumber": "+12025551234",
    "message": "Your verification code: 789012"
  }'
```

### Monitor Logs (Console Mode)

When running locally, watch the terminal for formatted email output:

```
📧 [CONSOLE EMAIL] Sending email to test@example.com
╔════════════════════════════════════════════════════════════════╗
║                      📧 EMAIL MESSAGE                          ║
╠════════════════════════════════════════════════════════════════╣
║ To:      Alice <test@example.com>
║ Subject: Welcome to QuietMatch! 🎉
...
```

### Monitor Logs (Docker Mode)

```bash
docker-compose logs -f notification-service
```

Or for live updates:

```bash
docker-compose logs --tail=50 -f notification-service
```

### Monitor RabbitMQ (Event Queue)

**Open RabbitMQ Management UI:**

http://localhost:15672
- Username: `guest`
- Password: `guest`

**Check queues:**
1. Click "Queues" tab
2. Look for `notification-service` queue
3. Monitor message rates and counts

**Publish test event manually:**
1. Go to "Exchanges" tab
2. Find exchange (e.g., `UserRegistered`)
3. Publish test message:
   ```json
   {
     "userId": "550e8400-e29b-41d4-a716-446655440000",
     "email": "test@example.com",
     "provider": "Google",
     "registeredAt": "2025-11-29T12:00:00Z",
     "correlationId": "550e8400-e29b-41d4-a716-446655440001"
   }
   ```

### Monitor SendGrid (Production)

**SendGrid Dashboard:**
1. Log in to https://app.sendgrid.com/
2. Go to **Activity Feed** → see real-time email delivery
3. Check **Stats** → delivery rates, bounces, spam reports

**Check if email was sent:**
```bash
# After sending via API, check SendGrid Activity Feed
# Shows: Processed → Delivered → Opened (if recipient opens)
```

---

## Troubleshooting

### Issue: Service won't start

**Error:** `Failed to bind to address http://localhost:5003`

**Solution:** Port already in use. Kill existing process:

```bash
lsof -ti:5003 | xargs kill -9
# Or use different port:
dotnet run --urls "http://localhost:5004"
```

---

### Issue: "Connection refused" to RabbitMQ

**Error:** `RabbitMQ.Client.Exceptions.BrokerUnreachableException`

**Solutions:**

1. **Check RabbitMQ is running:**
   ```bash
   docker-compose ps rabbitmq
   # Should show "Up" status
   ```

2. **Start RabbitMQ:**
   ```bash
   docker-compose up -d rabbitmq
   ```

3. **Check RabbitMQ logs:**
   ```bash
   docker-compose logs rabbitmq
   # Look for "Server startup complete"
   ```

4. **Restart RabbitMQ:**
   ```bash
   docker-compose restart rabbitmq
   ```

---

### Issue: SendGrid emails not sending

**Error:** `Email sending failed (Status: 401 Unauthorized)`

**Solutions:**

1. **Check API key is set:**
   ```bash
   echo $Email__SendGrid__ApiKey
   # Should output your API key (starting with SG.)
   ```

2. **Verify API key permissions:**
   - Log in to SendGrid dashboard
   - Settings → API Keys
   - Check your key has "Mail Send" → "Full Access"

3. **Check sender verification:**
   - SendGrid requires verified sender email/domain
   - Settings → Sender Authentication
   - Verify your sender email or domain

4. **Check SendGrid free tier limits:**
   - Free tier: 100 emails/day
   - Go to dashboard to see usage

---

### Issue: SendGrid emails going to spam

**Solutions:**

1. **Verify sender domain (recommended):**
   - SendGrid → Settings → Sender Authentication
   - Verify your domain (requires DNS changes)

2. **Use authenticated sender:**
   - Single Sender Verification is less trusted than domain verification
   - Recipients might mark as spam initially

3. **Improve email content:**
   - Add unsubscribe link
   - Use plain text version too
   - Avoid spam trigger words

---

### Issue: Templates not found

**Error:** `Template not found: WelcomeEmail`

**Solutions:**

1. **Check templates directory exists:**
   ```bash
   ls ~/code/QuietMatch/src/Services/Notification/DatingApp.NotificationService.Infrastructure/Templates/
   # Should show: WelcomeEmail.html, ProfileCompletedEmail.html
   ```

2. **Check template path configuration:**
   - Look for startup log: `Template provider initialized with path: ...`
   - Path should point to Templates directory

3. **If running in Docker:**
   ```bash
   docker exec notification-service ls /app/Templates
   # Should show template files
   ```

---

### Issue: Unit tests failing

**Run tests with verbose output:**

```bash
cd ~/code/QuietMatch/src/Services/Notification/DatingApp.NotificationService.Tests.Unit
dotnet test --verbosity detailed
```

**Common issues:**

1. **Template file missing** - Run from correct directory
2. **Mock setup incorrect** - Check test setup in constructor
3. **Async test not awaited** - Ensure all async tests use `async/await`

---

### Issue: Docker build fails

**Error:** `COPY failed: file not found`

**Solutions:**

1. **Build from correct directory:**
   ```bash
   cd ~/code/QuietMatch/src/Services/Notification
   docker build -t quietmatch-notification:latest .
   ```

2. **Check .dockerignore isn't excluding necessary files:**
   ```bash
   cat .dockerignore
   # Templates/ should NOT be ignored
   ```

3. **Clean and rebuild:**
   ```bash
   docker build --no-cache -t quietmatch-notification:latest .
   ```

---

### Debug Mode

**Run with detailed logging:**

```bash
export ASPNETCORE_ENVIRONMENT=Development
export Logging__LogLevel__Default=Debug
dotnet run --urls "http://localhost:5003"
```

**Check what adapter is loaded:**

Look for startup log:
```
✅ Email Provider: Console (development mode)
# or
✅ Email Provider: SendGrid (from: noreply@quietmatch.com)
```

---

## Quick Reference

### Environment Variables

| Variable | Values | Default | Description |
|----------|--------|---------|-------------|
| `Email__Provider` | `Console`, `SendGrid` | `Console` | Email adapter to use |
| `Email__SendGrid__ApiKey` | `SG.xxx` | `""` | SendGrid API key (required if Provider=SendGrid) |
| `Email__SendGrid__FromEmail` | Email address | `noreply@quietmatch.com` | Sender email |
| `Email__SendGrid__FromName` | Text | `QuietMatch` | Sender display name |
| `Templates__BasePath` | Path | `./Templates` | Template files location |
| `RabbitMQ__Host` | Hostname | `localhost` | RabbitMQ server |
| `RabbitMQ__Port` | Port | `5672` | RabbitMQ port |
| `RabbitMQ__Username` | Username | `guest` | RabbitMQ auth |
| `RabbitMQ__Password` | Password | `guest` | RabbitMQ auth |

### Ports

| Port | Service | URL |
|------|---------|-----|
| 5003 | NotificationService API | http://localhost:5003 |
| 5003 | Swagger UI | http://localhost:5003/swagger |
| 5672 | RabbitMQ AMQP | amqp://localhost:5672 |
| 15672 | RabbitMQ Management | http://localhost:15672 |

### Key Commands

```bash
# Build
dotnet build

# Run locally (Console mode)
dotnet run --urls "http://localhost:5003"

# Run locally (SendGrid mode)
export Email__Provider="SendGrid"
export Email__SendGrid__ApiKey="SG.xxx"
dotnet run --urls "http://localhost:5003"

# Run tests
dotnet test

# Docker build
docker build -t quietmatch-notification:latest .

# Docker run
docker-compose up -d notification-service

# View logs
docker-compose logs -f notification-service

# Health check
curl http://localhost:5003/health
```

---

## Summary

**For Development (No External Accounts):**
```bash
cd ~/code/QuietMatch/src/Services/Notification/DatingApp.NotificationService.Api
dotnet run --urls "http://localhost:5003"
# Open http://localhost:5003/swagger and test!
```

**For Production (SendGrid Required):**
```bash
# 1. Get SendGrid API key from https://app.sendgrid.com/
# 2. Set environment variable:
export Email__Provider="SendGrid"
export Email__SendGrid__ApiKey="SG.your_key_here"

# 3. Run:
dotnet run --urls "http://localhost:5003"
```

**For Event Testing (RabbitMQ Required):**
```bash
docker-compose up -d rabbitmq
cd ~/code/QuietMatch/src/Services/Notification/DatingApp.NotificationService.Api
dotnet run --urls "http://localhost:5003"
```

That's it! The NotificationService is designed to work out-of-the-box with no configuration needed for development. 🎉
