using DatingApp.IdentityService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DatingApp.IdentityService.Infrastructure.Data;

/// <summary>
/// Database context for the IdentityService.
/// </summary>
/// <remarks>
/// Layered Architecture: DbContext lives in Infrastructure layer, depends on Domain entities.
/// Applies all entity configurations from assembly automatically.
/// </remarks>
public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Users registered in QuietMatch via OAuth providers.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Refresh tokens for session management.
    /// </summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from this assembly
        // This finds and applies all classes implementing IEntityTypeConfiguration<T>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }
}
