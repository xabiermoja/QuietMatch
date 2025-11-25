using DatingApp.IdentityService.Application.DTOs;
using DatingApp.IdentityService.Application.Services;
using DatingApp.IdentityService.Application.Validators;
using Microsoft.AspNetCore.Mvc;

namespace DatingApp.IdentityService.Api.Controllers;

/// <summary>
/// Authentication controller for OAuth-based sign-in.
/// </summary>
/// <remarks>
/// Exposes authentication endpoints for social login (Google, Apple).
/// Returns RFC 7807 Problem Details for errors.
/// </remarks>
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Authenticates a user with Google Sign-In.
    /// </summary>
    /// <param name="request">Google ID token from client</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Login response with JWT tokens and user info</returns>
    /// <response code="200">Authentication successful - returns access token, refresh token, and user info</response>
    /// <response code="400">Invalid request - ID token is missing, malformed, or invalid</response>
    /// <response code="429">Rate limit exceeded - too many login attempts</response>
    /// <response code="500">Internal server error - service temporarily unavailable</response>
    [HttpPost("login/google")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> LoginWithGoogle(
        [FromBody] LoginWithGoogleRequest request,
        CancellationToken ct)
    {
        // Input validation
        var validator = new LoginWithGoogleRequestValidator();
        var validationResult = await validator.ValidateAsync(request, ct);

        if (!validationResult.IsValid)
        {
            _logger.LogWarning(
                "Login request validation failed: {Errors}",
                string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

            return BadRequest(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Invalid Request",
                Status = StatusCodes.Status400BadRequest,
                Detail = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)),
                Instance = HttpContext.Request.Path
            });
        }

        try
        {
            // Authenticate with Google
            var response = await _authService.LoginWithGoogleAsync(request.IdToken, ct);

            if (response is null)
            {
                // Google token validation failed
                _logger.LogWarning("Google ID token validation failed");

                return BadRequest(new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Title = "Invalid ID Token",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "The provided ID token is invalid or expired.",
                    Instance = HttpContext.Request.Path
                });
            }

            // Success
            _logger.LogInformation(
                "User {UserId} authenticated successfully via Google (IsNewUser: {IsNewUser})",
                response.UserId,
                response.IsNewUser);

            return Ok(response);
        }
        catch (Exception ex)
        {
            // Unexpected error (database down, Google API unreachable, etc.)
            _logger.LogError(ex, "Error during Google authentication");

            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "Authentication Service Unavailable",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "Unable to validate your Google account. Please try again later.",
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Refreshes an expired access token using a valid refresh token.
    /// </summary>
    /// <param name="request">Refresh token from client</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>New access token and refresh token</returns>
    /// <response code="200">Token refresh successful - returns new access token and refresh token</response>
    /// <response code="400">Invalid request - refresh token is missing or malformed</response>
    /// <response code="401">Unauthorized - refresh token is invalid, expired, or revoked</response>
    /// <response code="429">Rate limit exceeded - too many refresh attempts</response>
    /// <response code="500">Internal server error - service temporarily unavailable</response>
    /// <remarks>
    /// Implements OAuth 2.0 Refresh Token Flow (RFC 6749 Section 6).
    ///
    /// Token Rotation: This endpoint implements token rotation for security.
    /// Each successful refresh returns a NEW refresh token and revokes the old one.
    /// The client must store the new refresh token and discard the old one.
    ///
    /// Error Handling:
    /// - 401 Unauthorized: Token is invalid, expired, or already used (rotated)
    /// - Clients should redirect to login page when receiving 401
    /// </remarks>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        CancellationToken ct)
    {
        // Input validation
        var validator = new RefreshTokenRequestValidator();
        var validationResult = await validator.ValidateAsync(request, ct);

        if (!validationResult.IsValid)
        {
            _logger.LogWarning(
                "Refresh token request validation failed: {Errors}",
                string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

            return BadRequest(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Invalid Request",
                Status = StatusCodes.Status400BadRequest,
                Detail = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)),
                Instance = HttpContext.Request.Path
            });
        }

        try
        {
            // Process refresh token
            var response = await _authService.RefreshTokenAsync(request.RefreshToken, ct);

            if (response is null)
            {
                // Refresh token validation failed (invalid, expired, or revoked)
                _logger.LogWarning("Refresh token validation failed");

                return Unauthorized(new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                    Title = "Invalid Refresh Token",
                    Status = StatusCodes.Status401Unauthorized,
                    Detail = "The provided refresh token is invalid, expired, or has been revoked. Please sign in again.",
                    Instance = HttpContext.Request.Path
                });
            }

            // Success
            _logger.LogInformation("Token refresh successful");

            return Ok(response);
        }
        catch (Exception ex)
        {
            // Unexpected error (database down, etc.)
            _logger.LogError(ex, "Error during token refresh");

            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "Token Refresh Service Unavailable",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "Unable to refresh your access token. Please try again later.",
                Instance = HttpContext.Request.Path
            });
        }
    }
}
