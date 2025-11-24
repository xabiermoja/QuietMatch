# Feature Workflow - QuietMatch

> **How to plan, track, and implement features using our hybrid approach**
>
> This document defines the workflow for adding new features to QuietMatch using feature files + GitHub Issues.

---

## Table of Contents

- [Overview: Hybrid Approach](#overview-hybrid-approach)
- [Workflow Steps](#workflow-steps)
- [Feature File Template](#feature-file-template)
- [GitHub Issue Creation](#github-issue-creation)
- [Implementation Process](#implementation-process)
- [Completion Checklist](#completion-checklist)

---

## Overview: Hybrid Approach

QuietMatch uses a **hybrid feature-driven development approach**:

### Feature Files (Source of Truth)
**Location**: `docs/40_features/f####_feature_name/`

**Structure**:
- `f####_feature_name.md` - Comprehensive specification
- `plan.md` - Detailed implementation plan with doc references

**Feature File Contains**:
- ✅ Full acceptance criteria (all of them)
- ✅ Detailed API specifications
- ✅ Database schema changes
- ✅ Sequence diagrams
- ✅ Testing strategy (unit, integration, manual)
- ✅ High-level implementation checklist
- ✅ Security requirements
- ✅ Configuration details
- ✅ Dependencies

**🔒 IMMUTABLE INPUT**: Feature file is the **specification** - do NOT alter during implementation without human approval

**Implementation Plan Contains**:
- ✅ Step-by-step tasks (expanded from checklist)
- ✅ Documentation references for each task
- ✅ Order dependencies
- ✅ Progress tracking (checkboxes)
- ✅ Notes and decisions made during implementation

**📝 LIVING DOCUMENT**: Plan.md is updated during implementation for progress tracking

**Who reads**: Developers implementing the feature (you!)

---

### GitHub Issues (Tracking)
**Location**: GitHub Issues tab

**Purpose**: Track implementation status and link to PRs

**Contains**:
- ✅ Link to feature file (for full details)
- ✅ One-sentence summary
- ✅ Top 5 acceptance criteria (not all)
- ✅ Top 5 implementation tasks (not all)
- ✅ Status labels (backlog, in-progress, review, done)
- ✅ Assignee
- ✅ Milestone (which phase)

**Who reads**: Team members checking progress, GitHub automation

---

### Why This Approach?

| Aspect | Feature Files | GitHub Issues |
|--------|---------------|---------------|
| **Detail Level** | Comprehensive | Summary |
| **Offline Access** | ✅ Yes (in repo) | ❌ No (need internet) |
| **Tracking** | Manual update | Native GitHub features |
| **Collaboration** | Via PR comments | Via issue comments |
| **Automation** | ❌ Limited | ✅ Great (boards, actions) |
| **Source of Truth** | ✅ YES | ❌ No (points to file) |

**Best of both worlds**: Rich documentation + GitHub integration

---

## 🚨 Change Control and Approval

### What Can Be Updated WITHOUT Human Approval

**In plan.md (implementation plan)**:
- ✅ Check off completed tasks
- ✅ Add notes about implementation details
- ✅ Add newly discovered sub-tasks (within scope)
- ✅ Update timestamps
- ✅ Document technical decisions that don't affect requirements

**Example**: "Added validation for email format" (implementation detail)

---

### What REQUIRES Human Approval

**Changes to feature file** (f####_feature_name.md):
- ❌ Modifying acceptance criteria
- ❌ Changing API specifications
- ❌ Altering database schema
- ❌ Adding/removing requirements

**Changes to plan.md that alter scope**:
- ❌ Adding entirely new phases or major features
- ❌ Removing acceptance criteria from scope
- ❌ Significantly changing architecture approach

**When You Need Approval**:
1. **Stop implementation**
2. **Document the issue** in plan.md under "Blockers/Questions"
3. **Ask human for approval** via issue comment or discussion
4. **Wait for approval** before proceeding
5. **Update feature file** only after approval received
6. **Update plan.md** to reflect approved changes

**Example**: "Discovered Google OAuth requires additional 'profile' scope not in original spec - needs approval to add AC15"

---

## Workflow Steps

### Step 1: Plan Feature (Write Feature File)

**When**: Before writing any code

**Purpose**: Define business requirements and technical specifications - the **WHAT**, not the **HOW**

**Process**:
1. Create folder: `docs/40_features/f####_feature_name/`
   - Use next sequential number (f0001, f0002, etc.)
   - Use lowercase with underscores: `f0001_sign_in_with_google/`

2. Create feature file: `f####_feature_name.md`
   - Copy template from `f0001_sign_in_with_google.md`

3. Fill in all sections with **BUSINESS SPECIFICATIONS ONLY**:

**✅ What to Include** (Business Specifications):
- **Business Goals**: User value, why this feature matters
- **Acceptance Criteria**: ALL functional, non-functional, security requirements
- **User Stories**: Gherkin scenarios (Given/When/Then)
- **API Specification**: Endpoints, request/response formats, HTTP status codes, error responses
- **Database Schema**: DDL statements (CREATE TABLE), relationships, indexes
- **Sequence Diagrams**: Mermaid diagrams showing flows
- **Events Published/Consumed**: Event names, payload structure, subscribers
- **Configuration**: Environment variables needed
- **Testing Requirements**: WHAT needs to be tested (not test code)
  - Example: "Must test that invalid tokens return 400"
  - Example: "Must verify UserRegistered event is published for new users"
- **Security Requirements**: Authentication, authorization, encryption, GDPR
- **Dependencies**: What must be done before/after this feature

**❌ What NOT to Include** (Implementation Details):
- ❌ Code implementations or snippets (except API request/response examples)
- ❌ Test code (xUnit tests, Moq setup, assertions)
- ❌ Class names or method signatures
- ❌ Package/library choices (e.g., "use FluentValidation")
- ❌ Folder structure or file organization
- ❌ "How to implement" instructions

**Example - Good vs Bad**:
```markdown
✅ GOOD (Business Spec):
## Testing Requirements
- Verify invalid Google ID token returns 400 Bad Request
- Verify new user creation triggers UserRegistered event
- Verify rate limiting blocks after 5 attempts per IP per minute
- Verify JWT token contains correct claims (sub, email, jti)

❌ BAD (Implementation Details):
## Testing Strategy
[Fact]
public async Task LoginWithGoogle_WhenTokenInvalid_ShouldReturn400()
{
    var mockService = new Mock<IGoogleAuthService>();
    ...
}
```

4. Commit to repo:
   ```bash
   git add docs/40_features/f0002_your_feature/
   git commit -m "docs: add feature spec for F0002 Your Feature"
   git push
   ```

---

### Step 2: Create Detailed Implementation Plan

**When**: After feature file is committed, before starting implementation

**Purpose**: Create a detailed, smart implementation roadmap that applies all architecture guidelines to this specific feature - the **HOW**

**Process**:
1. Create file: `docs/40_features/f####_feature_name/plan.md`

2. Create a **VERY DETAILED, VERY SMART** implementation roadmap:

**✅ What to Include** (Detailed Smart Roadmap):

**Structure**:
- **Phases**: Organize by architecture layers (Setup, Domain, Infrastructure, Application, API, Messaging, Testing, Docker)
- **Tasks**: Very specific, actionable items with checkboxes
- **Documentation References**: Link to specific docs/sections for each task
- **Architecture Application**: Show how guidelines apply to THIS feature
- **Technical Decisions**: Explain WHY certain approaches are used
- **Dependencies**: Mark order and blocking tasks
- **Entity/Property Lists**: Enumerate what needs to be created (but not the code)
- **Configuration Details**: Specific environment variables, connection strings
- **Testing Breakdown**: Specific test scenarios to implement
- **Progress Tracking**: Checkboxes, notes sections, blockers section

**Example Structure**:
```markdown
# Implementation Plan - F0002: Your Feature Name

**Status**: 🟡 In Progress
**Feature File**: [Link to feature spec]
**Architecture Pattern**: Layered (or Onion/Hexagonal/CQRS)
**Started**: TBD
**Last Updated**: 2025-11-21
**Estimated Total Time**: X hours

## Prerequisites
### Documentation to Review Before Starting
- [ ] Read Architecture Guidelines - Layered Architecture section
- [ ] Read Service Templates - Folder structure for Layered
- [ ] Read Security & Auth - JWT implementation
- [ ] Read Feature Specification completely

### Environment Setup
- [ ] Docker Desktop running
- [ ] Start infrastructure: `docker-compose up -d`
- [ ] Verify database connection

## Phase 0: Setup (X minutes)
- [ ] Create feature branch: `feature/f0002-your-feature-name`
- [ ] Create project structure following Layered template
  - **Reference**: Service Templates - Layered Architecture
  - **Structure**: Domain, Application, Infrastructure, API, Tests
- [ ] Add project references (respecting layer dependencies)
- [ ] Install required NuGet packages
  - **Domain**: (usually none)
  - **Infrastructure**: EF Core, messaging libraries
  - **Application**: FluentValidation, MediatR
  - **API**: Authentication, logging, rate limiting
- [ ] Create PATTERNS.md documenting architecture choice

## Phase 1: Domain Layer (X time)
- [ ] Create User entity
  - **Reference**: Architecture Guidelines - Domain Entities
  - **Reference**: Feature Spec - Database Schema (lines X-Y)
  - **Properties**: Id (Guid), Email (string), Provider (enum), ExternalUserId (string), CreatedAt, LastLoginAt
  - **Business Rules**: Factory method CreateFromGoogle(), RecordLogin() method
  - **Why**: Rich domain model, encapsulates business logic

- [ ] Create RefreshToken entity
  - **Reference**: Security & Auth - Token Management
  - **Properties**: Id, UserId, TokenHash (SHA-256), ExpiresAt, CreatedAt, RevokedAt, IsRevoked
  - **Business Rules**: Create() factory, Revoke() method, IsValid() validation
  - **Relationships**: Many-to-one with User

- [ ] Create repository interfaces
  - **Reference**: Architecture Guidelines - Repository Pattern
  - **Why**: Interfaces in Domain, implementations in Infrastructure (Layered pattern)
  - IUserRepository: GetByIdAsync, GetByExternalUserIdAsync, GetByEmailAsync, AddAsync, UpdateAsync
  - IRefreshTokenRepository: GetByTokenHashAsync, GetActiveByUserIdAsync, AddAsync, UpdateAsync

## Phase 2: Infrastructure - Persistence (X time)
- [ ] Create IdentityDbContext
  - **Reference**: Service Templates - Infrastructure DbContext
  - DbSet<User>, DbSet<RefreshToken>
  - Override OnModelCreating to apply configurations

- [ ] Create entity configurations
  - **Reference**: EF Core Fluent API best practices
  - UserConfiguration: Table mapping, indexes, constraints
  - RefreshTokenConfiguration: Table mapping, foreign keys, indexes
  - **Apply from Feature Spec**: All DDL requirements from Database Schema section

- [ ] Implement repositories
  - **Reference**: Repository Pattern guidelines
  - UserRepository: Implement all IUserRepository methods
  - RefreshTokenRepository: Implement all IRefreshTokenRepository methods
  - **Note**: Use async/await, CancellationToken support

- [ ] Create and apply EF Core migration
  - Migration name: `Add_Users_RefreshTokens_Tables`
  - Verify migration SQL matches feature spec DDL
  - Apply to local database

## Phase 3: Infrastructure - External Services (X time)
- [ ] Create Google OAuth validation service
  - **Reference**: Security & Auth - Google OAuth section
  - **Reference**: Feature Spec - AC6 (validate ID token with Google)
  - Interface: IGoogleAuthService
  - Implementation: GoogleAuthService using Google.Apis.Auth library
  - Methods: ValidateIdTokenAsync(idToken) → GoogleUserInfo
  - **Security**: Verify aud, iss, exp claims

- [ ] Create JWT token generator
  - **Reference**: Security & Auth - JWT Implementation
  - **Reference**: Feature Spec - AC9, AC10 (generate tokens)
  - Interface: IJwtTokenGenerator
  - Methods: GenerateAccessToken(userId, email), GenerateRefreshToken(), HashToken(token)
  - **Configuration**: Read from appsettings (SecretKey, Issuer, Audience, Expiry)
  - **Security**: Use HMAC-SHA256, include jti claim

## Phase 4: Application Layer (X time)
- [ ] Create DTOs
  - LoginWithGoogleRequest: IdToken property
  - LoginResponse: AccessToken, RefreshToken, ExpiresIn, TokenType, UserId, IsNewUser, Email
  - **Reference**: Feature Spec - API Specification request/response

- [ ] Create AuthService
  - **Reference**: Feature Spec - Acceptance Criteria (all ACs)
  - Method: LoginWithGoogleAsync(idToken) → LoginResponse
  - **Logic Flow**:
    1. Validate ID token with Google (AC6)
    2. Check if user exists by externalUserId
    3. If new: Create user (AC7), publish UserRegistered event
    4. If existing: Update lastLoginAt (AC8)
    5. Generate JWT access token (AC9)
    6. Create refresh token, hash and store (AC10)
    7. Return response (AC11)

- [ ] Create FluentValidation validators
  - **Reference**: Architecture Guidelines - Validation
  - LoginWithGoogleRequestValidator: Validate idToken not empty

## Phase 5: API Layer (X time)
- [ ] Create AuthController
  - **Reference**: Feature Spec - API Specification
  - Endpoint: POST /api/v1/auth/login/google
  - **Request**: LoginWithGoogleRequest
  - **Responses**: 200 (success), 400 (invalid token), 429 (rate limit), 500 (server error)
  - **Error Format**: RFC 7807 Problem Details

- [ ] Configure Dependency Injection (Program.cs)
  - Register DbContext with connection string
  - Register repositories (scoped)
  - Register services (scoped)
  - Register validators
  - **Reference**: Architecture Guidelines - DI Patterns

- [ ] Configure JWT authentication
  - **Reference**: Security & Auth - JWT Middleware
  - Add JWT Bearer authentication
  - Configure token validation parameters

- [ ] Configure rate limiting
  - **Reference**: Feature Spec - NF3 (5 requests per IP per minute)
  - Use AspNetCoreRateLimit library
  - Configure IP-based rate limiting

- [ ] Configure Serilog logging
  - Console sink (development)
  - Seq sink (structured logging)
  - Include correlation IDs

- [ ] Create appsettings.json configuration
  - ConnectionStrings:IdentityDb
  - Google:ClientId, Google:ClientSecret
  - Jwt:SecretKey, Jwt:Issuer, Jwt:Audience, Jwt:AccessTokenExpiryMinutes
  - IpRateLimiting configuration
  - Serilog configuration

## Phase 6: Messaging Integration (X time)
- [ ] Create UserRegistered event
  - **Reference**: Feature Spec - Events Published
  - **Reference**: Messaging Guidelines - Event Design
  - Properties: UserId, Email, Provider, RegisteredAt, CorrelationId
  - **Naming**: Past tense (event happened)

- [ ] Configure MassTransit
  - **Reference**: Messaging Guidelines - MassTransit Setup
  - Register MassTransit in DI
  - Configure RabbitMQ for local (development)
  - Configure Azure Service Bus for production
  - **Environment-based**: Use IsDevelopment() check

- [ ] Update AuthService to publish event
  - **Reference**: Feature Spec - AC7 (publish on new user)
  - Inject IPublishEndpoint
  - Publish UserRegistered after creating new user
  - Include correlation ID for tracing

## Phase 7: Testing (X time)
- [ ] Unit Tests - Domain Layer
  - **Reference**: Feature Spec - Testing Requirements
  - Test User.CreateFromGoogle() sets properties correctly
  - Test User.RecordLogin() updates timestamp
  - Test RefreshToken.Create() sets expiry correctly
  - Test RefreshToken.Revoke() marks as revoked
  - Test RefreshToken.IsValid() checks expiry and revocation

- [ ] Unit Tests - Application Layer
  - Test AuthService.LoginWithGoogle with valid token creates new user
  - Test AuthService.LoginWithGoogle with valid token updates existing user
  - Test AuthService.LoginWithGoogle with invalid token returns null
  - Test AuthService.LoginWithGoogle publishes UserRegistered for new users
  - **Use Moq** for mocking dependencies

- [ ] Integration Tests - API Layer
  - **Reference**: Architecture Guidelines - Integration Testing
  - Test POST /api/v1/auth/login/google with new user returns 200 + isNewUser=true
  - Test POST /api/v1/auth/login/google with existing user returns 200 + isNewUser=false
  - Test POST /api/v1/auth/login/google with invalid token returns 400
  - Test rate limiting blocks after 5 attempts (returns 429)
  - **Use Testcontainers** for real PostgreSQL and RabbitMQ
  - **Use WebApplicationFactory** for in-memory API testing

- [ ] Manual Testing Checklist
  - **Reference**: Feature Spec - Manual Testing section
  - Test with real Google OAuth flow
  - Verify tokens in database
  - Verify event in RabbitMQ UI
  - Verify logs in Seq
  - Test error scenarios

## Phase 8: Docker Integration (X time)
- [ ] Create Dockerfile for IdentityService
  - Multi-stage build (build → publish → runtime)
  - Base: mcr.microsoft.com/dotnet/aspnet:8.0
  - SDK: mcr.microsoft.com/dotnet/sdk:8.0

- [ ] Update docker-compose.yml
  - Add identity-service configuration
  - Configure environment variables
  - Set up depends_on (postgres, rabbitmq)
  - Map ports

- [ ] Test Docker build and run locally

## Completion Checklist
- [ ] All acceptance criteria from feature spec met
- [ ] All tests passing (unit + integration)
- [ ] Manual testing complete
- [ ] Docker container runs successfully
- [ ] Code follows architecture guidelines (Layered pattern)
- [ ] Uses ubiquitous language
- [ ] No hardcoded values (all in config)
- [ ] Security requirements met (JWT, hashing, validation)
- [ ] Ready for PR

## Blockers / Questions
*Document any issues requiring human approval*

## Notes & Decisions
*Document implementation discoveries and decisions*
```

**❌ What NOT to Include** (Final Code):
- ❌ Complete class implementations (no full C# code)
- ❌ Copy-paste bash command sequences
- ❌ Complete method bodies with every line of code
- ❌ Full test method implementations

**✅ What TO Include** (Smart Guidance):
- ✅ What to create (entity names, properties, method signatures)
- ✅ Why certain approaches (architecture reasoning)
- ✅ Which docs to reference for implementation details
- ✅ Configuration values and structure
- ✅ Test scenarios to implement (not the test code itself)
- ✅ Logical flow descriptions
- ✅ Architecture pattern application

3. Commit the plan:
   ```bash
   git add docs/40_features/f0002_your_feature/
   git commit -m "docs: add implementation plan for F0002"
   git push
   ```

**During Implementation**:
- ✅ Check off tasks as you complete them
- 📝 Add implementation notes and technical details to plan.md
- 🔗 Document decisions made (within scope)
- ⏰ Update "Last Updated" timestamp regularly
- 🛑 **STOP and ask for human approval** if you need to change feature file or scope

**Important Reminders**:
- 🔒 **Feature file is IMMUTABLE** - it's the input specification
- 📝 **Plan.md is for tracking** - update progress freely, but scope changes need approval
- 🚨 **Scope changes = human approval required** - don't alter requirements on your own

**Benefits**:
- **Clear roadmap**: Know exactly what to build next
- **Documentation compliance**: Each task references guidelines
- **Progress tracking**: See at a glance what's done vs. remaining
- **Onboarding**: New developers can follow the plan to understand implementation
- **Learning**: Explicit links to docs teach patterns as you build

---

### Step 3: Create GitHub Issue

**When**: After feature file and plan are committed

**Process**:
1. Go to GitHub Issues tab
2. Click "New Issue"
3. Choose template: "Feature Request"
4. Fill in form:
   ```
   Feature ID: F0002
   Feature File: docs/40_features/f0002_your_feature.md
   Priority: P1
   Service: Profile
   Size: M (4-8 hours)

   Summary: One-sentence description

   Key Acceptance Criteria (top 5 only):
   - [ ] AC1: Most important criterion
   - [ ] AC2: Second most important
   - [ ] AC3: Third most important
   - [ ] AC4: Fourth most important
   - [ ] AC5: Fifth most important

   Implementation Checklist (key tasks only):
   - [ ] Task 1
   - [ ] Task 2
   - [ ] Task 3
   ```

5. Assign to yourself
6. Add to milestone: "Phase X - Name"
7. Submit issue → Note the issue number (e.g., #5)

---

### Step 4: Link Feature File and Issue

**Update feature file header**:
```markdown
**GitHub Issue**: [#5](https://github.com/yourusername/QuietMatch/issues/5)
**Assignee**: @yourusername
**Status**: 🟡 In Progress (updated from "Not Started")
```

**Commit update**:
```bash
git add docs/40_features/f0002_your_feature/
git commit -m "docs: link F0002 to GitHub issue #5"
git push
```

---

### Step 5: Implementation

**Prerequisites**:
- ✅ Feature file complete
- ✅ Implementation plan (plan.md) created
- ✅ GitHub issue created and linked
- ✅ Relevant documentation reviewed

**Branch naming**:
```bash
git checkout -b feature/f0002-your-feature-name
```

**Commit messages** (reference issue):
```bash
git commit -m "feat(profile): add entity for F0002 (#5)"
git commit -m "feat(profile): add service for F0002 (#5)"
git commit -m "test(profile): add tests for F0002 (#5)"
```

**During implementation**:
- ✅ Check off items in **plan.md** as you complete them
- 📝 Add notes to plan.md about implementation decisions and discoveries (within scope)
- ⏰ Update "Last Updated" timestamp in plan.md regularly
- 🔄 Update issue checkboxes periodically (optional, not required)
- 📋 **Add TODOs for future work**: When you discover work that's out of current scope, add it to `/TODO.md`
  - See [TODO Management Guidelines](../60_operations/todo-management.md) for format
  - Examples: Production hardening, missing endpoints, future enhancements
- 💾 Commit plan.md updates frequently:
  ```bash
  git add docs/40_features/f0002_your_feature/plan.md
  git commit -m "docs: update F0002 implementation progress"
  ```

**Following the Plan**:
- Work through plan.md phases sequentially
- Reference documentation linked in each task
- Don't skip ahead - dependencies matter
- Add sub-tasks to plan.md as discovered (within scope)

**🚨 Critical Rule**:
- 🔒 **DO NOT modify feature file** (f####_feature_name.md) during implementation
- 🛑 **If you discover requirements need to change**, STOP and ask human for approval
- ✅ **Only update plan.md** for progress tracking and implementation notes

---

### Step 6: Create Pull Request

**When**: Feature implementation complete, all tests passing

**PR Title**: `feat(service): Feature name (#5)`

**PR Description**:
```markdown
## Feature
Closes #5

Implements F0002: Your Feature Name

📄 **Full Specification**: [docs/40_features/f0002_your_feature.md](link)

## Summary
One-paragraph description of what this PR does.

## Changes
- Added X entity
- Implemented Y service
- Created Z controller
- All tests passing

## Testing
- [x] Unit tests (17 passing)
- [x] Integration tests (5 passing)
- [x] Manual testing complete

## Documentation
- [x] Feature file updated
- [x] API docs updated (Swagger)
- [x] Architecture docs updated (if applicable)

## Checklist
- [x] Follows architecture guidelines
- [x] Uses ubiquitous language
- [x] All acceptance criteria met
- [x] Tests passing
- [x] No console errors
```

**Important**: Use `Closes #5` so issue auto-closes when PR merges

---

### Step 7: Review and Merge

1. **Self-review** or request review (if team)
2. **CI checks pass** (tests, linting)
3. **Merge PR** → Issue #5 auto-closes
4. **Delete feature branch**

---

### Step 8: Update Documentation

**Update feature folder**:
```markdown
# In f####_feature_name.md (feature file):
**Status**: ✅ Complete (updated from "In Progress")
**GitHub Issue**: [#5](link) (Closed)
**Pull Request**: [#42](link) (Merged)
**Completed**: 2025-11-25

# In plan.md (implementation plan):
**Status**: ✅ Complete
**Completed**: 2025-11-25
**Total Implementation Time**: X hours
**Notes**: Any lessons learned, deviations from plan, or insights
```

**Update PROGRESS.md** (when milestone complete):
```markdown
- [x] Implement ProfileService (Onion architecture)
  - [x] F0002: Your Feature ✅
  - [ ] F0003: Next Feature
```

**Commit**:
```bash
git add docs/40_features/f0002_your_feature/ PROGRESS.md
git commit -m "docs: mark F0002 as complete"
git push
```

---

## Feature File Template

Use `docs/40_features/f0001_sign_in_with_google.md` as the canonical template.

**Key sections every feature MUST have**:
- [ ] Header with status, priority, GitHub issue, assignee, effort
- [ ] Overview
- [ ] Goals (primary, secondary, non-goals)
- [ ] Acceptance Criteria (functional, non-functional, security)
- [ ] User Stories (with Gherkin scenarios)
- [ ] API Specification (endpoints, request/response, errors)
- [ ] Sequence Diagram (Mermaid)
- [ ] Database Changes (if applicable)
- [ ] Events Published (if applicable)
- [ ] Configuration (environment variables)
- [ ] Testing Strategy (unit, integration, manual)
- [ ] Implementation Checklist (backend, frontend, tests)
- [ ] Dependencies (upstream, downstream)
- [ ] Risks & Mitigations
- [ ] References (links to architecture docs)

---

## GitHub Issue Creation

### Labels to Use

**Priority** (pick one):
- `priority: P0` - Critical (MVP blocker)
- `priority: P1` - High (should have)
- `priority: P2` - Medium (nice to have)
- `priority: P3` - Low (future)

**Type** (pick one):
- `type: feature` - New feature
- `type: bug` - Bug fix
- `type: enhancement` - Improve existing

**Service** (pick one or more):
- `service: identity`
- `service: profile`
- `service: matching`
- `service: scheduling`
- `service: notification`
- `service: verification`
- `service: payment`
- `service: realtime`
- `service: graphql`
- `service: infrastructure`

**Size** (pick one):
- `size: XS` - < 2 hours
- `size: S` - 2-4 hours
- `size: M` - 4-8 hours
- `size: L` - 1-2 days
- `size: XL` - > 2 days (consider splitting)

**Architecture** (optional, for learning focus):
- `arch: layered`
- `arch: onion`
- `arch: hexagonal`
- `arch: cqrs`
- `arch: saga`

---

## Implementation Process

### Daily Workflow

**Morning**:
1. Check GitHub issue for current feature
2. Read feature file for full context
3. Review `.claude/rules.md` for architecture rules
4. Start coding

**During**:
1. Reference feature file for acceptance criteria
2. Check off completed items
3. Commit frequently with issue references (#5)

**End of Day**:
1. Push commits
2. Update feature file with progress (optional)
3. Update issue checkboxes (optional)

---

### When to Split a Feature

If estimated effort > 2 days (XL), consider splitting:

**Example**: "F0010: Complete Matching System" (too big!)

**Split into**:
- F0010: Rule-Based Matching Algorithm (M - 8 hours)
- F0011: Match Presentation UI (M - 6 hours)
- F0012: Match Acceptance Flow (S - 4 hours)

**Benefits**:
- ✅ Smaller, focused PRs
- ✅ Easier to review
- ✅ Can deliver incrementally
- ✅ Less merge conflicts

---

## Completion Checklist

Before marking a feature as complete:

### Code Quality
- [ ] Follows assigned architecture pattern (Layered/Onion/Hexagonal/CQRS)
- [ ] Uses ubiquitous language from domain model
- [ ] No hardcoded values (use configuration)
- [ ] No commented-out code
- [ ] Meaningful variable/method names
- [ ] Comments explain "why", not "what"

### Testing
- [ ] Unit tests written (80%+ coverage for business logic)
- [ ] Integration tests written (database, messaging)
- [ ] API tests written (endpoints)
- [ ] All tests passing
- [ ] Manual testing complete (follow feature file checklist)

### Security
- [ ] JWT authentication on all endpoints
- [ ] Input validation (FluentValidation)
- [ ] No SQL injection vulnerabilities (use EF Core)
- [ ] Sensitive data encrypted (if applicable)
- [ ] GDPR considerations addressed

### Documentation
- [ ] Feature file updated with final status
- [ ] API documentation updated (Swagger)
- [ ] PATTERNS.md updated (if new patterns introduced)
- [ ] Architecture docs updated (if architectural changes)
- [ ] README updated (if setup changes)

### GitHub
- [ ] PR created with proper description
- [ ] PR linked to issue (`Closes #X`)
- [ ] CI checks passing
- [ ] PR reviewed (if team)
- [ ] PR merged
- [ ] Issue auto-closed
- [ ] Feature branch deleted

### Final Updates
- [ ] Feature file status: ✅ Complete
- [ ] Feature file links to closed issue and merged PR
- [ ] PROGRESS.md updated (if milestone complete)
- [ ] Committed and pushed

---

## Example: Complete Lifecycle

### Day 1: Planning
```bash
# 1. Create feature file
vim docs/40_features/f0002_create_profile.md
# (fill in comprehensive spec)

git add docs/40_features/f0002_create_profile.md
git commit -m "docs: add feature spec for F0002 Create Profile"
git push

# 2. Create GitHub Issue #6 (via web UI)
# - Link to feature file
# - Add labels: type: feature, priority: P0, service: profile, size: M
# - Assign to self
# - Add to milestone: Phase 1

# 3. Update feature file with issue link
vim docs/40_features/f0002_create_profile.md
# (add GitHub Issue: #6)

git add docs/40_features/f0002_create_profile.md
git commit -m "docs: link F0002 to issue #6"
git push
```

### Day 2-3: Implementation
```bash
# 4. Create branch
git checkout -b feature/f0002-create-profile

# 5. Implement
git commit -m "feat(profile): add MemberProfile entity (#6)"
git commit -m "feat(profile): add ProfileService (#6)"
git commit -m "feat(profile): add ProfileController (#6)"
git commit -m "test(profile): add ProfileService tests (#6)"

# 6. Update feature file progress
vim docs/40_features/f0002_create_profile.md
# (check off completed items)

git add docs/40_features/f0002_create_profile.md
git commit -m "docs: update F0002 progress"
git push
```

### Day 4: Review
```bash
# 7. Create PR
gh pr create --title "feat(profile): Create profile feature" \
             --body "Closes #6

Implements F0002: Create Profile

📄 Full spec: docs/40_features/f0002_create_profile.md

Changes:
- MemberProfile entity with Onion architecture
- ProfileService with validation
- ProfileController with API endpoints
- All tests passing (23 tests)

Testing:
- [x] Unit tests
- [x] Integration tests
- [x] Manual testing"

# 8. Merge PR (via GitHub UI)
# → Issue #6 auto-closes

# 9. Update docs
git checkout main
git pull

vim docs/40_features/f0002_create_profile.md
# (update status to Complete, add PR link)

vim PROGRESS.md
# (check off F0002 if needed)

git add docs/40_features/f0002_create_profile.md PROGRESS.md
git commit -m "docs: mark F0002 as complete"
git push
```

---

## FAQ

### Q: Do I need to update both the feature file AND the issue?
**A**: No! Feature file is source of truth. Issue is for tracking only. Update issue checkboxes if it helps you, but not required.

### Q: What if I discover new requirements during implementation?
**A**: Update the feature file first, commit it, then optionally add a comment to the GitHub issue mentioning the change.

### Q: Can I skip the feature file for small bug fixes?
**A**: Yes! Feature files are for features (new functionality). For bugs, just create a GitHub Issue with type: bug.

### Q: What if the feature spans multiple services?
**A**: One feature file, but multiple labels on the issue (e.g., `service: matching`, `service: scheduling`). Consider SAGA documentation in the feature file.

### Q: Should I create the feature file or GitHub issue first?
**A**: Feature file first. It's the comprehensive spec. GitHub issue references it.

### Q: How often should I update PROGRESS.md?
**A**: When you complete a major milestone (service done, phase done), not per feature. PROGRESS.md is high-level.

---

## References

- **Feature Template**: [`docs/40_features/f0001_sign_in_with_google.md`](../40_features/f0001_sign_in_with_google.md)
- **Architecture Guidelines**: [`docs/10_architecture/02_architecture-guidelines.md`](../10_architecture/02_architecture-guidelines.md)
- **Service Templates**: [`docs/10_architecture/03_service-templates.md`](../10_architecture/03_service-templates.md)
- **Domain Language**: [`docs/20_domain/01_domain-ubiquitous-language.md`](../20_domain/01_domain-ubiquitous-language.md)
- **GitHub Project**: [QuietMatch Board](https://github.com/yourusername/QuietMatch/projects)

---

**Last Updated**: 2025-11-20
**Document Owner**: Engineering Team
**Status**: Living Document
