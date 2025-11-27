using DatingApp.ProfileService.Core.Domain.Entities;
using DatingApp.ProfileService.Core.Domain.Events;
using DatingApp.ProfileService.Core.Domain.Exceptions;
using DatingApp.ProfileService.Core.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace DatingApp.ProfileService.Tests.Unit.Domain;

/// <summary>
/// Unit tests for MemberProfile aggregate root.
/// </summary>
public class MemberProfileTests
{
    [Fact]
    public void CreateSkeleton_ShouldCreateProfileWithDefaultValues()
    {
        // Arrange
        var userId = new MemberId(Guid.NewGuid());
        var email = "test@example.com";

        // Act
        var profile = MemberProfile.CreateSkeleton(userId, email);

        // Assert
        profile.Should().NotBeNull();
        profile.UserId.Should().Be(userId);
        profile.CompletionPercentage.Should().Be(0);
        profile.IsComplete.Should().BeFalse();
        profile.ExposureLevel.Should().Be(ExposureLevel.MatchedOnly); // Default: highest privacy
        profile.DomainEvents.Should().HaveCount(1);
        profile.DomainEvents.First().Should().BeOfType<ProfileCreated>();
    }

    [Fact]
    public void UpdateBasicInfo_WithValidData_ShouldUpdateProfileAndRaiseEvent()
    {
        // Arrange
        var profile = MemberProfile.CreateSkeleton(new MemberId(Guid.NewGuid()), "test@example.com");
        profile.ClearDomainEvents();

        var location = new Location("New York", "USA", 40.7128m, -74.0060m);
        var dateOfBirth = DateTime.UtcNow.AddYears(-25);

        // Act
        profile.UpdateBasicInfo("John Doe", dateOfBirth, "Male", location);

        // Assert
        profile.FullName.Should().Be("John Doe");
        profile.DateOfBirth.Should().Be(dateOfBirth);
        profile.Gender.Should().Be("Male");
        profile.Location.Should().Be(location);
        profile.CompletionPercentage.Should().Be(20); // Basic info is 20%
        profile.DomainEvents.Should().HaveCount(1);
        profile.DomainEvents.First().Should().BeOfType<ProfileUpdated>();
    }

    [Fact]
    public void UpdateBasicInfo_WithAgeLessThan18_ShouldThrowException()
    {
        // Arrange
        var profile = MemberProfile.CreateSkeleton(new MemberId(Guid.NewGuid()), "test@example.com");
        var location = new Location("New York", "USA");
        var dateOfBirth = DateTime.UtcNow.AddYears(-17); // 17 years old

        // Act & Assert
        var act = () => profile.UpdateBasicInfo("John Doe", dateOfBirth, "Male", location);
        act.Should().Throw<ProfileDomainException>()
            .WithMessage("*at least 18 years old*");
    }

