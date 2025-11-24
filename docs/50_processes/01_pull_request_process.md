# Pull Request Process

## Overview

This document describes the pull request (PR) process for the QuietMatch project, including automated checks, code review requirements, and merge criteria.

## Automated Quality Gates

### GitHub Actions CI Pipeline

All PRs automatically trigger CI checks defined in `.github/workflows/identity-service-ci.yml`:

**Build & Test Job**:
- ✅ .NET 8.0 build (Release configuration)
- ✅ Unit tests execution (55+ tests)
- ✅ Test result reporting
- ✅ Code coverage reporting (Codecov)
- ✅ PostgreSQL integration testing

**Code Quality Job**:
- ✅ Code formatting verification (`dotnet format`)
- ✅ Security vulnerability scanning
- ✅ Coding standards compliance

**Docker Build Job**:
- ✅ Docker image build verification
- ✅ Image vulnerability scanning (Trivy)
- ✅ Security best practices check

### AI-Powered Code Review (CodeRabbit)

CodeRabbit automatically reviews all PRs for:

**Architecture & Design**:
- Layered architecture boundaries
- SOLID principles adherence
- Design pattern usage
- Dependency injection patterns

**Code Quality**:
- Naming conventions
- Code smells and anti-patterns
- DRY principle violations
- Complexity analysis
- Code duplication

**Security**:
- Hardcoded secrets detection
- SQL injection vulnerabilities
- XSS vulnerabilities
- Authentication/authorization issues
- Token handling security

**Performance**:
- Async/await usage
- Database query optimization
- N+1 query detection
- Caching opportunities

**Testing**:
- Test coverage adequacy
- Test quality (AAA pattern)
- Edge case coverage
- Test independence

## PR Creation Checklist

Before creating a PR, ensure:

### Code Quality
- [ ] All unit tests pass locally
- [ ] No compiler warnings
- [ ] Code follows C# conventions
- [ ] SOLID principles followed
- [ ] No code smells

### Security
- [ ] No hardcoded secrets
- [ ] Sensitive data properly encrypted/hashed
- [ ] Input validation implemented
- [ ] Security best practices followed

### Documentation
- [ ] Code comments added where needed
- [ ] README updated (if applicable)
- [ ] API docs updated (if applicable)
- [ ] Architecture docs updated (if applicable)

### Testing
- [ ] Unit tests added for new functionality
- [ ] Integration tests updated (if applicable)
- [ ] Manual testing completed
- [ ] Test coverage maintained/improved

## PR Template

When creating a PR, the template (`.github/pull_request_template.md`) will guide you through:

1. **Description**: Brief summary of changes
2. **Related Issue**: Link to feature/bug
3. **Type of Change**: Feature, bug fix, refactor, etc.
4. **Changes Made**: Detailed list
5. **Architecture & Design**: Decisions made
6. **Testing**: Coverage and results
7. **Code Quality Checklist**: Standards compliance
8. **Security Checklist**: Security verification
9. **Performance Considerations**: Optimization notes
10. **Documentation**: Updates made
11. **Database Changes**: Migration info
12. **Breaking Changes**: Migration guide
13. **Deployment Notes**: Special steps

## Review Process

### Automated Reviews

1. **GitHub Actions** runs immediately on PR creation
   - Build verification
   - Test execution
   - Security scanning
   - Docker build

2. **CodeRabbit** reviews within minutes
   - Line-by-line analysis
   - Architectural feedback
   - Security suggestions
   - Performance recommendations

### Human Review

Required approvals: **1 maintainer**

Reviewers should verify:
- Architecture boundaries maintained
- Code quality standards met
- Security best practices followed
- Tests adequate and passing
- Documentation complete
- No breaking changes (or properly documented)

### Review Checklist

**Architecture**:
- [ ] Layered architecture maintained
- [ ] No upward dependencies (e.g., Domain → Infrastructure)
- [ ] Proper dependency injection
- [ ] Interfaces used where appropriate

**Code Quality**:
- [ ] Clear, self-documenting code
- [ ] Proper error handling
- [ ] Async/await used correctly
- [ ] LINQ usage optimized

**Security**:
- [ ] No security vulnerabilities
- [ ] Proper authentication/authorization
- [ ] Input sanitization
- [ ] Sensitive data protection

**Testing**:
- [ ] Adequate test coverage
- [ ] Tests are clear and maintainable
- [ ] Edge cases covered
- [ ] No flaky tests

**Performance**:
- [ ] Database queries optimized
- [ ] Proper indexes used
- [ ] No obvious performance issues
- [ ] Caching considered

## Merge Criteria

A PR can be merged when:

1. ✅ All CI checks pass (green)
2. ✅ CodeRabbit review completed
3. ✅ At least 1 maintainer approval
4. ✅ All conversations resolved
5. ✅ No merge conflicts
6. ✅ Branch up to date with target

## Merge Strategy

**Squash and Merge** (default):
- Combines all commits into one
- Keeps main/develop history clean
- Commit message = PR title + description

**When to use Rebase and Merge**:
- Feature with logical, well-crafted commits
- Commits tell a story
- Approved by maintainers

**Never use Merge Commit**:
- Creates unnecessary merge commits
- Makes history harder to follow

## Post-Merge

After merging:

1. **Delete feature branch** (automatic)
2. **Verify deployment** (if auto-deployed)
3. **Close related issues** (if not auto-closed)
4. **Update project board** (if applicable)
5. **Notify stakeholders** (if needed)

## Branch Naming Conventions

```
feature/f0001-short-description
bugfix/issue-123-short-description
hotfix/critical-issue-description
refactor/component-name-improvement
docs/document-name-update
```

