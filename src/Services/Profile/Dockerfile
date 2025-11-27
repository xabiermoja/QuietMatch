# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["DatingApp.ProfileService.Api/DatingApp.ProfileService.Api.csproj", "DatingApp.ProfileService.Api/"]
COPY ["DatingApp.ProfileService.Core/DatingApp.ProfileService.Core.csproj", "DatingApp.ProfileService.Core/"]
COPY ["DatingApp.ProfileService.Infrastructure/DatingApp.ProfileService.Infrastructure.csproj", "DatingApp.ProfileService.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "DatingApp.ProfileService.Api/DatingApp.ProfileService.Api.csproj"

# Copy all source files
COPY . .

# Build the application
WORKDIR "/src/DatingApp.ProfileService.Api"
RUN dotnet build "DatingApp.ProfileService.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "DatingApp.ProfileService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Copy published files from publish stage
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=http://+:80

# Run the application
ENTRYPOINT ["dotnet", "DatingApp.ProfileService.Api.dll"]
