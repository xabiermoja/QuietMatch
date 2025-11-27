using DatingApp.ProfileService.Core.Domain.Entities;
using DatingApp.ProfileService.Core.Domain.Interfaces;
using DatingApp.ProfileService.Core.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace DatingApp.ProfileService.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of IProfileRepository (adapter for domain port).
/// </summary>
/// <remarks>
/// Implements repository pattern using Entity Framework Core.
///
/// Features:
/// - Async/await for all operations
/// - Include() for loading value objects (owned entities)
/// - AsNoTracking() for read-only queries
/// - SaveChangesAsync() for write operations
/// - Respects soft delete global query filter
///
/// This is the adapter that connects the domain layer to the infrastructure persistence.
/// </remarks>
public class ProfileRepository : IProfileRepository
{
    private readonly ProfileDbContext _context;

    public ProfileRepository(ProfileDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Retrieves a member profile by user ID.
    /// </summary>
    /// <remarks>
    /// Includes all owned entities (Location, Personality, Values, Lifestyle, Preferences).
    /// Uses AsNoTracking() for better performance on read-only queries.
    /// </remarks>
    public async Task<MemberProfile?> GetByUserIdAsync(MemberId userId, CancellationToken ct = default)
    {
        return await _context.MemberProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);
    }

    /// <summary>
    /// Adds a new member profile to the repository.
    /// </summary>
    /// <remarks>
    /// Called when skeleton profile is created from UserRegistered event.
    /// </remarks>
    public async Task AddAsync(MemberProfile profile, CancellationToken ct = default)
    {
        await _context.MemberProfiles.AddAsync(profile, ct);
        await _context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Updates an existing member profile.
    /// </summary>
    /// <remarks>
    /// EF Core change tracking automatically detects modifications.
    /// SaveChangesAsync() persists all changes.
    /// </remarks>
    public async Task UpdateAsync(MemberProfile profile, CancellationToken ct = default)
    {
        _context.MemberProfiles.Update(profile);
        await _context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Checks if a profile exists for the given user ID.
    /// </summary>
    /// <remarks>
    /// Efficient existence check without loading the entire entity.
    /// </remarks>
    public async Task<bool> ExistsAsync(MemberId userId, CancellationToken ct = default)
    {
        return await _context.MemberProfiles
            .AnyAsync(p => p.UserId == userId, ct);
    }
}