    [Fact]
    public void UpdatePersonality_ShouldUpdateProfileAndIncreaseCompletion()
    {
        // Arrange
        var profile = MemberProfile.CreateSkeleton(new MemberId(Guid.NewGuid()), "test@example.com");
        profile.ClearDomainEvents();

        var personality = new PersonalityProfile(4, 3, 5, 4, 2, "I love coding", "Live and let live");

        // Act
        profile.UpdatePersonality(personality);

        // Assert
        profile.Personality.Should().Be(personality);
        profile.CompletionPercentage.Should().Be(20); // Personality is 20%
        profile.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void CompletionPercentage_WithAllSectionsFilled_ShouldBe100()
    {
        // Arrange
        var profile = MemberProfile.CreateSkeleton(new MemberId(Guid.NewGuid()), "test@example.com");
        profile.ClearDomainEvents();

        var location = new Location("New York", "USA");
        var personality = new PersonalityProfile(4, 3, 5, 4, 2);
        var values = new Values(5, 4, 3, 5, 4, 3, 4, 5);
        var lifestyle = new Lifestyle(ExerciseFrequency.Regularly, DietType.Vegetarian,
            SmokingStatus.Never, DrinkingFrequency.Socially, true, ChildrenPreference.Maybe);
        var preferences = new PreferenceSet(new AgeRange(25, 35), 50,
            new List<string> { "English", "Spanish" }, GenderPreference.Women);

        // Act
        profile.UpdateBasicInfo("John Doe", DateTime.UtcNow.AddYears(-25), "Male", location);
        profile.UpdatePersonality(personality);
        profile.UpdateValues(values);
        profile.UpdateLifestyle(lifestyle);
        profile.UpdatePreferences(preferences);

        // Assert
        profile.CompletionPercentage.Should().Be(100);
        profile.IsComplete.Should().BeTrue();
    }

    [Fact]
    public void IsComplete_WhenReaching80Percent_ShouldRaiseProfileCompletedEvent()
    {
        // Arrange
        var profile = MemberProfile.CreateSkeleton(new MemberId(Guid.NewGuid()), "test@example.com");
        profile.ClearDomainEvents();

        var location = new Location("New York", "USA");
        var personality = new PersonalityProfile(4, 3, 5, 4, 2);
        var values = new Values(5, 4, 3, 5, 4, 3, 4, 5);
        var lifestyle = new Lifestyle(ExerciseFrequency.Regularly, DietType.Vegetarian,
            SmokingStatus.Never, DrinkingFrequency.Socially, true, ChildrenPreference.Maybe);

        // Act
        profile.UpdateBasicInfo("John Doe", DateTime.UtcNow.AddYears(-25), "Male", location);
        profile.UpdatePersonality(personality);
        profile.UpdateValues(values);
        profile.UpdateLifestyle(lifestyle); // Now at 80%

        // Assert
        profile.CompletionPercentage.Should().Be(80);
        profile.IsComplete.Should().BeTrue();

        // Should have ProfileCompleted event (only raised once on transition to complete)
        profile.DomainEvents.Should().Contain(e => e is ProfileCompleted);
    }

    [Fact]
    public void IsComplete_AfterAlreadyComplete_ShouldNotRaiseDuplicateEvent()
    {
        // Arrange
        var profile = MemberProfile.CreateSkeleton(new MemberId(Guid.NewGuid()), "test@example.com");

        var location = new Location("New York", "USA");
        var personality = new PersonalityProfile(4, 3, 5, 4, 2);
        var values = new Values(5, 4, 3, 5, 4, 3, 4, 5);
        var lifestyle = new Lifestyle(ExerciseFrequency.Regularly, DietType.Vegetarian,
            SmokingStatus.Never, DrinkingFrequency.Socially, true, ChildrenPreference.Maybe);

        profile.UpdateBasicInfo("John Doe", DateTime.UtcNow.AddYears(-25), "Male", location);
        profile.UpdatePersonality(personality);
        profile.UpdateValues(values);
        profile.UpdateLifestyle(lifestyle); // Now at 80% - complete

        profile.ClearDomainEvents();

        // Act - Update again after already complete
        var updatedValues = new Values(4, 4, 3, 5, 4, 3, 4, 5);
        profile.UpdateValues(updatedValues);

        // Assert
        profile.IsComplete.Should().BeTrue();
        // Should NOT have ProfileCompleted event (already was complete)
        profile.DomainEvents.Should().NotContain(e => e is ProfileCompleted);
    }

    [Fact]
    public void CanShareWith_MatchedOnly_ShouldOnlyShareWithAcceptedMatches()
    {
        // Arrange
        var profile = MemberProfile.CreateSkeleton(new MemberId(Guid.NewGuid()), "test@example.com");
        profile.UpdateExposureLevel(ExposureLevel.MatchedOnly);

        // Act & Assert
        profile.CanShareWith(MatchStatus.None).Should().BeFalse();
        profile.CanShareWith(MatchStatus.Candidate).Should().BeFalse();
        profile.CanShareWith(MatchStatus.Accepted).Should().BeTrue();
    }

    [Fact]
    public void CanShareWith_AllMatches_ShouldShareWithCandidatesAndAccepted()
    {
        // Arrange
        var profile = MemberProfile.CreateSkeleton(new MemberId(Guid.NewGuid()), "test@example.com");
        profile.UpdateExposureLevel(ExposureLevel.AllMatches);

        // Act & Assert
        profile.CanShareWith(MatchStatus.None).Should().BeFalse();
        profile.CanShareWith(MatchStatus.Candidate).Should().BeTrue();
        profile.CanShareWith(MatchStatus.Accepted).Should().BeTrue();
    }

    [Fact]
    public void CanShareWith_Public_ShouldShareWithEveryone()
    {
        // Arrange
        var profile = MemberProfile.CreateSkeleton(new MemberId(Guid.NewGuid()), "test@example.com");
        profile.UpdateExposureLevel(ExposureLevel.Public);

        // Act & Assert
        profile.CanShareWith(MatchStatus.None).Should().BeTrue();
        profile.CanShareWith(MatchStatus.Candidate).Should().BeTrue();
        profile.CanShareWith(MatchStatus.Accepted).Should().BeTrue();
    }

    [Fact]
    public void SoftDelete_ShouldSetDeletedAt()
    {
        // Arrange
        var profile = MemberProfile.CreateSkeleton(new MemberId(Guid.NewGuid()), "test@example.com");

        // Act
        profile.SoftDelete();

        // Assert
        profile.DeletedAt.Should().NotBeNull();
        profile.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var profile = MemberProfile.CreateSkeleton(new MemberId(Guid.NewGuid()), "test@example.com");
        profile.DomainEvents.Should().HaveCount(1);

        // Act
        profile.ClearDomainEvents();

        // Assert
        profile.DomainEvents.Should().BeEmpty();
    }
}
