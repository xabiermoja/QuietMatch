# IdentityService - Docker Setup

This guide explains how to run the IdentityService using Docker for local development.

## Prerequisites

- Docker Desktop installed (https://www.docker.com/products/docker-desktop)
- Docker Compose (included with Docker Desktop)
- Google OAuth credentials (https://console.cloud.google.com/)

## Quick Start

### 1. Configure Google OAuth

Before running the service, you need to configure Google OAuth credentials:

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select an existing one
3. Enable the "Google+ API"
4. Create OAuth 2.0 credentials (Web application)
5. Add authorized redirect URIs (e.g., `http://localhost:8080/signin-google`)
6. Copy the Client ID and Client Secret

### 2. Update docker-compose.yml

Edit `docker-compose.yml` and replace the placeholder values:

```yaml
Google__ClientId: "your-actual-client-id.apps.googleusercontent.com"
Google__ClientSecret: "your-actual-client-secret"
```

**IMPORTANT**: Never commit real credentials to version control. For production, use environment variables or secrets management.

### 3. Start All Services

```bash
# From the Identity service directory
cd src/Services/Identity

# Build and start all services (PostgreSQL, RabbitMQ, Seq, IdentityService)
docker-compose up --build
```

This will start:
- **PostgreSQL** (port 5432) - Database
- **RabbitMQ** (ports 5672, 15672) - Message broker
- **Seq** (port 5341) - Structured logging UI
- **IdentityService API** (port 8080) - The authentication service

### 4. Apply Database Migrations

Once the services are running, apply EF Core migrations:

```bash
# In a new terminal, run migrations inside the container
docker exec -it identity-service-api dotnet ef database update

# Or run migrations from your host machine (requires .NET SDK)
dotnet ef database update --project DatingApp.IdentityService.Infrastructure --startup-project DatingApp.IdentityService.Api
```

### 5. Verify Services Are Running

- **API Health**: http://localhost:8080/health (if health endpoint exists)
- **Swagger UI**: http://localhost:8080 (development only)
- **RabbitMQ Management**: http://localhost:15672 (guest/guest)
- **Seq Logs**: http://localhost:5341

## Docker Commands

### Start services (detached mode)
```bash
docker-compose up -d
```

### View logs
```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f identity-service
```

### Stop services
```bash
docker-compose down
```

### Stop services and remove volumes (deletes data)
```bash
docker-compose down -v
```

### Rebuild after code changes
```bash
docker-compose up --build
```

### Execute commands in container
```bash
# Run shell
docker exec -it identity-service-api /bin/bash

# Run EF migrations
docker exec -it identity-service-api dotnet ef database update
```

## Environment Variables

The following environment variables are configured in `docker-compose.yml`:

### Database
- `ConnectionStrings__IdentityDb`: PostgreSQL connection string

### JWT Configuration
- `Jwt__SecretKey`: Secret key for signing JWTs (256 bits)
- `Jwt__Issuer`: Token issuer (https://quietmatch.com)
- `Jwt__Audience`: Token audience (https://api.quietmatch.com)
- `Jwt__AccessTokenExpiryMinutes`: Access token lifetime (15 minutes)

### Google OAuth
- `Google__ClientId`: Your Google OAuth Client ID
- `Google__ClientSecret`: Your Google OAuth Client Secret

### RabbitMQ
- `RabbitMq__Host`: RabbitMQ hostname (rabbitmq)
- `RabbitMq__Username`: RabbitMQ username (guest)
- `RabbitMq__Password`: RabbitMQ password (guest)

### Logging
- `Serilog__WriteTo__1__Args__serverUrl`: Seq server URL

## Security Notes

⚠️ **IMPORTANT**: The configuration in `docker-compose.yml` is for **local development only**.

**DO NOT use in production**:
- Default passwords (postgres, rabbitmq)
- Insecure JWT secret key
- HTTP instead of HTTPS
- Non-root container user (already configured in Dockerfile)

For production deployment:
1. Use Kubernetes secrets or Azure Key Vault
2. Enable HTTPS with valid certificates
3. Use strong, randomly generated passwords
4. Configure proper network isolation
5. Enable rate limiting and firewall rules

## Troubleshooting

### Port conflicts
If ports 5432, 5672, 15672, 5341, or 8080 are already in use, modify the port mappings in `docker-compose.yml`:

```yaml
ports:
  - "5433:5432"  # Changed from 5432 to 5433
```

### Database connection errors
Ensure PostgreSQL is healthy before starting the API:

```bash
docker-compose logs postgres
```

### RabbitMQ connection errors
Check RabbitMQ status:

```bash
docker-compose logs rabbitmq
```

### View application logs
```bash
docker-compose logs -f identity-service
```

Or use Seq UI: http://localhost:5341

### Clean restart
```bash
docker-compose down -v
docker-compose up --build
```

## Architecture

```
┌─────────────────────┐
│  IdentityService    │
│      (API)          │
│    Port: 8080       │
└──────────┬──────────┘
           │
           ├─────────► PostgreSQL (5432)
           │           Database
           │
           ├─────────► RabbitMQ (5672)
           │           Message Broker
           │
           └─────────► Seq (5341)
                       Structured Logs
```

## Development Workflow

1. Make code changes in your IDE
2. Rebuild the Docker image:
   ```bash
   docker-compose up --build identity-service
   ```
3. Test the changes via API calls or Swagger UI
4. View logs in Seq: http://localhost:5341
5. Monitor message queue in RabbitMQ UI: http://localhost:15672

## Production Considerations

For production deployment, consider:

1. **Container Registry**: Push images to Azure Container Registry (ACR) or Docker Hub
2. **Orchestration**: Deploy to Azure Container Apps, Kubernetes (AKS), or Azure App Service
3. **Secrets Management**: Use Azure Key Vault, Kubernetes Secrets, or HashiCorp Vault
4. **Monitoring**: Integrate with Application Insights or Prometheus/Grafana
5. **Database**: Use Azure Database for PostgreSQL with managed backups
6. **Message Broker**: Use Azure Service Bus or managed RabbitMQ
7. **Logging**: Use Azure Monitor, ELK stack, or cloud-native solutions
8. **Health Checks**: Implement liveness and readiness probes
9. **Scaling**: Configure horizontal pod autoscaling
10. **Security**: Network policies, pod security policies, image scanning

## Additional Resources

- [Docker Documentation](https://docs.docker.com/)
- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [ASP.NET Core in Docker](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/)
- [PostgreSQL Docker Hub](https://hub.docker.com/_/postgres)
- [RabbitMQ Docker Hub](https://hub.docker.com/_/rabbitmq)
