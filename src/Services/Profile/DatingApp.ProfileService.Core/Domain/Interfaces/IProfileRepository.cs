using DatingApp.ProfileService.Core.Domain.Entities;
using DatingApp.ProfileService.Core.Domain.ValueObjects;

namespace DatingApp.ProfileService.Core.Domain.Interfaces;

/// <summary>
/// Repository interface (Port) for MemberProfile aggregate.
/// </summary>
/// <remarks>
/// This is a port defined in the Domain layer following Onion Architecture.
/// The concrete implementation (adapter) will be in the Infrastructure layer.
///
/// Methods follow repository pattern conventions:
/// - GetByUserIdAsync: Retrieve profile by user ID
/// - AddAsync: Add new profile
/// - UpdateAsync: Update existing profile
/// - ExistsAsync: Check if profile exists
///
/// All methods are async and support cancellation tokens.
/// </remarks>
public interface IProfileRepository
{
    /// <summary>
    /// Retrieves a member profile by user ID.
    /// </summary>
    /// <param name="userId">The unique member ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The member profile if found, null otherwise</returns>
    Task<MemberProfile?> GetByUserIdAsync(MemberId userId, CancellationToken ct = default);

    /// <summary>
    /// Adds a new member profile to the repository.
    /// </summary>
    /// <param name="profile">The profile to add</param>
    /// <param name="ct">Cancellation token</param>
    Task AddAsync(MemberProfile profile, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing member profile.
    /// </summary>
    /// <param name="profile">The profile to update</param>
    /// <param name="ct">Cancellation token</param>
    Task UpdateAsync(MemberProfile profile, CancellationToken ct = default);

    /// <summary>
    /// Checks if a profile exists for the given user ID.
    /// </summary>
    /// <param name="userId">The unique member ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if profile exists, false otherwise</returns>
    Task<bool> ExistsAsync(MemberId userId, CancellationToken ct = default);
}
