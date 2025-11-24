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
