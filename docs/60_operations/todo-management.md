# TODO Management Guidelines

> **Purpose**: This document defines how to manage the project-level TODO list for tracking technical debt, future work, and follow-up tasks that emerge during feature development.

---

## Table of Contents

- [Overview](#overview)
- [When to Create a TODO](#when-to-create-a-todo)
- [TODO Format](#todo-format)
- [Priority Levels](#priority-levels)
- [TODO Lifecycle](#todo-lifecycle)
- [Best Practices](#best-practices)
- [Examples](#examples)

---

## Overview

### What is the Project TODO List?

The project-level TODO list (`/TODO.md`) is a centralized backlog for tracking work that:

1. **Emerged from feature implementation** but is out of scope
2. **Should be done eventually** but not blocking current work
3. **Is not a new feature** (features belong in `docs/40_features/`)
4. **Improves quality, security, or maintainability**

### What is NOT in the TODO List?

- ❌ **New features** → Create a feature spec in `docs/40_features/`
- ❌ **Bugs requiring immediate fix** → Create GitHub issue with `type: bug`
- ❌ **Current sprint/milestone work** → Track in GitHub Issues or Project Board
- ❌ **Questions or discussions** → Use GitHub Discussions

---

## When to Create a TODO

### During Feature Implementation

**You should add a TODO when**:

1. **Security Hardening**: You identify security improvements needed for production but not blocking MVP
   - Example: "Add rate limiting", "Implement Azure Key Vault integration"

2. **Technical Debt**: You make a conscious decision to simplify now and improve later
   - Example: "Refactor to use repository pattern", "Extract to separate service"

3. **Missing Endpoints**: You implement part of a system but other endpoints are not in current scope
   - Example: "Add refresh token endpoint", "Implement token revocation"

4. **Production Readiness**: You identify gaps between development and production
   - Example: "Add health check endpoints", "Configure HTTPS enforcement"

5. **Future Enhancements**: You discover related functionality that would be valuable but not essential
   - Example: "Add Sign In with Apple", "Implement email verification"

6. **Performance Optimizations**: You identify potential improvements that aren't critical yet
   - Example: "Add Redis caching for user lookups", "Implement query result pagination"

### During Code Review

**Reviewers should suggest TODOs when**:

1. **Scope creep identified**: Reviewer spots work that should be deferred
   - Example: "Let's add avatar upload to TODO and deliver core profile first"

2. **Quality improvements**: Reviewer identifies enhancements that aren't blocking
   - Example: "Consider adding retry logic (TODO item)"

3. **Documentation gaps**: Reviewer notices missing docs that should exist eventually
   - Example: "Add runbook for production deployment (TODO)"

---

## TODO Format

Each TODO entry must include:

### Required Fields

```markdown
### 🟡 T####: Clear, Actionable Title

**Source**: [F#### - Feature Name](path/to/feature/spec.md) or [GitHub Issue #123](link)
**Status**: 🟡 Ready / 🔴 Blocked / 🟢 In Progress
**Effort**: X-Y hours (realistic estimate)
**Created**: YYYY-MM-DD

#### Description
High-level summary of what needs to be done and why it matters.

#### Current Limitations
List of what works today (✅) and what's missing (❌).

#### What Needs to Be Done
Detailed, step-by-step breakdown of the work:
1. Step one with specifics
2. Step two with code examples
3. Step three with configuration details

#### Acceptance Criteria
- [ ] Criterion 1
- [ ] Criterion 2
- [ ] Tests passing
- [ ] Documentation updated

#### References
- Link to relevant docs
- Link to external resources
```

### TODO ID Format

- **Pattern**: `T####` (T0001, T0002, etc.)
- **Numbering**: Sequential, starting from T0001
- **Scope**: Project-wide (not per-feature or per-service)

### Status Indicators

| Symbol | Status | Meaning |
|--------|--------|---------|
| 🟡 | Ready | Can be implemented when capacity available |
| 🔴 | Blocked | Waiting on external dependency or decision |
| 🟢 | In Progress | Currently being worked on |
| ✅ | Done | Completed (moved to archive) |

---

## Priority Levels

### P0 - Critical

**What qualifies**:
- Security vulnerabilities requiring immediate fix
- Data loss or corruption risks
- Production outages or blockers

**Action**: Should be converted to GitHub Issues and worked on ASAP, not left in TODO.

**Example**: "SQL injection vulnerability in user input" ❌ (fix now, don't TODO)

---

### P1 - High Priority

**What qualifies**:
- Production readiness requirements (HTTPS, Key Vault, health checks)
- Essential security hardening (rate limiting, CORS, security headers)
- Missing critical endpoints (refresh token, token revocation)
- GDPR compliance gaps

**Timeline**: Should be completed before production launch.

**Examples**:
- "Production Security Hardening for IdentityService"
- "Implement Refresh Token Endpoint"
- "Add Health Check Endpoints"

---

### P2 - Medium Priority

**What qualifies**:
- Quality improvements (better error messages, validation)
- Performance optimizations (caching, query tuning)
- Developer experience (better logging, local dev tools)
- Technical debt that's not urgent

**Timeline**: Complete within 1-2 sprints after MVP.

**Examples**:
- "Add structured logging with correlation IDs"
- "Implement Redis caching for user profiles"
- "Create database migration rollback scripts"

---

### P3 - Low Priority

**What qualifies**:
- Nice-to-have features not in MVP scope
- Experimental or exploratory work
- Long-term improvements
- Alternative authentication providers

**Timeline**: Revisit quarterly or when capacity allows.

**Examples**:
- "Implement Sign In with Apple"
- "Add two-factor authentication (2FA)"
- "Explore ML-based fraud detection"

---

## TODO Lifecycle

### 1. Creation

**When**: During feature implementation or code review

**Process**:
1. Identify work that should be deferred
2. Add entry to `/TODO.md` in appropriate priority section
3. Assign TODO ID (next sequential number)
4. Fill in all required fields (source, effort, description, criteria)
5. Set status to 🟡 Ready
6. Commit with message: `docs: add T#### to TODO list`

### 2. Prioritization

**When**: Weekly or bi-weekly TODO review meeting (team activity)

**Process**:
1. Review all new TODOs
2. Validate priority assignments
3. Identify blockers (change status to 🔴 Blocked if needed)
4. Move high-priority items to GitHub Issues if ready to schedule

### 3. Implementation

**When**: TODO is scheduled for work (capacity available)

**Process**:
1. Create GitHub Issue linking to TODO item
   - Title: "T####: TODO title"
   - Body: Link to TODO.md section, copy acceptance criteria
2. Update TODO status to 🟢 In Progress
3. Implement following standard feature workflow
4. Create PR referencing GitHub Issue

### 4. Completion

**When**: Work is done, tests passing, PR merged

**Process**:
1. Update TODO status to ✅ Done
2. Add completion date
3. Move TODO to "Archive" section at bottom of TODO.md
4. Close GitHub Issue (if created)
5. Commit: `docs: mark T#### as complete`

### 5. Archival

**Completed TODOs** are moved to the Archive section with:
- Original TODO content (for reference)
- Completion date
- Link to PR that completed the work
- Brief notes on outcome

**Why archive instead of delete?**
- Historical record of decisions
- Reference for similar future work
- Team learning and context preservation

---

## Best Practices

### ✅ DO

1. **Be specific and actionable**
   - ✅ "Add sliding window rate limiting (5 req/5min per IP)"
   - ❌ "Improve security"

2. **Include acceptance criteria**
   - ✅ "[ ] HTTPS enforced on all endpoints"
   - ❌ "Make it secure"

3. **Link to source**
   - ✅ "Source: F0001 - Sign In with Google"
   - ❌ No source reference

4. **Estimate effort realistically**
   - ✅ "Effort: 4-6 hours"
   - ❌ "Effort: ???"

5. **Update status promptly**
   - ✅ Change to 🟢 when you start work
   - ❌ Leave stale 🟡 status when already done

### ❌ DON'T

1. **Don't use as a brain dump**
   - ❌ "Maybe we should do X, Y, Z someday"
   - ✅ Only add work you intend to do eventually

2. **Don't leave TODOs hanging forever**
   - ❌ P1 item sitting for 6 months with no action
   - ✅ Review quarterly, downgrade or archive if not relevant

3. **Don't duplicate GitHub Issues**
   - ❌ Create TODO and GitHub Issue at same time
   - ✅ TODO first, GitHub Issue when scheduling work

4. **Don't mix features with TODOs**
   - ❌ "F0002: Add profile creation" in TODO.md
   - ✅ Features belong in `docs/40_features/`

5. **Don't skip documentation**
   - ❌ Just add code comments like `// TODO: Fix this`
   - ✅ Document properly in TODO.md with context

---

## Examples

### Good TODO Entry

```markdown
### 🟡 T0001: Production Security Hardening for IdentityService

**Source**: [F0001 - Sign In with Google](docs/40_features/f0001_sign_in_with_google/f0001_sign_in_with_google.md)
**Status**: 🟡 Ready
**Effort**: 8-12 hours
**Created**: 2025-11-24

#### Description
The IdentityService needs security hardening before production deployment, including HTTPS enforcement, Azure Key Vault integration, advanced rate limiting, and security headers.

#### Current Limitations
- ✅ JWT tokens are signed with HMAC-SHA256 (secure)
- ❌ HTTPS not enforced (development only)
- ❌ Secrets stored in appsettings.json (not Key Vault)

#### What Needs to Be Done
1. Configure HTTPS redirection middleware and HSTS headers
2. Migrate all secrets to Azure Key Vault
3. Implement sliding window rate limiting (5 req/5min per IP)
4. Configure CORS policy for production domains
5. Add security headers (X-Content-Type-Options, X-Frame-Options, etc.)

#### Acceptance Criteria
- [ ] HTTPS enforced on all endpoints
- [ ] All secrets stored in Azure Key Vault
- [ ] Advanced rate limiting active
- [ ] CORS policy configured
- [ ] Security headers present
- [ ] Deployment guide updated

#### References
- Feature Spec: docs/40_features/f0001_sign_in_with_google/f0001_sign_in_with_google.md
- Azure Key Vault Docs: https://learn.microsoft.com/en-us/azure/key-vault/
```

**Why this is good**:
- Clear, actionable title
- Source feature linked
- Current state documented (what works, what's missing)
- Step-by-step work breakdown
- Measurable acceptance criteria
- External references provided

---

### Bad TODO Entry ❌

```markdown
### Make it more secure

Need to add security stuff before production.

TODO:
- HTTPS?
- Other things
```

**Why this is bad**:
- Vague title (what specifically?)
- No source, status, effort, or creation date
- No description of current state
- No actionable steps
- No acceptance criteria
- No references

---

## TODO Review Process

### Weekly Review (Team Activity)

**Attendees**: Engineering team, tech lead, product owner (optional)

**Agenda**:
1. **New TODOs** (5 min): Review items added this week
2. **P1 Items** (10 min): Ensure high-priority items are progressing
3. **Blocked Items** (5 min): Identify how to unblock
4. **Stale Items** (5 min): Downgrade or archive items > 3 months old with no progress
5. **Prioritization** (10 min): Adjust priorities based on product roadmap

**Outcome**:
- P1 items moved to GitHub Issues and scheduled
- Stale items archived or downgraded
- Team alignment on what's important

---

## Migration Path

### Converting TODO Comments to Project TODO

If you have code comments like `// TODO: Add rate limiting`, migrate them:

1. **Search for TODO comments**:
   ```bash
   git grep -n "TODO:" src/
   ```

2. **Evaluate each comment**:
   - Is it still relevant? (If no, delete the comment)
   - Is it critical? (If yes, create GitHub Issue immediately)
   - Is it future work? (If yes, add to TODO.md)

3. **Replace comment with reference**:
   ```csharp
   // See TODO.md T0042 for planned rate limiting implementation
   ```

4. **Remove after completion**:
   - When TODO is done, remove the code comment entirely

---

## Glossary

| Term | Definition |
|------|------------|
| **TODO** | Work item tracked in `/TODO.md` for future implementation |
| **Feature** | New capability tracked in `docs/40_features/f####_name.md` |
| **GitHub Issue** | Scheduled work tracked in GitHub Issues tab |
| **Technical Debt** | Code quality issues that should be addressed eventually |
| **Blocked** | Cannot proceed due to external dependency or decision |

---

## FAQ

### Q: Should I add TODOs during feature implementation or after?

**A**: During! When you discover work that's out of scope, add it to TODO.md immediately. Don't wait until the end or you'll forget important context.

---

### Q: How is a TODO different from a GitHub Issue?

**A**:
- **TODO**: Unscheduled future work discovered during development
- **GitHub Issue**: Scheduled work being tracked for a milestone/sprint

Workflow: TODO → (when ready to schedule) → GitHub Issue → Implementation

---

### Q: Do I need approval to add a TODO?

**A**: No, any developer can add a TODO. However:
- P0 items should be discussed immediately (probably need to fix now, not TODO)
- P1 items should be reviewed by tech lead weekly
- P2/P3 items can be added freely

---

### Q: What if my TODO is a large feature?

**A**: If effort > 16 hours (2 days), it's probably a feature:
- Create a feature spec in `docs/40_features/`
- Link the feature spec in TODO.md as "See Feature F####"
- Use TODO as a reminder to schedule the feature

---

### Q: Should I reference TODOs in code comments?

**A**: Yes, but minimally:
```csharp
// See TODO.md T0042: Rate limiting will be added in future release
```

Don't duplicate content—just reference the TODO ID.

---

### Q: What if a TODO becomes obsolete?

**A**: Update the status and move to Archive with a note:
```markdown
**Status**: ✅ Obsolete
**Archived**: 2025-12-01
**Reason**: No longer needed due to architecture change (switched to API Gateway)
```

---

## References

- **Project TODO List**: [`/TODO.md`](/TODO.md)
- **Feature Workflow**: [`docs/60_operations/feature-workflow.md`](feature-workflow.md)
- **GitHub Issues Guide**: [`docs/60_operations/github-issues.md`](github-issues.md) (if exists)

---

**Last Updated**: 2025-11-24
**Maintained By**: QuietMatch Engineering Team
**Status**: Living Document
