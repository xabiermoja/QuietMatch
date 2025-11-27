namespace DatingApp.ProfileService.Core.Domain.Exceptions;

/// <summary>
/// Exception thrown when a domain rule is violated in the Profile bounded context.
/// </summary>
/// <remarks>
/// Domain exceptions represent business rule violations (e.g., invalid personality scores, invalid age ranges).
/// They are distinct from infrastructure exceptions (e.g., database connection failures).
/// </remarks>
public class ProfileDomainException : Exception
{
    public ProfileDomainException(string message) : base(message)
    {
    }

    public ProfileDomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
