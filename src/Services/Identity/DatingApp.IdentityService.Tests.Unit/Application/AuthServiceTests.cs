using DatingApp.IdentityService.Application.Services;
using DatingApp.IdentityService.Domain.Entities;
using DatingApp.IdentityService.Domain.Enums;
using DatingApp.IdentityService.Domain.Repositories;
using DatingApp.IdentityService.Infrastructure.Events;
using DatingApp.IdentityService.Infrastructure.Services;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;

namespace DatingApp.IdentityService.Tests.Unit.Application;

public class AuthServiceTests
{
    private readonly Mock<IGoogleAuthService> _mockGoogleAuthService;
    private readonly Mock<IJwtTokenGenerator> _mockJwtTokenGenerator;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IRefreshTokenRepository> _mockRefreshTokenRepository;
    private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;
    private readonly Mock<ILogger<AuthService>> _mockLogger;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _mockGoogleAuthService = new Mock<IGoogleAuthService>();
        _mockJwtTokenGenerator = new Mock<IJwtTokenGenerator>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockRefreshTokenRepository = new Mock<IRefreshTokenRepository>();
        _mockPublishEndpoint = new Mock<IPublishEndpoint>();
        _mockLogger = new Mock<ILogger<AuthService>>();

        _authService = new AuthService(
            _mockGoogleAuthService.Object,
            _mockJwtTokenGenerator.Object,
            _mockUserRepository.Object,
            _mockRefreshTokenRepository.Object,
            _mockPublishEndpoint.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task LoginWithGoogleAsync_WithValidTokenAndNewUser_ShouldCreateUserAndReturnResponse()
    {
        // Arrange
        var idToken = "valid-google-id-token";
        var googleUserInfo = new GoogleUserInfo("google-sub-123", "test@example.com", "Test User", true);
        var userId = Guid.NewGuid();
        var accessToken = "generated-access-token";
        var refreshToken = "generated-refresh-token";
        var refreshTokenHash = "hashed-refresh-token";

        _mockGoogleAuthService
            .Setup(x => x.ValidateIdTokenAsync(idToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(googleUserInfo);

        _mockUserRepository
            .Setup(x => x.GetByExternalUserIdAsync(AuthProvider.Google, googleUserInfo.Sub, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null); // User doesn't exist

        _mockJwtTokenGenerator
            .Setup(x => x.GenerateAccessToken(It.IsAny<Guid>(), googleUserInfo.Email))
            .Returns(accessToken);

        _mockJwtTokenGenerator
            .Setup(x => x.GenerateRefreshToken())
            .Returns(refreshToken);

        _mockJwtTokenGenerator
            .Setup(x => x.HashToken(refreshToken))
            .Returns(refreshTokenHash);

        // Act
        var response = await _authService.LoginWithGoogleAsync(idToken);

        // Assert
        response.Should().NotBeNull();
        response!.AccessToken.Should().Be(accessToken);
        response.RefreshToken.Should().Be(refreshToken);
        response.ExpiresIn.Should().Be(900); // 15 minutes = 900 seconds
        response.TokenType.Should().Be("Bearer");
        response.Email.Should().Be(googleUserInfo.Email);
        response.IsNewUser.Should().BeTrue();

        // Verify user was created
        _mockUserRepository.Verify(
            x => x.AddAsync(It.Is<User>(u =>
                u.Email == googleUserInfo.Email &&
                u.Provider == AuthProvider.Google &&
                u.ExternalUserId == googleUserInfo.Sub
            ), It.IsAny<CancellationToken>()),
            Times.Once
        );

        // Verify refresh token was created
        _mockRefreshTokenRepository.Verify(
            x => x.AddAsync(It.Is<RefreshToken>(rt =>
                rt.TokenHash == refreshTokenHash &&
                !rt.IsRevoked
            ), It.IsAny<CancellationToken>()),
            Times.Once
        );

        // Verify UserRegistered event was published
        _mockPublishEndpoint.Verify(
            x => x.Publish(It.Is<UserRegistered>(e =>
                e.Email == googleUserInfo.Email &&
                e.Provider == AuthProvider.Google.ToString()
            ), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task LoginWithGoogleAsync_WithValidTokenAndExistingUser_ShouldUpdateLastLoginAndReturnResponse()
    {
        // Arrange
        var idToken = "valid-google-id-token";
        var googleUserInfo = new GoogleUserInfo("google-sub-123", "test@example.com", "Test User", true);
        var existingUser = User.CreateFromGoogle(googleUserInfo.Email, googleUserInfo.Sub);
        existingUser.RecordLogin(); // Simulate that user has logged in before
        var accessToken = "generated-access-token";
        var refreshToken = "generated-refresh-token";
        var refreshTokenHash = "hashed-refresh-token";

        _mockGoogleAuthService
            .Setup(x => x.ValidateIdTokenAsync(idToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(googleUserInfo);

        _mockUserRepository
            .Setup(x => x.GetByExternalUserIdAsync(AuthProvider.Google, googleUserInfo.Sub, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        _mockJwtTokenGenerator
            .Setup(x => x.GenerateAccessToken(existingUser.Id, googleUserInfo.Email))
            .Returns(accessToken);

        _mockJwtTokenGenerator
            .Setup(x => x.GenerateRefreshToken())
            .Returns(refreshToken);

        _mockJwtTokenGenerator
            .Setup(x => x.HashToken(refreshToken))
            .Returns(refreshTokenHash);

        // Act
        var response = await _authService.LoginWithGoogleAsync(idToken);

        // Assert
        response.Should().NotBeNull();
        response!.IsNewUser.Should().BeFalse();
        response.Email.Should().Be(googleUserInfo.Email);

        // Verify user was updated (RecordLogin called)
        _mockUserRepository.Verify(
            x => x.UpdateAsync(It.Is<User>(u => u.LastLoginAt != null), It.IsAny<CancellationToken>()),
            Times.Once
        );

        // Verify user was NOT created
        _mockUserRepository.Verify(
            x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never
        );

        // Verify UserRegistered event was NOT published for existing user
        _mockPublishEndpoint.Verify(
            x => x.Publish(It.IsAny<UserRegistered>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task LoginWithGoogleAsync_WithInvalidToken_ShouldReturnNull()
    {
        // Arrange
        var idToken = "invalid-google-id-token";

        _mockGoogleAuthService
            .Setup(x => x.ValidateIdTokenAsync(idToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GoogleUserInfo?)null); // Token validation failed

        // Act
        var response = await _authService.LoginWithGoogleAsync(idToken);

        // Assert
        response.Should().BeNull();

        // Verify no user operations occurred
        _mockUserRepository.Verify(
            x => x.GetByExternalUserIdAsync(It.IsAny<AuthProvider>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never
        );

        _mockUserRepository.Verify(
            x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task LoginWithGoogleAsync_ShouldGenerateCorrectJwtClaims()
    {
        // Arrange
        var idToken = "valid-google-id-token";
        var googleUserInfo = new GoogleUserInfo("google-sub-123", "test@example.com", "Test User", true);
        var user = User.CreateFromGoogle(googleUserInfo.Email, googleUserInfo.Sub);

        _mockGoogleAuthService
            .Setup(x => x.ValidateIdTokenAsync(idToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(googleUserInfo);

        _mockUserRepository
            .Setup(x => x.GetByExternalUserIdAsync(AuthProvider.Google, googleUserInfo.Sub, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockJwtTokenGenerator
            .Setup(x => x.GenerateAccessToken(It.IsAny<Guid>(), It.IsAny<string>()))
            .Returns("access-token");

        _mockJwtTokenGenerator
            .Setup(x => x.GenerateRefreshToken())
            .Returns("refresh-token");

        _mockJwtTokenGenerator
            .Setup(x => x.HashToken(It.IsAny<string>()))
            .Returns("hashed-token");

        // Act
        await _authService.LoginWithGoogleAsync(idToken);

        // Assert
        _mockJwtTokenGenerator.Verify(
            x => x.GenerateAccessToken(user.Id, googleUserInfo.Email),
            Times.Once,
            "Access token should be generated with correct userId and email"
        );
    }

    [Fact]
    public async Task LoginWithGoogleAsync_ShouldHashRefreshTokenBeforeStorage()
    {
        // Arrange
        var idToken = "valid-google-id-token";
        var googleUserInfo = new GoogleUserInfo("google-sub-123", "test@example.com", "Test User", true);
        var refreshToken = "plain-text-refresh-token";
        var refreshTokenHash = "hashed-refresh-token";

        _mockGoogleAuthService
            .Setup(x => x.ValidateIdTokenAsync(idToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(googleUserInfo);

        _mockUserRepository
            .Setup(x => x.GetByExternalUserIdAsync(AuthProvider.Google, googleUserInfo.Sub, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _mockJwtTokenGenerator
            .Setup(x => x.GenerateAccessToken(It.IsAny<Guid>(), It.IsAny<string>()))
            .Returns("access-token");

        _mockJwtTokenGenerator
            .Setup(x => x.GenerateRefreshToken())
            .Returns(refreshToken);

        _mockJwtTokenGenerator
            .Setup(x => x.HashToken(refreshToken))
            .Returns(refreshTokenHash);

        // Act
        await _authService.LoginWithGoogleAsync(idToken);

        // Assert
        _mockJwtTokenGenerator.Verify(
            x => x.HashToken(refreshToken),
            Times.Once,
            "Refresh token must be hashed before storage"
        );

        _mockRefreshTokenRepository.Verify(
            x => x.AddAsync(It.Is<RefreshToken>(rt =>
                rt.TokenHash == refreshTokenHash // Verify hashed token is stored
            ), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}
