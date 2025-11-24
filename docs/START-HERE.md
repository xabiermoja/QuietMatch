# 🎯 QuietMatch Documentation - START HERE

> **Welcome to QuietMatch!** This document is your gateway to understanding and contributing to a privacy-focused, GDPR-compliant dating platform built with modern .NET microservices architecture.

---

## 🚀 **New to QuietMatch? Read This First**

### What is QuietMatch?

QuietMatch is a **values-based dating platform** for the EU market that prioritizes:
- **Intentional connections** over endless swiping
- **Privacy and GDPR compliance** by design
- **Blind matching** based on personality, values, and lifestyle
- **Automated scheduling** to simplify date planning

Unlike traditional dating apps, QuietMatch doesn't emphasize photos or superficial criteria. Instead, it uses personality compatibility and shared values to create meaningful matches.

### What is This Project?

This is also a **comprehensive learning platform** designed to master:
- **4 distinct architecture patterns**: Layered, Onion, Hexagonal (Ports & Adapters), and CQRS
- **Microservices patterns**: SAGA, Event-Driven Architecture, API Gateway
- **Modern .NET practices**: .NET 8, ASP.NET Core, Entity Framework Core, gRPC, GraphQL
- **Cloud deployment**: Local-first development with Docker, Azure cloud migration
- **Security best practices**: OAuth2, JWT, field-level encryption
- **GDPR compliance**: Privacy by design, user rights implementation

Each microservice intentionally uses a **different architecture pattern** to provide hands-on learning.

---

## 📚 **Documentation Structure**

Our documentation is organized into 7 main sections:

```
docs/
├── 00_overview/           # Project vision, goals, and roadmap
├── 10_architecture/       # Architecture patterns, guidelines, and templates
├── 20_domain/             # Domain model, ubiquitous language, and SAGA processes
├── 30_microservices/      # Individual microservice documentation
├── 40_features/           # Feature-by-feature implementation guides
├── 50_references/         # Glossary, decision records, patterns library
└── 60_operations/         # Local dev setup, deployment, monitoring
```

---

## 🧭 **Quick Navigation - "I Want To..."**

### **Understand the Project**

| Task | Document |
|------|----------|
| Understand the product vision and business goals | [`00_overview/00_product-vision.md`](00_overview/00_product-vision.md) |
| See the full feature roadmap | [`00_overview/02_roadmap-feature-index.md`](00_overview/02_roadmap-feature-index.md) |
| Learn about the domain model and terminology | [`20_domain/01_domain-ubiquitous-language.md`](20_domain/01_domain-ubiquitous-language.md) |
| Look up a specific term or acronym | [`50_references/glossary.md`](50_references/glossary.md) |

### **Learn the Architecture**

| Task | Document |
|------|----------|
| Get a high-level overview of the system | [`10_architecture/01_architecture-overview.md`](10_architecture/01_architecture-overview.md) |
| Understand architectural rules and principles | [`10_architecture/02_architecture-guidelines.md`](10_architecture/02_architecture-guidelines.md) |
| See folder structure templates for each pattern | [`10_architecture/03_service-templates.md`](10_architecture/03_service-templates.md) |
| Learn about security and authentication | [`10_architecture/05_security-and-auth.md`](10_architecture/05_security-and-auth.md) |
| Understand messaging and event-driven patterns | [`10_architecture/06_messaging-and-integration.md`](10_architecture/06_messaging-and-integration.md) |
| See SAGA processes and distributed transactions | [`20_domain/04_saga-processes.md`](20_domain/04_saga-processes.md) |

### **Work on a Microservice**

| Task | Document |
|------|----------|
| Understand which pattern each service uses | [`.claude/rules.md`](../.claude/rules.md) (Architecture Enforcement table) |
| Learn about the IdentityService (Layered) | [`30_microservices/identity-service.md`](30_microservices/identity-service.md) |
| Learn about the ProfileService (Onion) | [`30_microservices/profile-service.md`](30_microservices/profile-service.md) |
| Learn about the MatchingService (Hexagonal) | [`30_microservices/matching-service.md`](30_microservices/matching-service.md) |
| Add a new microservice | [`10_architecture/02_architecture-guidelines.md`](10_architecture/02_architecture-guidelines.md) (New Service Checklist) |
| Track future work and technical debt | [`/TODO.md`](../TODO.md) - Project TODO list |
| Learn TODO management process | [`60_operations/todo-management.md`](60_operations/todo-management.md) |

