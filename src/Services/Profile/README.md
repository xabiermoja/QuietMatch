# Profile Service

The Profile Service manages user profiles for the QuietMatch dating application, including personality traits, values, lifestyle preferences, and matching criteria.

## Architecture

This service follows Clean Architecture principles with Domain-Driven Design (DDD) patterns:

```
DatingApp.ProfileService.Api/              # API Layer (Controllers, Middleware)
DatingApp.ProfileService.Core/             # Domain + Application Layer
  ├── Domain/                              # Domain Layer
  │   ├── Entities/                        # Aggregate Roots
  │   ├── ValueObjects/                    # Immutable Value Objects
  │   ├── Events/                          # Domain Events
  │   └── Interfaces/                      # Repository Abstractions
  └── Application/                         # Application Layer
      ├── Services/                        # Use Cases / Application Services
      └── DTOs/                            # Data Transfer Objects
DatingApp.ProfileService.Infrastructure/   # Infrastructure Layer
  ├── Persistence/                         # EF Core, Repositories
  ├── Messaging/                           # RabbitMQ Integration
  └── Security/                            # Encryption Services
DatingApp.ProfileService.Tests.Unit/       # Unit Tests
```

### Key Design Patterns

- **Aggregate Root**: `MemberProfile` enforces business invariants
- **Value Objects**: Immutable objects like `PersonalityProfile`, `Values`, `Location`
- **Domain Events**: `ProfileCreated`, `ProfileUpdated`, `ProfileCompleted`
- **Repository Pattern**: Abstraction over data persistence
- **CQRS-lite**: Separate commands (create/update) from queries (get)
- **Encryption at Rest**: Sensitive data encrypted in database

See [PATTERNS.md](./PATTERNS.md) for detailed pattern documentation.

## Domain Model

### MemberProfile Aggregate

The `MemberProfile` is the aggregate root with the following sections:

1. **Basic Info** (20% completion)
   - Full Name, Date of Birth, Gender, Location

2. **Personality** (20% completion)
   - Big Five traits: Openness, Conscientiousness, Extraversion, Agreeableness, Neuroticism
   - About Me, Life Philosophy

3. **Values** (20% completion)
   - Family Orientation, Career Ambition, Spirituality, Adventure
   - Intellectual Curiosity, Social Justice, Financial Security, Environmentalism

4. **Lifestyle** (20% completion)
   - Exercise Frequency, Diet Type, Smoking Status, Drinking Frequency
   - Pets, Children Preference

5. **Preferences** (20% completion)
   - Age Range, Max Distance, Preferred Languages, Gender Preference

**Profile Completion**: 80% or higher triggers a `ProfileCompleted` event for matching eligibility.

### Privacy Model

Profiles support three exposure levels:
- **Public**: Visible to all matched users
- **MatchedOnly**: Visible only to mutual matches
- **Private**: Not shared in matching

## API Endpoints

All endpoints require JWT authentication with `userId` claim.

### Get Profile
```http
GET /api/profiles/{userId}
Authorization: Bearer <token>
```

**Response**: `200 OK`
```json
{
  "userId": "guid",
  "email": "user@example.com",
  "fullName": "John Doe",
  "age": 28,
  "gender": "Male",
  "location": {
    "city": "New York",
    "country": "USA",
    "latitude": 40.7128,
    "longitude": -74.0060
  },
  "personality": {
    "openness": 4,
    "conscientiousness": 3,
    "extraversion": 5,
    "agreeableness": 4,
    "neuroticism": 2,
    "aboutMe": "I love coding and hiking",
    "lifePhilosophy": "Live and let live"
  },
  "completionPercentage": 80,
  "isComplete": true
}
```

### Create Basic Profile
```http
POST /api/profiles/{userId}/basic
Authorization: Bearer <token>
Content-Type: application/json
```

**Request Body**:
```json
{
  "fullName": "John Doe",
  "dateOfBirth": "1995-05-15T00:00:00Z",
  "gender": "Male",
  "location": {
    "city": "New York",
    "country": "USA",
    "latitude": 40.7128,
    "longitude": -74.0060
  }
}
```

