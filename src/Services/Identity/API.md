# IdentityService API Reference

Version: 1.0
Base URL: `http://localhost:8080` (development)
Protocol: HTTP (development), HTTPS (production recommended)

## Overview

The IdentityService provides authentication and identity management for the QuietMatch dating application. This initial release (F0001) implements Sign In with Google using OAuth 2.0.

## Authentication

All endpoints except `/api/v1/auth/login/google` require a valid JWT access token in the Authorization header:

```
Authorization: Bearer {access_token}
```

## Endpoints

### POST /api/v1/auth/login/google

Authenticates a user using Google OAuth 2.0 and issues JWT tokens.

**Request**

- Method: `POST`
- Content-Type: `application/json`
- Body:
```json
{
  "idToken": "string (required)"
}
```

**Parameters**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| idToken | string | Yes | Google ID token obtained from Google Sign-In client |

**Response**

**Success (200 OK)**

```json
{
  "accessToken": "string",
  "refreshToken": "string",
  "expiresIn": 900,
  "tokenType": "Bearer",
  "userId": "guid",
  "isNewUser": boolean,
  "email": "string"
}
```

| Field | Type | Description |
|-------|------|-------------|
| accessToken | string | JWT access token for API authentication (15 min lifetime) |
| refreshToken | string | Refresh token for obtaining new access tokens (7 day lifetime) |
| expiresIn | integer | Access token expiry in seconds (900 = 15 minutes) |
| tokenType | string | Always "Bearer" |
| userId | string (GUID) | Unique identifier for the user |
| isNewUser | boolean | `true` if this is the user's first sign-in, `false` otherwise |
| email | string | User's email address from Google |

**Error Responses**

**Invalid Token (401 Unauthorized)**

```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Invalid Google ID token"
}
```

**Validation Error (400 Bad Request)**

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "IdToken": [
      "'Id Token' must not be empty."
    ]
  }
}
```

**Example Requests**

**cURL**

```bash
curl -X POST http://localhost:8080/api/v1/auth/login/google \
  -H "Content-Type: application/json" \
  -d '{
    "idToken": "eyJhbGciOiJSUzI1NiIsImtpZCI6IjE..."
  }'
```

**JavaScript (fetch)**

```javascript
const response = await fetch('http://localhost:8080/api/v1/auth/login/google', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    idToken: googleIdToken
  })
});

const data = await response.json();

if (response.ok) {
  // Store tokens
  localStorage.setItem('accessToken', data.accessToken);
  localStorage.setItem('refreshToken', data.refreshToken);

  // Redirect based on isNewUser
  if (data.isNewUser) {
    window.location.href = '/onboarding';
  } else {
    window.location.href = '/dashboard';
  }
} else {
  console.error('Authentication failed:', data);
}
```

**Python (requests)**

```python
import requests

url = "http://localhost:8080/api/v1/auth/login/google"
payload = {
    "idToken": google_id_token
}
headers = {
    "Content-Type": "application/json"
}

response = requests.post(url, json=payload, headers=headers)

if response.status_code == 200:
    data = response.json()
    access_token = data['accessToken']
    refresh_token = data['refreshToken']
    is_new_user = data['isNewUser']
else:
    print(f"Error: {response.status_code}")
    print(response.json())
