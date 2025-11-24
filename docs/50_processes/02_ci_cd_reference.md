# CI/CD Quick Reference

## Overview

Quick reference for Continuous Integration and Continuous Deployment setup in QuietMatch.

## CI Pipeline Summary

### Trigger Conditions

| Event | Branches | Paths |
|-------|----------|-------|
| Pull Request | `main`, `develop` | `src/Services/Identity/**` |
| Push | `main`, `develop` | `src/Services/Identity/**` |

### Jobs Overview

```
┌─────────────────────────────────────────┐
│         CI Pipeline Stages              │
├─────────────────────────────────────────┤
│                                         │
│  1. BUILD & TEST                        │
│     ├─ Setup .NET 8.0                   │
│     ├─ Restore Dependencies             │
│     ├─ Build (Release)                  │
│     ├─ Run Unit Tests (55+)             │
│     ├─ Publish Test Results             │
│     └─ Upload Coverage Report           │
│                                         │
│  2. CODE QUALITY                        │
│     ├─ Check Code Formatting            │
│     └─ Run Security Scan                │
│                                         │
│  3. DOCKER BUILD                        │
│     ├─ Build Docker Image               │
│     └─ Scan for Vulnerabilities         │
│                                         │
│  4. CODERABBIT AI REVIEW (automatic)    │
│     ├─ Architecture Analysis            │
│     ├─ Security Review                  │
│     ├─ Performance Check                │
│     └─ Best Practices Verification      │
│                                         │
└─────────────────────────────────────────┘
```

## CodeRabbit Review Checks

### Automatic Checks

| Category | Checks |
|----------|--------|
| **Architecture** | Layered boundaries, SOLID principles, DI patterns |
| **Security** | Secrets, SQL injection, XSS, auth/authz, token handling |
| **Performance** | Async/await, DB queries, N+1, caching |
| **Code Quality** | Naming, smells, complexity, duplication |
| **Testing** | Coverage, AAA pattern, edge cases, independence |

### Path-Specific Rules

```
src/Services/Identity/**/*.cs
├─ Verify layered architecture
├─ Check async/await usage
├─ Ensure SOLID principles
├─ Verify error handling
├─ Check security vulnerabilities
└─ Verify DI usage

src/Services/Identity/**/Tests/**/*.cs
├─ Ensure AAA pattern
├─ Check test naming
├─ Verify mocks usage
├─ Ensure test independence
└─ Check edge cases

src/Services/Identity/Dockerfile
├─ Security best practices
├─ Multi-stage optimization
├─ Layer caching
└─ No secrets in image

**/*.md
├─ Check broken links
├─ Verify code examples
├─ Consistent formatting
└─ Spelling/grammar
```

## Environment Variables

### Required for CI

| Variable | Description | Set In |
|----------|-------------|--------|
| `POSTGRES_DB` | Test database name | Workflow (service) |
| `POSTGRES_USER` | Test DB user | Workflow (service) |
| `POSTGRES_PASSWORD` | Test DB password | Workflow (service) |

### Optional Integrations

| Integration | Token | Purpose |
|-------------|-------|---------|
| Codecov | `CODECOV_TOKEN` | Code coverage reporting |
| Security Scan | Auto | Vulnerability detection |
| Trivy | Auto | Container security |

## Build Commands Reference

### Local Development

```bash
# Restore dependencies
dotnet restore src/Services/Identity/DatingApp.IdentityService.Api/DatingApp.IdentityService.Api.csproj

# Build
dotnet build src/Services/Identity/DatingApp.IdentityService.Api/DatingApp.IdentityService.Api.csproj --configuration Release

# Run tests
dotnet test src/Services/Identity/DatingApp.IdentityService.Tests.Unit/DatingApp.IdentityService.Tests.Unit.csproj --configuration Release

# Check formatting
dotnet format src/Services/Identity/DatingApp.IdentityService.Api/DatingApp.IdentityService.Api.csproj --verify-no-changes

# Docker build
cd src/Services/Identity
docker build -t identity-service:local -f Dockerfile .
```

### CI Commands (Reference)