> **Pro Tip**: Each microservice has its own `PATTERNS.md` file in its source folder explaining the architecture pattern in detail, with alternatives and trade-offs.

### **Implement a Feature**

| Task | Document |
|------|----------|
| See all planned features | [`00_overview/02_roadmap-feature-index.md`](00_overview/02_roadmap-feature-index.md) |
| Implement "Sign in with Google" | [`40_features/f0001_sign_in_with_google.md`](40_features/f0001_sign_in_with_google.md) |
| Understand feature documentation format | Any file in `40_features/` (they all follow the same template) |

### **Set Up Development Environment**

| Task | Document |
|------|----------|
| Set up local development environment | [`60_operations/local-dev-setup.md`](60_operations/local-dev-setup.md) |
| Understand Docker Compose setup | [`60_operations/docker-compose-reference.md`](60_operations/docker-compose-reference.md) |
| Troubleshoot common issues | [`60_operations/troubleshooting.md`](60_operations/troubleshooting.md) |

### **Deploy to Azure**

| Task | Document |
|------|----------|
| Understand deployment strategies | [`10_architecture/07_deployment-and-devops.md`](10_architecture/07_deployment-and-devops.md) |
| Learn about Azure service mapping | [`10_architecture/08_cloud-provider-notes.md`](10_architecture/08_cloud-provider-notes.md) |

---

## 🎓 **Learning Paths**

### **Path 1: Architecture Patterns (Beginner)**

If you're new to architecture patterns, follow this sequence:

1. Read [`10_architecture/01_architecture-overview.md`](10_architecture/01_architecture-overview.md) - Get the big picture
2. Read [`10_architecture/02_architecture-guidelines.md`](10_architecture/02_architecture-guidelines.md) - Understand the rules
3. Study **IdentityService** (Layered) - Start with the simplest pattern
   - Read [`30_microservices/identity-service.md`](30_microservices/identity-service.md)
   - Read `src/Services/Identity/PATTERNS.md` (when implemented)
4. Study **ProfileService** (Onion) - Move to domain-centric design
   - Read [`30_microservices/profile-service.md`](30_microservices/profile-service.md)
   - Read `src/Services/Profile/PATTERNS.md` (when implemented)
5. Study **MatchingService** (Hexagonal) - Learn dependency inversion
   - Read [`30_microservices/matching-service.md`](30_microservices/matching-service.md)
   - Read `src/Services/Matching/PATTERNS.md` (when implemented)
6. Study **SchedulingService** (CQRS) - Separate reads and writes
   - Read [`30_microservices/scheduling-service.md`](30_microservices/scheduling-service.md)
   - Read `src/Services/Scheduling/PATTERNS.md` (when implemented)

### **Path 2: Microservices Communication (Intermediate)**

Once you understand the patterns, learn how services communicate:

1. Read [`10_architecture/06_messaging-and-integration.md`](10_architecture/06_messaging-and-integration.md) - Async messaging
2. Read [`20_domain/04_saga-processes.md`](20_domain/04_saga-processes.md) - Distributed transactions
3. Read [`30_microservices/grpc-apis.md`](30_microservices/grpc-apis.md) - Synchronous internal APIs
4. Read [`30_microservices/graphql-gateway.md`](30_microservices/graphql-gateway.md) - Client-facing API aggregation
5. Implement the **Blind Date Creation SAGA** - Hands-on distributed workflow

### **Path 3: Cloud Deployment (Advanced)**

Ready to deploy? Follow this path:

1. Complete [`60_operations/local-dev-setup.md`](60_operations/local-dev-setup.md) - Ensure everything runs locally
2. Read [`10_architecture/07_deployment-and-devops.md`](10_architecture/07_deployment-and-devops.md) - Deployment strategies
3. Read [`10_architecture/08_cloud-provider-notes.md`](10_architecture/08_cloud-provider-notes.md) - Azure specifics
4. Implement **Blue-Green deployment** locally with Docker Compose
5. Migrate one microservice to Azure Container Apps
6. Set up CI/CD pipeline

---

## 🛠️ **Technology Stack**

### Core Technologies

- **Backend**: .NET 8 (LTS), C# 12, ASP.NET Core
- **Data**: PostgreSQL 16, Entity Framework Core
- **Messaging**: RabbitMQ (local) → Azure Service Bus (cloud)
- **Caching**: Redis (distributed cache, SignalR backplane)
- **Real-time**: SignalR with Redis backplane
- **API Protocols**: REST, gRPC, GraphQL

### Frontend (Future)

