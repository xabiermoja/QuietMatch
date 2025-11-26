# Observability Patterns

**Document Status**: ✅ Active
**Last Updated**: 2025-11-26
**Scope**: All microservices

---

## Overview

This document defines standard observability patterns for all QuietMatch microservices. Consistent observability enables effective monitoring, debugging, and operational excellence in production.

### Key Principles

1. **Standardization**: All services follow identical patterns for health checks, logging, and metrics
2. **Dependency Awareness**: Health checks verify all critical dependencies
3. **Production Ready**: Observability patterns support Azure Container Apps orchestration
4. **Developer Experience**: Clear, actionable information for debugging

---

## Health Check Pattern

### Purpose

Health checks enable:
- **Orchestration**: Azure Container Apps can restart unhealthy instances
- **Load Balancing**: Traffic routing avoids unhealthy instances
- **Monitoring**: Automated alerting on service degradation
- **Dependency Validation**: Verify all critical dependencies are available

### Standard Health Check Packages

All microservices use packages from the **AspNetCore.Diagnostics.HealthChecks** ecosystem:

#### Database Health Checks

```bash
# PostgreSQL
dotnet add package AspNetCore.HealthChecks.NpgSql

# MongoDB (if used)
dotnet add package AspNetCore.HealthChecks.MongoDb

# Redis (caching, sessions)
dotnet add package AspNetCore.HealthChecks.Redis
```

#### Message Broker Health Checks

```bash
# RabbitMQ (event bus)
dotnet add package AspNetCore.HealthChecks.Rabbitmq

# Azure Service Bus (alternative)
dotnet add package AspNetCore.HealthChecks.AzureServiceBus
```

#### Cloud Service Health Checks

```bash
# Azure Blob Storage (file uploads)
dotnet add package AspNetCore.HealthChecks.AzureStorage

# Azure Key Vault (secrets)
dotnet add package AspNetCore.HealthChecks.AzureKeyVault
```

#### HTTP/Service Health Checks

```bash
# Downstream HTTP services
dotnet add package AspNetCore.HealthChecks.Uris

# gRPC services
dotnet add package AspNetCore.HealthChecks.Grpc
```

### Standard Implementation

Every microservice must implement three health check endpoints:

```csharp
// Program.cs - Service Configuration

// Add health checks for all dependencies
builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionString: postgresConnection,
        name: "postgres",
        tags: new[] { "db", "ready" })
    .AddRabbitMQ(
        rabbitConnectionString: rabbitConnection,
        name: "rabbitmq",
        tags: new[] { "messagebus", "ready" })
    .AddRedis(
        redisConnectionString: redisConnection,
        name: "redis",
        tags: new[] { "cache", "live" });

// Map three standard endpoints
app.MapHealthChecks("/health");                    // Simple 200 OK
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

### Standard Endpoints

#### `/health` - Simple Health Check
- **Purpose**: Basic liveness check
- **Response**: 200 OK (healthy) or 503 Service Unavailable (unhealthy)
- **Format**: Plain text "Healthy" or "Unhealthy"
- **Use Case**: Quick health check for monitoring tools

#### `/health/ready` - Readiness Probe
- **Purpose**: Verify service is ready to accept traffic
- **Checks**: Only dependencies tagged with `ready` (critical dependencies)
- **Response**: 200 OK (ready) or 503 Service Unavailable (not ready)
- **Use Case**: Azure Container Apps readiness probe

#### `/health/live` - Liveness Probe
- **Purpose**: Detailed health status with all dependencies
- **Checks**: All health checks (critical + optional)
- **Response**: JSON with per-dependency status
- **Use Case**: Debugging, detailed monitoring

**Example `/health/live` Response:**
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0123456",
  "entries": {
    "postgres": {
      "status": "Healthy",
      "duration": "00:00:00.0100000",
      "data": {}
    },
    "rabbitmq": {
      "status": "Healthy",
      "duration": "00:00:00.0023456",
      "data": {}
    },
    "redis": {
      "status": "Degraded",
      "duration": "00:00:00.0050000",
      "data": {
        "error": "Connection timeout"
      }
    }
  }
}
```

### Health Check Tags

Use tags to categorize dependencies:

- **`ready`**: Critical dependencies required to accept traffic (DB, message broker)
- **`live`**: All dependencies including optional ones (cache, external APIs)
- **`db`**: Database dependencies
- **`cache`**: Caching dependencies (Redis, in-memory cache)
- **`messagebus`**: Event bus dependencies (RabbitMQ, Azure Service Bus)
- **`external`**: External API dependencies (Google API, SendGrid, etc.)

