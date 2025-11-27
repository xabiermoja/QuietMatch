using DatingApp.ProfileService.Core.Application.DTOs;
using DatingApp.ProfileService.Core.Application.Services;
using DatingApp.ProfileService.Core.Domain.Entities;
using DatingApp.ProfileService.Core.Domain.Events;
using DatingApp.ProfileService.Core.Domain.Exceptions;
using DatingApp.ProfileService.Core.Domain.Interfaces;
using DatingApp.ProfileService.Core.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace DatingApp.ProfileService.Tests.Unit.Application;

/// <summary>
/// Unit tests for ProfileService application service.
/// </summary>
public class ProfileServiceTests
{
    private readonly Mock<IProfileRepository> _mockRepository;
    private readonly Mock<IMessagePublisher> _mockPublisher;
    private readonly Core.Application.Services.ProfileService _service;

    public ProfileServiceTests()
    {
        _mockRepository = new Mock<IProfileRepository>();
        _mockPublisher = new Mock<IMessagePublisher>();
        _service = new Core.Application.Services.ProfileService(_mockRepository.Object, _mockPublisher.Object);
    }

    #region CreateBasicProfileAsync Tests

    [Fact]
    public async Task CreateBasicProfileAsync_WithValidRequest_ShouldUpdateProfileAndPublishEvents()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var memberId = new MemberId(userId);
        var existingProfile = MemberProfile.CreateSkeleton(memberId, "test@example.com");
        existingProfile.ClearDomainEvents(); // Clear ProfileCreated event from skeleton creation

        _mockRepository.Setup(r => r.GetByUserIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);

        var request = new CreateProfileRequest
        {
            FullName = "John Doe",
            DateOfBirth = DateTime.UtcNow.AddYears(-25),
            Gender = "Male",
            Location = new LocationDto
            {
                City = "New York",
                Country = "USA",
                Latitude = 40.7128m,
                Longitude = -74.0060m
            }
        };

        // Act
        var result = await _service.CreateBasicProfileAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.FullName.Should().Be("John Doe");
        result.Gender.Should().Be("Male");
        result.CompletionPercentage.Should().Be(20); // Basic info is 20%

        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<MemberProfile>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockPublisher.Verify(p => p.PublishAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateBasicProfileAsync_WhenProfileNotFound_ShouldThrowException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var memberId = new MemberId(userId);

        _mockRepository.Setup(r => r.GetByUserIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MemberProfile?)null);

        var request = new CreateProfileRequest
        {
            FullName = "John Doe",
            DateOfBirth = DateTime.UtcNow.AddYears(-25),
            Gender = "Male",
            Location = new LocationDto { City = "New York", Country = "USA" }
        };

