using DatingApp.IdentityService.Domain.Entities;
using DatingApp.IdentityService.Domain.Enums;
using DatingApp.IdentityService.Domain.Repositories;
using DatingApp.IdentityService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DatingApp.IdentityService.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for User entity using Entity Framework Core.
/// </summary>
/// <remarks>
/// Layered Architecture: Implements interface from Domain, uses DbContext from Infrastructure.
/// All methods save changes to database immediately (no Unit of Work pattern for simplicity).
/// </remarks>
public class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _dbContext;

    public UserRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc />
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        // FindAsync is optimized for primary key lookups
        return await _dbContext.Users.FindAsync(new object[] { id }, ct);
    }

    /// <inheritdoc />
    public async Task<User?> GetByExternalUserIdAsync(
        AuthProvider provider,
        string externalUserId,
        CancellationToken ct = default)
    {
        // Primary authentication lookup - uses unique index on (Provider, ExternalUserId)
        // This is the most frequent query in authentication flow
        return await _dbContext.Users
            .FirstOrDefaultAsync(
                u => u.Provider == provider && u.ExternalUserId == externalUserId,
                ct);
    }

    /// <inheritdoc />
    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        // Note: Email is NOT unique across providers (same email can use Google and Apple)
        // Returns first match - caller should filter by provider if needed
        // Uses index on Email for performance
        return await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == email, ct);
    }

    /// <inheritdoc />
    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        if (user is null)
            throw new ArgumentNullException(nameof(user));

        await _dbContext.Users.AddAsync(user, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        if (user is null)
            throw new ArgumentNullException(nameof(user));

        // EF Core tracks changes automatically if entity was loaded from context
        // If entity is detached, this marks it as modified
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync(ct);
    }
}