### Health Check Per Microservice

#### IdentityService
```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(postgresConnection, name: "postgres", tags: new[] { "db", "ready" })
    .AddRabbitMQ(rabbitConnection, name: "rabbitmq", tags: new[] { "messagebus", "ready" });
```

#### ProfileService
```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(postgresConnection, name: "postgres", tags: new[] { "db", "ready" })
    .AddRabbitMQ(rabbitConnection, name: "rabbitmq", tags: new[] { "messagebus", "ready" })
    .AddAzureBlobStorage(blobConnection, name: "blob-storage", tags: new[] { "storage", "ready" });
```

#### MatchingService
```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(postgresConnection, name: "postgres", tags: new[] { "db", "ready" })
    .AddRedis(redisConnection, name: "redis", tags: new[] { "cache", "live" })
    .AddRabbitMQ(rabbitConnection, name: "rabbitmq", tags: new[] { "messagebus", "ready" });
```

#### NotificationService
```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(postgresConnection, name: "postgres", tags: new[] { "db", "ready" })
    .AddRabbitMQ(rabbitConnection, name: "rabbitmq", tags: new[] { "messagebus", "ready" })
    .AddUrlGroup(new Uri("https://fcm.googleapis.com/"), name: "fcm", tags: new[] { "external", "live" });
```

#### ChatService
```csharp
builder.Services.AddHealthChecks()
    .AddMongoDb(mongoConnection, name: "mongodb", tags: new[] { "db", "ready" })
    .AddRedis(redisConnection, name: "redis", tags: new[] { "cache", "ready" })
    .AddRabbitMQ(rabbitConnection, name: "rabbitmq", tags: new[] { "messagebus", "ready" });
```

#### GraphQL Gateway
```csharp
builder.Services.AddHealthChecks()
    .AddUrlGroup(new Uri("https://identity-service/health"), name: "identity-service", tags: new[] { "downstream", "ready" })
    .AddUrlGroup(new Uri("https://profile-service/health"), name: "profile-service", tags: new[] { "downstream", "ready" })
    .AddUrlGroup(new Uri("https://matching-service/health"), name: "matching-service", tags: new[] { "downstream", "ready" })
    .AddRedis(redisConnection, name: "redis", tags: new[] { "cache", "live" });
```

### Azure Container Apps Configuration

Configure health probes in Azure Container Apps:

```bicep
// Bicep template
resource containerApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'identity-service'
  properties: {
    template: {
      containers: [
        {
          name: 'identity-service'
          image: 'acr.azurecr.io/identity-service:latest'
          probes: [
            {
              type: 'Readiness'
              httpGet: {
                path: '/health/ready'
                port: 8080
              }
              initialDelaySeconds: 5
              periodSeconds: 10
              failureThreshold: 3
            }
            {
              type: 'Liveness'
              httpGet: {
                path: '/health/live'
                port: 8080
              }
              initialDelaySeconds: 10
              periodSeconds: 30
              failureThreshold: 3
            }
          ]
        }
      ]
    }
  }
}
```

### Best Practices

1. **Fail Fast**: Health checks should fail quickly (< 5 seconds timeout)
2. **Idempotent**: Health checks should not modify state
3. **Lightweight**: Use simple queries (e.g., `SELECT 1` for databases)
4. **Graceful Degradation**: Optional dependencies (cache) should not fail readiness
5. **Avoid Cascading Failures**: Don't health-check downstream services in readiness probe

### Testing Health Checks

```bash
# Simple health check
curl http://localhost:5000/health

# Readiness probe (critical dependencies only)
curl http://localhost:5000/health/ready

# Liveness probe (detailed JSON)
curl http://localhost:5000/health/live | jq
```

---

## Logging Pattern

**TODO**: Document structured logging patterns (Serilog, Application Insights)

---

## Metrics Pattern

**TODO**: Document metrics collection patterns (Prometheus, Application Insights)

---

## Distributed Tracing

**TODO**: Document distributed tracing patterns (OpenTelemetry, Application Insights)

---

## References

- [ASP.NET Core Health Checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)
- [AspNetCore.Diagnostics.HealthChecks GitHub](https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks)
- [Azure Container Apps Health Probes](https://learn.microsoft.com/en-us/azure/container-apps/health-probes)
- [12-Factor App - Telemetry](https://12factor.net/logs)
