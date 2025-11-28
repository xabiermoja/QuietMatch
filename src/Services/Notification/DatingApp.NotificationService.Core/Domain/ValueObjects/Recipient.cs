using System.Text.RegularExpressions;

namespace DatingApp.NotificationService.Core.Domain.ValueObjects;

/// <summary>
/// Value object representing a notification recipient.
/// </summary>
/// <remarks>
/// Immutable value object with validation.
/// Email must be in valid format, Name is required.
/// </remarks>
public record Recipient
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Recipient's email address
    /// </summary>
    public string Email { get; }

    /// <summary>
    /// Recipient's display name
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Creates a new Recipient with validation.
    /// </summary>
    /// <param name="email">Email address (must be valid format)</param>
    /// <param name="name">Display name (required)</param>
    /// <exception cref="ArgumentException">Thrown when validation fails</exception>
    public Recipient(string email, string name)
    {
        // Trim email first, THEN validate
        var trimmedEmail = email?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(trimmedEmail))
            throw new ArgumentException("Email is required", nameof(email));

        if (!EmailRegex.IsMatch(trimmedEmail))
            throw new ArgumentException($"Invalid email format: {trimmedEmail}", nameof(email));

        Email = trimmedEmail.ToLowerInvariant();
        Name = name?.Trim() ?? string.Empty; // Allow empty names
    }

    /// <summary>
    /// Creates a Recipient with email only (name defaults to email).
    /// </summary>
    public static Recipient FromEmail(string email) => new(email, email);

    public override string ToString() => $"{Name} <{Email}>";
}
