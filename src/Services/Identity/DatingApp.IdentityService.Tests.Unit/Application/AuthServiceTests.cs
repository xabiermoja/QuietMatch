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

    #region RefreshTokenAsync Tests

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ShouldReturnNewTokensAndRotate()
    {
        // Arrange
        var plainTextRefreshToken = "plain-text-refresh-token";
        var refreshTokenHash = "hashed-refresh-token";
        var userId = Guid.NewGuid();
        var user = User.CreateFromGoogle("test@example.com", "google-sub-123");
        var storedRefreshToken = RefreshToken.Create(userId, refreshTokenHash, validityDays: 7);

        var newAccessToken = "new-access-token";
        var newRefreshToken = "new-refresh-token";
        var newRefreshTokenHash = "new-hashed-refresh-token";

        _mockJwtTokenGenerator
            .Setup(x => x.HashToken(plainTextRefreshToken))
            .Returns(refreshTokenHash);

        _mockRefreshTokenRepository
            .Setup(x => x.GetByTokenHashAsync(refreshTokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedRefreshToken);

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockJwtTokenGenerator
            .Setup(x => x.GenerateAccessToken(user.Id, user.Email))
            .Returns(newAccessToken);

        _mockJwtTokenGenerator
            .Setup(x => x.GenerateRefreshToken())
            .Returns(newRefreshToken);

        _mockJwtTokenGenerator
            .Setup(x => x.HashToken(newRefreshToken))
            .Returns(newRefreshTokenHash);

        // Act
        var response = await _authService.RefreshTokenAsync(plainTextRefreshToken);

        // Assert
        response.Should().NotBeNull();
        response!.AccessToken.Should().Be(newAccessToken);
        response.RefreshToken.Should().Be(newRefreshToken);
        response.ExpiresIn.Should().Be(900); // 15 minutes = 900 seconds
        response.TokenType.Should().Be("Bearer");

        // Verify old token was revoked
        _mockRefreshTokenRepository.Verify(
            x => x.UpdateAsync(It.Is<RefreshToken>(rt =>
                rt.IsRevoked == true &&
                rt.RevokedAt != null
            ), It.IsAny<CancellationToken>()),
            Times.Once,
            "Old refresh token must be revoked"
        );

        // Verify new refresh token was created
        _mockRefreshTokenRepository.Verify(
            x => x.AddAsync(It.Is<RefreshToken>(rt =>
                rt.TokenHash == newRefreshTokenHash &&
                rt.UserId == user.Id &&
                !rt.IsRevoked
            ), It.IsAny<CancellationToken>()),
            Times.Once,
            "New refresh token must be created"
        );
    }

    [Fact]
    public async Task RefreshTokenAsync_WithInvalidToken_ShouldReturnNull()
    {
        // Arrange
        var plainTextRefreshToken = "invalid-refresh-token";
        var refreshTokenHash = "hashed-invalid-token";

        _mockJwtTokenGenerator
            .Setup(x => x.HashToken(plainTextRefreshToken))
            .Returns(refreshTokenHash);

        _mockRefreshTokenRepository
            .Setup(x => x.GetByTokenHashAsync(refreshTokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null); // Token not found in database

        // Act
        var response = await _authService.RefreshTokenAsync(plainTextRefreshToken);

        // Assert
        response.Should().BeNull("Invalid token should not be accepted");

        // Verify no tokens were generated
        _mockJwtTokenGenerator.Verify(
            x => x.GenerateAccessToken(It.IsAny<Guid>(), It.IsAny<string>()),
            Times.Never
        );

        _mockJwtTokenGenerator.Verify(
            x => x.GenerateRefreshToken(),
            Times.Never
        );

        // Verify no database operations occurred
        _mockRefreshTokenRepository.Verify(
            x => x.UpdateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
            Times.Never
        );

        _mockRefreshTokenRepository.Verify(
            x => x.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task RefreshTokenAsync_WithExpiredToken_ShouldReturnNull()
    {
        // Arrange
        var plainTextRefreshToken = "expired-refresh-token";
        var refreshTokenHash = "hashed-expired-token";
        var userId = Guid.NewGuid();

        // Create a valid token first, then manually set expiry to past using reflection
        var expiredToken = RefreshToken.Create(userId, refreshTokenHash, validityDays: 7);

        // Use reflection to set ExpiresAt to the past (simulating an expired token)
        var expiresAtProperty = typeof(RefreshToken).GetProperty("ExpiresAt");
        expiresAtProperty!.SetValue(expiredToken, DateTime.UtcNow.AddDays(-1));

        _mockJwtTokenGenerator
            .Setup(x => x.HashToken(plainTextRefreshToken))
            .Returns(refreshTokenHash);

        _mockRefreshTokenRepository
            .Setup(x => x.GetByTokenHashAsync(refreshTokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredToken);

        // Act
        var response = await _authService.RefreshTokenAsync(plainTextRefreshToken);

        // Assert
        response.Should().BeNull("Expired token should not be accepted");
        expiredToken.IsExpired().Should().BeTrue("Token should be expired");

        // Verify no tokens were generated
        _mockJwtTokenGenerator.Verify(
            x => x.GenerateAccessToken(It.IsAny<Guid>(), It.IsAny<string>()),
            Times.Never
        );

        // Verify no database operations occurred
        _mockRefreshTokenRepository.Verify(
            x => x.UpdateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
            Times.Never
        );

        _mockRefreshTokenRepository.Verify(
            x => x.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task RefreshTokenAsync_WithRevokedToken_ShouldReturnNull()
    {
        // Arrange
        var plainTextRefreshToken = "revoked-refresh-token";
        var refreshTokenHash = "hashed-revoked-token";
        var userId = Guid.NewGuid();

        var revokedToken = RefreshToken.Create(userId, refreshTokenHash, validityDays: 7);
        revokedToken.Revoke(); // Revoke the token

        _mockJwtTokenGenerator
            .Setup(x => x.HashToken(plainTextRefreshToken))
            .Returns(refreshTokenHash);

        _mockRefreshTokenRepository
            .Setup(x => x.GetByTokenHashAsync(refreshTokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(revokedToken);

        // Act
        var response = await _authService.RefreshTokenAsync(plainTextRefreshToken);

        // Assert
        response.Should().BeNull("Revoked token should not be accepted");

        // Verify no tokens were generated
        _mockJwtTokenGenerator.Verify(
            x => x.GenerateAccessToken(It.IsAny<Guid>(), It.IsAny<string>()),
            Times.Never
        );

        // Verify no new database operations occurred (token is already revoked)
        _mockRefreshTokenRepository.Verify(
            x => x.UpdateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
            Times.Never
        );

        _mockRefreshTokenRepository.Verify(
            x => x.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidTokenButUserNotFound_ShouldReturnNull()
    {
        // Arrange
        var plainTextRefreshToken = "valid-refresh-token";
        var refreshTokenHash = "hashed-refresh-token";
        var userId = Guid.NewGuid();
        var storedRefreshToken = RefreshToken.Create(userId, refreshTokenHash, validityDays: 7);

        _mockJwtTokenGenerator
            .Setup(x => x.HashToken(plainTextRefreshToken))
            .Returns(refreshTokenHash);

        _mockRefreshTokenRepository
            .Setup(x => x.GetByTokenHashAsync(refreshTokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedRefreshToken);

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null); // User not found (edge case)

        // Act
        var response = await _authService.RefreshTokenAsync(plainTextRefreshToken);

        // Assert
        response.Should().BeNull("Should return null when user not found");

        // Verify no tokens were generated
        _mockJwtTokenGenerator.Verify(
            x => x.GenerateAccessToken(It.IsAny<Guid>(), It.IsAny<string>()),
            Times.Never
        );

        _mockJwtTokenGenerator.Verify(
            x => x.GenerateRefreshToken(),
            Times.Never
        );
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldHashIncomingTokenForLookup()
    {
        // Arrange
        var plainTextRefreshToken = "plain-text-token";
        var refreshTokenHash = "expected-hash";

        _mockJwtTokenGenerator
            .Setup(x => x.HashToken(plainTextRefreshToken))
            .Returns(refreshTokenHash);

        _mockRefreshTokenRepository
            .Setup(x => x.GetByTokenHashAsync(refreshTokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        // Act
        await _authService.RefreshTokenAsync(plainTextRefreshToken);

        // Assert
        _mockJwtTokenGenerator.Verify(
            x => x.HashToken(plainTextRefreshToken),
            Times.Once,
            "Incoming refresh token must be hashed before database lookup"
        );

        _mockRefreshTokenRepository.Verify(
            x => x.GetByTokenHashAsync(refreshTokenHash, It.IsAny<CancellationToken>()),
            Times.Once,
            "Should look up token by its hash"
        );
    }

    #endregion

    #region RevokeTokenAsync Tests

    [Fact]
    public async Task RevokeTokenAsync_WithValidToken_ShouldRevokeTokenAndReturnTrue()
    {
        // Arrange
        var plainTextRefreshToken = "valid-refresh-token";
        var refreshTokenHash = "hashed-refresh-token";
        var userId = Guid.NewGuid();
        var validToken = RefreshToken.Create(userId, refreshTokenHash, validityDays: 7);

        _mockJwtTokenGenerator
            .Setup(x => x.HashToken(plainTextRefreshToken))
            .Returns(refreshTokenHash);

        _mockRefreshTokenRepository
            .Setup(x => x.GetByTokenHashAsync(refreshTokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validToken);

        // Act
        var result = await _authService.RevokeTokenAsync(plainTextRefreshToken);

        // Assert
        result.Should().BeTrue("Valid token should be revoked successfully");
        validToken.IsRevoked.Should().BeTrue("Token should be marked as revoked");
        validToken.RevokedAt.Should().NotBeNull("RevokedAt should be set");

        // Verify token was updated in database
        _mockRefreshTokenRepository.Verify(
            x => x.UpdateAsync(It.Is<RefreshToken>(rt =>
                rt.IsRevoked == true &&
                rt.RevokedAt != null
            ), It.IsAny<CancellationToken>()),
            Times.Once,
            "Token should be updated with revoked status"
        );
    }

    [Fact]
    public async Task RevokeTokenAsync_WithNonExistentToken_ShouldBeIdempotentAndReturnTrue()
    {
        // Arrange
        var plainTextRefreshToken = "non-existent-token";
        var refreshTokenHash = "hashed-non-existent-token";

        _mockJwtTokenGenerator
            .Setup(x => x.HashToken(plainTextRefreshToken))
            .Returns(refreshTokenHash);

        _mockRefreshTokenRepository
            .Setup(x => x.GetByTokenHashAsync(refreshTokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null); // Token not found

        // Act
        var result = await _authService.RevokeTokenAsync(plainTextRefreshToken);

        // Assert
        result.Should().BeTrue("Non-existent token should be treated as already revoked (idempotent)");

        // Verify no database updates occurred
        _mockRefreshTokenRepository.Verify(
            x => x.UpdateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "No update should occur for non-existent token"
        );
    }

    [Fact]
    public async Task RevokeTokenAsync_WithAlreadyRevokedToken_ShouldBeIdempotentAndReturnTrue()
    {
        // Arrange
        var plainTextRefreshToken = "already-revoked-token";
        var refreshTokenHash = "hashed-revoked-token";
        var userId = Guid.NewGuid();
        var revokedToken = RefreshToken.Create(userId, refreshTokenHash, validityDays: 7);
        revokedToken.Revoke(); // Already revoked

        _mockJwtTokenGenerator
            .Setup(x => x.HashToken(plainTextRefreshToken))
            .Returns(refreshTokenHash);

        _mockRefreshTokenRepository
            .Setup(x => x.GetByTokenHashAsync(refreshTokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(revokedToken);

        // Act
        var result = await _authService.RevokeTokenAsync(plainTextRefreshToken);

        // Assert
        result.Should().BeTrue("Already-revoked token should return success (idempotent)");

        // Verify no database updates occurred (token is already revoked)
        _mockRefreshTokenRepository.Verify(
            x => x.UpdateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "No update should occur for already-revoked token"
        );
    }

    [Fact]
    public async Task RevokeTokenAsync_ShouldHashIncomingTokenForLookup()
    {
        // Arrange
        var plainTextRefreshToken = "plain-text-token";
        var refreshTokenHash = "expected-hash";

        _mockJwtTokenGenerator
            .Setup(x => x.HashToken(plainTextRefreshToken))
            .Returns(refreshTokenHash);

        _mockRefreshTokenRepository
            .Setup(x => x.GetByTokenHashAsync(refreshTokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        // Act
        await _authService.RevokeTokenAsync(plainTextRefreshToken);

        // Assert
        _mockJwtTokenGenerator.Verify(
            x => x.HashToken(plainTextRefreshToken),
            Times.Once,
            "Incoming refresh token must be hashed before database lookup"
        );

        _mockRefreshTokenRepository.Verify(
            x => x.GetByTokenHashAsync(refreshTokenHash, It.IsAny<CancellationToken>()),
            Times.Once,
            "Should look up token by its hash"
        );
    }

    #endregion

    #region RevokeAllTokensForUserAsync Tests

    [Fact]
    public async Task RevokeAllTokensForUserAsync_WithActiveTokens_ShouldRevokeAllAndReturnCount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token1 = RefreshToken.Create(userId, "hash1", validityDays: 7);
        var token2 = RefreshToken.Create(userId, "hash2", validityDays: 7);
        var token3 = RefreshToken.Create(userId, "hash3", validityDays: 7);
        var activeTokens = new List<RefreshToken> { token1, token2, token3 };

        _mockRefreshTokenRepository
            .Setup(x => x.GetActiveByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeTokens);

        // Act
        var result = await _authService.RevokeAllTokensForUserAsync(userId);

        // Assert
        result.Should().Be(3, "Should return count of revoked tokens");

        // Verify RevokeAllByUserIdAsync was called
        _mockRefreshTokenRepository.Verify(
            x => x.RevokeAllByUserIdAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once,
            "Should revoke all tokens for the user"
        );
    }

    [Fact]
    public async Task RevokeAllTokensForUserAsync_WithNoActiveTokens_ShouldReturnZero()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var emptyTokenList = new List<RefreshToken>();

        _mockRefreshTokenRepository
            .Setup(x => x.GetActiveByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyTokenList);

        // Act
        var result = await _authService.RevokeAllTokensForUserAsync(userId);

        // Assert
        result.Should().Be(0, "Should return 0 when no active tokens exist");

        // Verify RevokeAllByUserIdAsync was NOT called (optimization)
        _mockRefreshTokenRepository.Verify(
            x => x.RevokeAllByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "Should not attempt to revoke when no active tokens exist"
        );
    }

    [Fact]
    public async Task RevokeAllTokensForUserAsync_WithSingleToken_ShouldRevokeAndReturnOne()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = RefreshToken.Create(userId, "hash1", validityDays: 7);
        var activeTokens = new List<RefreshToken> { token };

        _mockRefreshTokenRepository
            .Setup(x => x.GetActiveByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeTokens);

        // Act
        var result = await _authService.RevokeAllTokensForUserAsync(userId);

        // Assert
        result.Should().Be(1, "Should return 1 when single token is revoked");

        _mockRefreshTokenRepository.Verify(
            x => x.RevokeAllByUserIdAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    #endregion
}