```bash
# What CI runs automatically:

# 1. Build
dotnet build --configuration Release --no-restore

# 2. Test with coverage
dotnet test --configuration Release --no-build --collect:"XPlat Code Coverage"

# 3. Format check
dotnet format --verify-no-changes --verbosity diagnostic

# 4. Docker build
docker build --tag identity-service:$SHA --file Dockerfile .

# 5. Security scan
trivy image --format sarif identity-service:$SHA
```

## Status Badges

Add to README.md:

```markdown
![CI](https://github.com/xabiermoja/QuietMatch/workflows/Identity%20Service%20CI/badge.svg)
[![codecov](https://codecov.io/gh/xabiermoja/QuietMatch/branch/main/graph/badge.svg)](https://codecov.io/gh/xabiermoja/QuietMatch)
```

## Troubleshooting Quick Fixes

### ❌ Build Fails

```bash
# Clear and rebuild
dotnet clean
dotnet restore
dotnet build
```

### ❌ Tests Fail

```bash
# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test
dotnet test --filter "FullyQualifiedName~TestName"
```

### ❌ Format Check Fails

```bash
# Auto-fix formatting
dotnet format

# Then commit
git add -u
git commit -m "style: fix code formatting"
```

### ❌ Docker Build Fails

```bash
# Build with no cache
docker build --no-cache -f Dockerfile .

# Check .dockerignore
cat .dockerignore
```

### ❌ CodeRabbit Not Reviewing

1. Check: https://app.coderabbit.ai
2. Verify app installed on repo
3. Review `.github/.coderabbit.yaml` syntax
4. Re-trigger: Close and reopen PR

---

## Common CI Issues (Lessons from F0001)

> **Based on real issues encountered during IdentityService implementation**

### Issue 1: Build Succeeds, Tests Fail with "Invalid Argument"

**Symptoms**:
```
The argument /path/to/Tests.Unit.dll is invalid
MSB4181: The "VSTestTask" task returned false but did not log an error
```

**Root Cause**:
- Workflow only builds the API project: `dotnet build Api.csproj`
- Tries to run tests with `--no-build` flag
- Test project DLL was never built, so it doesn't exist

**Solution**:
```yaml
# ❌ BAD - Only builds one project
- name: Restore dependencies
  run: dotnet restore src/Services/Identity/DatingApp.IdentityService.Api/DatingApp.IdentityService.Api.csproj

- name: Build
  run: dotnet build src/Services/Identity/DatingApp.IdentityService.Api/DatingApp.IdentityService.Api.csproj --configuration Release --no-restore

# ✅ GOOD - Builds entire solution
- name: Restore dependencies
  run: dotnet restore src/Services/Identity/DatingApp.IdentityService.sln

- name: Build
  run: dotnet build src/Services/Identity/DatingApp.IdentityService.sln --configuration Release --no-restore
```

**Why This Happens**:
- Building a single project only builds its direct dependencies
- Test projects reference the API/Application/Infrastructure projects, but the reverse isn't true
- Building the API project doesn't trigger building the test project
- Solution files know about all projects and build everything

**Best Practice**:
- Always restore and build the **solution file** (*.sln) in CI
- Only build individual projects if you're 100% sure they don't need others
- Use `--no-build` in test step only if you built the entire solution first

---

### Issue 2: Docker Build Succeeds, Trivy Scan Fails with "No Such Image"

**Symptoms**:
```
Fatal error: unable to find the specified image "identity-service:abc123" in ["docker" "containerd" "podman" "remote"]
docker error: unable to inspect the image: Error response from daemon: No such image: identity-service:abc123
```

**Root Cause**:
- `docker/build-push-action` uses Docker Buildx by default
- Buildx builds images but doesn't load them into Docker daemon
- Trivy scans images from the Docker daemon
- Image exists in Buildx cache but not in Docker daemon

