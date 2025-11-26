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

    /// <summary>
    /// Refreshes an expired access token using a valid refresh token.
    /// </summary>
    /// <param name="refreshToken">The refresh token from the client</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>RefreshTokenResponse with new tokens if successful, null if validation failed</returns>
    /// <remarks>
    /// Implements OAuth 2.0 Refresh Token Flow (RFC 6749 Section 6).
    ///
    /// Flow:
    /// 1. Hash the incoming refresh token
    /// 2. Look up token in database by hash
    /// 3. Validate token is not expired or revoked
    /// 4. Get associated user
    /// 5. Generate new access token
    /// 6. Token Rotation: Generate new refresh token, revoke old one (security best practice)
    /// 7. Return response with new tokens
    ///
    /// Security Considerations:
    /// - Refresh tokens are hashed before storage (SHA-256)
    /// - Token rotation prevents token replay attacks
    /// - Expired/revoked tokens are rejected immediately
    /// - Rate limiting should be applied at API level (5 requests/min recommended)
    /// </remarks>
    public async Task<RefreshTokenResponse?> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        _logger.LogInformation("Processing refresh token request");

        // 1. Hash the incoming refresh token (same algorithm used when storing)
        var tokenHash = _jwtTokenGenerator.HashToken(refreshToken);

        // 2. Look up token in database by hash
        var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, ct);

        if (storedToken is null)
        {
            _logger.LogWarning("Refresh token not found in database (invalid or already rotated)");
            return null;
        }

        // 3. Validate token is not expired or revoked
        if (!storedToken.IsValid())
        {
            _logger.LogWarning(
                "Refresh token validation failed - TokenId={TokenId}, IsRevoked={IsRevoked}, IsExpired={IsExpired}",
                storedToken.Id,
                storedToken.IsRevoked,
                storedToken.IsExpired());
            return null;
        }

        // 4. Get associated user
        var user = await _userRepository.GetByIdAsync(storedToken.UserId, ct);

        if (user is null)
        {
            _logger.LogError(
                "User not found for valid refresh token - UserId={UserId}, TokenId={TokenId}",
                storedToken.UserId,
                storedToken.Id);
            return null;
        }

        _logger.LogInformation(
            "Refresh token validated successfully for UserId={UserId}",
            user.Id);

        // 5. Generate new access token
        var newAccessToken = _jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email);

        // 6. Token Rotation: Generate new refresh token and revoke old one
        var newRefreshTokenValue = _jwtTokenGenerator.GenerateRefreshToken();
        var newRefreshTokenHash = _jwtTokenGenerator.HashToken(newRefreshTokenValue);

        // Revoke old refresh token (security: prevent reuse)
        storedToken.Revoke();
        await _refreshTokenRepository.UpdateAsync(storedToken, ct);

        // Create new refresh token entity (7 days validity)
        var newRefreshToken = RefreshToken.Create(user.Id, newRefreshTokenHash, validityDays: 7);
        await _refreshTokenRepository.AddAsync(newRefreshToken, ct);

        _logger.LogInformation(
            "Token rotation successful - OldTokenId={OldTokenId} revoked, NewTokenId={NewTokenId} created for UserId={UserId}",
            storedToken.Id,
            newRefreshToken.Id,
            user.Id);

        // 7. Return response with new tokens
        return new RefreshTokenResponse(
            AccessToken: newAccessToken,
            RefreshToken: newRefreshTokenValue, // Return plain text token to client (only hash is stored)
            ExpiresIn: 900, // 15 minutes = 900 seconds
            TokenType: "Bearer"
        );
    }

    /// <summary>
    /// Revokes a refresh token, making it unusable for future refresh operations.
    /// </summary>
    /// <param name="refreshToken">The refresh token to revoke</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if token was revoked, false if token not found</returns>
    /// <remarks>
    /// Implements RFC 7009 - OAuth 2.0 Token Revocation.
    ///
    /// Idempotent Operation:
    /// - If token doesn't exist: returns true (no-op, considered success)
    /// - If token already revoked: returns true (no-op, already in desired state)
    /// - If token is valid: revokes it and returns true
    ///
    /// Use Cases:
    /// - User explicitly logs out from a specific device
    /// - User revokes access from security settings
    /// - Security incident requires token invalidation
    /// </remarks>
    public async Task<bool> RevokeTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        _logger.LogInformation("Processing token revocation request");

        // 1. Hash the incoming refresh token
        var tokenHash = _jwtTokenGenerator.HashToken(refreshToken);

        // 2. Find token in database
        var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, ct);

        if (storedToken is null)
        {
            _logger.LogInformation("Token not found in database - treating as already revoked (idempotent)");
            return true; // Idempotent: token doesn't exist = already "revoked"
        }

        // 3. If already revoked, return success (idempotent)
        if (storedToken.IsRevoked)
        {
            _logger.LogInformation(
                "Token already revoked - TokenId={TokenId}, RevokedAt={RevokedAt} (idempotent)",
                storedToken.Id,
                storedToken.RevokedAt);
            return true;
        }

        // 4. Revoke the token
        storedToken.Revoke();
        await _refreshTokenRepository.UpdateAsync(storedToken, ct);

        _logger.LogInformation(
            "Token revoked successfully - TokenId={TokenId}, UserId={UserId}",
            storedToken.Id,
            storedToken.UserId);

        return true;
    }

    /// <summary>
    /// Revokes all active refresh tokens for a user (logout from all devices).
    /// </summary>
    /// <param name="userId">The user's unique ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Number of tokens revoked</returns>
    /// <remarks>
    /// Use Cases:
    /// - User clicks "Logout from all devices" in security settings
    /// - Security incident requires invalidating all user sessions
    /// - Password reset should invalidate all existing tokens
    ///
    /// Implementation:
    /// - Uses repository method to revoke all tokens in one transaction
    /// - Only revokes active (non-revoked, non-expired) tokens
    /// - Returns count of tokens actually revoked
    /// </remarks>
    public async Task<int> RevokeAllTokensForUserAsync(Guid userId, CancellationToken ct = default)
    {
        _logger.LogInformation("Revoking all tokens for UserId={UserId}", userId);

        // Get all active tokens before revoking
        var activeTokens = await _refreshTokenRepository.GetActiveByUserIdAsync(userId, ct);
        var tokenCount = activeTokens.Count();

        if (tokenCount == 0)
        {
            _logger.LogInformation("No active tokens found for UserId={UserId}", userId);
            return 0;
        }

        // Revoke all active tokens
        await _refreshTokenRepository.RevokeAllByUserIdAsync(userId, ct);

        _logger.LogInformation(
            "Revoked {TokenCount} tokens for UserId={UserId}",
            tokenCount,
            userId);

        return tokenCount;
    }
}
