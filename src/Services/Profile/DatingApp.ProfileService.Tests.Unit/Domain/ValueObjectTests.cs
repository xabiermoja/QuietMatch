using DatingApp.ProfileService.Core.Domain.Exceptions;
using DatingApp.ProfileService.Core.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace DatingApp.ProfileService.Tests.Unit.Domain;

/// <summary>
/// Unit tests for domain value objects.
/// </summary>
public class ValueObjectTests
{
    #region PersonalityProfile Tests

    [Fact]
    public void PersonalityProfile_WithValidScores_ShouldCreate()
    {
        // Act
        var personality = new PersonalityProfile(3, 4, 5, 2, 1, "About me", "Philosophy");

        // Assert
        personality.Openness.Should().Be(3);
        personality.Conscientiousness.Should().Be(4);
        personality.Extraversion.Should().Be(5);
        personality.Agreeableness.Should().Be(2);
        personality.Neuroticism.Should().Be(1);
        personality.AboutMe.Should().Be("About me");
        personality.LifePhilosophy.Should().Be("Philosophy");
    }

    [Theory]
    [InlineData(0)] // Below min
    [InlineData(6)] // Above max
    public void PersonalityProfile_WithInvalidScores_ShouldThrowException(int invalidScore)
    {
        // Act & Assert
        var act = () => new PersonalityProfile(invalidScore, 3, 3, 3, 3);
        act.Should().Throw<ProfileDomainException>()
            .WithMessage("*must be between 1 and 5*");
    }

    [Fact]
    public void PersonalityProfile_WithTooLongText_ShouldThrowException()
    {
        // Arrange
        var tooLongText = new string('x', 501); // Max is 500

        // Act & Assert
        var act = () => new PersonalityProfile(3, 3, 3, 3, 3, tooLongText);
        act.Should().Throw<ProfileDomainException>()
            .WithMessage("*cannot exceed 500 characters*");
    }

    #endregion

    #region Values Tests

    [Fact]
    public void Values_WithValidScores_ShouldCreate()
    {
        // Act
        var values = new Values(5, 4, 3, 5, 4, 3, 4, 5);

        // Assert
        values.FamilyOrientation.Should().Be(5);
        values.CareerAmbition.Should().Be(4);
        values.Spirituality.Should().Be(3);
        values.Adventure.Should().Be(5);
        values.IntellectualCuriosity.Should().Be(4);
        values.SocialJustice.Should().Be(3);
        values.FinancialSecurity.Should().Be(4);
        values.Environmentalism.Should().Be(5);
    }

    [Theory]
    [InlineData(0)] // Below min
    [InlineData(6)] // Above max
    public void Values_WithInvalidScores_ShouldThrowException(int invalidScore)
    {
        // Act & Assert
        var act = () => new Values(invalidScore, 3, 3, 3, 3, 3, 3, 3);
        act.Should().Throw<ProfileDomainException>()
            .WithMessage("*must be between 1 and 5*");
    }

    #endregion

    #region AgeRange Tests

    [Fact]
    public void AgeRange_WithValidRange_ShouldCreate()
    {
        // Act
        var ageRange = new AgeRange(25, 35);

        // Assert
        ageRange.Min.Should().Be(25);
        ageRange.Max.Should().Be(35);
    }

    [Fact]
    public void AgeRange_WithMinBelow18_ShouldThrowException()
    {
        // Act & Assert
        var act = () => new AgeRange(17, 30);
        act.Should().Throw<ProfileDomainException>()
            .WithMessage("*at least 18*");
    }

    [Fact]
    public void AgeRange_WithMaxAbove100_ShouldThrowException()
    {
        // Act & Assert
        var act = () => new AgeRange(25, 101);
        act.Should().Throw<ProfileDomainException>()
            .WithMessage("*cannot exceed 100*");
    }

    [Fact]
    public void AgeRange_WithMinGreaterThanMax_ShouldThrowException()
    {
        // Act & Assert
        var act = () => new AgeRange(40, 30);
        act.Should().Throw<ProfileDomainException>()
            .WithMessage("*cannot be greater than maximum*");
    }

    #endregion

    #region Location Tests

    [Fact]
    public void Location_WithValidData_ShouldCreate()
    {
        // Act
        var location = new Location("New York", "USA", 40.7128m, -74.0060m);

        // Assert
        location.City.Should().Be("New York");
        location.Country.Should().Be("USA");
        location.Latitude.Should().Be(40.7128m);
        location.Longitude.Should().Be(-74.0060m);
    }

    [Fact]
    public void Location_WithoutCoordinates_ShouldCreate()
    {
        // Act
        var location = new Location("London", "UK");

        // Assert
        location.City.Should().Be("London");
        location.Country.Should().Be("UK");
        location.Latitude.Should().BeNull();
        location.Longitude.Should().BeNull();
    }

