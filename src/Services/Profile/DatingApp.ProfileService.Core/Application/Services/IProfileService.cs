using DatingApp.ProfileService.Core.Application.DTOs;

namespace DatingApp.ProfileService.Core.Application.Services;

/// <summary>
/// Interface for profile management application service.
/// </summary>
/// <remarks>
/// Defines use cases for profile operations.
/// Implementation orchestrates domain logic and infrastructure concerns.
/// </remarks>
public interface IProfileService
{
    /// <summary>
    /// Creates basic profile information for a user.
    /// </summary>
    Task<ProfileResponse> CreateBasicProfileAsync(Guid userId, CreateProfileRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates profile sections (personality, values, lifestyle, preferences, exposure).
    /// </summary>
    Task<ProfileResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a profile by user ID.
    /// </summary>
    Task<ProfileResponse?> GetProfileAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Soft-deletes a profile (GDPR compliance).
    /// </summary>
    Task DeleteProfileAsync(Guid userId, CancellationToken ct = default);
}
