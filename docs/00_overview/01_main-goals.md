# Main Goals - QuietMatch

> **This document defines the technical, learning, and business objectives for the QuietMatch project.**

---

## Table of Contents

- [Project Purpose](#project-purpose)
- [Learning Objectives](#learning-objectives)
- [Technical Objectives](#technical-objectives)
- [Business Objectives](#business-objectives)
- [Quality & Compliance Objectives](#quality--compliance-objectives)
- [Success Criteria](#success-criteria)

---

## Project Purpose

QuietMatch serves **dual purposes**:

1. **Product**: Build a privacy-focused dating platform that solves real problems in the online dating space
2. **Learning**: Master modern .NET microservices architecture through hands-on implementation

This documentation-first, pattern-driven approach ensures every technical decision serves both goals: creating a viable product while teaching industry best practices.

---

## Learning Objectives

### 1. **Architecture Patterns Mastery**

**Goal**: Deeply understand and implement 4 distinct architecture patterns by applying each to a real microservice.

#### Patterns to Master:

##### **Layered Architecture** (IdentityService, SchedulingService, RealTimeService, GraphQLGateway)
- **What**: Traditional N-tier architecture (Presentation → Application → Domain → Infrastructure)
- **When**: Simple CRUD services, clear separation of concerns
- **Learn**: Dependency management, layer responsibilities, when to use vs. avoid
- **Deliverable**: Implement IdentityService using Layered architecture with comprehensive comments explaining layer boundaries

##### **Onion Architecture** (ProfileService)
- **What**: Domain-centric design with dependencies pointing inward
- **When**: Rich domain logic, complex business rules
- **Learn**: Domain-driven design, dependency inversion, core domain isolation
- **Deliverable**: Implement ProfileService with core domain logic for privacy rules, validation, and GDPR compliance

##### **Hexagonal Architecture / Ports & Adapters** (MatchingService, NotificationService, VerificationService, PaymentService)
- **What**: Domain core surrounded by ports (interfaces) and adapters (implementations)
- **When**: Multiple external dependencies, need for adapter swapping (e.g., different matching engines)
- **Learn**: Port/adapter pattern, dependency inversion at system boundary, testability through mocking ports
- **Deliverable**: Implement MatchingService with swappable matching engines (RuleBasedMatchingEngine → future EmbeddingBasedMatchingEngine)

##### **CQRS (Command Query Responsibility Segregation)** (SchedulingService, potentially MatchingService)
- **What**: Separate models/handlers for writes (commands) and reads (queries)
- **When**: Different optimization needs for reads vs. writes, event sourcing, high-scale systems
- **Learn**: Command handlers, query handlers, eventual consistency, read models
- **Deliverable**: Implement SchedulingService with MediatR, separate write (create availability slot) and read (query open slots) models

### 2. **Microservices Patterns**

**Goal**: Implement production-ready microservices communication patterns.

#### Patterns to Implement:

- **SAGA Pattern**: Orchestrated distributed transactions across microservices
  - **Example**: Blind Date Creation SAGA (Match Acceptance → Availability Reservation → Notifications → Date Confirmation)
  - **Learn**: Compensating transactions, state management, failure handling

- **Event-Driven Architecture**: Async messaging between services
  - **Example**: `MatchAccepted` event triggers scheduling, notifications, analytics
  - **Learn**: Event design, eventual consistency, idempotency

- **API Gateway Pattern**: Single entry point for client requests
  - **Example**: GraphQL Gateway aggregates data from Profile, Matching, Scheduling services
  - **Learn**: Request routing, response aggregation, protocol translation

- **Database per Service**: Each microservice owns its data
  - **Example**: IdentityService has `identity_db`, ProfileService has `profile_db`
  - **Learn**: Data sovereignty, eventual consistency across services, managing distributed data

- **Outbox Pattern**: Transactional messaging (ensure event publishing and DB write are atomic)
  - **Example**: Profile update writes to DB and publishes `ProfileUpdated` event atomically
  - **Learn**: Two-phase commit alternative, message relay pattern

### 3. **Communication Protocols**

**Goal**: Master multiple inter-service communication patterns.

#### REST APIs
- **When**: External clients (web app, mobile app), CRUD operations
- **Tech**: ASP.NET Core Web API, Swagger/Scalar for documentation
- **Learn**: RESTful design, versioning, hypermedia, authentication (JWT)

#### gRPC
- **When**: High-performance internal service-to-service calls
- **Tech**: gRPC .NET, protocol buffers
- **Learn**: Contract-first design (.proto files), streaming, performance benefits over REST

#### GraphQL
- **When**: Client-facing API aggregation, flexible querying
- **Tech**: HotChocolate, schema stitching
- **Learn**: Schema design, resolvers, query optimization, N+1 problem solutions

#### Async Messaging
- **When**: Event-driven workflows, decoupled services
- **Tech**: RabbitMQ (local), Azure Service Bus (cloud), MassTransit abstraction
- **Learn**: Message design, publish/subscribe, competing consumers, dead letter queues

### 4. **Security & Authentication**

**Goal**: Implement production-grade authentication and authorization.

#### Topics to Master:

- **OAuth 2.0 / OpenID Connect**: Implement custom IdentityService (not Duende)
  - Social login flows (Google, Apple Sign-In)
  - Authorization code flow
  - Token issuance and validation

- **JWT (JSON Web Tokens)**:
  - Access tokens (short-lived, 15 min)
  - Refresh tokens (long-lived, 7 days)
  - Token validation across all APIs (REST, gRPC, GraphQL)

- **Service-to-Service Authentication**:
  - Client credentials flow for M2M (machine-to-machine)
  - Managed identities (Azure future)

- **Field-Level Encryption**:
  - AES-256 for sensitive data
  - EF Core value converters for transparent encryption/decryption
  - Key management (Azure Key Vault for production)

### 5. **Cloud Migration Path**

**Goal**: Design cloud-agnostic systems that can run locally and in Azure.

#### Local Development
- **Infrastructure**: Docker Compose
- **Database**: PostgreSQL (Docker container)
- **Messaging**: RabbitMQ (Docker container)
- **Caching**: Redis (Docker container)
- **Logging**: Seq (structured logs)

#### Azure Cloud
- **Compute**: Azure Container Apps (ACA) or AKS (Kubernetes)
- **Database**: Azure Database for PostgreSQL - Flexible Server
- **Messaging**: Azure Service Bus
- **Caching**: Azure Cache for Redis
- **Logging**: Application Insights
- **Secrets**: Azure Key Vault
- **Identity**: Azure Managed Identity

#### Abstraction Strategy
- Use interfaces for all external dependencies (`IMessagePublisher`, `IDatabaseContext`, etc.)
- Configuration via environment variables (12-factor app)
- Infrastructure as Code (Bicep templates for Azure)

### 6. **DevOps & Deployment**

**Goal**: Understand modern deployment strategies.

#### Blue-Green Deployment
- **Concept**: Run two identical environments (blue = current, green = new), switch traffic instantly
- **Local**: Docker Compose profiles
- **Azure**: Container Apps revisions or Kubernetes deployments
- **Learn**: Zero-downtime deployments, rollback strategies

#### Database Migrations
- **Strategy**: Expand/Contract pattern for backward compatibility
- **Tool**: Entity Framework Core Migrations
- **Learn**: Schema versioning, data migration safety, rollback planning

#### CI/CD
- **Tool**: GitHub Actions or Azure DevOps
- **Pipeline**: Build → Test → Deploy to staging → Deploy to production (blue-green)
- **Learn**: Automated testing, deployment gates, rollback automation

---

## Technical Objectives

### 1. **Implement All 10 Microservices**

| Service | Architecture | Primary Tech | Key Learning |
|---------|--------------|--------------|--------------|
| IdentityService | Layered | ASP.NET Core, OAuth2, JWT | Authentication flows, token management |
| ProfileService | Onion | ASP.NET Core, EF Core, Encryption | Domain-centric design, privacy by design |
| MatchingService | Hexagonal | ASP.NET Core, gRPC, pgvector | Ports/adapters, ML integration (embeddings) |
| SchedulingService | Layered + CQRS | ASP.NET Core, MediatR, SAGA | CQRS, orchestrated SAGA, calendar integration |
| NotificationService | Hexagonal | ASP.NET Core, Email/Push adapters | Multi-channel notifications, template engine |
| VerificationService | Hexagonal | ASP.NET Core, Twilio API | Third-party integrations, pay-per-use billing |
| PaymentService | Hexagonal | ASP.NET Core, Stripe API | Webhooks, subscription management, SAGA |
| RealTimeService | Layered | SignalR, Redis backplane | Real-time communication, serverless SignalR |
| GraphQLGateway | Layered | HotChocolate, schema stitching | GraphQL, API aggregation, federation |
| ExternalIntegrationAPI | Layered | ASP.NET Core, REST | Public API design, rate limiting, API keys |

### 2. **Technology Stack Mastery**

#### .NET Ecosystem
- **.NET 8** (LTS release)
- **C# 12** (latest language features)
- **ASP.NET Core** (Web API, minimal APIs where appropriate)
- **Entity Framework Core 9** (ORM, migrations, value converters)
- **MediatR** (CQRS, in-process messaging)
- **FluentValidation** (command/input validation)
- **AutoMapper** (DTO mapping)

#### Data & Caching
- **PostgreSQL 16** (primary database)
- **pgvector** extension (vector similarity search for embeddings)
- **Redis** (distributed caching, SignalR backplane)
- **Entity Framework Core** (Code-First migrations)

#### Messaging & Integration
- **RabbitMQ** (local development)
- **Azure Service Bus** (cloud production)
- **MassTransit** (abstraction over message brokers, SAGA support)

#### API Technologies
- **gRPC** (high-performance internal APIs)
- **HotChocolate** (GraphQL server)
- **Swagger/Scalar** (REST API documentation)

#### DevOps & Monitoring
- **Docker** (containerization)
- **Docker Compose** (local orchestration)
- **Kubernetes** (future: AKS)
- **Seq** (local structured logging)
- **Application Insights** (Azure monitoring)
- **OpenTelemetry** (distributed tracing)

### 3. **Testing Strategy**

**Goal**: Achieve high test coverage with meaningful tests.

#### Unit Tests
- **Framework**: xUnit
- **Mocking**: NSubstitute or Moq
- **Coverage Target**: 80% for domain logic, application services
- **Example**: Test matching algorithm logic without external dependencies

#### Integration Tests
- **Framework**: xUnit + WebApplicationFactory
- **Infrastructure**: Testcontainers (real PostgreSQL, RabbitMQ, Redis containers)
- **Coverage Target**: All database interactions, message publishing
- **Example**: Test ProfileService creates profile and publishes `ProfileCreated` event

#### API Tests
- **Framework**: xUnit + WebApplicationFactory
- **Coverage**: All HTTP endpoints, gRPC services, GraphQL queries
- **Example**: Test `/api/identity/login` endpoint returns JWT token

#### End-to-End Tests (Future)
- **Framework**: Playwright or Selenium
- **Coverage**: Critical user flows (signup → match → schedule date)

---

## Business Objectives

### 1. **MVP Delivery (Months 1-6)**

**Goal**: Ship a functional MVP with core features.

#### MVP Features

- **User Authentication**: Sign in with Google, JWT issuance
- **Profile Creation**: Personality questionnaire, privacy settings
- **Basic Matching**: Rule-based compatibility scoring
- **Blind Match Presentation**: Show compatibility breakdown (no photos)
- **Automated Scheduling**: Availability matching, date proposal
- **Email Notifications**: Match accepted, date scheduled
- **GDPR Compliance**: Data export, deletion workflows

#### MVP Success Criteria

- [ ] 100 beta users successfully onboarded
- [ ] 50 matches generated
- [ ] 20 dates scheduled via automated scheduling
- [ ] Zero security incidents or data breaches
- [ ] All GDPR data deletion requests fulfilled within 30 days
- [ ] Positive feedback from 70% of beta users

### 2. **Post-MVP Enhancements (Months 7-12)**

**Goal**: Refine product based on user feedback, add revenue features.

#### Features

- **Freemium Subscriptions**: Implement PaymentService, Stripe integration
- **Premium Matching**: More matches per month for paid users
- **ID Verification**: VerificationService with Twilio Verify
- **Real-Time Notifications**: SignalR for instant match updates
- **Mobile App**: .NET MAUI Blazor Hybrid app (iOS, Android)
- **Embedding-Based Matching**: AI personality analysis with vector search

#### Revenue Target

- **Year 1**: €25,000 ARR (Annual Recurring Revenue)
- **Users**: 5,000 registered users
- **Conversion**: 5% freemium conversion rate (250 paid users × €99/year)

### 3. **Long-Term Business Goals (Year 2-3)**

- Expand to 5 EU countries
- 100,000 active users
- €1.5M ARR
- Profitability
- B Corp certification (social enterprise)

---

## Quality & Compliance Objectives

### 1. **GDPR Compliance**

**Goal**: Full compliance with all GDPR requirements.

#### Implementation Checklist

- [ ] **Data Minimization**: Only collect necessary data
- [ ] **Privacy by Design**: Encryption, access control, audit logging
- [ ] **User Rights**:
  - [ ] Right to Access (data export)
  - [ ] Right to Erasure (account deletion SAGA)
  - [ ] Right to Rectification (profile editing)
  - [ ] Right to Data Portability (JSON export)
  - [ ] Right to Object (opt-out of matching criteria)
- [ ] **Consent Management**: Granular consent flags, version tracking
- [ ] **Transparent Privacy Policy**: Plain language, in-app explanations
- [ ] **Data Retention Policy**: Soft delete (30 days) → hard delete

### 2. **Security Standards**

**Goal**: Implement industry-standard security practices.

#### Requirements

- [ ] **Encryption**: AES-256 at rest, TLS 1.3 in transit
- [ ] **Authentication**: OAuth2 + JWT for all APIs
- [ ] **Authorization**: Role-based access control (RBAC)
- [ ] **Secrets Management**: Azure Key Vault (no hardcoded secrets)
- [ ] **Audit Logging**: All data access logged with correlation IDs
- [ ] **Penetration Testing**: Annual security audit
- [ ] **Dependency Scanning**: Automated vulnerability checks (Dependabot)
- [ ] **OWASP Top 10**: Address all common vulnerabilities

### 3. **Code Quality**

**Goal**: Maintainable, well-documented, testable code.

#### Standards

- [ ] **Ubiquitous Language**: Domain terms used consistently across code, docs, UI
- [ ] **Meaningful Comments**: Explain architectural decisions, complex business logic
- [ ] **SOLID Principles**: Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, Dependency Inversion
- [ ] **DRY (Don't Repeat Yourself)**: Shared BuildingBlocks libraries for common functionality
- [ ] **Test Coverage**: 80%+ for critical business logic
- [ ] **Code Reviews**: All changes reviewed before merging
- [ ] **Automated Formatting**: EditorConfig, consistent style

### 4. **Documentation Quality**

**Goal**: Comprehensive, up-to-date documentation for developers and users.

#### Requirements

- [ ] **Architecture Docs**: Detailed explanation of all patterns used
- [ ] **PATTERNS.md**: Each microservice explains its architecture (why, how, alternatives)
- [ ] **API Docs**: Auto-generated Swagger/Scalar for REST, GraphQL schema docs
- [ ] **Feature Docs**: Each feature has a spec file with acceptance criteria, sequence diagrams
- [ ] **Ubiquitous Language**: Glossary of all domain terms
- [ ] **Decision Records**: ADRs for all significant architectural decisions
- [ ] **Runbooks**: Deployment, troubleshooting, monitoring guides

---

## Success Criteria

### Learning Success

**Goal**: Master all targeted patterns and technologies.

- [ ] Implement all 4 architecture patterns (Layered, Onion, Hexagonal, CQRS)
- [ ] Implement at least 2 SAGAs (Blind Date Creation, User Deletion)
- [ ] Successfully deploy to Azure (at least one microservice)
- [ ] Achieve blue-green deployment working locally
- [ ] All team members can explain:
  - [ ] When to use Layered vs. Onion vs. Hexagonal
  - [ ] How SAGA compensations work
  - [ ] How JWT authentication flows work
  - [ ] How CQRS improves scalability
- [ ] Comprehensive documentation of learnings in PATTERNS.md files

### Technical Success

**Goal**: Production-ready system that can scale.

- [ ] All 10 microservices running in Docker Compose
- [ ] End-to-end user flow works: Signup → Profile → Match → Schedule → Date
- [ ] SAGA handles failure scenarios gracefully (compensations tested)
- [ ] 90%+ API uptime in production
- [ ] Sub-second response times for 95% of API calls
- [ ] Zero critical security vulnerabilities
- [ ] All GDPR workflows functional and tested

### Business Success

**Goal**: Viable product with user traction.

- [ ] MVP shipped and deployed to Azure
- [ ] 100+ beta users providing feedback
- [ ] 30%+ match acceptance rate (users accept curated matches)
- [ ] 70%+ date conversion rate (accepted matches → scheduled dates)
- [ ] 85%+ attendance rate (scheduled dates actually happen)
- [ ] Positive user sentiment (70%+ rate app as "good" or "excellent")
- [ ] Revenue-generating (freemium or subscriptions live)

---

## Milestones & Timeline

### Phase 1: Foundation (Months 1-2)

- [ ] Project setup (repos, CI/CD, Docker Compose)
- [ ] Implement IdentityService (Layered architecture)
- [ ] Implement ProfileService (Onion architecture)
- [ ] Implement basic GraphQL Gateway
- [ ] Local dev environment fully functional

**Deliverable**: Users can sign in and create profiles locally.

### Phase 2: Core Matching (Months 3-4)

- [ ] Implement MatchingService (Hexagonal architecture)
- [ ] Implement SchedulingService (CQRS)
- [ ] Implement NotificationService
- [ ] Implement Blind Date Creation SAGA
- [ ] Write comprehensive integration tests

**Deliverable**: End-to-end flow works (signup → match → schedule).

### Phase 3: Real-Time & Enhancements (Months 5-6)

- [ ] Implement RealTimeService (SignalR)
- [ ] Implement VerificationService
- [ ] Implement PaymentService (Stripe)
- [ ] Implement embedding-based matching (AI/ML)
- [ ] Build Blazor web app (frontend)

**Deliverable**: MVP shipped with all core features.

### Phase 4: Cloud & Scale (Months 7-9)

- [ ] Deploy to Azure (Container Apps or AKS)
- [ ] Blue-green deployment setup
- [ ] Monitoring and observability (Application Insights)
- [ ] Performance optimization
- [ ] Security audit and penetration testing

**Deliverable**: Production-ready system running in Azure.

### Phase 5: Mobile & Growth (Months 10-12)

- [ ] Build .NET MAUI Blazor Hybrid mobile app
- [ ] User onboarding and growth experiments
- [ ] Analytics and feedback loops
- [ ] Iterate based on user feedback

**Deliverable**: Mobile apps published to App Store and Google Play.

---

## Key Performance Indicators (KPIs)

### Learning KPIs

- **Documentation Completeness**: All PATTERNS.md files written and reviewed
- **Test Coverage**: 80%+ for business logic
- **Team Knowledge**: All team members can implement a new microservice following a pattern

### Technical KPIs

- **API Response Time**: p95 < 500ms
- **Uptime**: 99%+
- **Deployment Frequency**: Weekly deployments to staging
- **Mean Time to Recovery (MTTR)**: < 1 hour for critical issues
- **Security Vulnerabilities**: Zero critical, < 5 medium

### Business KPIs

- **User Acquisition**: 100 beta users (Month 6), 5,000 users (Year 1)
- **Match Acceptance Rate**: 30%+
- **Date Conversion Rate**: 70%+
- **Freemium Conversion**: 5%+ (paid subscribers)
- **Revenue**: €25K ARR (Year 1)
- **Net Promoter Score (NPS)**: 40+ (good for early-stage product)

---

## Non-Goals (Out of Scope for MVP)

To maintain focus, the following are explicitly **out of scope** for the MVP:

- [ ] ~~Video calling within the app~~ (future enhancement)
- [ ] ~~In-app messaging~~ (dates happen off-platform)
- [ ] ~~User-initiated search/browsing~~ (curated matches only)
- [ ] ~~Geolocation-based matching~~ (future, privacy-sensitive)
- [ ] ~~Venue management system~~ (MVP uses venue *types* only)
- [ ] ~~Mobile apps~~ (MVP is web-only, mobile in Phase 5)
- [ ] ~~Multi-language support~~ (MVP is English-only)
- [ ] ~~Social media integrations~~ (besides login)
- [ ] ~~Friend/referral features~~ (future growth feature)
- [ ] ~~Events and group dates~~ (future community feature)

---

## Alignment with Product Vision

These goals directly support the [Product Vision](00_product-vision.md):

- **Privacy-First**: GDPR compliance, encryption, data minimization
- **Values-Based Matching**: Rich domain model for personality, values, lifestyle
- **Automated Scheduling**: SchedulingService with availability matching
- **Blind Dating**: Match presentation without photos, consent-based reveal
- **Modern Tech Stack**: Microservices, cloud-native, mobile-ready

---

**Next Steps**:
- Review [Roadmap & Feature Index](02_roadmap-feature-index.md) for detailed feature breakdown
- Explore [Architecture Overview](../10_architecture/01_architecture-overview.md) to understand system design
- Read [Architecture Guidelines](../10_architecture/02_architecture-guidelines.md) for implementation rules

---

**Last Updated**: 2025-11-20
**Document Owner**: Engineering Team
**Status**: Living Document (updated monthly)
