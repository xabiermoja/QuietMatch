using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DatingApp.IdentityService.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace DatingApp.IdentityService.Tests.Unit.Infrastructure;

public class JwtTokenGeneratorTests
{
    private readonly IConfiguration _validConfiguration;

    public JwtTokenGeneratorTests()
    {
        _validConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Jwt:SecretKey", "ThisIsAVerySecretKeyThatIsAtLeast256BitsLongForHmacSha256Signing" },
                { "Jwt:Issuer", "https://quietmatch.com" },
                { "Jwt:Audience", "https://api.quietmatch.com" },
                { "Jwt:AccessTokenExpiryMinutes", "15" }
            })
            .Build();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new JwtTokenGenerator(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    [Fact]
    public void Constructor_WithMissingSecretKey_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Jwt:Issuer", "https://quietmatch.com" },
                { "Jwt:Audience", "https://api.quietmatch.com" }
            })
            .Build();

        // Act
        var act = () => new JwtTokenGenerator(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt:SecretKey is not configured*");
    }

    [Fact]
    public void Constructor_WithMissingIssuer_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Jwt:SecretKey", "ThisIsAVerySecretKeyThatIsAtLeast256BitsLongForHmacSha256Signing" },
                { "Jwt:Audience", "https://api.quietmatch.com" }
            })
            .Build();

        // Act
        var act = () => new JwtTokenGenerator(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt:Issuer is not configured*");
    }

    [Fact]
    public void Constructor_WithMissingAudience_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Jwt:SecretKey", "ThisIsAVerySecretKeyThatIsAtLeast256BitsLongForHmacSha256Signing" },
                { "Jwt:Issuer", "https://quietmatch.com" }
            })
            .Build();

        // Act
        var act = () => new JwtTokenGenerator(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt:Audience is not configured*");
    }

    [Fact]
    public void Constructor_WithMissingAccessTokenExpiryMinutes_ShouldUseDefault15Minutes()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Jwt:SecretKey", "ThisIsAVerySecretKeyThatIsAtLeast256BitsLongForHmacSha256Signing" },
                { "Jwt:Issuer", "https://quietmatch.com" },
                { "Jwt:Audience", "https://api.quietmatch.com" }
                // AccessTokenExpiryMinutes is missing - should default to 15
            })
            .Build();

        // Act
        var generator = new JwtTokenGenerator(configuration);
        var token = generator.GenerateAccessToken(Guid.NewGuid(), "test@example.com");

        // Assert - parse token and check expiry
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var expiryTime = jwtToken.ValidTo;
        var now = DateTime.UtcNow;

        // Should expire in approximately 15 minutes (allow 1 minute tolerance)
        expiryTime.Should().BeCloseTo(now.AddMinutes(15), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void Constructor_WithValidConfiguration_ShouldCreateInstance()
    {
        // Act
        var generator = new JwtTokenGenerator(_validConfiguration);

        // Assert
        generator.Should().NotBeNull();
    }

    #endregion

    #region GenerateAccessToken Tests

    [Fact]
    public void GenerateAccessToken_WithEmptyUserId_ShouldThrowArgumentException()
    {
        // Arrange
        var generator = new JwtTokenGenerator(_validConfiguration);

        // Act
        var act = () => generator.GenerateAccessToken(Guid.Empty, "test@example.com");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*User ID cannot be empty*")
            .WithParameterName("userId");
    }

    [Fact]
    public void GenerateAccessToken_WithNullEmail_ShouldThrowArgumentException()
    {
        // Arrange
        var generator = new JwtTokenGenerator(_validConfiguration);

        // Act
        var act = () => generator.GenerateAccessToken(Guid.NewGuid(), null!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Email cannot be empty*")
            .WithParameterName("email");
    }

    [Fact]
    public void GenerateAccessToken_WithEmptyEmail_ShouldThrowArgumentException()
    {
        // Arrange
        var generator = new JwtTokenGenerator(_validConfiguration);

        // Act
        var act = () => generator.GenerateAccessToken(Guid.NewGuid(), "");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Email cannot be empty*")
            .WithParameterName("email");
    }

    [Fact]
    public void GenerateAccessToken_WithValidParameters_ShouldReturnJwtToken()
    {
        // Arrange
        var generator = new JwtTokenGenerator(_validConfiguration);
        var userId = Guid.NewGuid();
        var email = "test@example.com";

        // Act
        var token = generator.GenerateAccessToken(userId, email);

        // Assert
        token.Should().NotBeNullOrWhiteSpace();

        // Verify it's a valid JWT (3 parts separated by dots)
        var parts = token.Split('.');
        parts.Should().HaveCount(3, "JWT should have header.payload.signature format");
    }

    [Fact]
    public void GenerateAccessToken_WithValidParameters_ShouldIncludeCorrectClaims()
    {
        // Arrange
        var generator = new JwtTokenGenerator(_validConfiguration);
        var userId = Guid.NewGuid();
        var email = "test@example.com";

        // Act
        var token = generator.GenerateAccessToken(userId, email);

        // Assert - parse and verify claims
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Verify subject (userId)
        var subClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
        subClaim.Should().NotBeNull();
        subClaim!.Value.Should().Be(userId.ToString());

        // Verify email
        var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email);
        emailClaim.Should().NotBeNull();
        emailClaim!.Value.Should().Be(email);

        // Verify JTI (unique token ID) exists
        var jtiClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti);
        jtiClaim.Should().NotBeNull();
        jtiClaim!.Value.Should().NotBeNullOrWhiteSpace();

        // Verify IAT (issued at) exists
        var iatClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Iat);
        iatClaim.Should().NotBeNull();
    }

    [Fact]
    public void GenerateAccessToken_WithValidParameters_ShouldHaveCorrectIssuerAndAudience()
    {
        // Arrange
        var generator = new JwtTokenGenerator(_validConfiguration);
        var userId = Guid.NewGuid();
        var email = "test@example.com";

        // Act
        var token = generator.GenerateAccessToken(userId, email);

        // Assert - parse and verify issuer/audience
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Issuer.Should().Be("https://quietmatch.com");
        jwtToken.Audiences.Should().Contain("https://api.quietmatch.com");
    }

    [Fact]
    public void GenerateAccessToken_WithValidParameters_ShouldExpireIn15Minutes()
    {
        // Arrange
        var generator = new JwtTokenGenerator(_validConfiguration);
        var userId = Guid.NewGuid();
        var email = "test@example.com";

        // Act
        var token = generator.GenerateAccessToken(userId, email);

        // Assert - parse and verify expiry
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var expiryTime = jwtToken.ValidTo;
        var now = DateTime.UtcNow;

        // Should expire in approximately 15 minutes (allow 1 minute tolerance for test execution)
        expiryTime.Should().BeCloseTo(now.AddMinutes(15), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void GenerateAccessToken_CalledMultipleTimes_ShouldGenerateUniqueTokens()
    {
        // Arrange
        var generator = new JwtTokenGenerator(_validConfiguration);
        var userId = Guid.NewGuid();
        var email = "test@example.com";

        // Act
        var token1 = generator.GenerateAccessToken(userId, email);
        var token2 = generator.GenerateAccessToken(userId, email);

        // Assert - tokens should be different due to different JTI and IAT
        token1.Should().NotBe(token2);
    }

    #endregion

    #region GenerateRefreshToken Tests

    [Fact]
    public void GenerateRefreshToken_ShouldReturnNonEmptyString()
    {
        // Arrange
        var generator = new JwtTokenGenerator(_validConfiguration);

        // Act
        var refreshToken = generator.GenerateRefreshToken();

        // Assert
        refreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnBase64EncodedString()
    {
        // Arrange
        var generator = new JwtTokenGenerator(_validConfiguration);

        // Act
        var refreshToken = generator.GenerateRefreshToken();

        // Assert - should be valid Base64
        var act = () => Convert.FromBase64String(refreshToken);
        act.Should().NotThrow();
    }

    [Fact]
    public void GenerateRefreshToken_ShouldGenerate32ByteToken()
    {
        // Arrange
        var generator = new JwtTokenGenerator(_validConfiguration);

        // Act
        var refreshToken = generator.GenerateRefreshToken();
        var bytes = Convert.FromBase64String(refreshToken);

        // Assert - 32 bytes of random data
        bytes.Should().HaveCount(32);
    }

    [Fact]
    public void GenerateRefreshToken_CalledMultipleTimes_ShouldGenerateUniqueTokens()
    {
        // Arrange
        var generator = new JwtTokenGenerator(_validConfiguration);

        // Act
        var token1 = generator.GenerateRefreshToken();
        var token2 = generator.GenerateRefreshToken();
        var token3 = generator.GenerateRefreshToken();

        // Assert - all tokens should be unique
        var tokens = new[] { token1, token2, token3 };
        tokens.Distinct().Should().HaveCount(3);
    }

    #endregion

    #region HashToken Tests

    [Fact]
    public void HashToken_WithNullToken_ShouldThrowArgumentException()
    {
        // Arrange
        var generator = new JwtTokenGenerator(_validConfiguration);

        // Act
        var act = () => generator.HashToken(null!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Token cannot be empty*")
            .WithParameterName("token");
    }

    [Fact]
    public void HashToken_WithEmptyToken_ShouldThrowArgumentException()
    {
        // Arrange
        var generator = new JwtTokenGenerator(_validConfiguration);

        // Act
        var act = () => generator.HashToken("");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Token cannot be empty*")
            .WithParameterName("token");
    }

    [Fact]
    public void HashToken_WithWhitespaceToken_ShouldThrowArgumentException()
    {
        // Arrange
        var generator = new JwtTokenGenerator(_validConfiguration);

        // Act
        var act = () => generator.HashToken("   ");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Token cannot be empty*")
            .WithParameterName("token");
    }

    [Fact]
    public void HashToken_WithValidToken_ShouldReturnNonEmptyHash()
    {
        // Arrange
        var generator = new JwtTokenGenerator(_validConfiguration);
        var token = "sample-refresh-token";

        // Act
        var hash = generator.HashToken(token);

        // Assert
        hash.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HashToken_WithValidToken_ShouldReturnBase64EncodedHash()
    {
        // Arrange
        var generator = new JwtTokenGenerator(_validConfiguration);
        var token = "sample-refresh-token";

        // Act
        var hash = generator.HashToken(token);

        // Assert - should be valid Base64
        var act = () => Convert.FromBase64String(hash);
        act.Should().NotThrow();
    }

    [Fact]
    public void HashToken_WithValidToken_ShouldReturnSha256Hash()
    {
        // Arrange
        var generator = new JwtTokenGenerator(_validConfiguration);
        var token = "sample-refresh-token";

        // Act
        var hash = generator.HashToken(token);
        var hashBytes = Convert.FromBase64String(hash);

        // Assert - SHA-256 produces 32 bytes
        hashBytes.Should().HaveCount(32);
    }

    [Fact]
    public void HashToken_WithSameToken_ShouldProduceSameHash()
    {
        // Arrange
        var generator = new JwtTokenGenerator(_validConfiguration);
        var token = "sample-refresh-token";

        // Act
        var hash1 = generator.HashToken(token);
        var hash2 = generator.HashToken(token);

        // Assert - hashing is deterministic
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void HashToken_WithDifferentTokens_ShouldProduceDifferentHashes()
    {
        // Arrange
        var generator = new JwtTokenGenerator(_validConfiguration);
        var token1 = "sample-refresh-token-1";
        var token2 = "sample-refresh-token-2";

        // Act
        var hash1 = generator.HashToken(token1);
        var hash2 = generator.HashToken(token2);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    #endregion
}
