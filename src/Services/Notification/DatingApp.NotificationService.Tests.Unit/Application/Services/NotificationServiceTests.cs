using DatingApp.NotificationService.Core.Domain.ValueObjects;
using DatingApp.NotificationService.Core.Ports;
using FluentAssertions;
using Moq;
using Xunit;
using ApplicationServices = DatingApp.NotificationService.Core.Application.Services;

namespace DatingApp.NotificationService.Tests.Unit.Application.Services;

/// <summary>
/// Tests for NotificationService application service.
/// </summary>
/// <remarks>
/// HEXAGONAL ARCHITECTURE TESTING - THIS IS THE KEY PATTERN!
///
/// Application Service Testing Rules:
/// 1. Mock ALL ports (IEmailProvider, ISmsProvider, ITemplateProvider, INotificationLogger)
/// 2. Test orchestration logic (does service call ports correctly?)
/// 3. Test error handling
/// 4. DON'T test adapter implementation (that's tested separately)
///
/// Why This Works:
/// - Service depends ONLY on ports (interfaces)
/// - Ports are trivial to mock
/// - Tests verify domain logic, not infrastructure
/// - Can swap real adapters without changing tests
///
/// This demonstrates the **testability** benefit of Hexagonal Architecture!
/// </remarks>
public class NotificationServiceTests
{
    private readonly Mock<IEmailProvider> _mockEmailProvider;
    private readonly Mock<ISmsProvider> _mockSmsProvider;
    private readonly Mock<ITemplateProvider> _mockTemplateProvider;
    private readonly Mock<INotificationLogger<ApplicationServices.NotificationService>> _mockLogger;
    private readonly ApplicationServices.NotificationService _service;

    public NotificationServiceTests()
    {
        // Arrange - create mocks for all ports
        _mockEmailProvider = new Mock<IEmailProvider>();
        _mockSmsProvider = new Mock<ISmsProvider>();
        _mockTemplateProvider = new Mock<ITemplateProvider>();
        _mockLogger = new Mock<INotificationLogger<ApplicationServices.NotificationService>>();

        // Create service with mocked ports
        _service = new ApplicationServices.NotificationService(
            _mockEmailProvider.Object,
            _mockSmsProvider.Object,
            _mockTemplateProvider.Object,
            _mockLogger.Object);
    }

    #region SendWelcomeEmailAsync Tests

    [Fact]
    public async Task SendWelcomeEmailAsync_WithValidInput_ShouldCallTemplateProvider()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "john@example.com";
        var name = "John";

        _mockTemplateProvider
            .Setup(x => x.RenderAsync("WelcomeEmail", It.IsAny<object>()))
            .ReturnsAsync("<html>Welcome John!</html>");

