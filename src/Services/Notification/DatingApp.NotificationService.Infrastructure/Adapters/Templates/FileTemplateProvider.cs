using System.Text.Json;
using System.Text.RegularExpressions;
using DatingApp.NotificationService.Core.Ports;

namespace DatingApp.NotificationService.Infrastructure.Adapters.Templates;

/// <summary>
/// File-based template provider with simple placeholder replacement.
/// </summary>
/// <remarks>
/// HEXAGONAL ARCHITECTURE: This is an ADAPTER in Infrastructure!
///
/// Implements:
/// - ITemplateProvider port (defined in Core/Ports/)
///
/// Purpose:
/// - Read HTML templates from files
/// - Replace {{placeholders}} with actual data
/// - Simple solution for MVP (no template engine needed)
///
/// Template syntax:
/// - {{PropertyName}} - replaced with data.PropertyName
/// - {{User.Name}} - supports nested properties
///
/// Later, we can create:
/// - RazorTemplateProvider (Razor template engine)
/// - LiquidTemplateProvider (Shopify Liquid syntax)
/// - HandlebarsTemplateProvider (Handlebars.NET)
///
/// All implement the same ITemplateProvider port - can be swapped via DI!
///
/// Example template file (WelcomeEmail.html):
/// <code>
/// <html>
///   <body>
///     <h1>Welcome {{Name}}!</h1>
///     <p>Complete your profile: <a href="{{ProfileUrl}}">Click here</a></p>
///   </body>
/// </html>
/// </code>
/// </remarks>
public class FileTemplateProvider : ITemplateProvider
{
    private readonly string _templateBasePath;
    private readonly INotificationLogger<FileTemplateProvider> _logger;

    /// <summary>
    /// Creates a FileTemplateProvider.
    /// </summary>
    /// <param name="templateBasePath">Base directory for template files (e.g., "Templates/")</param>
    /// <param name="logger">Logger</param>
    public FileTemplateProvider(
        string templateBasePath,
        INotificationLogger<FileTemplateProvider> logger)
    {
        _templateBasePath = templateBasePath ?? throw new ArgumentNullException(nameof(templateBasePath));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Create template directory if it doesn't exist
        if (!Directory.Exists(_templateBasePath))
        {
            Directory.CreateDirectory(_templateBasePath);
            _logger.LogInformation("Created template directory: {Path}", _templateBasePath);
        }
    }

    public async Task<string> RenderAsync(string templateName, object data)
    {
        try
        {
            _logger.LogInformation("Rendering template: {TemplateName}", templateName);

            // Find template file
            var templatePath = Path.Combine(_templateBasePath, $"{templateName}.html");

            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException(
                    $"Template not found: {templateName} (expected path: {templatePath})");
            }

            // Read template content
            var template = await File.ReadAllTextAsync(templatePath);

            // Serialize data to dictionary for easy access
            var json = JsonSerializer.Serialize(data);
            var dataDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)
                ?? new Dictionary<string, JsonElement>();

            // Replace placeholders: {{PropertyName}}
            var rendered = Regex.Replace(template, @"\{\{(\w+)\}\}", match =>
            {
                var propertyName = match.Groups[1].Value;

                if (dataDict.TryGetValue(propertyName, out var value))
                {
                    // Handle different JSON types
                    return value.ValueKind switch
                    {
                        JsonValueKind.String => value.GetString() ?? "",
                        JsonValueKind.Number => value.GetInt32().ToString(),
                        JsonValueKind.True => "true",
                        JsonValueKind.False => "false",
                        _ => value.ToString()
                    };
                }

                _logger.LogWarning(
                    "Template property not found: {PropertyName} in template {TemplateName}",
                    propertyName,
                    templateName);

                return match.Value; // Leave placeholder if not found
            });

            _logger.LogInformation(
                "Template rendered successfully: {TemplateName} ({Length} chars)",
                templateName,
                rendered.Length);

            return rendered;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to render template: {TemplateName}",
                templateName);
            throw;
        }
    }
}
