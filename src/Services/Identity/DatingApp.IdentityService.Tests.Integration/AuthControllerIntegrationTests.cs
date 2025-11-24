using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DatingApp.IdentityService.Application.DTOs;
using DatingApp.IdentityService.Domain.Enums;
using DatingApp.IdentityService.Infrastructure.Data;
using DatingApp.IdentityService.Infrastructure.Events;
using DatingApp.IdentityService.Infrastructure.Services;
using FluentAssertions;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Testcontainers.PostgreSql;
using Xunit;

namespace DatingApp.IdentityService.Tests.Integration;

/// <summary>
/// Integration tests for AuthController.
/// Tests the full authentication flow with real database (Testcontainers)
/// and mocked message publishing.
/// </summary>
/// <remarks>
/// These tests verify:
/// - HTTP API endpoints work correctly
/// - Database persistence works with PostgreSQL
/// - Event publishing is called with correct data
/// - JWT token generation produces valid tokens
/// - Complete authentication flow from HTTP request to database commit
///
/// Google OAuth is mocked since we cannot use real Google tokens in automated tests.
/// Message publishing is mocked to avoid requiring RabbitMQ infrastructure.
/// </remarks>
public class AuthControllerIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private Mock<IGoogleAuthService> _mockGoogleAuthService = null!;
    private Mock<IPublishEndpoint> _mockPublishEndpoint = null!;

    public AuthControllerIntegrationTests()
    {
        // Create PostgreSQL container
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("identity_db_test")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .Build();
    }

    public async Task InitializeAsync()
    {
        // Start PostgreSQL container
        await _postgresContainer.StartAsync();

        // Create mocks before factory initialization
        _mockGoogleAuthService = new Mock<IGoogleAuthService>();
        _mockPublishEndpoint = new Mock<IPublishEndpoint>();

        // Capture mocks in local variables for use in lambda
        var googleAuthMock = _mockGoogleAuthService;
        var publishMock = _mockPublishEndpoint;
        var connectionString = _postgresContainer.GetConnectionString();

        // Create WebApplicationFactory with test configuration
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    // Replace PostgreSQL connection with test container
                    services.RemoveAll<DbContextOptions<IdentityDbContext>>();
                    services.AddDbContext<IdentityDbContext>(options =>
                        options.UseNpgsql(connectionString));

                    // Replace Google Auth Service with mock
                    services.RemoveAll<IGoogleAuthService>();
                    services.AddSingleton(googleAuthMock.Object);

                    // Replace MassTransit publish endpoint with mock
                    services.RemoveAll<IPublishEndpoint>();
                    services.AddSingleton(publishMock.Object);
                });

                builder.UseEnvironment("Testing");
            });

        _client = _factory.CreateClient();

        // Apply migrations using a temporary scope
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _factory?.Dispose();
        await _postgresContainer.DisposeAsync();
    }

    /// <summary>
    /// Helper method to get a database context for test assertions.
    /// Creates a new scope - caller must dispose.
    /// </summary>
    private IdentityDbContext GetDbContext()
    {
        var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    }

    [Fact]
    public async Task LoginWithGoogle_WithValidToken_ShouldReturn200AndCreateUser()
    {
        // Arrange
        var googleUserInfo = new GoogleUserInfo(
            Sub: "google-sub-123456",
            Email: "newuser@example.com",
            Name: "New User",
            EmailVerified: true
        );

        _mockGoogleAuthService
            .Setup(x => x.ValidateIdTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(googleUserInfo);

        var request = new LoginWithGoogleRequest("valid-google-token");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login/google", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        loginResponse.Should().NotBeNull();
        loginResponse!.AccessToken.Should().NotBeNullOrWhiteSpace();
        loginResponse.RefreshToken.Should().NotBeNullOrWhiteSpace();
        loginResponse.Email.Should().Be(googleUserInfo.Email);
        loginResponse.IsNewUser.Should().BeTrue();
        loginResponse.ExpiresIn.Should().Be(900); // 15 minutes
        loginResponse.TokenType.Should().Be("Bearer");

        // Verify user was created in database
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == googleUserInfo.Email);

        user.Should().NotBeNull();
        user!.Provider.Should().Be(AuthProvider.Google);
        user.ExternalUserId.Should().Be(googleUserInfo.Sub);
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify refresh token was stored in database
        var refreshToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.UserId == user.Id);

        refreshToken.Should().NotBeNull();
        refreshToken!.IsRevoked.Should().BeFalse();
        refreshToken.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromHours(1));
    }

    [Fact]
    public async Task LoginWithGoogle_WithValidTokenForExistingUser_ShouldReturn200AndUpdateLastLogin()
    {
        // Arrange - Create existing user
        var googleUserInfo = new GoogleUserInfo(
            Sub: "google-sub-existing",
            Email: "existinguser@example.com",
            Name: "Existing User",
            EmailVerified: true
        );

        Domain.Entities.User existingUser;
        DateTime? previousLoginTime;

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            existingUser = Domain.Entities.User.CreateFromGoogle(googleUserInfo.Email, googleUserInfo.Sub);
            existingUser.RecordLogin(); // Simulate previous login
            dbContext.Users.Add(existingUser);
            await dbContext.SaveChangesAsync();
            previousLoginTime = existingUser.LastLoginAt;
        }

        // Wait a bit to ensure time difference
        await Task.Delay(100);

        _mockGoogleAuthService
            .Setup(x => x.ValidateIdTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(googleUserInfo);

        var request = new LoginWithGoogleRequest("valid-google-token");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login/google", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        loginResponse.Should().NotBeNull();
        loginResponse!.IsNewUser.Should().BeFalse();
        loginResponse.UserId.Should().Be(existingUser.Id);

        // Verify LastLoginAt was updated
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var updatedUser = await dbContext.Users.FindAsync(existingUser.Id);
            updatedUser.Should().NotBeNull();
            updatedUser!.LastLoginAt.Should().NotBeNull();
            updatedUser.LastLoginAt.Should().BeAfter(previousLoginTime!.Value);
        }
    }

    [Fact]
    public async Task LoginWithGoogle_WithInvalidToken_ShouldReturn401()
    {
        // Arrange
        _mockGoogleAuthService
            .Setup(x => x.ValidateIdTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GoogleUserInfo?)null); // Token validation failed

        var request = new LoginWithGoogleRequest("invalid-google-token");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login/google", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LoginWithGoogle_WithEmptyToken_ShouldReturn400()
    {
        // Arrange
        var request = new LoginWithGoogleRequest("");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login/google", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Should return ProblemDetails
        var problemDetails = await response.Content.ReadFromJsonAsync<JsonElement>();
        problemDetails.GetProperty("type").GetString().Should().Contain("validation");
    }

    [Fact]
    public async Task LoginWithGoogle_WithValidToken_ShouldPublishUserRegisteredEvent()
    {
        // Arrange
        var googleUserInfo = new GoogleUserInfo(
            Sub: "google-sub-for-event-test",
            Email: "eventtest@example.com",
            Name: "Event Test User",
            EmailVerified: true
        );

        _mockGoogleAuthService
            .Setup(x => x.ValidateIdTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(googleUserInfo);

        var request = new LoginWithGoogleRequest("valid-google-token");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login/google", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify UserRegistered event was published
        _mockPublishEndpoint.Verify(
            x => x.Publish(
                It.Is<UserRegistered>(e =>
                    e.Email == googleUserInfo.Email &&
                    e.Provider == AuthProvider.Google.ToString() &&
                    e.CorrelationId != Guid.Empty),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "UserRegistered event should be published for new user");
    }

    [Fact]
    public async Task LoginWithGoogle_WithExistingUser_ShouldNotPublishUserRegisteredEvent()
    {
        // Arrange - Create existing user
        var googleUserInfo = new GoogleUserInfo(
            Sub: "google-sub-no-event",
            Email: "noevent@example.com",
            Name: "No Event User",
            EmailVerified: true
        );

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var existingUser = Domain.Entities.User.CreateFromGoogle(googleUserInfo.Email, googleUserInfo.Sub);
            dbContext.Users.Add(existingUser);
            await dbContext.SaveChangesAsync();
        }

        _mockGoogleAuthService
            .Setup(x => x.ValidateIdTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(googleUserInfo);

        var request = new LoginWithGoogleRequest("valid-google-token");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login/google", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify UserRegistered event was NOT published for existing user
        _mockPublishEndpoint.Verify(
            x => x.Publish(It.IsAny<UserRegistered>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "UserRegistered event should NOT be published for existing user");
    }

    [Fact]
    public async Task LoginWithGoogle_ShouldGenerateValidJwtToken()
    {
        // Arrange
        var googleUserInfo = new GoogleUserInfo(
            Sub: "google-sub-jwt-test",
            Email: "jwttest@example.com",
            Name: "JWT Test User",
            EmailVerified: true
        );

        _mockGoogleAuthService
            .Setup(x => x.ValidateIdTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(googleUserInfo);

        var request = new LoginWithGoogleRequest("valid-google-token");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login/google", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        loginResponse.Should().NotBeNull();

        var accessToken = loginResponse!.AccessToken;
        accessToken.Should().NotBeNullOrWhiteSpace();

        // Verify JWT format (header.payload.signature)
        var parts = accessToken.Split('.');
        parts.Should().HaveCount(3);

        // Verify JWT can be parsed
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(accessToken);

        jwt.Issuer.Should().Be("https://quietmatch.com");
        jwt.Audiences.Should().Contain("https://api.quietmatch.com");

        // Verify claims
        jwt.Claims.Should().Contain(c => c.Type == "sub");
        jwt.Claims.Should().Contain(c => c.Type == "email" && c.Value == googleUserInfo.Email);
        jwt.Claims.Should().Contain(c => c.Type == "jti");
    }
}
