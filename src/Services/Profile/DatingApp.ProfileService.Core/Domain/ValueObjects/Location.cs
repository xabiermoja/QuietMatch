namespace DatingApp.ProfileService.Core.Domain.ValueObjects;

/// <summary>
/// Value Object representing a geographic location.
/// </summary>
/// <remarks>
/// Contains city/country for display purposes and lat/lon for distance calculations.
/// Coordinates are used by MatchingService to compute proximity.
/// </remarks>
public record Location
{
    public string City { get; init; }
    public string Country { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }

    public Location(string city, string country, decimal? latitude = null, decimal? longitude = null)
    {
        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City is required", nameof(city));

        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("Country is required", nameof(country));

        // Validate coordinates if provided
        if (latitude.HasValue && (latitude.Value < -90 || latitude.Value > 90))
            throw new ArgumentException("Latitude must be between -90 and 90", nameof(latitude));

        if (longitude.HasValue && (longitude.Value < -180 || longitude.Value > 180))
            throw new ArgumentException("Longitude must be between -180 and 180", nameof(longitude));

        City = city;
        Country = country;
        Latitude = latitude;
        Longitude = longitude;
    }
}
