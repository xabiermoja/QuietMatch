using DatingApp.NotificationService.Core.Ports;
using DatingApp.NotificationService.Infrastructure.Adapters.Templates;
using FluentAssertions;
using Moq;
using Xunit;

namespace DatingApp.NotificationService.Tests.Unit.Infrastructure.Adapters;

/// <summary>
/// Tests for FileTemplateProvider adapter.
/// </summary>
/// <remarks>
/// HEXAGONAL ARCHITECTURE TESTING - ADAPTER PATTERN:
///
/// Adapter Testing Rules:
/// 1. Mock external dependencies (file system → in-memory, network → mock)
/// 2. Test that adapter correctly implements port contract
/// 3. Test error handling specific to this adapter
/// 4. DON'T test the port interface itself (that's tested in application service tests)
///
/// What we're testing:
/// - Template file reading
/// - Placeholder replacement ({{Name}} → actual value)
/// - Error handling (missing template, invalid data)
/// - Logging behavior
///
/// Note: In production, you might use TestContainers or in-memory file systems
/// For this example, we use real temp files for simplicity
/// </remarks>
public class FileTemplateProviderTests : IDisposable
{
    private readonly string _testTemplatesPath;
    private readonly Mock<INotificationLogger<FileTemplateProvider>> _mockLogger;

    public FileTemplateProviderTests()
    {
        // Arrange - create temporary directory for test templates
        _testTemplatesPath = Path.Combine(Path.GetTempPath(), $"test-templates-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testTemplatesPath);

        _mockLogger = new Mock<INotificationLogger<FileTemplateProvider>>();
    }

    public void Dispose()
    {
        // Cleanup - delete temporary test files
        if (Directory.Exists(_testTemplatesPath))
        {
            Directory.Delete(_testTemplatesPath, true);
        }
    }

    [Fact]
    public async Task RenderAsync_WithValidTemplate_ShouldReplacePlaceholders()
    {
        // Arrange
        var templateContent = "<html><body>Hello {{Name}}!</body></html>";
        var templatePath = Path.Combine(_testTemplatesPath, "WelcomeEmail.html");
        await File.WriteAllTextAsync(templatePath, templateContent);

        var provider = new FileTemplateProvider(_testTemplatesPath, _mockLogger.Object);

        // Act
        var result = await provider.RenderAsync("WelcomeEmail", new { Name = "John" });

        // Assert
        result.Should().Be("<html><body>Hello John!</body></html>");
    }

    [Fact]
    public async Task RenderAsync_WithMultiplePlaceholders_ShouldReplaceAll()
    {
        // Arrange
        var templateContent = "<p>Hello {{Name}}, your email is {{Email}}!</p>";
        var templatePath = Path.Combine(_testTemplatesPath, "Test.html");
        await File.WriteAllTextAsync(templatePath, templateContent);

        var provider = new FileTemplateProvider(_testTemplatesPath, _mockLogger.Object);

        // Act
        var result = await provider.RenderAsync("Test", new
        {
            Name = "John",
            Email = "john@example.com"
        });

        // Assert
        result.Should().Be("<p>Hello John, your email is john@example.com!</p>");
    }

    [Fact]
    public async Task RenderAsync_WithMissingProperty_ShouldKeepPlaceholder()
    {
        // Arrange
        var templateContent = "Hello {{Name}}, welcome {{Title}}!";
        var templatePath = Path.Combine(_testTemplatesPath, "Test.html");
        await File.WriteAllTextAsync(templatePath, templateContent);

        var provider = new FileTemplateProvider(_testTemplatesPath, _mockLogger.Object);

        // Act - only provide Name, not Title
        var result = await provider.RenderAsync("Test", new { Name = "John" });

        // Assert - should replace Name but keep {{Title}} placeholder
        result.Should().Be("Hello John, welcome {{Title}}!");

        // Verify warning was logged for missing property
        _mockLogger.Verify(
            x => x.LogWarning(
                It.Is<string>(s => s.Contains("Template property not found")),
                It.IsAny<object[]>()),
            Times.Once);
    }

    [Fact]
    public async Task RenderAsync_WithMissingTemplate_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var provider = new FileTemplateProvider(_testTemplatesPath, _mockLogger.Object);

        // Act
        var act = async () => await provider.RenderAsync("NonExistent", new { });

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage("*Template not found: NonExistent*");
    }

    [Fact]
    public async Task RenderAsync_ShouldLogSuccess()
    {
        // Arrange
        var templateContent = "Hello {{Name}}!";
        var templatePath = Path.Combine(_testTemplatesPath, "Test.html");
        await File.WriteAllTextAsync(templatePath, templateContent);

        var provider = new FileTemplateProvider(_testTemplatesPath, _mockLogger.Object);

        // Act
        await provider.RenderAsync("Test", new { Name = "John" });

        // Assert - verify logging calls
        _mockLogger.Verify(
            x => x.LogInformation(
                It.Is<string>(s => s.Contains("Rendering template")),
                It.IsAny<object[]>()),
            Times.Once,
            "Should log start of rendering");

        _mockLogger.Verify(
            x => x.LogInformation(
                It.Is<string>(s => s.Contains("Template rendered successfully")),
                It.IsAny<object[]>()),
            Times.Once,
            "Should log successful rendering");
    }

    [Fact]
    public void Constructor_ShouldCreateTemplateDirectory_IfNotExists()
    {
        // Arrange
        var newPath = Path.Combine(Path.GetTempPath(), $"new-templates-{Guid.NewGuid()}");

        try
        {
            // Act
            var provider = new FileTemplateProvider(newPath, _mockLogger.Object);

            // Assert
            Directory.Exists(newPath).Should().BeTrue(
                "Constructor should create directory if it doesn't exist");
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(newPath))
            {
                Directory.Delete(newPath, true);
            }
        }
    }

    [Fact]
    public void Constructor_WithNullPath_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var act = () => new FileTemplateProvider(null!, _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("templateBasePath");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var act = () => new FileTemplateProvider(_testTemplatesPath, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }
}
