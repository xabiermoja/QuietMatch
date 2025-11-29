using DatingApp.NotificationService.Core.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace DatingApp.NotificationService.Tests.Unit.Domain.ValueObjects;

/// <summary>
/// Tests for Recipient value object.
/// </summary>
/// <remarks>
/// HEXAGONAL ARCHITECTURE TESTING:
/// - Value objects are in the CORE/DOMAIN
/// - They have ZERO external dependencies
/// - NO MOCKING NEEDED! Pure business logic testing
///
/// What we're testing:
/// - Email validation rules
/// - Email normalization (lowercase, trimmed)
/// - Name handling
/// </remarks>
public class RecipientTests
{
    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name+tag@example.co.uk")]
    [InlineData("admin@subdomain.example.com")]
    [InlineData("test123@test-domain.com")]
    public void Recipient_WithValidEmail_ShouldCreate(string email)
    {
        // Arrange & Act
        var recipient = new Recipient(email, "John Doe");

        // Assert
        recipient.Email.Should().NotBeNullOrEmpty();
        recipient.Name.Should().Be("John Doe");
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing-at-sign.com")]
    [InlineData("@no-local-part.com")]
    [InlineData("no-domain@")]
    public void Recipient_WithInvalidEmail_ShouldThrowArgumentException(string invalidEmail)
    {
        // Arrange & Act
        var act = () => new Recipient(invalidEmail, "John Doe");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid email format*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Recipient_WithEmptyEmail_ShouldThrowArgumentException(string invalidEmail)
    {
        // Arrange & Act
        var act = () => new Recipient(invalidEmail, "John Doe");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Email is required*");
    }

    [Fact]
    public void Recipient_ShouldNormalizeEmail_ToLowerCase()
    {
        // Arrange & Act
        var recipient = new Recipient("Test@EXAMPLE.COM", "John");

        // Assert - email should be normalized to lowercase
        recipient.Email.Should().Be("test@example.com");
    }

    [Fact]
    public void Recipient_ShouldTrimEmail()
    {
        // Arrange & Act
        var recipient = new Recipient("  test@example.com  ", "John");

        // Assert - whitespace should be trimmed
        recipient.Email.Should().Be("test@example.com");
    }

    [Fact]
    public void Recipient_ShouldTrimName()
    {
        // Arrange & Act
        var recipient = new Recipient("test@example.com", "  John Doe  ");

        // Assert - name should be trimmed
        recipient.Name.Should().Be("John Doe");
    }

    [Fact]
    public void Recipient_WithEmptyName_ShouldStillCreate()
    {
        // Arrange & Act
        var recipient = new Recipient("test@example.com", "");

        // Assert - empty name is allowed (some users might not have names)
        recipient.Email.Should().Be("test@example.com");
        recipient.Name.Should().BeEmpty();
    }
}
