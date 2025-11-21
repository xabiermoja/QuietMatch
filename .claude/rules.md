# QuietMatch Project Rules

## 🎯 Project Context
**QuietMatch** is a privacy-focused, GDPR-compliant dating platform for the EU market that emphasizes values-based matching over appearance-based swiping. This is a **learning-focused project** designed to master modern .NET microservices architecture.

---

## 📚 **BEFORE STARTING ANY TASK - READ THESE DOCUMENTS**

### Essential Pre-Work Reading
1. **Start Here**: [`docs/START-HERE.md`](../docs/START-HERE.md) - Master navigation and project overview
2. **Architecture Guidelines**: [`docs/10_architecture/02_architecture-guidelines.md`](../docs/10_architecture/02_architecture-guidelines.md) - The authoritative architectural rulebook
3. **Ubiquitous Language**: [`docs/20_domain/01_domain-ubiquitous-language.md`](../docs/20_domain/01_domain-ubiquitous-language.md) - Domain terminology and naming conventions
4. **Product Vision**: [`docs/00_overview/00_product-vision.md`](../docs/00_overview/00_product-vision.md) - Why we're building this

### Task-Specific Reading
- **Implementing a microservice**: Read the service's `PATTERNS.md` file (explains architecture style, why, and how)
- **Working with distributed transactions**: [`docs/20_domain/04_saga-processes.md`](../docs/20_domain/04_saga-processes.md)
- **Adding authentication/authorization**: [`docs/10_architecture/05_security-and-auth.md`](../docs/10_architecture/05_security-and-auth.md)
- **Adding messaging/events**: [`docs/10_architecture/06_messaging-and-integration.md`](../docs/10_architecture/06_messaging-and-integration.md)
- **Implementing a feature**: Find or create a feature file in `docs/40_features/`
- **Local development setup**: [`docs/60_operations/local-dev-setup.md`](../docs/60_operations/local-dev-setup.md)
- **Azure deployment**: [`docs/10_architecture/07_deployment-and-devops.md`](../docs/10_architecture/07_deployment-and-devops.md)

---

## 🏗️ **ARCHITECTURE ENFORCEMENT**

### Microservices and Their Assigned Architecture Patterns

Each microservice **MUST** follow its assigned architecture pattern. This is non-negotiable for learning purposes.

| Microservice | Architecture Pattern | Why This Pattern | Documentation |
|--------------|---------------------|------------------|---------------|
| **IdentityService** | **Layered** | Simple CRUD for tokens, straightforward layers make auth clear | `src/Services/Identity/PATTERNS.md` |
| **ProfileService** | **Onion** | Rich domain logic (privacy rules, validation), domain-centric design | `src/Services/Profile/PATTERNS.md` |
| **MatchingService** | **Hexagonal (Ports & Adapters)** | Multiple matching engines (rule-based, embedding-based), easily swappable | `src/Services/Matching/PATTERNS.md` |
| **SchedulingService** | **Layered + CQRS** | Clear separation of availability writes vs. slot queries | `src/Services/Scheduling/PATTERNS.md` |
| **NotificationService** | **Hexagonal** | Multiple notification channels (email, push, SignalR), adapter pattern | `src/Services/Notification/PATTERNS.md` |
| **VerificationService** | **Hexagonal** | Multiple verification providers (Twilio, Veriff), ports for each | `src/Services/Verification/PATTERNS.md` |
| **PaymentService** | **Hexagonal** | Stripe adapter, future support for other payment providers | `src/Services/Payment/PATTERNS.md` |
| **RealTimeService** | **Layered** | SignalR hub with Redis backplane, simple message routing | `src/Services/RealTime/PATTERNS.md` |
| **GraphQLGateway** | **Layered** | Query aggregation, resolver layers map to service calls | `src/Gateway/GraphQL/PATTERNS.md` |

### Pattern Quick Reference

- **Layered Architecture**: Presentation → Application → Domain → Infrastructure (top-down dependencies)
- **Onion Architecture**: Domain core → Application → Infrastructure (dependencies point inward)
- **Hexagonal Architecture**: Domain core + Ports (interfaces) + Adapters (implementations), dependency inversion everywhere

For detailed folder structures and implementation examples, see `docs/10_architecture/03_service-templates.md`.

---

