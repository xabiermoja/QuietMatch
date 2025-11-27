using DatingApp.ProfileService.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DatingApp.ProfileService.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core DbContext for Profile service.
/// </summary>
/// <remarks>
/// Manages member profile data persistence to PostgreSQL.
///
/// Features:
/// - Entity configurations via IEntityTypeConfiguration
/// - Field-level encryption for FullName via EncryptedStringConverter
/// - Owned entities for value objects
/// - PostgreSQL-specific features (JSONB for complex value objects)
/// - Soft delete support (DeletedAt filter)
/// </remarks>
public class ProfileDbContext : DbContext
{
    public ProfileDbContext(DbContextOptions<ProfileDbContext> options) : base(options)
    {
    }

    public DbSet<MemberProfile> MemberProfiles => Set<MemberProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProfileDbContext).Assembly);
    }

    /// <summary>
    /// Applies global query filters (e.g., soft delete).
    /// </summary>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        // Enable sensitive data logging in development (can be configured via appsettings)
        // optionsBuilder.EnableSensitiveDataLogging();
    }
}
