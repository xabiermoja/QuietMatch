namespace DatingApp.IdentityService.Application.DTOs;

/// <summary>
/// Request DTO for Google Sign-In authentication.
/// </summary>
/// <param name="IdToken">The Google ID token (JWT) received from the client</param>
/// <remarks>
/// This is the input from the client after they complete Google OAuth flow.
/// The client exchanges the authorization code for an ID token with Google,
/// then sends this ID token to our API for validation.
/// We validate it server-side to prevent tampering.
/// </remarks>
public record LoginWithGoogleRequest(string IdToken);