**Response**: `201 Created` with profile DTO

### Update Profile
```http
PUT /api/profiles/{userId}
Authorization: Bearer <token>
Content-Type: application/json
```

**Request Body** (all fields optional):
```json
{
  "personality": {
    "openness": 4,
    "conscientiousness": 3,
    "extraversion": 5,
    "agreeableness": 4,
    "neuroticism": 2,
    "aboutMe": "I love coding and hiking",
    "lifePhilosophy": "Live and let live"
  },
  "values": {
    "familyOrientation": 5,
    "careerAmbition": 4,
    "spirituality": 3,
    "adventure": 5,
    "intellectualCuriosity": 4,
    "socialJustice": 3,
    "financialSecurity": 4,
    "environmentalism": 5
  },
  "lifestyle": {
    "exerciseFrequency": "Regularly",
    "dietType": "Vegetarian",
    "smokingStatus": "Never",
    "drinkingFrequency": "Socially",
    "hasPets": true,
    "wantsChildren": "Maybe"
  },
  "preferences": {
    "preferredAgeRange": {
      "min": 25,
      "max": 35
    },
    "maxDistanceKm": 50,
    "preferredLanguages": ["English", "Spanish"],
    "genderPreference": "Women"
  },
  "exposureLevel": "MatchedOnly"
}
```

**Response**: `200 OK` with updated profile DTO

### Delete Profile
```http
DELETE /api/profiles/{userId}
Authorization: Bearer <token>
```

**Response**: `204 No Content`

Note: This is a soft delete. The profile is marked as deleted but not removed from the database.

## Event Publishing

The service publishes domain events to RabbitMQ:

### ProfileCreated
```json
{
  "memberId": "guid",
  "email": "user@example.com",
  "createdAt": "2025-11-27T10:00:00Z"
}
```

### ProfileUpdated
```json
{
  "memberId": "guid",
  "updatedAt": "2025-11-27T10:05:00Z"
}
```

### ProfileCompleted
```json
{
  "memberId": "guid",
  "completedAt": "2025-11-27T10:10:00Z"
}
```

Published to exchange: `profile-events` with routing keys:
- `profile.created`
- `profile.updated`
- `profile.completed`

## Configuration

### Environment Variables

```bash
# Database
ConnectionStrings__ProfileDb=Host=postgres;Port=5432;Database=profile_db;Username=admin;Password=***

# RabbitMQ
RabbitMQ__Host=rabbitmq
RabbitMQ__Username=guest
RabbitMQ__Password=guest

# JWT Authentication
Jwt__Issuer=DatingApp.IdentityService
Jwt__Audience=DatingApp.ProfileService
Jwt__Secret=*** # Min 32 bytes

# Encryption (Base64-encoded key)
Encryption__Key=***

# Logging (optional)
Seq__Url=http://seq:80
```

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

## Local Development

### Prerequisites

- .NET 8.0 SDK
- Docker and Docker Compose
- PostgreSQL 16 with pgvector (via Docker)
- RabbitMQ (via Docker)

### Setup

1. **Start infrastructure**:
```bash
docker-compose up -d postgres rabbitmq redis seq
```

2. **Run database migrations**:
```bash
cd src/Services/Profile/DatingApp.ProfileService.Api
dotnet ef database update
```

3. **Run the service**:
```bash
dotnet run
```

Service will be available at `http://localhost:5002`

### Running Tests

```bash
# Unit tests
cd src/Services/Profile/DatingApp.ProfileService.Tests.Unit
dotnet test

# All tests with coverage
dotnet test /p:CollectCoverage=true
```

## Docker Deployment

### Build Image

```bash
docker build -f src/Services/Profile/Dockerfile -t quietmatch-profile:latest .
```

### Run with Docker Compose

```bash
# Start all services including profile-service
docker-compose up -d

# View logs
docker-compose logs -f profile-service

# Stop services
docker-compose down
```

Profile service will be available at `http://localhost:5002`

## Database Schema

### MemberProfiles Table