```

## JWT Access Token

The access token is a JSON Web Token (JWT) with the following structure:

**Header**
```json
{
  "alg": "HS256",
  "typ": "JWT"
}
```

**Payload (Claims)**
```json
{
  "sub": "12345678-1234-1234-1234-123456789abc",
  "email": "user@example.com",
  "jti": "unique-token-id",
  "iat": 1700000000,
  "exp": 1700000900,
  "iss": "https://quietmatch.com",
  "aud": "https://api.quietmatch.com"
}
```

| Claim | Description |
|-------|-------------|
| sub | Subject (user ID) |
| email | User's email address |
| jti | JWT ID (unique identifier for this token) |
| iat | Issued at (Unix timestamp) |
| exp | Expiry time (Unix timestamp, 15 minutes after iat) |
| iss | Issuer (QuietMatch) |
| aud | Audience (API consumers) |

**Signature**

Tokens are signed using HMAC-SHA256 with a secret key configured in the service.

## Refresh Token

The refresh token is a cryptographically random 32-byte value encoded in Base64 (44 characters). It is used to obtain new access tokens without re-authenticating with Google.

**Storage**:
- Client: Store securely (httpOnly cookie recommended for web, secure storage for mobile)
- Server: SHA-256 hash stored in database

**Lifetime**: 7 days

**Note**: The refresh token endpoint is not yet implemented. It will be added in a future feature.

## Error Handling

All errors follow RFC 7807 (Problem Details for HTTP APIs) format:

```json
{
  "type": "string (URI reference)",
  "title": "string",
  "status": integer,
  "detail": "string (optional)",
  "instance": "string (optional)"
}
```

### Common Error Codes

| Status Code | Description |
|-------------|-------------|
| 400 | Bad Request - Invalid input (validation failed) |
| 401 | Unauthorized - Invalid or expired Google ID token |
| 500 | Internal Server Error - Unexpected server error |
| 503 | Service Unavailable - Database or RabbitMQ unavailable |

## Rate Limiting

⚠️ **Not yet implemented**. Future versions will implement rate limiting:
- 10 requests per minute per IP for authentication endpoints
- 429 Too Many Requests status code when exceeded

## Security Considerations

### HTTPS

**Development**: HTTP is acceptable
**Production**: HTTPS is **required**

All tokens must be transmitted over HTTPS in production to prevent interception.

### Token Storage

**Web Applications**:
- Access Token: Memory or sessionStorage (never localStorage)
- Refresh Token: httpOnly, Secure, SameSite cookie

**Mobile Applications**:
- Use platform-specific secure storage (Keychain on iOS, Keystore on Android)

### Token Revocation

Refresh tokens can be revoked by:
1. User logout (future endpoint)
2. Password change (future feature)
3. Security breach detection (manual admin action)

Revoked tokens are marked in the database and cannot be used to obtain new access tokens.

## Integration Events

When a new user signs in for the first time, a `UserRegistered` event is published to RabbitMQ:

```json
{
  "userId": "guid",
  "email": "string",
  "provider": "Google",
  "registeredAt": "datetime (ISO 8601)",
  "correlationId": "guid"
}
```

This event is consumed by the ProfileService to create a user profile.

## Swagger / OpenAPI

Swagger UI is available in development mode:

```
http://localhost:8080
```

OpenAPI specification can be accessed at:

```
http://localhost:8080/swagger/v1/swagger.json
```

## Monitoring & Logging

All requests and responses are logged using Serilog with structured logging.

**Log Sinks**:
- Console (all environments)
- Seq (Docker environment) - http://localhost:5341

**Log Levels**:
- Information: Successful authentication, user creation
- Warning: Failed authentication attempts
- Error: Unexpected errors, external service failures

**Structured Data**:
- Request ID
- User ID (after authentication)
- Email (after authentication)
- Correlation ID (for events)

## Health Checks

⚠️ **Not yet implemented**. Future versions will include:

```
GET /health
GET /health/ready
GET /health/live
```

## Versioning

API versioning is implemented via URL path:

```
/api/v1/auth/login/google
```

Future versions will use `/api/v2/...` for breaking changes.

## Deprecation Policy

When endpoints are deprecated:
1. Announcement 3 months in advance
2. `Deprecated` header in responses
3. Alternative endpoint documented
4. Minimum 6 months support after deprecation

## Future Endpoints (Planned)

### POST /api/v1/auth/refresh
Obtain new access token using refresh token

### POST /api/v1/auth/revoke
Revoke refresh token (logout)

### POST /api/v1/auth/login/apple
Sign in with Apple

### GET /api/v1/auth/me
Get current user information

## Support

For issues or questions:
- GitHub Issues: https://github.com/your-org/quietmatch/issues
- Documentation: See TESTING.md for testing procedures
- Architecture: See docs/10_architecture/02_architecture-guidelines.md

## Changelog

### Version 1.0 (2025-11-24) - F0001
- Initial release
- POST /api/v1/auth/login/google endpoint
- JWT access token generation
- Refresh token generation
- Google OAuth 2.0 integration
- User creation and authentication
- Event publishing for new users