- **Web**: Blazor Web App (.NET 8) with SSR + interactive components
- **Mobile**: .NET MAUI Blazor Hybrid (iOS, Android)

### Infrastructure

- **Local**: Docker, Docker Compose
- **Cloud**: Azure (Container Apps, Service Bus, PostgreSQL Flexible Server, Cache for Redis)
- **CI/CD**: GitHub Actions or Azure DevOps
- **Monitoring**: Seq (local), Application Insights (Azure)

---

## 📋 **Core Principles & Patterns**

### Architecture Principles

1. **Domain-Driven Design (DDD)**: Ubiquitous language, bounded contexts, aggregates
2. **Cloud-Agnostic Abstractions**: Use interfaces for external dependencies
3. **Privacy by Design**: GDPR compliance from day one
4. **Testability**: Unit, integration, and API tests for every service
5. **Observability**: Structured logging, distributed tracing, health checks

### Communication Patterns

- **Synchronous**: REST (external), gRPC (internal high-performance)
- **Asynchronous**: Event-driven messaging (RabbitMQ/Azure Service Bus)
- **Query Aggregation**: GraphQL Gateway for client queries

### Data Patterns

- **Database per Service**: Each microservice owns its data
- **SAGA Pattern**: Orchestrated distributed transactions
- **CQRS**: Separate read/write models where appropriate
- **Outbox Pattern**: Transactional messaging

### Security Patterns

- **OAuth2 + OIDC**: Social login only (Google, Apple)
- **JWT Everywhere**: REST, gRPC, GraphQL all use JWT
- **Service-to-Service Auth**: Client credentials flow for M2M
- **Field-Level Encryption**: Sensitive data encrypted at rest

---

## 🎯 **Microservices Overview**

| Service | Architecture | Primary Responsibility | Tech Highlights |
|---------|--------------|------------------------|-----------------|
| **IdentityService** | Layered | Social login, JWT issuance | OAuth2, JWT, Refresh tokens |
| **ProfileService** | Onion | Member profiles, preferences, privacy | Encryption, GDPR, Rich domain logic |
| **MatchingService** | Hexagonal | Compatibility scoring, match generation | Rule engine, Future: Embeddings/ML |
| **SchedulingService** | Layered + CQRS | Availability, blind date scheduling | SAGA orchestration, Calendar integration |
| **NotificationService** | Hexagonal | Email, push, real-time notifications | Multi-channel adapters, Templates |
| **VerificationService** | Hexagonal | Phone & ID verification | Twilio Verify, Future: Veriff |
| **PaymentService** | Hexagonal | Subscriptions, Stripe integration | Webhooks, SAGA for activation |
| **RealTimeService** | Layered | SignalR hub for real-time updates | Redis backplane, Serverless-compatible |
| **GraphQLGateway** | Layered | Query aggregation for clients | HotChocolate, Schema stitching |

For detailed documentation on each service, see `docs/30_microservices/`.

---

## 🔐 **Security & Compliance**

### Authentication Flow

1. **User Login**: User clicks "Sign in with Google" → Redirects to Google
2. **Google Callback**: Google returns authorization code → IdentityService exchanges for Google user info
3. **Token Issuance**: IdentityService creates local user (if new) and issues:
   - **Access Token** (JWT, 15 min expiry)
   - **Refresh Token** (opaque, 7 days expiry)
4. **API Calls**: Client includes access token in `Authorization: Bearer {token}` header
5. **Token Refresh**: When access token expires, client uses refresh token to get a new access token

### GDPR Compliance

QuietMatch implements the following user rights:

- **Right to Access**: Users can download all their data (JSON export)
- **Right to Rectification**: Users can update their profile at any time
- **Right to Erasure**: "Delete Account" triggers a SAGA across all services
- **Right to Data Portability**: Export in machine-readable format
- **Right to Object**: Opt-out of automated matching (future: manual review)

See [`10_architecture/04_data-and-privacy.md`](10_architecture/04_data-and-privacy.md) for details.

---

## 🚦 **Getting Started Checklist**

### For First-Time Contributors

- [ ] Read this document (START-HERE.md)
- [ ] Read [`.claude/rules.md`](../.claude/rules.md) - Project rules and context
- [ ] Read [`00_overview/00_product-vision.md`](00_overview/00_product-vision.md) - Understand the business
- [ ] Read [`10_architecture/02_architecture-guidelines.md`](10_architecture/02_architecture-guidelines.md) - Architecture rules
- [ ] Read [`20_domain/01_domain-ubiquitous-language.md`](20_domain/01_domain-ubiquitous-language.md) - Learn the terminology
- [ ] Set up local dev environment: [`60_operations/local-dev-setup.md`](60_operations/local-dev-setup.md)
- [ ] Run `docker-compose up` and verify all infrastructure services start
- [ ] Choose a microservice to implement and read its `PATTERNS.md` file