## 🔐 **SECURITY & AUTHENTICATION RULES**

### Non-Negotiable Security Requirements

1. **JWT Everywhere**: All APIs (REST, GraphQL, gRPC) MUST validate JWT tokens
2. **Social Login Only**: No passwords stored locally - only Google, Apple Sign-In, etc.
3. **Custom IdentityService**: We build our own OAuth2/OIDC provider (not Duende IdentityServer)
4. **Token Types**:
   - **User Tokens**: Access token (15 min) + Refresh token (7 days)
   - **Service-to-Service (M2M)**: Client credentials flow → short-lived access tokens (5 min)
5. **Field-Level Encryption**: Sensitive profile data encrypted at rest (AES-256)
6. **GDPR by Design**: Every data field must have a retention policy and deletion workflow

See: `docs/10_architecture/05_security-and-auth.md`

---

## 📨 **MESSAGING & INTEGRATION RULES**

### Abstraction Layer (Required)

All messaging MUST go through these abstractions (never call RabbitMQ or Azure Service Bus directly):

```csharp
IMessagePublisher  // Publish events/commands
IMessageConsumer   // Subscribe to events/commands
IOutbox            // Outbox pattern for transactional messaging
```

### Technology Progression

- **Local Development**: RabbitMQ (via Docker Compose)
- **Azure Cloud**: Azure Service Bus
- **Implementation**: Use MassTransit (supports both RabbitMQ and Azure Service Bus)

### Event Naming Convention

- **Domain Events**: `{Aggregate}{Action}` (e.g., `MatchAccepted`, `ProfileUpdated`)
- **Commands**: `{Verb}{Aggregate}` (e.g., `CreateBlindDate`, `SendNotification`)
- **Integration Events**: `{Service}.{Event}` (e.g., `Matching.MatchAccepted`)

See: `docs/10_architecture/06_messaging-and-integration.md`

---

## 🔄 **SAGA PATTERN RULES**

### When to Use SAGAs

Use SAGAs for multi-step workflows across microservices where atomicity is required:

- **Blind Date Creation**: Match acceptance → Availability reservation → Notifications → Date confirmation
- **User Deletion**: Profile → Matching → Scheduling → Notifications → Payments → Verification
- **Subscription Activation**: Payment success → Profile feature unlock → Notification

### SAGA Implementation

- **Pattern**: Orchestration-based (centralized coordinator)
- **Technology**: MassTransit SAGA state machines
- **Storage**: SAGA state table in the orchestrating service's database
- **Compensations**: Every step must have a compensating action for rollback

See: `docs/20_domain/04_saga-processes.md`

---

## 🗂️ **CQRS PATTERN RULES**

### Services Using CQRS

- **SchedulingService**: Write availability slots vs. query available times
- **MatchingService**: Generate matches (write) vs. fetch suggestions (read)
- **ProfileService** (future): Profile updates (write) vs. profile search (read, possibly with separate read models)

### Technology

- **Command/Query Handlers**: MediatR
- **Validation**: FluentValidation on commands
- **Read Models**: Materialized views or separate read-optimized tables
- **Caching**: Redis for frequently accessed read models

---

## 🎨 **NAMING & UBIQUITOUS LANGUAGE**

### Core Domain Concepts (Use These Names Consistently)

- **Member**: A registered user of QuietMatch
- **Profile**: Member's personality, values, preferences, lifestyle
- **Match**: Two members who have been algorithmically paired
- **BlindDate**: A scheduled meeting between matched members
- **AvailabilitySlot**: Time windows when a member is free
- **ExposureLevel**: Privacy setting controlling what data is shared
- **ConsentToken**: GDPR consent record for specific data processing
- **DealBreaker**: Non-negotiable preference (e.g., "must want children")

**Rule**: Use domain language in code, variables, classes, database tables, API endpoints, and UI labels.

See: `docs/20_domain/01_domain-ubiquitous-language.md`

---

## 🧪 **TESTING REQUIREMENTS**

### Every Microservice MUST Have

1. **Unit Tests**: Domain logic, application services (use xUnit)
2. **Integration Tests**: Database interactions, message publishing (use Testcontainers for real Postgres/RabbitMQ)
3. **API Tests**: Controller/endpoint tests (WebApplicationFactory)

### Test Naming Convention

