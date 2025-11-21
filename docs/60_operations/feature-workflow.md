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

**Process**:
1. Create file: `docs/40_features/f####_feature_name.md`
   - Use next sequential number (f0001, f0002, etc.)
   - Use lowercase with underscores: `f0001_sign_in_with_google.md`

2. Copy template from `f0001_sign_in_with_google.md`

3. Fill in all sections:
   ```markdown
   # Feature F0001: Your Feature Name

   **Status**: 🔴 Not Started
   **Priority**: P0/P1/P2/P3
   **GitHub Issue**: (leave blank for now)
   **Assignee**: TBD
   **Sprint**: Phase X
   **Estimated Effort**: X hours

   ## Overview
   ...detailed description...

   ## Acceptance Criteria
   - [ ] AC1: ...
   - [ ] AC2: ...
   (list ALL criteria)

   ## API Specification
   ...full API docs...

   ## Database Changes
   ...schema...

   ## Testing Strategy
   ...complete testing plan...
   ```

4. Commit to repo:
   ```bash
   git add docs/40_features/f0002_your_feature.md
   git commit -m "docs: add feature spec for F0002 Your Feature"
   git push
   ```

---

### Step 2: Create Detailed Implementation Plan

**When**: After feature file is committed, before starting implementation

**Purpose**: Break down the high-level implementation checklist into detailed, step-by-step tasks with documentation references

**Process**:
1. Create file: `docs/40_features/f####_feature_name/plan.md`
   - Create folder for feature: `docs/40_features/f####_feature_name/`
   - Move feature file into folder: `f####_feature_name.md`
   - Create plan file: `plan.md`

2. Expand the implementation checklist into detailed tasks:
   ```markdown
   # Implementation Plan - F0002: Your Feature Name

   **Status**: 🟡 In Progress
   **Started**: 2025-11-21
   **Last Updated**: 2025-11-21

   ## Setup
   - [ ] Create feature branch: `feature/f0002-your-feature-name`
   - [ ] Review relevant documentation (list specific docs)

   ## Phase 1: Domain Layer
   - [ ] Create domain entities
     - **Reference**: `docs/10_architecture/03_service-templates.md` (Section X.X)
     - **Why**: Domain-first approach per architecture guidelines
     - [ ] Create Entity1 with value objects
     - [ ] Create Entity2 with business rules
     - [ ] Add domain events if needed

   ## Phase 2: Infrastructure Layer
   - [ ] Set up database context
     - **Reference**: `docs/10_architecture/02_architecture-guidelines.md` (EF Core section)
     - [ ] Create DbContext
     - [ ] Configure entity mappings
     - [ ] Create migration: `Add_Entity1_Entity2_Tables`

   ## Phase 3: Application Layer
   - [ ] Implement application services
     - **Reference**: Feature file acceptance criteria
     - [ ] Create Service1 with business logic
     - [ ] Add FluentValidation

   ## Phase 4: API Layer
   - [ ] Add endpoints
     - **Reference**: Feature file API specification
     - [ ] Create Controller with endpoints
     - [ ] Add JWT authentication middleware
     - [ ] Add rate limiting

   ## Phase 5: Messaging
   - [ ] Configure event publishing
     - **Reference**: `docs/10_architecture/06_messaging-and-integration.md`
     - [ ] Create event definitions
     - [ ] Configure MassTransit
     - [ ] Implement outbox pattern

   ## Phase 6: Testing
   - [ ] Write unit tests
     - **Reference**: Feature file testing strategy
     - [ ] Domain logic tests
     - [ ] Application service tests
   - [ ] Write integration tests
     - [ ] API endpoint tests
     - [ ] Database tests

   ## Phase 7: Docker Integration
   - [ ] Create Dockerfile
   - [ ] Update docker-compose.yml
   - [ ] Test locally

   ## Completion
   - [ ] All tests passing
   - [ ] Manual testing checklist complete
   - [ ] Feature file updated
   - [ ] Ready for PR
   ```

3. Add specific documentation references for each task:
   - Link to architecture guidelines
   - Link to service templates
   - Link to security docs
   - Link to messaging docs
   - Reference specific sections/line numbers

4. Include dependencies and order:
   - Note which tasks must be done before others
   - Mark blocking dependencies

5. Commit the plan:
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