    [Fact]
    public void Location_WithEmptyCity_ShouldThrowException()
    {
        // Act & Assert
        var act = () => new Location("", "USA");
        act.Should().Throw<ArgumentException>()
            .WithMessage("*City*");
    }

    [Theory]
    [InlineData(-91)] // Below min
    [InlineData(91)]  // Above max
    public void Location_WithInvalidLatitude_ShouldThrowException(decimal invalidLat)
    {
        // Act & Assert
        var act = () => new Location("New York", "USA", invalidLat, 0);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Latitude*");
    }

    [Theory]
    [InlineData(-181)] // Below min
    [InlineData(181)]  // Above max
    public void Location_WithInvalidLongitude_ShouldThrowException(decimal invalidLon)
    {
        // Act & Assert
        var act = () => new Location("New York", "USA", 0, invalidLon);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Longitude*");
    }

    #endregion

    #region PreferenceSet Tests

    [Fact]
    public void PreferenceSet_WithValidData_ShouldCreate()
    {
        // Arrange
        var ageRange = new AgeRange(25, 35);
        var languages = new List<string> { "English", "Spanish" };

        // Act
        var preferences = new PreferenceSet(ageRange, 50, languages, GenderPreference.Women);

        // Assert
        preferences.PreferredAgeRange.Should().Be(ageRange);
        preferences.MaxDistanceKm.Should().Be(50);
        preferences.PreferredLanguages.Should().BeEquivalentTo(languages);
        preferences.GenderPreference.Should().Be(GenderPreference.Women);
    }

    [Theory]
    [InlineData(0)]   // Below min
    [InlineData(501)] // Above max
    public void PreferenceSet_WithInvalidDistance_ShouldThrowException(int invalidDistance)
    {
        // Arrange
        var ageRange = new AgeRange(25, 35);
        var languages = new List<string> { "English" };

        // Act & Assert
        var act = () => new PreferenceSet(ageRange, invalidDistance, languages, GenderPreference.NoPreference);
        act.Should().Throw<ProfileDomainException>()
            .WithMessage("*must be between 1 and 500*");
    }

    [Fact]
    public void PreferenceSet_WithEmptyLanguages_ShouldThrowException()
    {
        // Arrange
        var ageRange = new AgeRange(25, 35);
        var emptyLanguages = new List<string>();

        // Act & Assert
        var act = () => new PreferenceSet(ageRange, 50, emptyLanguages, GenderPreference.NoPreference);
        act.Should().Throw<ProfileDomainException>()
            .WithMessage("*at least one preferred language*");
    }

    [Fact]
    public void PreferenceSet_ShouldCreateDefensiveCopyOfLanguages()
    {
        // Arrange
        var ageRange = new AgeRange(25, 35);
        var languages = new List<string> { "English" };

        // Act
        var preferences = new PreferenceSet(ageRange, 50, languages, GenderPreference.NoPreference);
        languages.Add("French"); // Modify original list

        // Assert - Should not affect preferences
        preferences.PreferredLanguages.Should().HaveCount(1);
        preferences.PreferredLanguages.Should().Contain("English");
        preferences.PreferredLanguages.Should().NotContain("French");
    }

    #endregion

    #region Lifestyle Tests

    [Fact]
    public void Lifestyle_WithValidData_ShouldCreate()
    {
        // Act
        var lifestyle = new Lifestyle(
            ExerciseFrequency.Regularly,
            DietType.Vegetarian,
            SmokingStatus.Never,
            DrinkingFrequency.Socially,
            true,
            ChildrenPreference.Maybe
        );

        // Assert
        lifestyle.ExerciseFrequency.Should().Be(ExerciseFrequency.Regularly);
        lifestyle.DietType.Should().Be(DietType.Vegetarian);
        lifestyle.SmokingStatus.Should().Be(SmokingStatus.Never);
        lifestyle.DrinkingFrequency.Should().Be(DrinkingFrequency.Socially);
        lifestyle.HasPets.Should().BeTrue();
        lifestyle.WantsChildren.Should().Be(ChildrenPreference.Maybe);
    }

    #endregion

    #region MemberId Tests

    [Fact]
    public void MemberId_WithValidGuid_ShouldCreate()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var memberId = new MemberId(guid);

        // Assert
        memberId.Value.Should().Be(guid);
    }

    [Fact]
    public void MemberId_ImplicitConversion_ToGuid_ShouldWork()
    {
        // Arrange
        var memberId = new MemberId(Guid.NewGuid());

        // Act
        Guid guid = memberId; // Implicit conversion

        // Assert
        guid.Should().Be(memberId.Value);
    }

    [Fact]
    public void MemberId_ImplicitConversion_FromGuid_ShouldWork()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        MemberId memberId = guid; // Implicit conversion

        // Assert
        memberId.Value.Should().Be(guid);
    }

    #endregion
}