        _mockEmailProvider
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>()))
            .ReturnsAsync(true);

        // Act
        await _service.SendWelcomeEmailAsync(userId, email, name);

        // Assert - verify template provider was called with correct template name
        _mockTemplateProvider.Verify(
            x => x.RenderAsync("WelcomeEmail", It.Is<object>(
                data => data.GetType().GetProperty("Name")!.GetValue(data)!.ToString() == "John")),
            Times.Once,
            "Service should call template provider with WelcomeEmail template and user's name");
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_WithValidInput_ShouldCallEmailProvider()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "john@example.com";
        var name = "John";

        _mockTemplateProvider
            .Setup(x => x.RenderAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync("<html>Welcome!</html>");

        _mockEmailProvider
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>()))
            .ReturnsAsync(true);

        // Act
        await _service.SendWelcomeEmailAsync(userId, email, name);

        // Assert - verify email provider was called with correct email details
        _mockEmailProvider.Verify(
            x => x.SendAsync(It.Is<EmailMessage>(
                m => m.To.Email == "john@example.com" &&
                     m.To.Name == "John" &&
                     m.Subject == "Welcome to QuietMatch! 🎉" &&
                     m.Body == "<html>Welcome!</html>")),
            Times.Once,
            "Service should call email provider with correct email message");
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_WhenEmailSentSuccessfully_ShouldReturnTrue()
    {
        // Arrange
        _mockTemplateProvider
            .Setup(x => x.RenderAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync("<html>Welcome!</html>");

        _mockEmailProvider
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.SendWelcomeEmailAsync(
            Guid.NewGuid(),
            "test@example.com",
            "Test");

        // Assert
        result.Should().BeTrue("Email was sent successfully");
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_WhenEmailSendingFails_ShouldReturnFalse()
    {
        // Arrange
        _mockTemplateProvider
            .Setup(x => x.RenderAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync("<html>Welcome!</html>");

        _mockEmailProvider
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.SendWelcomeEmailAsync(
            Guid.NewGuid(),
            "test@example.com",
            "Test");

        // Assert
        result.Should().BeFalse("Email sending failed");
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_WithNullName_ShouldUseEmailAsName()
    {
        // Arrange
        var email = "john@example.com";

        _mockTemplateProvider
            .Setup(x => x.RenderAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync("<html>Welcome!</html>");

        _mockEmailProvider
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>()))
            .ReturnsAsync(true);

        // Act
        await _service.SendWelcomeEmailAsync(Guid.NewGuid(), email, null);

        // Assert - should use email as fallback for name
        _mockEmailProvider.Verify(
            x => x.SendAsync(It.Is<EmailMessage>(
                m => m.To.Name == email)),
            Times.Once,
            "When name is null, should use email as display name");
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_WhenExceptionThrown_ShouldReturnFalseAndLogError()
    {
        // Arrange
        _mockTemplateProvider
            .Setup(x => x.RenderAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ThrowsAsync(new Exception("Template not found"));

        // Act
        var result = await _service.SendWelcomeEmailAsync(
            Guid.NewGuid(),
            "test@example.com",
            "Test");

        // Assert
        result.Should().BeFalse("Exception occurred");

        _mockLogger.Verify(
            x => x.LogError(
                It.IsAny<Exception>(),
                It.IsAny<string>(),
                It.IsAny<object[]>()),
            Times.Once,
            "Error should be logged");
    }

    #endregion

    #region SendSmsAsync Tests

    [Fact]
    public async Task SendSmsAsync_WithValidInput_ShouldCallSmsProvider()
    {
        // Arrange
        var phoneNumber = "+12025551234";
        var message = "Your verification code is: 123456";

        _mockSmsProvider
            .Setup(x => x.SendAsync(It.IsAny<SmsMessage>()))
            .ReturnsAsync(true);

        // Act
        await _service.SendSmsAsync(phoneNumber, message);

        // Assert
        _mockSmsProvider.Verify(
            x => x.SendAsync(It.Is<SmsMessage>(
                m => m.PhoneNumber == phoneNumber &&
                     m.Message == message)),
            Times.Once,
            "Service should call SMS provider with correct message");
    }

    [Fact]
    public async Task SendSmsAsync_WhenSmsSentSuccessfully_ShouldReturnTrue()
    {
        // Arrange
        _mockSmsProvider
            .Setup(x => x.SendAsync(It.IsAny<SmsMessage>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.SendSmsAsync("+12025551234", "Test message");

        // Assert
        result.Should().BeTrue("SMS was sent successfully");
    }

    [Fact]
    public async Task SendSmsAsync_WhenSmsSendingFails_ShouldReturnFalse()
    {
        // Arrange
        _mockSmsProvider
            .Setup(x => x.SendAsync(It.IsAny<SmsMessage>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.SendSmsAsync("+12025551234", "Test message");

        // Assert
        result.Should().BeFalse("SMS sending failed");
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullEmailProvider_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var act = () => new ApplicationServices.NotificationService(
            null!,
            _mockSmsProvider.Object,
            _mockTemplateProvider.Object,
            _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("emailProvider");
    }

    [Fact]
    public void Constructor_WithNullSmsProvider_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var act = () => new ApplicationServices.NotificationService(
            _mockEmailProvider.Object,
            null!,
            _mockTemplateProvider.Object,
            _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("smsProvider");
    }

    [Fact]
    public void Constructor_WithNullTemplateProvider_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var act = () => new ApplicationServices.NotificationService(
            _mockEmailProvider.Object,
            _mockSmsProvider.Object,
            null!,
            _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("templateProvider");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var act = () => new ApplicationServices.NotificationService(
            _mockEmailProvider.Object,
            _mockSmsProvider.Object,
            _mockTemplateProvider.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion
}
