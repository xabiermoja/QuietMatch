using DatingApp.ProfileService.Core.Application.DTOs;
using DatingApp.ProfileService.Core.Application.Services;
using DatingApp.ProfileService.Core.Application.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DatingApp.ProfileService.Api.Controllers;

/// <summary>
/// API controller for member profile management.
/// </summary>
/// <remarks>
/// Provides REST endpoints for profile CRUD operations.
///
/// Endpoints:
/// - POST /api/profiles: Create/update basic profile information
/// - PUT /api/profiles/{userId}: Update profile sections
/// - GET /api/profiles/{userId}: Retrieve profile by user ID
/// - GET /api/profiles/me: Retrieve current user's profile
/// - DELETE /api/profiles/{userId}: Soft-delete profile (GDPR)
///
/// Authentication: All endpoints require JWT bearer token.
/// Authorization: Users can only access/modify their own profiles (except admins).
/// </remarks>
[ApiController]
[Route("api/profiles")]
[Authorize] // All endpoints require authentication
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;
    private readonly IValidator<CreateProfileRequest> _createValidator;
    private readonly IValidator<UpdateProfileRequest> _updateValidator;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(
        IProfileService profileService,
        IValidator<CreateProfileRequest> createValidator,
        IValidator<UpdateProfileRequest> updateValidator,
        ILogger<ProfileController> logger)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
        _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates or updates basic profile information.
    /// </summary>
    /// <remarks>
    /// AC2: Create/update basic profile information (name, DOB, gender, location)
    /// </remarks>
    /// <param name="request">Basic profile information</param>
    /// <returns>Complete profile with updated information</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfileResponse>> CreateBasicProfile(
        [FromBody] CreateProfileRequest request,
        CancellationToken ct)
    {
        var userId = GetAuthenticatedUserId();

        _logger.LogInformation("Creating basic profile for UserId: {UserId}", userId);

        // Validate request
        var validationResult = await _createValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(
                validationResult.ToDictionary()
            ));
        }

        try
        {
            var profile = await _profileService.CreateBasicProfileAsync(userId, request, ct);
            return Ok(profile);
        }
        catch (Core.Domain.Exceptions.ProfileDomainException ex)
        {
            _logger.LogWarning(ex, "Domain validation failed for UserId: {UserId}", userId);
            return BadRequest(new ProblemDetails
            {
                Title = "Domain Validation Error",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    /// <summary>
    /// Updates profile sections (personality, values, lifestyle, preferences, exposure).
    /// </summary>
    /// <remarks>
    /// AC3-AC6: Update personality, values, lifestyle, preferences
    /// Supports partial updates - only provided sections are updated.
    /// </remarks>
    /// <param name="userId">User ID of the profile to update</param>
    /// <param name="request">Profile sections to update</param>
    /// <returns>Complete profile with updated information</returns>
    [HttpPut("{userId:guid}")]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfileResponse>> UpdateProfile(
        Guid userId,
        [FromBody] UpdateProfileRequest request,
        CancellationToken ct)
    {
        // Authorization: Users can only update their own profile
        var authenticatedUserId = GetAuthenticatedUserId();
        if (authenticatedUserId != userId)
        {
            _logger.LogWarning(
                "Unauthorized profile update attempt. AuthenticatedUserId: {AuthenticatedUserId}, TargetUserId: {TargetUserId}",
                authenticatedUserId, userId);
            return Forbid();
        }

        _logger.LogInformation("Updating profile for UserId: {UserId}", userId);

        // Validate request
        var validationResult = await _updateValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(
                validationResult.ToDictionary()
            ));
        }

        try
        {
            var profile = await _profileService.UpdateProfileAsync(userId, request, ct);
            return Ok(profile);
        }
        catch (Core.Domain.Exceptions.ProfileDomainException ex)
        {
            _logger.LogWarning(ex, "Domain validation failed for UserId: {UserId}", userId);
            return BadRequest(new ProblemDetails
            {
                Title = "Domain Validation Error",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    /// <summary>
    /// Retrieves a profile by user ID.
    /// </summary>
    /// <remarks>
    /// AC7: Retrieve complete profile
    /// Authorization: Users can only access their own profile (privacy protection).
    /// </remarks>
    /// <param name="userId">User ID of the profile to retrieve</param>
    /// <returns>Complete profile information</returns>
    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfileResponse>> GetProfile(
        Guid userId,
        CancellationToken ct)
    {
        // Authorization: Users can only access their own profile
        var authenticatedUserId = GetAuthenticatedUserId();
        if (authenticatedUserId != userId)
        {
            _logger.LogWarning(
                "Unauthorized profile access attempt. AuthenticatedUserId: {AuthenticatedUserId}, TargetUserId: {TargetUserId}",
                authenticatedUserId, userId);
            return Forbid();
        }

        _logger.LogInformation("Retrieving profile for UserId: {UserId}", userId);

        var profile = await _profileService.GetProfileAsync(userId, ct);

        if (profile == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Profile Not Found",
                Detail = $"Profile not found for user {userId}",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(profile);
    }

    /// <summary>
    /// Retrieves the current authenticated user's profile.
    /// </summary>
    /// <remarks>
    /// AC7: Retrieve complete profile
    /// Convenience endpoint for getting own profile without specifying user ID.
    /// </remarks>
    /// <returns>Complete profile information</returns>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfileResponse>> GetMyProfile(CancellationToken ct)
    {
        var userId = GetAuthenticatedUserId();

        _logger.LogInformation("Retrieving own profile for UserId: {UserId}", userId);

        var profile = await _profileService.GetProfileAsync(userId, ct);

        if (profile == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Profile Not Found",
                Detail = $"Profile not found for user {userId}",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(profile);
    }

    /// <summary>
    /// Soft-deletes a profile (GDPR compliance - right to erasure).
    /// </summary>
    /// <remarks>
    /// GDPR6: Right to erasure
    /// Soft delete allows for 30-day retention period before hard deletion.
    /// </remarks>
    /// <param name="userId">User ID of the profile to delete</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProfile(
        Guid userId,
        CancellationToken ct)
    {
        // Authorization: Users can only delete their own profile
        var authenticatedUserId = GetAuthenticatedUserId();
        if (authenticatedUserId != userId)
        {
            _logger.LogWarning(
                "Unauthorized profile deletion attempt. AuthenticatedUserId: {AuthenticatedUserId}, TargetUserId: {TargetUserId}",
                authenticatedUserId, userId);
            return Forbid();
        }

        _logger.LogInformation("Soft-deleting profile for UserId: {UserId}", userId);

        try
        {
            await _profileService.DeleteProfileAsync(userId, ct);
            return NoContent();
        }
        catch (Core.Domain.Exceptions.ProfileDomainException ex)
        {
            _logger.LogWarning(ex, "Profile deletion failed for UserId: {UserId}", userId);
            return NotFound(new ProblemDetails
            {
                Title = "Profile Not Found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
    }

    /// <summary>
    /// Extracts the authenticated user ID from the JWT claims.
    /// </summary>
    /// <returns>User ID from JWT "sub" claim</returns>
    private Guid GetAuthenticatedUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? throw new UnauthorizedAccessException("User ID not found in JWT token");

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException($"Invalid user ID format in JWT token: {userIdClaim}");
        }

        return userId;
    }
}