### For Implementing Your First Feature

- [ ] Find or create the feature file in `docs/40_features/`
- [ ] Read the relevant microservice documentation
- [ ] Review the SAGA processes if your feature spans multiple services
- [ ] Implement following the assigned architecture pattern
- [ ] Write tests (unit, integration, API)
- [ ] Update documentation (feature file, microservice README, API docs)
- [ ] Submit PR with clear description of changes

---

## 📖 **Documentation Standards**

### Every Markdown File Should Have

1. **Title**: Clear, descriptive heading
2. **Table of Contents**: For files longer than 100 lines
3. **Purpose**: Why this document exists
4. **Cross-References**: Links to related documents
5. **Examples**: Code snippets or diagrams where helpful
6. **Last Updated**: Date stamp for maintenance

### Naming Conventions

- **Files**: `kebab-case-with-numbers.md` (e.g., `05_security-and-auth.md`)
- **Folders**: `snake_case` or `PascalCase` for code, `kebab-case` for docs
- **Features**: `f0001_feature_name.md` (padded 4-digit ID)

---

## 🤝 **Contributing**

### Before Making Changes

1. **Discuss architecture decisions** - Open an issue or discussion
2. **Follow the assigned pattern** - Each service has a fixed architecture
3. **Update documentation** - Code and docs should evolve together
4. **Write tests** - No PR without tests
5. **Use ubiquitous language** - Stick to domain terminology

### Proposing Architecture Changes

Use **Architecture Decision Records (ADRs)**:
- Create a file in `docs/50_references/decision-records/`
- Template: `ADR-###-title.md`
- Format: Context, Decision, Consequences, Alternatives Considered

---

## 🆘 **Need Help?**

### Common Questions

| Question | Answer |
|----------|--------|
| Which architecture pattern should I use? | See the table in [`.claude/rules.md`](../.claude/rules.md) - each service has a pre-assigned pattern |
| How do I add authentication to my endpoint? | See [`10_architecture/05_security-and-auth.md`](10_architecture/05_security-and-auth.md) |
| How do I publish an event? | See [`10_architecture/06_messaging-and-integration.md`](10_architecture/06_messaging-and-integration.md) |
| What's the domain term for X? | Check [`20_domain/01_domain-ubiquitous-language.md`](20_domain/01_domain-ubiquitous-language.md) or [`50_references/glossary.md`](50_references/glossary.md) |
| My Docker container won't start | See [`60_operations/troubleshooting.md`](60_operations/troubleshooting.md) |

### Where to Ask

- **Architecture questions**: Create an issue tagged `architecture`
- **Implementation questions**: Check the relevant `PATTERNS.md` file in the service folder
- **Bug reports**: Create an issue with reproducible steps
- **General questions**: Start a discussion

---

## 📚 **Further Reading**

### External Resources

- **Domain-Driven Design**: Eric Evans, "Domain-Driven Design: Tackling Complexity"
- **Microservices Patterns**: Chris Richardson, "Microservices Patterns"
- **Clean Architecture**: Robert C. Martin, "Clean Architecture"
- **GDPR Compliance**: [gdpr.eu](https://gdpr.eu)
- **.NET Architecture**: [Microsoft Architecture Guides](https://dotnet.microsoft.com/learn/dotnet/architecture-guides)

### Internal Deep Dives

- **Hexagonal Architecture**: `src/Services/Matching/PATTERNS.md` (when implemented)
- **SAGA Implementation**: `docs/20_domain/04_saga-processes.md`
- **CQRS with MediatR**: `src/Services/Scheduling/PATTERNS.md` (when implemented)
- **GraphQL Schema Design**: `docs/30_microservices/graphql-gateway.md`

---

## 🎉 **Welcome to the Team!**

You're now ready to explore QuietMatch. Remember:

- **Take your time** - This is a learning project, not a race
- **Ask questions** - Architecture decisions should be discussed
- **Document your learnings** - If you figured something out, others will benefit
- **Have fun** - You're building something meaningful while mastering modern software architecture

**Next step**: Choose one of the learning paths above or jump straight to setting up your local environment!

---

**Last Updated**: 2025-11-20
**Maintainer**: QuietMatch Team
**Questions?**: Check the FAQ or open a discussion issue