Examples:
- `feature/f0001-sign-in-with-google`
- `bugfix/issue-42-fix-token-expiry`
- `hotfix/sql-injection-vulnerability`
- `refactor/auth-service-cleanup`
- `docs/update-api-reference`

## Commit Message Conventions

Follow Conventional Commits:

```
<type>(<scope>): <subject>

<body>

<footer>
```

**Types**:
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `refactor`: Code refactoring
- `test`: Test additions/updates
- `chore`: Build/tooling changes
- `perf`: Performance improvements
- `ci`: CI/CD changes

**Examples**:
```
feat(auth): implement Google OAuth sign-in

Added Google OAuth 2.0 authentication with JWT token generation.

Implements Feature F0001
```

```
fix(auth): prevent token expiry edge case

Fixed issue where tokens expiring during validation caused 500 errors.

Fixes #123
```

## CI/CD Configuration Reference

### Workflow Triggers

```yaml
on:
  pull_request:
    branches: [ main, develop ]
    paths:
      - 'src/Services/Identity/**'
  push:
    branches: [ main, develop ]
    paths:
      - 'src/Services/Identity/**'
```

**Why**:
- Only runs when Identity service changes
- Prevents unnecessary builds
- Saves CI minutes

### Test Database

```yaml
services:
  postgres:
    image: postgres:16-alpine
    env:
      POSTGRES_DB: identity_db_test
      POSTGRES_USER: test_user
      POSTGRES_PASSWORD: test_password
```

**Why**:
- Integration tests need real database
- PostgreSQL 16 matches production
- Isolated test environment

### Caching Strategy

```yaml
- name: Cache NuGet packages
  uses: actions/cache@v3
  with:
    path: ~/.nuget/packages
    key: ${{ runner.os }}-nuget-${{ hashFiles('**/packages.lock.json') }}
```

**Why**:
- Faster builds (30s → 10s)
- Reduced network usage
- Consistent package versions

## CodeRabbit Configuration Reference

Location: `.github/.coderabbit.yaml`

### Key Features

**Auto Review**: All PRs reviewed automatically
**Incremental Review**: Reviews new changes on PR updates
**Language-Specific**: C# and Dockerfile checks
**Path-Specific**: Different rules for different directories

### Custom Checks

**Security**:
- Hardcoded secrets detection
- Authentication/authorization verification
- Token hashing validation
- SQL injection prevention

**Performance**:
- Async/await usage
- Database query optimization
- N+1 query detection
- LINQ efficiency

**Architecture**:
- Layered architecture boundaries
- Dependency direction validation
- SOLID principles adherence
- Interface segregation

### Ignored Patterns

```yaml
ignore_patterns:
  - "**/bin/**"
  - "**/obj/**"
  - "**/Migrations/**"
  - "**/*.Designer.cs"
```

**Why**:
- Build artifacts shouldn't be reviewed
- Migrations are auto-generated
- Designer files are tool-generated

## Enabling CodeRabbit

1. Go to https://coderabbit.ai
2. Sign in with GitHub
3. Install CodeRabbit app
4. Select QuietMatch repository
5. Grant necessary permissions

**First Time Setup**:
- CodeRabbit reads `.coderabbit.yaml` automatically
- No additional configuration needed
- Reviews start on next PR

## Troubleshooting CI

### Build Fails

**Check**:
1. Run `dotnet build` locally
2. Verify all tests pass: `dotnet test`
3. Check for compiler warnings
4. Review CI logs for specific error

**Common Issues**:
- Missing NuGet packages
- Test failures
- Code formatting issues
- Security vulnerabilities detected

### Test Failures

**Check**:
1. Run tests locally: `dotnet test`
2. Check for environment differences
3. Verify database connectivity
4. Review test output in CI logs

**Common Issues**:
- Database connection string
- Timing issues in async tests
- Environment variable differences

### Docker Build Fails

**Check**:
1. Test Docker build locally: `docker build -f Dockerfile .`
2. Verify .dockerignore is correct
3. Check Dockerfile syntax
4. Review Docker build logs

**Common Issues**:
- File paths incorrect
- Missing files in context
- Base image pull issues

### CodeRabbit Not Reviewing

**Check**:
1. Verify CodeRabbit app is installed
2. Check repository permissions
3. Review `.coderabbit.yaml` syntax
4. Check CodeRabbit dashboard

**Common Issues**:
- App not installed
- Insufficient permissions
- Configuration file errors

## Best Practices

### PR Size

**Keep PRs small**:
- Target: < 400 lines changed
- Max: < 800 lines changed
- Large changes: Split into multiple PRs

**Why**:
- Easier to review
- Faster feedback
- Lower risk
- Easier to revert

### PR Description

**Be descriptive**:
- Explain the "why", not just the "what"
- Include screenshots/videos if UI changes
- Link to relevant documentation
- Describe testing performed

### Responding to Reviews

**Be respectful**:
- Thank reviewers for feedback
- Explain your reasoning if disagreeing
- Ask for clarification if needed
- Mark conversations as resolved

### Keeping PR Updated

**Stay current**:
- Rebase on target branch regularly
- Resolve conflicts promptly
- Address review feedback quickly
- Keep CI checks green

## Resources

- **GitHub Actions Docs**: https://docs.github.com/actions
- **CodeRabbit Docs**: https://docs.coderabbit.ai
- **Conventional Commits**: https://www.conventionalcommits.org
- **C# Coding Conventions**: https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions

## Contact

Questions about the PR process?
- Create an issue with label `question`
- Ask in team chat
- Contact project maintainers