**Solution**:
```yaml
# ❌ BAD - Image not available for scanning
- name: Build Docker image
  uses: docker/build-push-action@v5
  with:
    context: ./src/Services/Identity
    file: ./src/Services/Identity/Dockerfile
    push: false
    tags: identity-service:${{ github.sha }}

# ✅ GOOD - Image loaded into daemon
- name: Build Docker image
  uses: docker/build-push-action@v5
  with:
    context: ./src/Services/Identity
    file: ./src/Services/Identity/Dockerfile
    push: false
    load: true  # ← This is the key!
    tags: identity-service:${{ github.sha }}
```

**Why This Happens**:
- Docker Buildx is designed for multi-platform builds and pushing to registries
- It optimizes by not loading images into the local daemon by default
- Scanning tools like Trivy expect images in the local Docker daemon
- `load: true` explicitly loads the image after building

**Best Practice**:
- Use `load: true` when you need to scan or run the image locally in CI
- Use `push: true` when deploying to a registry (mutually exclusive with load)
- If you need both (scan and push), build twice or use a registry-based scanner

**Additional Fix**:
Also update CodeQL action version:
```yaml
# ❌ DEPRECATED
- uses: github/codeql-action/upload-sarif@v2

# ✅ CURRENT
- uses: github/codeql-action/upload-sarif@v3
```
(v1 and v2 deprecated as of 2025-01-10)

---

### Issue 3: Tests Pass, But CI Fails with "Resource Not Accessible"

**Symptoms**:
```
##[error]HttpError: Resource not accessible by integration
```
- Tests run successfully (55/55 passing)
- Test reporter action fails
- SARIF upload to Security tab fails

**Root Cause**:
- GitHub Actions jobs have restricted permissions by default
- Test reporter needs `checks: write` to create check runs
- Security scanning needs `security-events: write` to upload SARIF
- PR annotations need `pull-requests: write`

**Solution**:
```yaml
jobs:
  build-and-test:
    runs-on: ubuntu-latest
    # ✅ Add explicit permissions
    permissions:
      contents: read           # Checkout code
      checks: write            # Create check runs
      pull-requests: write     # Add PR annotations

    steps:
      # ... your steps

  docker-build:
    runs-on: ubuntu-latest
    # ✅ Add explicit permissions
    permissions:
      contents: read           # Checkout code
      security-events: write   # Upload SARIF to Security tab

    steps:
      # ... your steps
```

**Why This Happens**:
- GitHub Actions uses the GITHUB_TOKEN with limited default permissions
- Actions that create check runs, annotate PRs, or upload security data need explicit permissions
- This is a security feature to prevent malicious actions from accessing resources

**Best Practice**:
- Always add explicit `permissions:` block to jobs that use:
  - Test reporters (needs `checks: write`)
  - PR annotators (needs `pull-requests: write`)
  - Security scanners (needs `security-events: write`)
  - Status checks (needs `statuses: write`)
- Use principle of least privilege: only grant permissions the job actually needs
- Don't grant `write-all` or `permissions: {}` unless absolutely necessary

**Common Permissions**:
```yaml
permissions:
  contents: read          # Read repository code (checkout)
  contents: write         # Push to repository (auto-merge, etc.)
  checks: write           # Create/update check runs
  pull-requests: write    # Comment on PRs, add annotations
  issues: write           # Create/update issues
  security-events: write  # Upload code scanning results (SARIF)
  statuses: write         # Create commit statuses
```

---

## Setting Up CI for New Services

When creating CI workflows for ProfileService, MatchingService, etc., use this checklist:

### Workflow Configuration
- [ ] Trigger on PR and push to `main`/`develop`
- [ ] Filter paths to service directory: `src/Services/YourService/**`
- [ ] Filter paths to include workflow file itself: `.github/workflows/your-service-ci.yml`

### Build & Test Job
- [ ] Add `permissions` block with `contents: read`, `checks: write`, `pull-requests: write`
- [ ] Restore and build the **solution file** (*.sln), not individual projects
- [ ] Run tests with `--no-build` flag (since solution was already built)
- [ ] Use `if: always()` for test reporting steps so they run even if tests fail
- [ ] Configure test result reporter with proper permissions

### Code Quality Job
- [ ] Format check should target the **solution file** (*.sln)
- [ ] Install `dotnet-format` as global tool
- [ ] Use `--verify-no-changes` flag to fail if formatting issues found

