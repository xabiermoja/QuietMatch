using DatingApp.IdentityService.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace DatingApp.IdentityService.Tests.Unit.Infrastructure;

/// <summary>
/// Unit tests for GoogleAuthService.
/// </summary>
/// <remarks>
/// Note: GoogleAuthService calls Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync,
/// which is a static method that cannot be easily mocked. These unit tests focus on:
/// - Constructor validation
/// - Input validation (null/empty tokens)
/// - Configuration requirements
///
/// Actual Google ID token validation (signature, issuer, audience, expiry) is tested
/// via integration tests where real Google tokens or a test OAuth server is used.
/// </remarks>
public class GoogleAuthServiceTests
{
    private readonly Mock<ILogger<GoogleAuthService>> _mockLogger;

    public GoogleAuthServiceTests()
    {
        _mockLogger = new Mock<ILogger<GoogleAuthService>>();
    }

    [Fact]
    public void Constructor_WithMissingGoogleClientId_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Google:ClientId is missing
            })
            .Build();

        // Act
        var act = () => new GoogleAuthService(configuration, _mockLogger.Object);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Google:ClientId is not configured*");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Google:ClientId", "test-client-id.apps.googleusercontent.com" }
            })
            .Build();

        // Act
        var act = () => new GoogleAuthService(configuration, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithValidConfiguration_ShouldCreateInstance()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Google:ClientId", "test-client-id.apps.googleusercontent.com" }
            })
            .Build();

        // Act
        var service = new GoogleAuthService(configuration, _mockLogger.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task ValidateIdTokenAsync_WithNullToken_ShouldReturnNull()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Google:ClientId", "test-client-id.apps.googleusercontent.com" }
            })
            .Build();

        var service = new GoogleAuthService(configuration, _mockLogger.Object);

        // Act
        var result = await service.ValidateIdTokenAsync(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateIdTokenAsync_WithEmptyToken_ShouldReturnNull()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Google:ClientId", "test-client-id.apps.googleusercontent.com" }
            })
            .Build();

        var service = new GoogleAuthService(configuration, _mockLogger.Object);

        // Act
        var result = await service.ValidateIdTokenAsync("");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateIdTokenAsync_WithWhitespaceToken_ShouldReturnNull()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Google:ClientId", "test-client-id.apps.googleusercontent.com" }
            })
            .Build();

        var service = new GoogleAuthService(configuration, _mockLogger.Object);

        // Act
        var result = await service.ValidateIdTokenAsync("   ");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateIdTokenAsync_WithInvalidToken_ShouldReturnNull()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Google:ClientId", "test-client-id.apps.googleusercontent.com" }
            })
            .Build();

        var service = new GoogleAuthService(configuration, _mockLogger.Object);

        // Act
        // This will call Google's validation API with an invalid token
        // Google.Apis.Auth will throw InvalidJwtException, which should be caught and return null
        var result = await service.ValidateIdTokenAsync("invalid-jwt-token");

        // Assert
        result.Should().BeNull();
    }

    // NOTE: Testing with valid Google ID tokens requires:
    // 1. Real Google OAuth tokens (which expire and are environment-specific)
    // 2. Integration tests with a test OAuth server
    // 3. Or refactoring to wrap Google.Apis.Auth in a mockable abstraction
    //
    // These scenarios are covered by integration tests in the AuthControllerIntegrationTests,
    // where we test the entire authentication flow end-to-end.
}
