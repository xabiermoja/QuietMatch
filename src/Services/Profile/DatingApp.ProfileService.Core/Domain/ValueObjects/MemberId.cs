namespace DatingApp.ProfileService.Core.Domain.ValueObjects;

/// <summary>
/// Value Object representing a Member's unique identifier.
/// </summary>
/// <remarks>
/// Wraps Guid to provide type safety and prevent primitive obsession.
/// In the ubiquitous language, we use "MemberId" not "Guid" to represent user identity.
/// </remarks>
public record MemberId
{
    public Guid Value { get; init; }

    public MemberId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("MemberId cannot be empty", nameof(value));

        Value = value;
    }

    public static implicit operator Guid(MemberId memberId) => memberId.Value;
    public static implicit operator MemberId(Guid guid) => new(guid);

    public override string ToString() => Value.ToString();
}
