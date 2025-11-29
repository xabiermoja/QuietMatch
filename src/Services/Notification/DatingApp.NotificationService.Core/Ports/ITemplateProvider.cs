namespace DatingApp.NotificationService.Core.Ports;

/// <summary>
/// Port (interface) for rendering notification templates.
/// </summary>
/// <remarks>
/// HEXAGONAL ARCHITECTURE: Port defined in Domain, implemented in Infrastructure.
///
/// Multiple adapters can implement this port:
/// - FileTemplateProvider (read HTML files, simple placeholder replacement)
/// - RazorTemplateProvider (Razor template engine)
/// - LiquidTemplateProvider (Liquid template syntax)
/// - HandlebarsTemplateProvider (Handlebars.NET)
/// </remarks>
public interface ITemplateProvider
{
    /// <summary>
    /// Renders a notification template with the provided data.
    /// </summary>
    /// <param name="templateName">Name of the template (e.g., "WelcomeEmail", "ProfileCompleted")</param>
    /// <param name="data">Template data (will be serialized to JSON or used as object)</param>
    /// <returns>Rendered template as HTML string</returns>
    /// <remarks>
    /// Implementations should:
    /// - Support template inheritance/layouts
    /// - Handle missing templates gracefully (throw clear exception)
    /// - Cache compiled templates for performance
    /// - Escape user-provided data to prevent XSS
    /// </remarks>
    Task<string> RenderAsync(string templateName, object data);
}
