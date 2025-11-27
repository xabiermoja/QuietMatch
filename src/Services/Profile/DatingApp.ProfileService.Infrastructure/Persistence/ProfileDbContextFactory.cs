using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DatingApp.ProfileService.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF Core migrations.
/// </summary>
/// <remarks>
/// This factory allows EF Core tools (dotnet ef migrations) to create a DbContext instance
/// without requiring the full application DI container.
///
/// Used only at design time for migrations - not used at runtime.
/// </remarks>
public class ProfileDbContextFactory : IDesignTimeDbContextFactory<ProfileDbContext>
{
    public ProfileDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ProfileDbContext>();

        // Use a placeholder connection string for migrations
        // The actual connection string will be provided at runtime via appsettings.json
        optionsBuilder.UseNpgsql("Host=localhost;Database=profile_db;Username=postgres;Password=postgres");

        return new ProfileDbContext(optionsBuilder.Options);
    }
}