        // Act & Assert
        var act = async () => await _service.CreateBasicProfileAsync(userId, request);
        await act.Should().ThrowAsync<ProfileDomainException>()
            .WithMessage("*Profile not found*");
    }

    #endregion

    #region UpdateProfileAsync Tests

    [Fact]
    public async Task UpdateProfileAsync_WithPersonality_ShouldUpdateAndPublishEvent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var memberId = new MemberId(userId);
        var existingProfile = MemberProfile.CreateSkeleton(memberId, "test@example.com");
        existingProfile.ClearDomainEvents(); // Clear ProfileCreated event from skeleton creation

        _mockRepository.Setup(r => r.GetByUserIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);

        var request = new UpdateProfileRequest
        {
            Personality = new PersonalityDto
            {
                Openness = 4,
                Conscientiousness = 3,
                Extraversion = 5,
                Agreeableness = 4,
                Neuroticism = 2,
                AboutMe = "I love coding"
            }
        };

        // Act
        var result = await _service.UpdateProfileAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.Personality.Should().NotBeNull();
        result.Personality!.Openness.Should().Be(4);
        result.CompletionPercentage.Should().Be(20); // Personality is 20%

        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<MemberProfile>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockPublisher.Verify(p => p.PublishAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProfileAsync_WithMultipleSections_ShouldUpdateAll()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var memberId = new MemberId(userId);
        var existingProfile = MemberProfile.CreateSkeleton(memberId, "test@example.com");
        existingProfile.ClearDomainEvents(); // Clear ProfileCreated event from skeleton creation

        _mockRepository.Setup(r => r.GetByUserIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);

        var request = new UpdateProfileRequest
        {
            Personality = new PersonalityDto { Openness = 4, Conscientiousness = 3, Extraversion = 5, Agreeableness = 4, Neuroticism = 2 },
            Values = new ValuesDto { FamilyOrientation = 5, CareerAmbition = 4, Spirituality = 3, Adventure = 5, IntellectualCuriosity = 4, SocialJustice = 3, FinancialSecurity = 4, Environmentalism = 5 },
            Lifestyle = new LifestyleDto
            {
                ExerciseFrequency = "Regularly",
                DietType = "Vegetarian",
                SmokingStatus = "Never",
                DrinkingFrequency = "Socially",
                HasPets = true,
                WantsChildren = "Maybe"
            }
        };

        // Act
        var result = await _service.UpdateProfileAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.Personality.Should().NotBeNull();
        result.Values.Should().NotBeNull();
        result.Lifestyle.Should().NotBeNull();
        result.CompletionPercentage.Should().Be(60); // 3 sections × 20% each
    }

    [Fact]
    public async Task UpdateProfileAsync_WhenProfileNotFound_ShouldThrowException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var memberId = new MemberId(userId);

        _mockRepository.Setup(r => r.GetByUserIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MemberProfile?)null);

        var request = new UpdateProfileRequest
        {
            Personality = new PersonalityDto { Openness = 4, Conscientiousness = 3, Extraversion = 5, Agreeableness = 4, Neuroticism = 2 }
        };

        // Act & Assert
        var act = async () => await _service.UpdateProfileAsync(userId, request);
        await act.Should().ThrowAsync<ProfileDomainException>()
            .WithMessage("*Profile not found*");
    }

    [Fact]
    public async Task UpdateProfileAsync_WithCompleteProfile_ShouldRaiseProfileCompletedEvent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var memberId = new MemberId(userId);
        var existingProfile = MemberProfile.CreateSkeleton(memberId, "test@example.com");

        // Pre-fill basic info to get to 60% (need 80% to be complete)
        var location = new Location("New York", "USA");
        existingProfile.UpdateBasicInfo("John Doe", DateTime.UtcNow.AddYears(-25), "Male", location);
        existingProfile.UpdatePersonality(new PersonalityProfile(4, 3, 5, 4, 2));
        existingProfile.UpdateValues(new Values(5, 4, 3, 5, 4, 3, 4, 5));
        existingProfile.ClearDomainEvents(); // Clear previous events

        _mockRepository.Setup(r => r.GetByUserIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);

        var request = new UpdateProfileRequest
        {
            Lifestyle = new LifestyleDto
            {
                ExerciseFrequency = "Regularly",
                DietType = "Vegetarian",
                SmokingStatus = "Never",
                DrinkingFrequency = "Socially",
                HasPets = true,
                WantsChildren = "Maybe"
            }
        };

        // Act
        var result = await _service.UpdateProfileAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.CompletionPercentage.Should().Be(80);
        result.IsComplete.Should().BeTrue();

        // Should publish both ProfileUpdated AND ProfileCompleted events
        _mockPublisher.Verify(p => p.PublishAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    #endregion

    #region GetProfileAsync Tests

    [Fact]
    public async Task GetProfileAsync_WhenProfileExists_ShouldReturnProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var memberId = new MemberId(userId);
        var existingProfile = MemberProfile.CreateSkeleton(memberId, "test@example.com");
        var location = new Location("New York", "USA");
        existingProfile.UpdateBasicInfo("John Doe", DateTime.UtcNow.AddYears(-25), "Male", location);

        _mockRepository.Setup(r => r.GetByUserIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);

        // Act
        var result = await _service.GetProfileAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(userId);
        result.FullName.Should().Be("John Doe");
    }

    [Fact]
    public async Task GetProfileAsync_WhenProfileNotFound_ShouldReturnNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var memberId = new MemberId(userId);

        _mockRepository.Setup(r => r.GetByUserIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MemberProfile?)null);

        // Act
        var result = await _service.GetProfileAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region DeleteProfileAsync Tests

    [Fact]
    public async Task DeleteProfileAsync_WhenProfileExists_ShouldSoftDelete()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var memberId = new MemberId(userId);
        var existingProfile = MemberProfile.CreateSkeleton(memberId, "test@example.com");
        existingProfile.ClearDomainEvents(); // Clear ProfileCreated event from skeleton creation

        _mockRepository.Setup(r => r.GetByUserIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);

        // Act
        await _service.DeleteProfileAsync(userId);

        // Assert
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<MemberProfile>(p => p.DeletedAt != null), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteProfileAsync_WhenProfileNotFound_ShouldThrowException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var memberId = new MemberId(userId);

        _mockRepository.Setup(r => r.GetByUserIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MemberProfile?)null);

        // Act & Assert
        var act = async () => await _service.DeleteProfileAsync(userId);
        await act.Should().ThrowAsync<ProfileDomainException>()
            .WithMessage("*Profile not found*");
    }

    #endregion
}
