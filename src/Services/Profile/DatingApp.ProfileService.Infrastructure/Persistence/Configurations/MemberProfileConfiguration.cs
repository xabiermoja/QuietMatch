using DatingApp.ProfileService.Core.Domain.Entities;
using DatingApp.ProfileService.Core.Domain.ValueObjects;
using DatingApp.ProfileService.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DatingApp.ProfileService.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for MemberProfile aggregate root.
/// </summary>
/// <remarks>
/// Configures:
/// - Table mapping to "member_profiles" (PostgreSQL naming convention)
/// - Primary key on UserId (MemberId value object)
/// - Field-level encryption for FullName using EncryptedStringConverter
/// - Value objects as owned entities or JSON columns
/// - Soft delete global query filter
/// - Indexes for performance
/// - Domain events ignored (not persisted)
/// </remarks>
public class MemberProfileConfiguration : IEntityTypeConfiguration<MemberProfile>
{
    public void Configure(EntityTypeBuilder<MemberProfile> builder)
    {
        // Table name (PostgreSQL snake_case convention)
        builder.ToTable("member_profiles");

        // Primary Key - UserId (MemberId value object)
        builder.HasKey(p => p.UserId);
        builder.Property(p => p.UserId)
            .HasConversion(
                id => id.Value,
                value => new MemberId(value))
            .HasColumnName("user_id");

        // Personal Information
        builder.Property(p => p.FullName)
            .IsRequired()
            .HasMaxLength(500) // Encrypted data is longer than plaintext
            .HasColumnName("full_name")
            .HasConversion<EncryptedStringConverter>(); // Field-level encryption

        builder.Property(p => p.DateOfBirth)
            .IsRequired()
            .HasColumnName("date_of_birth");

        builder.Property(p => p.Gender)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("gender");

        // Location (owned entity)
        builder.OwnsOne(p => p.Location, location =>
        {
            location.Property(l => l.City)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("city");

            location.Property(l => l.Country)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("country");

            location.Property(l => l.Latitude)
                .HasColumnName("latitude")
                .HasColumnType("decimal(9,6)"); // Precision for coordinates

            location.Property(l => l.Longitude)
                .HasColumnName("longitude")
                .HasColumnType("decimal(9,6)");
        });

        // PersonalityProfile (owned entity - complex structure)
        builder.OwnsOne(p => p.Personality, personality =>
        {
            personality.Property(pp => pp.Openness)
                .IsRequired()
                .HasColumnName("personality_openness");

            personality.Property(pp => pp.Conscientiousness)
                .IsRequired()
                .HasColumnName("personality_conscientiousness");

            personality.Property(pp => pp.Extraversion)
                .IsRequired()
                .HasColumnName("personality_extraversion");

            personality.Property(pp => pp.Agreeableness)
                .IsRequired()
                .HasColumnName("personality_agreeableness");

            personality.Property(pp => pp.Neuroticism)
                .IsRequired()
                .HasColumnName("personality_neuroticism");

            personality.Property(pp => pp.AboutMe)
                .HasMaxLength(500)
                .HasColumnName("personality_about_me");

            personality.Property(pp => pp.LifePhilosophy)
                .HasMaxLength(500)
                .HasColumnName("personality_life_philosophy");
        });

        // Values (owned entity)
        builder.OwnsOne(p => p.Values, values =>
        {
            values.Property(v => v.FamilyOrientation)
                .IsRequired()
                .HasColumnName("values_family_orientation");

            values.Property(v => v.CareerAmbition)
                .IsRequired()
                .HasColumnName("values_career_ambition");

            values.Property(v => v.Spirituality)
                .IsRequired()
                .HasColumnName("values_spirituality");

            values.Property(v => v.Adventure)
                .IsRequired()
                .HasColumnName("values_adventure");

            values.Property(v => v.IntellectualCuriosity)
                .IsRequired()
                .HasColumnName("values_intellectual_curiosity");

            values.Property(v => v.SocialJustice)
                .IsRequired()
                .HasColumnName("values_social_justice");

            values.Property(v => v.FinancialSecurity)
                .IsRequired()
                .HasColumnName("values_financial_security");

            values.Property(v => v.Environmentalism)
                .IsRequired()
                .HasColumnName("values_environmentalism");
        });

        // Lifestyle (owned entity)
        builder.OwnsOne(p => p.Lifestyle, lifestyle =>
        {
            lifestyle.Property(l => l.ExerciseFrequency)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50)
                .HasColumnName("lifestyle_exercise_frequency");

            lifestyle.Property(l => l.DietType)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50)
                .HasColumnName("lifestyle_diet_type");

            lifestyle.Property(l => l.SmokingStatus)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50)
                .HasColumnName("lifestyle_smoking_status");

            lifestyle.Property(l => l.DrinkingFrequency)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50)
                .HasColumnName("lifestyle_drinking_frequency");

            lifestyle.Property(l => l.HasPets)
                .IsRequired()
                .HasColumnName("lifestyle_has_pets");

            lifestyle.Property(l => l.WantsChildren)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50)
                .HasColumnName("lifestyle_wants_children");
        });

        // PreferenceSet (owned entity with nested AgeRange)
        builder.OwnsOne(p => p.Preferences, preferences =>
        {
            // AgeRange (nested owned entity)
            preferences.OwnsOne(pref => pref.PreferredAgeRange, ageRange =>
            {
                ageRange.Property(ar => ar.Min)
                    .IsRequired()
                    .HasColumnName("preferences_age_min");

                ageRange.Property(ar => ar.Max)
                    .IsRequired()
                    .HasColumnName("preferences_age_max");
            });

            preferences.Property(pref => pref.MaxDistanceKm)
                .IsRequired()
                .HasColumnName("preferences_max_distance_km");

            preferences.Property(pref => pref.PreferredLanguages)
                .IsRequired()
                .HasColumnName("preferences_languages")
                .HasColumnType("jsonb"); // PostgreSQL JSONB for list

            preferences.Property(pref => pref.GenderPreference)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50)
                .HasColumnName("preferences_gender");
        });

        // Privacy
        builder.Property(p => p.ExposureLevel)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasColumnName("exposure_level");

        // Metadata
        builder.Property(p => p.CompletionPercentage)
            .IsRequired()
            .HasColumnName("completion_percentage");

        builder.Property(p => p.IsComplete)
            .IsRequired()
            .HasColumnName("is_complete");

        builder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at");

        builder.Property(p => p.UpdatedAt)
            .IsRequired()
            .HasColumnName("updated_at");

        builder.Property(p => p.DeletedAt)
            .HasColumnName("deleted_at");

        // Indexes
        builder.HasIndex(p => p.DeletedAt)
            .HasDatabaseName("ix_member_profiles_deleted_at");

        builder.HasIndex(p => p.IsComplete)
            .HasDatabaseName("ix_member_profiles_is_complete");

        builder.HasIndex(p => p.CreatedAt)
            .HasDatabaseName("ix_member_profiles_created_at");

        // Global query filter for soft delete
        builder.HasQueryFilter(p => p.DeletedAt == null);

        // Ignore domain events (not persisted)
        builder.Ignore(p => p.DomainEvents);
    }
}
