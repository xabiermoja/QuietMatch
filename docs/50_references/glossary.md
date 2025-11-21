# Glossary - QuietMatch

> **Quick reference for all QuietMatch terminology**
>
> For detailed definitions, see [Domain Ubiquitous Language](../20_domain/01_domain-ubiquitous-language.md)

---

## A

**Access Token**
Short-lived JWT token (15 minutes) used to authenticate API requests. Contains claims (user ID, email, roles).

**Aggregate Root**
In DDD, an entity that serves as the entry point to an aggregate. Controls access to child entities. Examples: Match, BlindDate, MemberProfile.

**Architecture Pattern**
Design approach for structuring code. QuietMatch uses: Layered, Onion, Hexagonal (Ports & Adapters), and CQRS.

**Availability Slot**
Time window when a Member is available for a BlindDate.

**Azure Container Apps (ACA)**
Serverless container platform used for deploying QuietMatch microservices in Azure.

---

## B

**Blind Date**
Scheduled in-person meeting between two matched Members. "Blind" refers to optional privacy (photos/names may not be revealed until date or consent).

**Blue-Green Deployment**
Deployment strategy with two identical environments (blue=current, green=new). Traffic switches instantly from blue to green for zero-downtime deployments.

**Bounded Context**
DDD concept: clear boundary within which a domain model is defined. Each microservice represents one bounded context.

---

## C

**Client Credentials Flow**
OAuth2 flow for service-to-service authentication (M2M). Services exchange clientId/clientSecret for access token.

**Compatibility Score**
Quantitative measure (0.0-1.0) of how well two Members' profiles align. Broken down by values, lifestyle, communication style, interests.

**Compensating Transaction**
Action that reverses the effect of a previous transaction. Used in SAGAs to handle failures (e.g., release reserved availability slot if date creation fails).

**Consent Token**
Record of a Member's consent for specific data processing activities. Required by GDPR.

**CQRS**
Command Query Responsibility Segregation. Pattern separating read operations (queries) from write operations (commands).

---

## D

**Database per Service**
Microservices pattern: Each service owns its database schema. No shared databases across services.

**Date Proposal**
System-generated suggestion for BlindDate time and venue, sent to both Members for confirmation.

**Deal Breaker**
Non-negotiable requirement or incompatibility. Potential matches violating a DealBreaker are filtered out automatically.

**Domain Event**
Something that happened in the domain (past tense). Examples: MatchAccepted, ProfileUpdated, BlindDateScheduled.

**Domain-Driven Design (DDD)**
Software development approach focused on modeling business domain. Uses ubiquitous language, bounded contexts, aggregates.

---

## E

**Entity Framework Core (EF Core)**
ORM (Object-Relational Mapping) library for .NET. Used for database access in all QuietMatch microservices.

**Embedding**
Vector representation of text (personality responses) used for semantic similarity search. Future enhancement for AI-powered matching.

**Event-Driven Architecture**
Design pattern where services communicate via events (asynchronous messaging) instead of direct calls.

**Exposure Level**
Privacy setting controlling how much of a Member's profile data is shared. Levels: MatchedOnly, AllMatches, Public.

---

## F

**Feature-Driven Development**
Development approach organizing work by features. Each feature has a spec file with acceptance criteria, diagrams, API contracts.

**Freemium**
Business model with free tier (limited features) and paid premium tiers.

---

## G

**GDPR**
General Data Protection Regulation. EU privacy law. QuietMatch implements all user rights: access, rectification, erasure, portability, etc.

**GraphQL Gateway**
API aggregation layer. Provides unified query interface for clients, fetching data from multiple microservices.

**gRPC**
High-performance RPC framework using protocol buffers. Used for internal service-to-service communication in QuietMatch.

---

## H

**Health Check**
Endpoint (`/health`) that reports service status. Used by orchestrators (Docker, Kubernetes) to monitor service health.

**Hexagonal Architecture**
Architecture pattern (Ports & Adapters). Domain core surrounded by ports (interfaces) and adapters (implementations). Used by: MatchingService, NotificationService, PaymentService, VerificationService.

---

## I

**Idempotency**
Property where performing an operation multiple times has the same effect as performing it once. Critical for message handling (events may be delivered more than once).

**Identity Provider**
Service handling authentication. QuietMatch uses custom IdentityService (not Duende IdentityServer).

---

## J

**JWT (JSON Web Token)**
Token format used for authentication. Contains claims (user ID, email, roles). All QuietMatch APIs validate JWTs.

---

## L

**Layered Architecture**
Traditional N-tier architecture: Presentation → Application → Domain → Infrastructure. Used by: IdentityService, SchedulingService, RealTimeService, GraphQLGateway.

**Lifestyle**
Domain concept: Daily habits, routines, preferences (social frequency, exercise, diet, pets, smoking, drinking).

---

## M

**M2M (Machine-to-Machine)**
Service-to-service communication. Uses client credentials flow for authentication.

**Match**
Pairing of two Members determined by matching algorithm to have high compatibility. Members can accept or decline.

**Match Candidate**
Potential Match identified by algorithm but not yet formally created as Match entity.

**Match Suggestion**
Curated Match presented to Member with compatibility breakdown and conversation starters.

**Matching Engine**
Component responsible for finding compatible Members. Current: RuleBasedMatchingEngine. Future: EmbeddingBasedMatchingEngine.

