using DatingApp.IdentityService.Domain.Entities;
using DatingApp.IdentityService.Domain.Enums;
using FluentAssertions;

namespace DatingApp.IdentityService.Tests.Unit.Domain;

public class UserTests
{
    [Fact]
    public void CreateFromGoogle_WithValidParameters_ShouldCreateUserWithCorrectProperties()
    {
        // Arrange
        var email = "test@example.com";
        var googleUserId = "google-sub-123456";

        // Act
        var user = User.CreateFromGoogle(email, googleUserId);

        // Assert
        user.Should().NotBeNull();
        user.Id.Should().NotBeEmpty();
        user.Email.Should().Be(email);
        user.Provider.Should().Be(AuthProvider.Google);
        user.ExternalUserId.Should().Be(googleUserId);
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        user.LastLoginAt.Should().BeNull(); // New user has never logged in
    }

    [Fact]
    public void CreateFromGoogle_WithEmptyEmail_ShouldThrowArgumentException()
    {
        // Arrange
        var email = "";
        var googleUserId = "google-sub-123456";

        // Act
        var act = () => User.CreateFromGoogle(email, googleUserId);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Email cannot be empty*");
    }

    [Fact]
    public void CreateFromGoogle_WithEmptyGoogleUserId_ShouldThrowArgumentException()
    {
        // Arrange
        var email = "test@example.com";
        var googleUserId = "";

        // Act
        var act = () => User.CreateFromGoogle(email, googleUserId);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Google user ID cannot be empty*");
    }

    [Fact]
    public void CreateFromApple_WithValidParameters_ShouldCreateUserWithCorrectProperties()
    {
        // Arrange
        var email = "test@example.com";
        var appleUserId = "apple-sub-123456";

        // Act
        var user = User.CreateFromApple(email, appleUserId);

        // Assert
        user.Should().NotBeNull();
        user.Id.Should().NotBeEmpty();
        user.Email.Should().Be(email);
        user.Provider.Should().Be(AuthProvider.Apple);
        user.ExternalUserId.Should().Be(appleUserId);
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        user.LastLoginAt.Should().BeNull();
    }

    [Fact]
    public void RecordLogin_ShouldUpdateLastLoginAt()
    {
        // Arrange
        var user = User.CreateFromGoogle("test@example.com", "google-sub-123456");
        user.LastLoginAt.Should().BeNull(); // Verify it's null initially

        // Act
        user.RecordLogin();

        // Assert
        user.LastLoginAt.Should().NotBeNull();
        user.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void RecordLogin_CalledMultipleTimes_ShouldUpdateToLatestTime()
    {
        // Arrange
        var user = User.CreateFromGoogle("test@example.com", "google-sub-123456");

        // Act
        user.RecordLogin();
        var firstLoginTime = user.LastLoginAt;

        Thread.Sleep(100); // Wait a bit to ensure time difference

        user.RecordLogin();
        var secondLoginTime = user.LastLoginAt;

        // Assert
        secondLoginTime.Should().BeAfter(firstLoginTime!.Value);
    }

    [Fact]
    public void IsNewUser_WhenLastLoginAtIsNull_ShouldReturnTrue()
    {
        // Arrange
        var user = User.CreateFromGoogle("test@example.com", "google-sub-123456");

        // Act
        var isNewUser = user.IsNewUser();

        // Assert
        isNewUser.Should().BeTrue();
    }

    [Fact]
    public void IsNewUser_WhenLastLoginAtIsSet_ShouldReturnFalse()
    {
        // Arrange
        var user = User.CreateFromGoogle("test@example.com", "google-sub-123456");
        user.RecordLogin();

        // Act
        var isNewUser = user.IsNewUser();

        // Assert
        isNewUser.Should().BeFalse();
    }
}
