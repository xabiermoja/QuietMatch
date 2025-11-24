using DatingApp.IdentityService.Domain.Entities;
using DatingApp.IdentityService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DatingApp.IdentityService.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework Core configuration for User entity.
/// </summary>
/// <remarks>
/// Fluent API configuration for precise database schema control.
/// Matches the DDL specification in F0001 feature file.
/// </remarks>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Table name: lowercase with underscores (PostgreSQL convention)
        builder.ToTable("users");

        // Primary key
        builder.HasKey(u => u.Id);

        // Properties
        builder.Property(u => u.Id)
            .IsRequired()
            .ValueGeneratedNever(); // We generate Guid in domain (User.CreateFromGoogle)

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255);

        // Enum stored as string in database (more readable, easier to extend)
        builder.Property(u => u.Provider)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion<string>(); // "Google", "Apple"

        builder.Property(u => u.ExternalUserId)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.LastLoginAt)
            .IsRequired(false); // Nullable - null for new users who haven't logged in yet

        // Indexes
        // Index on Email for fast lookup by email
        builder.HasIndex(u => u.Email)
            .HasDatabaseName("idx_users_email");

        // Unique index on (Provider, ExternalUserId) - prevent duplicate users from same provider
        // This is critical: Same Google account should map to same QuietMatch user
        builder.HasIndex(u => new { u.Provider, u.ExternalUserId })
            .IsUnique()
            .HasDatabaseName("idx_users_provider_external_user_id");

        // Relationships
        // One User -> Many RefreshTokens
        builder.HasMany(u => u.RefreshTokens)
            .WithOne(rt => rt.User)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade); // Delete tokens when user is deleted
    }
}