```csharp
{MethodUnderTest}_{Scenario}_{ExpectedBehavior}

// Example:
CreateBlindDate_WhenBothMembersAvailable_ShouldReserveSlotAndPublishEvent()
```

---

## 📝 **CODE DOCUMENTATION RULES**

### When to Add Comments

1. **Architectural Decisions**: Why a pattern was chosen
   ```csharp
   // Hexagonal Architecture: This port allows swapping matching engines
   // Current: RuleBasedEngine, Future: EmbeddingBasedEngine
   public interface IMatchingEngine { ... }
   ```

2. **Complex Business Logic**: Explain the "why" behind algorithms
   ```csharp
   // Calculate compatibility score using weighted factors:
   // - Values alignment: 40%
   // - Lifestyle compatibility: 30%
   // - Communication style: 20%
   // - Deal-breaker compliance: 10%
   ```

3. **GDPR/Privacy Decisions**: Document compliance reasoning
   ```csharp
   // GDPR Article 17: Right to erasure
   // Soft-delete profile but retain anonymized match data for 30 days
   // for fraud detection, then hard-delete via background job
   ```

4. **Technology Choices**: Why a library or approach was selected
   ```csharp
   // Using MassTransit for abstraction over RabbitMQ/Azure Service Bus
   // Enables local dev with RabbitMQ, production with Azure Service Bus
   // without changing application code
   ```

### What NOT to Comment

- Self-explanatory code (let good naming speak)
- Obvious CRUD operations
- Auto-generated code

---

## 🐳 **LOCAL DEVELOPMENT RULES**

### Environment Requirements

- **.NET 8 SDK** (LTS, stable)
- **Docker Desktop** (for infrastructure)
- **PostgreSQL** (via Docker)
- **RabbitMQ** (via Docker)
- **Redis** (via Docker)

### Docker Compose First

All infrastructure dependencies MUST be runnable via `docker-compose up`:
- PostgreSQL (multiple databases for each service)
- RabbitMQ (with management UI)
- Redis (for caching and SignalR backplane)
- Seq (structured logging)

Each microservice SHOULD have its own `Dockerfile` and run in Docker Compose.

See: `docs/60_operations/local-dev-setup.md`

---

## ☁️ **AZURE CLOUD RULES**

### Target Azure Services

| Local Development | Azure Cloud Equivalent |
|-------------------|------------------------|
| Docker Compose | Azure Container Apps (ACA) or AKS |
| RabbitMQ | Azure Service Bus |
| PostgreSQL (Docker) | Azure Database for PostgreSQL - Flexible Server |
| Redis (Docker) | Azure Cache for Redis |
| Seq | Azure Application Insights |
| Local JWT secret | Azure Key Vault |

### Design Principles

- **Cloud-Agnostic Abstractions**: Use interfaces for all external dependencies
- **12-Factor App**: Configuration via environment variables
- **Infrastructure as Code**: Bicep templates for Azure resources
- **Managed Identities**: Use Azure Managed Identity for service-to-service auth (not secrets)

See: `docs/10_architecture/08_cloud-provider-notes.md`

---

## 🚀 **DEPLOYMENT RULES**

### Blue-Green Deployment

- **Local**: Docker Compose profiles (blue/green)
- **Azure**: ACA revisions or Kubernetes deployments

### Database Migrations

- **Strategy**: Expand/Contract pattern for backward compatibility
- **Tool**: Entity Framework Core Migrations
- **Execution**: Applied via deployment pipeline (not on startup in production)

See: `docs/10_architecture/07_deployment-and-devops.md`

---

## 📋 **FEATURE DEVELOPMENT WORKFLOW**

### Implementing a New Feature

1. **Create/Update Feature File**: `docs/40_features/f000X_feature_name.md`
2. **Design Domain Model**: Update `docs/20_domain/01_domain-ubiquitous-language.md` if new concepts
3. **Choose Architecture Pattern**: Follow assigned microservice pattern
4. **Implement**:
   - Write domain models first
   - Add application services
   - Add infrastructure (DB, messaging)
   - Add API endpoints
   - Write tests
5. **Update Documentation**:
   - Update microservice README
   - Update API documentation
   - Update SAGA docs if workflow spans services

---

## 🆕 **ADDING A NEW MICROSERVICE**

### Checklist

