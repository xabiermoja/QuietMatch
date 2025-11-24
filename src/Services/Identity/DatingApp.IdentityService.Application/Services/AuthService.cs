using DatingApp.IdentityService.Application.DTOs;
using DatingApp.IdentityService.Domain.Entities;
using DatingApp.IdentityService.Domain.Enums;
using DatingApp.IdentityService.Domain.Repositories;
using DatingApp.IdentityService.Infrastructure.Events;
using DatingApp.IdentityService.Infrastructure.Services;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace DatingApp.IdentityService.Application.Services;

/// <summary>
/// Application service for authentication operations.
/// </summary>
/// <remarks>
/// Layered Architecture: Orchestrates domain entities, repositories, and infrastructure services.
/// This is where the business logic flow lives - the "how" of authentication.
/// </remarks>
public class AuthService
{
    private readonly IGoogleAuthService _googleAuthService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IGoogleAuthService googleAuthService,
        IJwtTokenGenerator jwtTokenGenerator,
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPublishEndpoint publishEndpoint,
        ILogger<AuthService> logger)
    {
        _googleAuthService = googleAuthService ?? throw new ArgumentNullException(nameof(googleAuthService));
        _jwtTokenGenerator = jwtTokenGenerator ?? throw new ArgumentNullException(nameof(jwtTokenGenerator));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
        _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Authenticates a user with Google and issues JWT tokens.
    /// </summary>
    /// <param name="idToken">Google ID token from client</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>LoginResponse if successful, null if authentication failed</returns>
    /// <remarks>
    /// Orchestration flow (implements acceptance criteria AC6-AC11 from F0001):
    /// 1. Validate Google ID token (AC6)
    /// 2. Find existing user or create new user (AC7, AC8)
    /// 3. Generate JWT access token (AC9)
    /// 4. Generate and store refresh token (AC10)
    /// 5. Return login response (AC11)
    /// </remarks>
    public async Task<LoginResponse?> LoginWithGoogleAsync(string idToken, CancellationToken ct = default)
    {
        // AC6: Validate ID token with Google API
        _logger.LogInformation("Validating Google ID token");
        var googleUser = await _googleAuthService.ValidateIdTokenAsync(idToken, ct);

        if (googleUser is null)
        {
            _logger.LogWarning("Google ID token validation failed");
            return null;
        }

        _logger.LogInformation(
            "Google ID token validated successfully for user {Email} (sub: {Sub})",
            googleUser.Email,
            googleUser.Sub);

        // Find existing user by provider + external user ID
        var user = await _userRepository.GetByExternalUserIdAsync(
            AuthProvider.Google,
            googleUser.Sub,
            ct);

        bool isNewUser;

        if (user is null)
        {
            // AC7: Create new user if not exists
            _logger.LogInformation(
                "New user detected - creating user for {Email}",
                googleUser.Email);

            user = User.CreateFromGoogle(googleUser.Email, googleUser.Sub);
            await _userRepository.AddAsync(user, ct);

            isNewUser = true;

            _logger.LogInformation(
                "User created successfully: UserId={UserId}, Email={Email}",
                user.Id,
                user.Email);

            // Publish UserRegistered event for ProfileService
            var userRegisteredEvent = new UserRegistered(
                UserId: user.Id,
                Email: user.Email,
                Provider: user.Provider.ToString(),
                RegisteredAt: user.CreatedAt,
                CorrelationId: Guid.NewGuid()
            );

            await _publishEndpoint.Publish(userRegisteredEvent, ct);

            _logger.LogInformation(
                "Published UserRegistered event for UserId={UserId}, CorrelationId={CorrelationId}",
                user.Id,
                userRegisteredEvent.CorrelationId);
        }
        else
        {
            // AC8: Update LastLoginAt for existing user
            _logger.LogInformation(
                "Existing user detected - updating last login for UserId={UserId}",
                user.Id);

            user.RecordLogin();
            await _userRepository.UpdateAsync(user, ct);

            isNewUser = false;
        }

        // AC9: Generate JWT access token
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email);

        // AC10: Generate refresh token
        var refreshTokenValue = _jwtTokenGenerator.GenerateRefreshToken();
        var refreshTokenHash = _jwtTokenGenerator.HashToken(refreshTokenValue);

        // Create refresh token entity (7 days validity)
        var refreshToken = RefreshToken.Create(user.Id, refreshTokenHash, validityDays: 7);
        await _refreshTokenRepository.AddAsync(refreshToken, ct);

        _logger.LogInformation(
            "Tokens generated successfully for UserId={UserId}, IsNewUser={IsNewUser}",
            user.Id,
            isNewUser);

        // AC11: Return login response
        return new LoginResponse(
            AccessToken: accessToken,
            RefreshToken: refreshTokenValue, // Return plain text token to client (only hash is stored)
            ExpiresIn: 900, // 15 minutes = 900 seconds
            TokenType: "Bearer",
            UserId: user.Id,
            IsNewUser: isNewUser,
            Email: user.Email
        );
    }
}
