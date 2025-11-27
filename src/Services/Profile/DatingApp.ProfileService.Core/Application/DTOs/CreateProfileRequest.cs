namespace DatingApp.ProfileService.Core.Application.DTOs;

/// <summary>
/// Request DTO for creating basic profile information.
/// </summary>
/// <remarks>
/// Used by POST /api/profiles endpoint.
/// Maps to MemberProfile.UpdateBasicInfo() domain method.
/// </remarks>
public class CreateProfileRequest
{
    public string FullName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public LocationDto Location { get; set; } = null!;
}

/// <summary>
/// Location DTO for address information.
/// </summary>
public class LocationDto
{
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}
