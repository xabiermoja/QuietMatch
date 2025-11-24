using DatingApp.IdentityService.Domain.Entities;
using DatingApp.IdentityService.Domain.Enums;

namespace DatingApp.IdentityService.Domain.Repositories;

/// <summary>
/// Repository interface for User entity operations.
/// </summary>
/// <remarks>
/// Layered Architecture: Interface defined in Domain, implemented in Infrastructure.
/// This enables dependency inversion - Application layer depends on abstraction, not implementation.
/// </remarks>
public interface IUserRepository
{
    /// <summary>
    /// Retrieves a user by their unique identifier.
    /// </summary>
    /// <param name="id">The user's unique ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The user if found, null otherwise</returns>
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a user by their OAuth provider and external user ID.
    /// </summary>
    /// <param name="provider">The OAuth provider (Google, Apple)</param>
    /// <param name="externalUserId">The provider's unique user ID (e.g., Google 'sub')</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The user if found, null otherwise</returns>
    /// <remarks>
    /// Primary lookup method during authentication.
    /// Used to check if a user already exists before creating a new one.
    /// Database has a unique index on (Provider, ExternalUserId) for performance.
    /// </remarks>
    Task<User?> GetByExternalUserIdAsync(AuthProvider provider, string externalUserId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a user by their email address.
    /// </summary>
    /// <param name="email">The user's email</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The user if found, null otherwise</returns>
    /// <remarks>
    /// Used for user lookup by email (e.g., password reset, profile search).
    /// Note: Email is not unique across providers (same email can be used with Google and Apple).
    /// </remarks>
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Adds a new user to the repository.
    /// </summary>
    /// <param name="user">The user entity to add</param>
    /// <param name="ct">Cancellation token</param>
    /// <remarks>
    /// Called when a new user signs in for the first time.
    /// Implementation should call SaveChangesAsync to persist to database.
    /// </remarks>
    Task AddAsync(User user, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing user in the repository.
    /// </summary>
    /// <param name="user">The user entity to update</param>
    /// <param name="ct">Cancellation token</param>
    /// <remarks>
    /// Called when user properties change (e.g., RecordLogin updates LastLoginAt).
    /// Implementation should call SaveChangesAsync to persist to database.
    /// </remarks>
    Task UpdateAsync(User user, CancellationToken ct = default);
}
