using DatingApp.IdentityService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DatingApp.IdentityService.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework Core configuration for RefreshToken entity.
/// </summary>
/// <remarks>
/// Fluent API configuration for precise database schema control.
/// Matches the DDL specification in F0001 feature file.
/// Security: TokenHash unique constraint prevents token reuse.
/// </remarks>
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        // Table name: lowercase with underscores (PostgreSQL convention)
        builder.ToTable("refresh_tokens");

        // Primary key
        builder.HasKey(rt => rt.Id);

        // Properties
        builder.Property(rt => rt.Id)
            .IsRequired()
            .ValueGeneratedNever(); // We generate Guid in domain (RefreshToken.Create)

        builder.Property(rt => rt.UserId)
            .IsRequired();

        builder.Property(rt => rt.TokenHash)
            .IsRequired()
            .HasMaxLength(255); // SHA-256 hash encoded as Base64 is ~44 chars, but leave room

        builder.Property(rt => rt.ExpiresAt)
            .IsRequired();

        builder.Property(rt => rt.CreatedAt)
            .IsRequired();

        builder.Property(rt => rt.RevokedAt)
            .IsRequired(false); // Nullable - null if token is still active

        builder.Property(rt => rt.IsRevoked)
            .IsRequired()
            .HasDefaultValue(false); // Default to not revoked

        // Indexes
        // Index on UserId for fast lookup of user's tokens
        builder.HasIndex(rt => rt.UserId)
            .HasDatabaseName("idx_refresh_tokens_user_id");

        // Unique index on TokenHash - prevent duplicate tokens
        // SECURITY: Ensures each token hash is unique across the system
        builder.HasIndex(rt => rt.TokenHash)
            .IsUnique()
            .HasDatabaseName("idx_refresh_tokens_token_hash");

        // Index on ExpiresAt for efficient cleanup queries (find expired tokens)
        builder.HasIndex(rt => rt.ExpiresAt)
            .HasDatabaseName("idx_refresh_tokens_expires_at");

        // Composite index on (UserId, IsRevoked) for fast lookup of active tokens per user
        builder.HasIndex(rt => new { rt.UserId, rt.IsRevoked })
            .HasDatabaseName("idx_refresh_tokens_user_id_is_revoked");

        // Relationships
        // Many RefreshTokens -> One User (configured in UserConfiguration)
        // Foreign key cascade delete is configured in UserConfiguration
    }
}