### Docker Build Job
- [ ] Add `permissions` block with `contents: read`, `security-events: write`
- [ ] Set up Docker Buildx
- [ ] Build with `load: true` if you're scanning the image
- [ ] Tag with `${{ github.sha }}` for uniqueness
- [ ] Use GitHub Actions cache: `cache-from: type=gha`, `cache-to: type=gha,mode=max`
- [ ] Scan with Trivy using the loaded image
- [ ] Upload SARIF results with CodeQL action **v3** (not v2)
- [ ] Use `if: always()` for SARIF upload so results are uploaded even if scan finds issues

### Service Dependencies
If your service needs database or message broker for tests:
```yaml
services:
  postgres:
    image: postgres:16-alpine
    env:
      POSTGRES_DB: your_service_db_test
      POSTGRES_USER: test_user
      POSTGRES_PASSWORD: test_password
    ports:
      - 5432:5432
    options: >-
      --health-cmd pg_isready
      --health-interval 10s
      --health-timeout 5s
      --health-retries 5
```

---

## Performance Benchmarks

### Expected CI Times

| Job | Duration | Notes |
|-----|----------|-------|
| Build & Test | ~2 min | With cache |
| Code Quality | ~30 sec | Format + scan |
| Docker Build | ~1 min | With cache |
| **Total** | **~3.5 min** | Typical PR |

### Optimization Tips

**Reduce CI time**:
1. Keep PRs small (< 400 lines)
2. Run tests locally first
3. Fix formatting before pushing
4. Leverage caching

**Cache hit rate**:
- Target: > 80%
- Improves with stable dependencies

## Configuration Files Reference

### GitHub Actions

**File**: `.github/workflows/identity-service-ci.yml`
```yaml
# Key sections:
on: # Triggers
jobs: # Pipeline stages
services: # Test dependencies
steps: # Individual actions
```

### CodeRabbit

**File**: `.github/.coderabbit.yaml`
```yaml
# Key sections:
reviews: # Auto-review settings
language_settings: # C#, Dockerfile
path_instructions: # Per-directory rules
checks: # Enabled validations
```

### PR Template

**File**: `.github/pull_request_template.md`
- Guides PR creation
- Ensures consistency
- Checklists for quality

## Useful Links

| Resource | URL |
|----------|-----|
| CI Workflow | [identity-service-ci.yml](/.github/workflows/identity-service-ci.yml) |
| CodeRabbit Config | [.coderabbit.yaml](/.github/.coderabbit.yaml) |
| PR Template | [pull_request_template.md](/.github/pull_request_template.md) |
| Actions Dashboard | https://github.com/xabiermoja/QuietMatch/actions |
| CodeRabbit Dashboard | https://app.coderabbit.ai |

## Maintenance

### Monthly Tasks

- [ ] Review CI logs for patterns
- [ ] Update base images (Docker)
- [ ] Check for workflow updates
- [ ] Review CodeRabbit feedback
- [ ] Update documentation

### Quarterly Tasks

- [ ] Audit security scan results
- [ ] Review and update CI timeouts
- [ ] Evaluate new CI features
- [ ] Update action versions
- [ ] Team retrospective on CI/CD

## Metrics to Track

**CI Health**:
- ✅ Success rate (target: > 95%)
- ⏱️ Average duration (target: < 5 min)
- 🔄 Queue time (target: < 30 sec)
- 📊 Cache hit rate (target: > 80%)

**Code Quality**:
- 🧪 Test coverage (target: > 80%)
- 🐛 Bug detection rate
- 🔒 Security findings
- 📝 Code review turnaround

## Contact & Support

**CI/CD Issues**:
- Label: `ci/cd`
- Priority: High
- SLA: < 4 hours

**CodeRabbit Issues**:
- Support: support@coderabbit.ai
- Docs: https://docs.coderabbit.ai
- Status: https://status.coderabbit.ai

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2025-11-24 | Initial CI/CD setup |

---

**Last Updated**: 2025-11-24
**Maintained By**: DevOps Team
