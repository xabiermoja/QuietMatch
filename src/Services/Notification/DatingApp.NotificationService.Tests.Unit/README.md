# Unit Tests - Hexagonal Architecture Testing Strategy

## Testing Philosophy

In Hexagonal Architecture, unit tests verify each "side" of the hexagon in **isolation**:

1. **Domain/Core Tests** (No Mocks Needed!)
   - Pure business logic
   - Zero external dependencies
   - Test invariants, validations, state transitions

2. **Application Service Tests** (Mock Ports Only!)
   - Mock **ALL ports** (IEmailProvider, ISmsProvider, ITemplateProvider, INotificationLogger)
   - Verify orchestration logic
   - Verify correct port calls

3. **Adapter Tests** (Mock External Dependencies!)
   - Mock external systems (file system, network, etc.)
   - Verify adapter correctly implements port contract
   - Verify error handling

## Why This Approach?

**Hexagonal makes unit testing EASY:**
- Domain has no dependencies → no mocking needed
- Ports are interfaces → trivial to mock
- Each adapter tested independently
- **Swappability** is automatically tested (if you can mock it, you can swap it!)

## Test Structure

```
Tests.Unit/
├── Domain/
│   ├── Entities/
│   │   └── NotificationTests.cs          # Aggregate root state machine
│   └── ValueObjects/
│       ├── RecipientTests.cs             # Email validation
│       ├── EmailMessageTests.cs          # Email invariants
│       └── SmsMessageTests.cs            # Phone validation
│
├── Application/
│   └── Services/
│       └── NotificationServiceTests.cs   # Service orchestration with mocked ports
│
└── Infrastructure/
    └── Adapters/
        ├── FileTemplateProviderTests.cs  # Template rendering logic
        └── ConsoleEmailProviderTests.cs  # Email adapter logic
```

## Tools Used

- **xUnit**: Test framework (.NET standard)
- **FluentAssertions**: Readable assertions (`result.Should().BeTrue()`)
- **Moq**: Mocking framework for ports/dependencies

## Running Tests

```bash
cd DatingApp.NotificationService.Tests.Unit
dotnet test

# With coverage
dotnet test /p:CollectCoverage=true
```

## Key Testing Patterns

### 1. Testing Value Objects (No Mocks)

```csharp
[Fact]
public void Recipient_WithInvalidEmail_ShouldThrowException()
{
    // Arrange & Act
    var act = () => new Recipient("not-an-email", "John");

    // Assert - FluentAssertions syntax
    act.Should().Throw<ArgumentException>()
       .WithMessage("*Invalid email format*");
}
```

### 2. Testing Application Service (Mock Ports)

```csharp
[Fact]
public async Task SendWelcomeEmailAsync_ShouldCallEmailProvider()
{
    // Arrange
    var mockEmailProvider = new Mock<IEmailProvider>();
    var mockSmsProvider = new Mock<ISmsProvider>();
    var mockTemplateProvider = new Mock<ITemplateProvider>();
    var mockLogger = new Mock<INotificationLogger<NotificationService>>();

    mockTemplateProvider
        .Setup(x => x.RenderAsync("WelcomeEmail", It.IsAny<object>()))
        .ReturnsAsync("<html>Welcome!</html>");

    mockEmailProvider
        .Setup(x => x.SendAsync(It.IsAny<EmailMessage>()))
        .ReturnsAsync(true);

    var service = new NotificationService(
        mockEmailProvider.Object,
        mockSmsProvider.Object,
        mockTemplateProvider.Object,
        mockLogger.Object);

    // Act
    var result = await service.SendWelcomeEmailAsync(
        Guid.NewGuid(),
        "test@example.com",
        "John");

    // Assert
    result.Should().BeTrue();
    mockEmailProvider.Verify(
        x => x.SendAsync(It.Is<EmailMessage>(
            m => m.To.Email == "test@example.com" &&
                 m.Subject == "Welcome to QuietMatch! 🎉")),
        Times.Once);
}
```

### 3. Testing Adapters (Mock External Dependencies)

```csharp
[Fact]
public async Task FileTemplateProvider_ShouldRenderTemplate()
{
    // Arrange - create real file system test fixture
    var tempPath = Path.GetTempPath();
    var templatePath = Path.Combine(tempPath, "test-templates");
    Directory.CreateDirectory(templatePath);

    var templateFile = Path.Combine(templatePath, "Test.html");
    await File.WriteAllTextAsync(templateFile, "<p>Hello {{Name}}!</p>");

    var mockLogger = new Mock<INotificationLogger<FileTemplateProvider>>();
    var provider = new FileTemplateProvider(templatePath, mockLogger.Object);

    // Act
    var result = await provider.RenderAsync("Test", new { Name = "John" });

    // Assert
    result.Should().Contain("<p>Hello John!</p>");

    // Cleanup
    Directory.Delete(templatePath, true);
}
```

## Coverage Goals

- **Domain**: 100% (it's all business logic!)
- **Application**: 90%+ (all happy paths + error cases)
- **Infrastructure**: 80%+ (focus on logic, not framework code)

## Integration with CI/CD

These tests run automatically in CI/CD pipeline:
- Fast execution (< 5 seconds total)
- No external dependencies required
- Deterministic (no flaky tests)
