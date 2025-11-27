using DatingApp.ProfileService.Core.Domain.Exceptions;

namespace DatingApp.ProfileService.Core.Domain.ValueObjects;

/// <summary>
/// Value Object representing a Member's core values and priorities.
/// </summary>
/// <remarks>
/// Values are rated on a 1-5 scale (1=Not Important, 5=Extremely Important).
/// These values are used for compatibility matching to find partners with aligned priorities.
///
/// The 8 core values captured:
/// - FamilyOrientation: Importance of family, desire for children, family time
/// - CareerAmbition: Professional success, career growth, work-life balance
/// - Spirituality: Religious beliefs, spiritual practices, faith
/// - Adventure: Travel, spontaneity, trying new things
/// - IntellectualCuriosity: Learning, deep conversations, intellectual growth
/// - SocialJustice: Equality, activism, social causes
/// - FinancialSecurity: Savings, financial stability, material comfort
/// - Environmentalism: Sustainability, eco-consciousness, climate action
/// </remarks>
public record Values
{
    private const int MinScore = 1;
    private const int MaxScore = 5;

    public int FamilyOrientation { get; init; }
    public int CareerAmbition { get; init; }
    public int Spirituality { get; init; }
    public int Adventure { get; init; }
    public int IntellectualCuriosity { get; init; }
    public int SocialJustice { get; init; }
    public int FinancialSecurity { get; init; }
    public int Environmentalism { get; init; }

    // Private constructor for EF Core
    private Values() { }

    public Values(
        int familyOrientation,
        int careerAmbition,
        int spirituality,
        int adventure,
        int intellectualCuriosity,
        int socialJustice,
        int financialSecurity,
        int environmentalism)
    {
        // Validate all value scores (1-5 scale)
        ValidateScore(familyOrientation, nameof(FamilyOrientation));
        ValidateScore(careerAmbition, nameof(CareerAmbition));
        ValidateScore(spirituality, nameof(Spirituality));
        ValidateScore(adventure, nameof(Adventure));
        ValidateScore(intellectualCuriosity, nameof(IntellectualCuriosity));
        ValidateScore(socialJustice, nameof(SocialJustice));
        ValidateScore(financialSecurity, nameof(FinancialSecurity));
        ValidateScore(environmentalism, nameof(Environmentalism));

        FamilyOrientation = familyOrientation;
        CareerAmbition = careerAmbition;
        Spirituality = spirituality;
        Adventure = adventure;
        IntellectualCuriosity = intellectualCuriosity;
        SocialJustice = socialJustice;
        FinancialSecurity = financialSecurity;
        Environmentalism = environmentalism;
    }

    private static void ValidateScore(int score, string valueName)
    {
        if (score < MinScore || score > MaxScore)
            throw new ProfileDomainException($"{valueName} must be between {MinScore} and {MaxScore} (received: {score})");
    }
}