- [ ] Decide on architecture pattern (Layered/Onion/Hexagonal)
- [ ] Create folder structure per pattern template (`docs/10_architecture/03_service-templates.md`)
- [ ] Create `README.md` (overview, endpoints, how to run)
- [ ] Create `PATTERNS.md` (explain pattern choice, why, how, alternatives)
- [ ] Create dedicated PostgreSQL database
- [ ] Add to `docker-compose.yml`
- [ ] Implement health check endpoint (`/health`)
- [ ] Add JWT authentication middleware
- [ ] Add structured logging (Serilog)
- [ ] Add OpenTelemetry tracing
- [ ] Register with API Gateway or GraphQL Gateway
- [ ] Add integration tests
- [ ] Document in `docs/30_microservices/{service-name}.md`

---

## ✅ **CODE REVIEW CHECKLIST**

Before committing code, verify:

- [ ] Follows assigned architecture pattern
- [ ] Uses ubiquitous language from domain model
- [ ] JWT authentication on all endpoints
- [ ] Uses `IMessagePublisher`/`IMessageConsumer` (not direct RabbitMQ calls)
- [ ] Includes meaningful comments for architectural decisions
- [ ] Has unit tests for business logic
- [ ] Has integration tests for external dependencies
- [ ] Database migrations are backward-compatible
- [ ] GDPR considerations addressed (data retention, encryption)
- [ ] Configuration externalized (no hardcoded secrets)
- [ ] Logging includes correlation IDs for tracing
- [ ] Feature documentation updated

---

## 🎓 **LEARNING GOALS REMINDER**

This project is designed to teach:

1. **4 Architecture Patterns**: Layered, Onion, Hexagonal, CQRS
2. **Distributed Transactions**: SAGA pattern (orchestration-based)
3. **Event-Driven Architecture**: Async messaging, event sourcing
4. **Microservices Communication**: REST, gRPC, GraphQL
5. **Cloud Migration**: Local-first → Azure
6. **Security**: OAuth2, JWT, field-level encryption
7. **GDPR Compliance**: Privacy by design, user rights
8. **DevOps**: Docker, Blue-Green deployment, IaC

**Every decision should be made with learning in mind** - prefer patterns that teach concepts, not just shortcuts.

---

## 🤝 **COLLABORATION RULES**

### For AI Assistants (Claude Code)

- **Always ask for clarification** on architectural decisions before implementing
- **Propose alternatives** with trade-offs when multiple approaches exist
- **Explain your reasoning** when choosing patterns or technologies
- **Reference documentation** when making decisions
- **Update documentation** as part of implementation
- **Use the TODO list** to track multi-step tasks

### For Human Developers

- **Read the docs first**, especially `START-HERE.md` and architecture guidelines
- **Follow the patterns** - this is a learning project, resist shortcuts
- **Update docs with your learnings** - if you learn something non-obvious, document it
- **Question the architecture** - propose improvements via Architecture Decision Records (ADRs)

---

## 📖 **DOCUMENTATION MAINTENANCE**

### Keeping Docs Fresh

- **Feature docs**: Update as features are implemented
- **API docs**: Auto-generate from code where possible (Swagger/Scalar)
- **Architecture docs**: Update when patterns evolve
- **Decision records**: Add ADR for any significant architectural decision

### Documentation Review

Before merging code:
- [ ] Feature file updated (if applicable)
- [ ] Microservice README updated (if endpoints changed)
- [ ] SAGA docs updated (if workflow changed)
- [ ] Ubiquitous language updated (if new domain concepts)

---

## 🔗 **QUICK LINKS**

- **Master Index**: [`docs/START-HERE.md`](../docs/START-HERE.md)
- **Architecture Guidelines**: [`docs/10_architecture/02_architecture-guidelines.md`](../docs/10_architecture/02_architecture-guidelines.md)
- **Domain Model**: [`docs/20_domain/01_domain-ubiquitous-language.md`](../docs/20_domain/01_domain-ubiquitous-language.md)
- **Local Dev Setup**: [`docs/60_operations/local-dev-setup.md`](../docs/60_operations/local-dev-setup.md)
- **Glossary**: [`docs/50_references/glossary.md`](../docs/50_references/glossary.md)

---

**Remember: This is a learning journey. Take time to understand each pattern, discuss alternatives, and document your decisions.**