| Column | Type | Description |
|--------|------|-------------|
| Id | uuid | Primary key |
| UserId | uuid | FK to Identity service (encrypted) |
| Email | text | User email (encrypted) |
| FullName | text | Full name (encrypted) |
| DateOfBirth | timestamp | Date of birth (encrypted) |
| Gender | text | Gender (encrypted) |
| LocationCity | text | City name (encrypted) |
| LocationCountry | text | Country name (encrypted) |
| LocationLatitude | decimal | GPS latitude (nullable) |
| LocationLongitude | decimal | GPS longitude (nullable) |
| Personality* | json | Big Five traits + text fields (encrypted) |
| Values* | json | 8 value dimensions (encrypted) |
| Lifestyle* | json | Lifestyle preferences (encrypted) |
| Preferences* | json | Matching preferences (encrypted) |
| CompletionPercentage | int | 0-100% |
| ExposureLevel | int | Privacy setting enum |
| CreatedAt | timestamp | Creation timestamp |
| UpdatedAt | timestamp | Last update timestamp |
| DeletedAt | timestamp | Soft delete timestamp (nullable) |

*JSON columns store encrypted value objects

### Indexes

- `IX_MemberProfiles_UserId`: Fast lookup by user ID
- `IX_MemberProfiles_DeletedAt`: Filter out deleted profiles

## Security

### Encryption at Rest

Sensitive PII is encrypted using AES-256 before storage:
- UserId, Email, FullName, DateOfBirth, Gender
- Location details (city, country)
- All JSON value objects (Personality, Values, Lifestyle, Preferences)

Encryption key is provided via `Encryption__Key` environment variable (Base64-encoded, min 32 bytes).

### JWT Authentication

All API endpoints require JWT bearer token with:
- Valid signature (verified against `Jwt__Secret`)
- Matching issuer and audience
- `userId` claim matching the route parameter

## Monitoring

### Health Checks

```http
GET /health
```

Returns service health status including database connectivity.

### Logging

Structured logs are sent to Seq (configured via `Seq__Url`) with the following levels:
- **Information**: API calls, domain events
- **Warning**: Validation failures, business rule violations
- **Error**: Exceptions, infrastructure failures

### Metrics

Key metrics to monitor:
- Profile creation rate
- Profile completion rate (reaching 80%)
- API response times
- Database query performance
- RabbitMQ message publishing rate

## Troubleshooting

### Database Connection Issues

Check the connection string and ensure PostgreSQL is running:
```bash
docker-compose ps postgres
docker-compose logs postgres
```

### RabbitMQ Publishing Failures

Verify RabbitMQ is healthy:
```bash
docker-compose ps rabbitmq
docker-compose logs rabbitmq
```

Access RabbitMQ Management UI: `http://localhost:15672` (guest/guest)

### Encryption Errors

Ensure `Encryption__Key` is:
- Base64-encoded
- At least 32 bytes when decoded
- Consistent across service restarts

### Profile Not Found Errors

Common causes:
- Skeleton profile not created by Identity service
- Incorrect userId in JWT claim
- Profile soft-deleted (check `DeletedAt` column)

## Contributing

### Code Style

- Follow C# coding conventions
- Use domain language (Ubiquitous Language)
- Keep aggregates consistent
- Validate in value object constructors
- Raise domain events for state changes

### Testing Requirements

- Unit test coverage > 80%
- All domain logic must be tested
- Integration tests for API endpoints
- Test both happy paths and error cases

### Commit Messages

Follow Conventional Commits:
- `feat(profile):` - New features
- `fix(profile):` - Bug fixes
- `test(profile):` - Test additions
- `refactor(profile):` - Code refactoring
- `docs(profile):` - Documentation

## References

- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Domain-Driven Design](https://martinfowler.com/bliki/DomainDrivenDesign.html)
- [Big Five Personality Traits](https://en.wikipedia.org/wiki/Big_Five_personality_traits)
- [EF Core Documentation](https://learn.microsoft.com/en-us/ef/core/)
- [RabbitMQ .NET Client](https://www.rabbitmq.com/dotnet.html)
