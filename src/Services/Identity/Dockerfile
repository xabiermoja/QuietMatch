# Multi-stage Dockerfile for IdentityService
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files for restore (layer caching optimization)
COPY ["DatingApp.IdentityService.Api/DatingApp.IdentityService.Api.csproj", "DatingApp.IdentityService.Api/"]
COPY ["DatingApp.IdentityService.Application/DatingApp.IdentityService.Application.csproj", "DatingApp.IdentityService.Application/"]
COPY ["DatingApp.IdentityService.Infrastructure/DatingApp.IdentityService.Infrastructure.csproj", "DatingApp.IdentityService.Infrastructure/"]
COPY ["DatingApp.IdentityService.Domain/DatingApp.IdentityService.Domain.csproj", "DatingApp.IdentityService.Domain/"]

# Restore dependencies
RUN dotnet restore "DatingApp.IdentityService.Api/DatingApp.IdentityService.Api.csproj"

# Copy all source code
COPY . .

# Build and publish
WORKDIR "/src/DatingApp.IdentityService.Api"
RUN dotnet build "DatingApp.IdentityService.Api.csproj" -c Release -o /app/build
RUN dotnet publish "DatingApp.IdentityService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy published app from build stage
COPY --from=build /app/publish .

# Set ownership to non-root user
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose ports
# 8080: HTTP (default ASP.NET Core port in .NET 8)
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl --fail http://localhost:8080/health || exit 1

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Entry point
ENTRYPOINT ["dotnet", "DatingApp.IdentityService.Api.dll"]
