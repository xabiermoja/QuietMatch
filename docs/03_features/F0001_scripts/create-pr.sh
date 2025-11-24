#!/bin/bash

# Script to push feature branch and create Pull Request for F0001
# This script should be run from the repository root

set -e  # Exit on error

BRANCH_NAME="feature/f0001-sign-in-with-google"
BASE_BRANCH="main"

echo "================================================"
echo "Creating PR for Feature F0001: Sign In with Google"
echo "================================================"
echo ""

# Check if we're on the correct branch
CURRENT_BRANCH=$(git branch --show-current)
if [ "$CURRENT_BRANCH" != "$BRANCH_NAME" ]; then
    echo "❌ Error: Not on branch $BRANCH_NAME"
    echo "Current branch: $CURRENT_BRANCH"
    echo "Please checkout the feature branch first:"
    echo "  git checkout $BRANCH_NAME"
    exit 1
fi

echo "✅ On correct branch: $BRANCH_NAME"
echo ""

# Check if there are uncommitted changes
if ! git diff-index --quiet HEAD --; then
    echo "⚠️  Warning: You have uncommitted changes"
    echo "Please commit or stash them first"
    exit 1
fi

echo "✅ No uncommitted changes"
echo ""

# Push the branch
echo "📤 Pushing branch to origin..."
if git push -u origin "$BRANCH_NAME"; then
    echo "✅ Branch pushed successfully"
else
    echo ""
    echo "❌ Push failed. This might be due to authentication issues."
    echo ""
    echo "To fix authentication, try one of these options:"
    echo ""
    echo "Option 1 - Update HTTPS credentials (macOS):"
    echo "  git credential-osxkeychain erase"
    echo "  # Then enter: host=github.com protocol=https and press Enter twice"
    echo "  git push -u origin $BRANCH_NAME"
    echo ""
    echo "Option 2 - Switch to SSH:"
    echo "  git remote set-url origin git@github.com:xabiermoja/QuietMatch.git"
    echo "  git push -u origin $BRANCH_NAME"
    echo ""
    echo "Option 3 - Use GitHub CLI:"
    echo "  gh auth refresh -s repo,workflow"
    echo "  git push -u origin $BRANCH_NAME"
    echo ""
    exit 1
fi

echo ""
echo "================================================"
echo "Creating Pull Request..."
echo "================================================"
echo ""