**MassTransit**
.NET library abstracting message brokers (RabbitMQ, Azure Service Bus). Provides SAGA support.

**Member**
Registered user with completed profile. Use "Member" in domain contexts (not "User", which is IdentityService only).

**Microservices**
Architectural style: Application composed of small, independent services communicating via APIs/events.

**Minimal API**
ASP.NET Core feature for defining endpoints without controllers. Used where appropriate in QuietMatch.

---

## N

**Notification**
Message sent to Member via email, push, or in-app. Types: MatchNotification, BlindDateNotification, SystemNotification.

---

## O

**OAuth 2.0**
Authorization framework. QuietMatch uses OAuth for social login (Google, Apple).

**OIDC (OpenID Connect)**
Identity layer on top of OAuth 2.0. Provides user authentication.

**Onion Architecture**
Domain-centric architecture with dependencies pointing inward. Core has zero dependencies. Used by: ProfileService.

**OpenTelemetry**
Observability framework for distributed tracing, metrics, and logs.

**Orchestration SAGA**
SAGA pattern with centralized coordinator (state machine). Chosen over choreography for simplicity. Used for: BlindDateCreationSAGA, UserDeletionSAGA.

**Outbox Pattern**
Pattern ensuring transactional messaging: DB write and event publishing happen atomically via outbox table.

---

## P

**pgvector**
PostgreSQL extension for vector similarity search. Used for embedding-based matching (future).

**Ports & Adapters**
See Hexagonal Architecture.

**Preference Set**
Member's preferences for ideal match (age range, location, languages, gender).

**Profile**
Comprehensive representation of Member's personality, values, preferences, lifestyle, privacy settings.

---

## R

**RabbitMQ**
Message broker used for local development. Replaced by Azure Service Bus in production.

**Refresh Token**
Long-lived token (7 days) used to obtain new access tokens without re-authentication.

**Repository Pattern**
Data access pattern abstracting database operations behind interfaces (IUserRepository, IMatchRepository).

---

## S

**SAGA**
Pattern for distributed transactions across microservices. Manages multi-step workflows with compensating transactions.

**Serilog**
Structured logging library for .NET. Used in all QuietMatch services.

**Service Bus**
Azure Service Bus. Managed message broker used in Azure production environment.

**SignalR**
Real-time communication library for .NET. Used for push notifications (with Redis backplane for scalability).

**Social Login**
Authentication via third-party providers (Google, Apple). QuietMatch stores no passwords locally.

**Soft Delete**
Marking records as deleted (DeletedAt timestamp) without physically removing them. Used for GDPR retention period.

**Strongly-Typed ID**
Value object wrapping primitive ID (Guid) with type safety. Examples: MemberId, MatchId, BlindDateId.

**Subscription**
Member's payment plan: Free, Premium (€9.99/month), Premium Plus (€19.99/month).

---

## T

**Testcontainers**
Library for running real dependencies (PostgreSQL, RabbitMQ, Redis) in Docker containers during integration tests.

**Twilio Verify**
Service for phone number verification via SMS. Used by VerificationService.

---

## U

**Ubiquitous Language**
DDD concept: Shared vocabulary used consistently across code, docs, UI, and communication. See [Domain Ubiquitous Language](../20_domain/01_domain-ubiquitous-language.md).

**User**
In IdentityService context only: Authenticated account. Once profile is created, becomes a "Member" in domain contexts.

---

## V

**Value Object**
DDD concept: Immutable object defined by its attributes, not identity. Examples: Email, CompatibilityScore, AvailabilitySlot.

**Values**
Core values guiding Member's life decisions (family, career, spirituality, adventure, etc.). Critical for compatibility matching.

**Venue Type**
Category of BlindDate location (CoffeeShop, Park, Restaurant, ActivityCenter). Not a specific venue.

**Verification Badge**
Visual indicator showing Member has verified phone or ID. Increases trust and match priority.

---

## Acronyms

**ACA** - Azure Container Apps
**ADR** - Architecture Decision Record
**AKS** - Azure Kubernetes Service
**API** - Application Programming Interface
**CQRS** - Command Query Responsibility Segregation
**DDD** - Domain-Driven Design
**DTO** - Data Transfer Object
**EF Core** - Entity Framework Core
**GDPR** - General Data Protection Regulation
**gRPC** - Google Remote Procedure Call
**JWT** - JSON Web Token
**M2M** - Machine-to-Machine
**MFA** - Multi-Factor Authentication (future)
**MVC** - Model-View-Controller
**OAuth** - Open Authorization
**OIDC** - OpenID Connect
**ORM** - Object-Relational Mapping
**PII** - Personally Identifiable Information
**RBAC** - Role-Based Access Control
**REST** - Representational State Transfer
**SAGA** - (not an acronym, from distributed transactions literature)
**SDK** - Software Development Kit
**SSR** - Server-Side Rendering (Blazor)
**TLS** - Transport Layer Security
**UUID** - Universally Unique Identifier (same as GUID)

---

**Need more detail?** See [Domain Ubiquitous Language](../20_domain/01_domain-ubiquitous-language.md) for comprehensive definitions with code examples.

---

**Last Updated**: 2025-11-20
**Maintainer**: Documentation Team
