using DatingApp.IdentityService.Domain.Entities;
using DatingApp.IdentityService.Domain.Repositories;
using DatingApp.IdentityService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DatingApp.IdentityService.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for RefreshToken entity using Entity Framework Core.
/// </summary>
/// <remarks>
/// Layered Architecture: Implements interface from Domain, uses DbContext from Infrastructure.
/// Optimized for token validation and session management queries.
/// </remarks>
public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IdentityDbContext _dbContext;

    public RefreshTokenRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc />
    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException("Token hash cannot be empty", nameof(tokenHash));

        // Primary token validation lookup - uses unique index on TokenHash
        // This is the most frequent query during token refresh flow
        return await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        var now = DateTime.UtcNow;

        // Find tokens that are:
        // 1. Not revoked (IsRevoked == false)
        // 2. Not expired (ExpiresAt > now)
        // Uses composite index on (UserId, IsRevoked) for performance
        return await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > now)
            .OrderByDescending(rt => rt.CreatedAt) // Most recent first
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RefreshToken>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        // Returns all tokens regardless of status (active, revoked, expired)
        // Used for administrative purposes and security audits
        return await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId)
            .OrderByDescending(rt => rt.CreatedAt)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task AddAsync(RefreshToken token, CancellationToken ct = default)
    {
        if (token is null)
            throw new ArgumentNullException(nameof(token));

        await _dbContext.RefreshTokens.AddAsync(token, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(RefreshToken token, CancellationToken ct = default)
    {
        if (token is null)
            throw new ArgumentNullException(nameof(token));

        // EF Core tracks changes automatically if entity was loaded from context
        // If entity is detached, this marks it as modified
        _dbContext.RefreshTokens.Update(token);
        await _dbContext.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task RevokeAllByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        var now = DateTime.UtcNow;

        // Find all active tokens for the user
        var activeTokens = await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > now)
            .ToListAsync(ct);

        // Revoke each token (calls domain method to ensure business rules)
        foreach (var token in activeTokens)
        {
            token.Revoke();
        }

        // Save all changes in one transaction
        await _dbContext.SaveChangesAsync(ct);
    }
}