# Create PR using GitHub CLI
PR_BODY=$(cat <<'EOF'
## Feature F0001: Sign In with Google

Complete implementation of Google OAuth 2.0 authentication for QuietMatch.

### 📊 Summary

| Category | Status |
|----------|--------|
| Backend Implementation | ✅ Complete (AC6-AC12) |
| Unit Tests | ✅ 55 tests (100% passing) |
| Docker Integration | ✅ Complete |
| Documentation | ✅ 5 comprehensive guides |
| CI/CD Setup | ✅ Automated quality gates |

### 🏗️ Architecture

**Layered Architecture** (API → Application → Infrastructure → Domain)

- **API Layer**: AuthController with POST /api/v1/auth/login/google
- **Application Layer**: AuthService orchestration, DTOs, FluentValidation
- **Infrastructure Layer**: GoogleAuthService, JwtTokenGenerator, EF Core, MassTransit
- **Domain Layer**: User & RefreshToken entities with business logic

### 🚀 Implementation Phases

**Completed (10 phases)**:

1. ✅ **Phase 0**: Project Setup (4 layers + 2 test projects)
2. ✅ **Phase 1**: Domain Layer (User, RefreshToken entities)
3. ✅ **Phase 2**: Infrastructure Persistence (EF Core, PostgreSQL, migrations)
4. ✅ **Phase 3**: Infrastructure External Services (Google OAuth, JWT)
5. ✅ **Phase 4**: Application Layer (AuthService, DTOs, validation)
6. ✅ **Phase 5**: API Layer (Controllers, middleware, configuration)
7. ✅ **Phase 6**: Messaging Integration (MassTransit, RabbitMQ, events)
8. ✅ **Phase 7**: Comprehensive Testing (55 unit tests)
9. ✅ **Phase 8**: Docker Integration (Dockerfile, docker-compose)
10. ✅ **Phase 9**: Documentation & Verification
11. ✅ **Phase 10**: CI/CD Setup (GitHub Actions + CodeRabbit)

### 🧪 Testing

**Unit Tests**: 55 tests (100% passing)
- Domain: 19 tests (User, RefreshToken)
- Application: 6 tests (AuthService)
- Infrastructure: 30 tests (GoogleAuthService, JwtTokenGenerator)

**Coverage**: Comprehensive coverage of all layers
- Entity factory methods and business logic
- Authentication orchestration
- Token generation and validation
- OAuth integration

### 🐳 Docker

**Multi-stage Dockerfile**: 403MB optimized image
**Docker Compose**: Complete stack
- PostgreSQL 16 (database)
- RabbitMQ 3.13 (message broker)
- Seq (structured logging)
- IdentityService API

### 📚 Documentation

**5 comprehensive guides** (2,500+ lines):

1. **PATTERNS.md** - Architecture decisions (why Layered Architecture)
2. **DOCKER.md** - Docker setup, troubleshooting, production considerations
3. **TESTING.md** - Manual testing guide with 8 detailed scenarios
4. **API.md** - Complete API reference with examples
5. **IMPLEMENTATION_SUMMARY.md** - Complete overview of implementation

**Process Documentation**:
6. **Pull Request Process** - Complete PR workflow and review guidelines
7. **CI/CD Reference** - Quick reference for automated quality gates

### 🔒 Security

- ✅ JWT tokens signed with HMAC-SHA256
- ✅ Refresh tokens hashed with SHA-256
- ✅ Server-side Google ID token validation
- ✅ Non-root Docker container user
- ✅ No secrets in source code
- ✅ Input validation with FluentValidation
- ✅ RFC 7807 error responses

### 🎯 Acceptance Criteria Status

From Feature Specification F0001:

| ID | Criteria | Status |
|----|----------|--------|
| AC6 | Backend validates Google ID token | ✅ Implemented |
| AC7 | New user record created | ✅ Implemented |
| AC8 | Existing user last login updated | ✅ Implemented |
| AC9 | JWT access token generated | ✅ Implemented |
| AC10 | Refresh token generated and stored | ✅ Implemented |
| AC11 | Tokens returned to client | ✅ Implemented |
| AC12 | Error handling | ✅ Implemented |

**Backend Scope**: 100% Complete ✅

### 🤖 CI/CD Automation

**GitHub Actions**:
- ✅ Build verification (Release configuration)
- ✅ Unit test execution (55 tests)
- ✅ Code coverage reporting
- ✅ Code formatting checks
- ✅ Security vulnerability scanning
- ✅ Docker build and security scan

**CodeRabbit AI**:
- ✅ Automatic code review on all PRs
- ✅ Architecture boundary verification
- ✅ Security vulnerability detection
- ✅ Performance optimization suggestions
- ✅ Test quality verification

### 📦 Deliverables

**Code**:
- ~3,500+ lines of production code
- 4 layers with clean separation
- 6 projects (4 production + 2 test)

**Tests**:
- 55 unit tests (100% passing)
- Test coverage across all layers

**Documentation**:
- 7 comprehensive guides
- API reference with examples
- Docker setup guide
- Testing procedures

**Infrastructure**:
- Dockerfile with multi-stage build
- docker-compose.yml for local dev
- GitHub Actions CI pipeline
- CodeRabbit AI configuration

### 🚦 Known Limitations

**Not in Scope for F0001**:
- ❌ Refresh token endpoint (future feature)
- ❌ Token revocation endpoint (future feature)
- ❌ Sign In with Apple (domain ready, impl pending)
- ❌ Rate limiting (recommended for production)
- ❌ Health check endpoints (future addition)

### 📋 Pre-merge Checklist

- [x] All commits follow conventional commit format
- [x] Code follows C# coding standards
- [x] All unit tests pass (55/55)
- [x] No compiler warnings
- [x] Documentation is comprehensive and accurate
- [x] Docker build successful
- [x] Security best practices followed
- [x] CI/CD pipeline configured

### 🎓 References

- **Feature Spec**: `docs/03_features/F0001_sign_in_with_google.md`
- **Implementation Summary**: `src/Services/Identity/IMPLEMENTATION_SUMMARY.md`
- **API Reference**: `src/Services/Identity/API.md`
- **Testing Guide**: `src/Services/Identity/TESTING.md`
- **Docker Guide**: `src/Services/Identity/DOCKER.md`
- **PR Process**: `docs/50_processes/01_pull_request_process.md`

### 🏁 Ready for

✅ **Development**: Fully functional with docker-compose
✅ **Staging**: With proper configuration (Google OAuth, secrets)
⚠️ **Production**: Requires security hardening (HTTPS, rate limiting, Key Vault)

---

**Total Commits**: 13 well-documented commits
**Lines Changed**: ~5,000+ (code + tests + docs)
**Time to Review**: 30-45 minutes

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>
EOF
)

if gh pr create \
    --title "feat: Implement Sign In with Google (F0001)" \
    --body "$PR_BODY" \
    --base "$BASE_BRANCH" \
    --head "$BRANCH_NAME"; then

    echo ""
    echo "================================================"
    echo "✅ SUCCESS!"
    echo "================================================"
    echo ""
    echo "Pull Request created successfully!"
    echo ""
    echo "The PR will be automatically reviewed by:"
    echo "  • GitHub Actions CI (build, test, security scan)"
    echo "  • CodeRabbit AI (code quality, architecture, security)"
    echo ""
    echo "Next steps:"
    echo "  1. Wait for CI checks to complete (~3-5 minutes)"
    echo "  2. Review CodeRabbit feedback"
    echo "  3. Address any issues found"
    echo "  4. Request human review from team members"
    echo ""
    echo "To view the PR:"
    echo "  gh pr view --web"
    echo ""
else
    echo ""
    echo "❌ Failed to create PR"
    echo ""
    echo "Manual PR creation:"
    echo "  1. Go to: https://github.com/xabiermoja/QuietMatch/compare/$BASE_BRANCH...$BRANCH_NAME"
    echo "  2. Click 'Create pull request'"
    echo "  3. The PR template will auto-fill"
    echo "  4. Review and submit"
    echo ""
    exit 1
fi
