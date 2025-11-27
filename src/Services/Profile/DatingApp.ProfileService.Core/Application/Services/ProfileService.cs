using DatingApp.ProfileService.Core.Application.DTOs;
using DatingApp.ProfileService.Core.Domain.Entities;
using DatingApp.ProfileService.Core.Domain.Exceptions;
using DatingApp.ProfileService.Core.Domain.Interfaces;
using DatingApp.ProfileService.Core.Domain.ValueObjects;

namespace DatingApp.ProfileService.Core.Application.Services;

/// <summary>
/// Application service for profile management use cases.
/// </summary>
/// <remarks>
/// This service orchestrates profile operations by:
/// 1. Validating input (via FluentValidation in API layer)
/// 2. Loading domain aggregates from repository
/// 3. Calling domain methods (business logic in MemberProfile)
/// 4. Persisting changes via repository
/// 5. Publishing domain events via message publisher
/// 6. Mapping domain entities to DTOs
///
/// Following Onion Architecture: Application layer coordinates but doesn't contain business logic.
/// </remarks>
public class ProfileService : IProfileService
{
    private readonly IProfileRepository _repository;
    private readonly IMessagePublisher _messagePublisher;

    public ProfileService(
        IProfileRepository repository,
        IMessagePublisher messagePublisher)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _messagePublisher = messagePublisher ?? throw new ArgumentNullException(nameof(messagePublisher));
    }

    /// <summary>
    /// Creates basic profile information for a user.
    /// </summary>
    /// <remarks>
    /// Use Case: AC2 - Create/Update Basic Information
    /// Called when user completes onboarding or updates basic info.
    /// </remarks>
    public async Task<ProfileResponse> CreateBasicProfileAsync(
        Guid userId,
        CreateProfileRequest request,
        CancellationToken ct = default)
    {
        var memberId = new MemberId(userId);

        // Load existing profile (should already exist as skeleton from UserRegistered event)
        var profile = await _repository.GetByUserIdAsync(memberId, ct);
        if (profile == null)
        {
            throw new ProfileDomainException($"Profile not found for user {userId}. Profile should be created via UserRegistered event.");
        }

        // Convert DTO to value object
        var location = new Location(
            request.Location.City,
            request.Location.Country,
            request.Location.Latitude,
            request.Location.Longitude
        );

        // Call domain method (business logic)
        profile.UpdateBasicInfo(request.FullName, request.DateOfBirth, request.Gender, location);

        // Persist
        await _repository.UpdateAsync(profile, ct);

        // Publish domain events
        await PublishDomainEventsAsync(profile, ct);

        return MapToResponse(profile);
    }

    /// <summary>
    /// Updates profile sections (personality, values, lifestyle, preferences, exposure).
    /// </summary>
    /// <remarks>
    /// Use Case: AC3-AC6 - Update personality/values/lifestyle/preferences
    /// Supports partial updates - only provided sections are updated.
    /// </remarks>
    public async Task<ProfileResponse> UpdateProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken ct = default)
    {
        var memberId = new MemberId(userId);

        var profile = await _repository.GetByUserIdAsync(memberId, ct);
        if (profile == null)
        {
            throw new ProfileDomainException($"Profile not found for user {userId}");
        }

        // Update personality if provided
        if (request.Personality != null)
        {
            var personality = new PersonalityProfile(
                request.Personality.Openness,
                request.Personality.Conscientiousness,
                request.Personality.Extraversion,
                request.Personality.Agreeableness,
                request.Personality.Neuroticism,
                request.Personality.AboutMe,
                request.Personality.LifePhilosophy
            );
            profile.UpdatePersonality(personality);
        }

        // Update values if provided
        if (request.Values != null)
        {
            var values = new Values(
                request.Values.FamilyOrientation,
                request.Values.CareerAmbition,
                request.Values.Spirituality,
                request.Values.Adventure,
                request.Values.IntellectualCuriosity,
                request.Values.SocialJustice,
                request.Values.FinancialSecurity,
                request.Values.Environmentalism
            );
            profile.UpdateValues(values);
        }

        // Update lifestyle if provided
        if (request.Lifestyle != null)
        {
            var lifestyle = new Lifestyle(
                Enum.Parse<ExerciseFrequency>(request.Lifestyle.ExerciseFrequency),
                Enum.Parse<DietType>(request.Lifestyle.DietType),
                Enum.Parse<SmokingStatus>(request.Lifestyle.SmokingStatus),
                Enum.Parse<DrinkingFrequency>(request.Lifestyle.DrinkingFrequency),
                request.Lifestyle.HasPets,
                Enum.Parse<ChildrenPreference>(request.Lifestyle.WantsChildren)
            );
            profile.UpdateLifestyle(lifestyle);
        }

        // Update preferences if provided
        if (request.Preferences != null)
        {
            var ageRange = new AgeRange(
                request.Preferences.PreferredAgeRange.Min,
                request.Preferences.PreferredAgeRange.Max
            );
            var preferences = new PreferenceSet(
                ageRange,
                request.Preferences.MaxDistanceKm,
                request.Preferences.PreferredLanguages,
                Enum.Parse<GenderPreference>(request.Preferences.GenderPreference)
            );
            profile.UpdatePreferences(preferences);
        }

        // Update exposure level if provided
        if (request.ExposureLevel.HasValue)
        {
            profile.UpdateExposureLevel((ExposureLevel)request.ExposureLevel.Value);
        }

        // Persist
        await _repository.UpdateAsync(profile, ct);

        // Publish domain events
        await PublishDomainEventsAsync(profile, ct);

        return MapToResponse(profile);
    }

    /// <summary>
    /// Retrieves a profile by user ID.
    /// </summary>
    /// <remarks>
    /// Use Case: AC7 - Retrieve complete profile
    /// </remarks>
    public async Task<ProfileResponse?> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var memberId = new MemberId(userId);
        var profile = await _repository.GetByUserIdAsync(memberId, ct);

        return profile == null ? null : MapToResponse(profile);
    }

    /// <summary>
    /// Soft-deletes a profile (GDPR compliance).
    /// </summary>
    /// <remarks>
    /// Use Case: GDPR6 - Right to erasure
    /// Soft delete allows for 30-day retention before hard deletion.
    /// </remarks>
    public async Task DeleteProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var memberId = new MemberId(userId);
        var profile = await _repository.GetByUserIdAsync(memberId, ct);

        if (profile == null)
        {
            throw new ProfileDomainException($"Profile not found for user {userId}");
        }

        profile.SoftDelete();
        await _repository.UpdateAsync(profile, ct);
    }

    /// <summary>
    /// Publishes all pending domain events from the aggregate.
    /// </summary>
    private async Task PublishDomainEventsAsync(MemberProfile profile, CancellationToken ct)
    {
        foreach (var domainEvent in profile.DomainEvents)
        {
            await _messagePublisher.PublishAsync(domainEvent, ct);
        }

        profile.ClearDomainEvents();
    }

    /// <summary>
    /// Maps MemberProfile domain entity to ProfileResponse DTO.
    /// </summary>
    private static ProfileResponse MapToResponse(MemberProfile profile)
    {
        return new ProfileResponse
        {
            UserId = profile.UserId.Value,
            FullName = profile.FullName,
            DateOfBirth = profile.DateOfBirth,
            Gender = profile.Gender,
            Location = new LocationDto
            {
                City = profile.Location.City,
                Country = profile.Location.Country,
                Latitude = profile.Location.Latitude,
                Longitude = profile.Location.Longitude
            },
            Personality = profile.Personality == null ? null : new PersonalityDto
            {
                Openness = profile.Personality.Openness,
                Conscientiousness = profile.Personality.Conscientiousness,
                Extraversion = profile.Personality.Extraversion,
                Agreeableness = profile.Personality.Agreeableness,
                Neuroticism = profile.Personality.Neuroticism,
                AboutMe = profile.Personality.AboutMe,
                LifePhilosophy = profile.Personality.LifePhilosophy
            },
            Values = profile.Values == null ? null : new ValuesDto
            {
                FamilyOrientation = profile.Values.FamilyOrientation,
                CareerAmbition = profile.Values.CareerAmbition,
                Spirituality = profile.Values.Spirituality,
                Adventure = profile.Values.Adventure,
                IntellectualCuriosity = profile.Values.IntellectualCuriosity,
                SocialJustice = profile.Values.SocialJustice,
                FinancialSecurity = profile.Values.FinancialSecurity,
                Environmentalism = profile.Values.Environmentalism
            },
            Lifestyle = profile.Lifestyle == null ? null : new LifestyleDto
            {
                ExerciseFrequency = profile.Lifestyle.ExerciseFrequency.ToString(),
                DietType = profile.Lifestyle.DietType.ToString(),
                SmokingStatus = profile.Lifestyle.SmokingStatus.ToString(),
                DrinkingFrequency = profile.Lifestyle.DrinkingFrequency.ToString(),
                HasPets = profile.Lifestyle.HasPets,
                WantsChildren = profile.Lifestyle.WantsChildren.ToString()
            },
            Preferences = profile.Preferences == null ? null : new PreferencesDto
            {
                PreferredAgeRange = new AgeRangeDto
                {
                    Min = profile.Preferences.PreferredAgeRange.Min,
                    Max = profile.Preferences.PreferredAgeRange.Max
                },
                MaxDistanceKm = profile.Preferences.MaxDistanceKm,
                PreferredLanguages = profile.Preferences.PreferredLanguages.ToList(),
                GenderPreference = profile.Preferences.GenderPreference.ToString()
            },
            ExposureLevel = (ExposureLevelDto)profile.ExposureLevel,
            CompletionPercentage = profile.CompletionPercentage,
            IsComplete = profile.IsComplete,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt
        };
    }
}
