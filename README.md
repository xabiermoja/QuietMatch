# QuietMatch - Privacy-Focused Dating Platform

> **Meaningful connections through values, not swipes**

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](CONTRIBUTING.md)

QuietMatch is a privacy-first dating platform for the EU market that emphasizes **personality compatibility, shared values, and intentional dating** over superficial browsing. Built as a comprehensive learning project for modern .NET microservices architecture.

---

## 🎯 Project Goals

### Business Goals
- Build a **GDPR-compliant** dating platform that prioritizes privacy and meaningful connections
- Implement **blind matching** based on personality, values, and lifestyle compatibility
- **Automated scheduling** to eliminate planning friction
- **EU-focused** with privacy regulations baked into the architecture

### Learning Goals
- Master **4 architecture patterns**: Layered, Onion, Hexagonal (Ports & Adapters), CQRS
- Implement **microservices patterns**: SAGA, Event-Driven Architecture, API Gateway
- Build **cloud-agnostic** systems (local Docker → Azure deployment)
- Practice **Domain-Driven Design** with ubiquitous language
- Gain hands-on experience with **.NET 8, gRPC, GraphQL, RabbitMQ, PostgreSQL, Redis**

---

## 🚀 Quick Start

### Prerequisites

- **.NET 8 SDK**: [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Docker Desktop**: [Download](https://www.docker.com/products/docker-desktop)
- **Git**: [Download](https://git-scm.com/)

### Clone & Run

```bash
# Clone the repository
git clone https://github.com/yourusername/QuietMatch.git
cd QuietMatch

# Start infrastructure (PostgreSQL, RabbitMQ, Redis, Seq)
docker-compose up -d

# Run database migrations (once infrastructure is up)
# dotnet ef database update --project src/Services/Identity

# Start services
docker-compose up --build

# Access services
# - API Gateway: http://localhost:5000
# - Seq Logs: http://localhost:5341
# - RabbitMQ Management: http://localhost:15672 (guest/guest)
```

**First time here?** Start with [`docs/START-HERE.md`](docs/START-HERE.md) for a complete guide.

---

## 📚 Documentation

### Essential Reading

| Document | Purpose |
|----------|---------|
| **[START HERE](docs/START-HERE.md)** | Complete navigation guide and project overview |
| **[Product Vision](docs/00_overview/00_product-vision.md)** | Business goals and user value proposition |
| **[Main Goals](docs/00_overview/01_main-goals.md)** | Learning objectives and technical goals |
| **[Architecture Guidelines](docs/10_architecture/02_architecture-guidelines.md)** | The authoritative rulebook for all implementation |
| **[Ubiquitous Language](docs/20_domain/01_domain-ubiquitous-language.md)** | Domain terminology and naming conventions |
| **[Glossary](docs/50_references/glossary.md)** | Quick reference for all terms |

### Architecture Documentation

- [Architecture Overview](docs/10_architecture/01_architecture-overview.md) - System design and component relationships
- [Service Templates](docs/10_architecture/03_service-templates.md) - Folder structure for each pattern
- [Security & Auth](docs/10_architecture/05_security-and-auth.md) - Authentication and authorization
- [Messaging & Integration](docs/10_architecture/06_messaging-and-integration.md) - Event-driven patterns

### Microservices

| Service | Architecture | Purpose | Docs |
|---------|--------------|---------|------|
| IdentityService | Layered | Social login, JWT issuance | [Docs](docs/30_microservices/identity-service.md) |
| ProfileService | Onion | Member profiles, privacy | [Docs](docs/30_microservices/profile-service.md) |
| MatchingService | Hexagonal | Compatibility scoring, match generation | [Docs](docs/30_microservices/matching-service.md) |
| SchedulingService | Layered + CQRS | Availability, blind date scheduling | [Docs](docs/30_microservices/scheduling-service.md) |
| NotificationService | Hexagonal | Email, push, real-time notifications | [Docs](docs/30_microservices/notification-service.md) |
| PaymentService | Hexagonal | Subscriptions, Stripe integration | [Docs](docs/30_microservices/payment-service.md) |
| VerificationService | Hexagonal | Phone & ID verification | [Docs](docs/30_microservices/verification-service.md) |
| RealTimeService | Layered | SignalR hub for real-time updates | [Docs](docs/30_microservices/realtime-service.md) |
| GraphQLGateway | Layered | Query aggregation for clients | [Docs](docs/30_microservices/graphql-gateway.md) |

---

## 🏗️ Architecture Overview

### Microservices Pattern

QuietMatch uses a microservices architecture with:
- **Database per Service**: Each service owns its data
- **Event-Driven Communication**: Async messaging via RabbitMQ/Azure Service Bus
- **API Gateway**: GraphQL gateway for client queries
- **gRPC**: High-performance internal service communication

### Technology Stack

**Backend**:
- **.NET 8** (LTS) - Framework
- **ASP.NET Core** - Web API
- **Entity Framework Core** - ORM
- **MassTransit** - Messaging abstraction
- **MediatR** - CQRS implementation

**Data & Infrastructure**:
- **PostgreSQL 16** - Primary database (with pgvector for embeddings)
- **RabbitMQ** - Message broker (local dev)
- **Azure Service Bus** - Message broker (cloud)
- **Redis** - Distributed caching, SignalR backplane
- **Docker** - Containerization

**Frontend** (Future):
- **Blazor Web App** - SEO-friendly SSR + interactive components
- **.NET MAUI Blazor Hybrid** - Cross-platform mobile (iOS, Android)

### Architecture Patterns

Each microservice uses a **different architecture pattern** for learning:

```
IdentityService      → Layered Architecture
ProfileService       → Onion Architecture
MatchingService      → Hexagonal Architecture (Ports & Adapters)
SchedulingService    → Layered + CQRS
NotificationService  → Hexagonal Architecture
```

**Why different patterns?** To learn when to use each pattern through hands-on implementation.

---

## 🔐 Privacy & GDPR

QuietMatch is **privacy-by-design**:

- ✅ **Minimal Data Collection**: Only what's necessary for matching
- ✅ **Field-Level Encryption**: Sensitive profile data encrypted (AES-256)
- ✅ **Data Sovereignty**: Each service owns its data, no shared databases
- ✅ **User Rights Implementation**:
  - Right to Access (data export)
  - Right to Erasure (account deletion SAGA)
  - Right to Rectification (profile editing)
  - Right to Data Portability (JSON export)
- ✅ **Consent Management**: Granular consent tracking
- ✅ **Audit Logging**: All data access logged

See [Data & Privacy](docs/10_architecture/04_data-and-privacy.md) for details.

---

## 🧪 Testing

QuietMatch has comprehensive test coverage:

### Unit Tests
- **Framework**: xUnit
- **Coverage**: Domain logic, application services
- **Run**: `dotnet test`

### Integration Tests
- **Framework**: xUnit + Testcontainers
- **Coverage**: Database interactions, message publishing
- **Infrastructure**: Real PostgreSQL, RabbitMQ, Redis in Docker
- **Run**: `dotnet test --filter Category=Integration`

### API Tests
- **Framework**: xUnit + WebApplicationFactory
- **Coverage**: All HTTP endpoints, gRPC services, GraphQL queries
- **Run**: `dotnet test --filter Category=Api`

**Test Philosophy**: Every microservice MUST have unit, integration, and API tests before merging.

---

## 🚢 Deployment

### Local Development

```bash
# Start all services with Docker Compose
docker-compose up --build

# Services available at:
# - API Gateway: http://localhost:5000
# - IdentityService: http://localhost:5001
# - ProfileService: http://localhost:5002
# - MatchingService: http://localhost:5003
# ...
```

### Azure (Production)

QuietMatch uses **Azure Container Apps** for serverless container deployment:

```bash
# Build and push Docker images
./scripts/build-and-push.sh

# Deploy infrastructure (Bicep templates)
az deployment group create \
  --resource-group quietmatch-rg \
  --template-file infrastructure/main.bicep

# Deploy services
./scripts/deploy-azure.sh
```

**Deployment Strategy**: Blue-Green deployment for zero downtime.

See [Deployment & DevOps](docs/10_architecture/07_deployment-and-devops.md) for details.

---

## 🤝 Contributing

We welcome contributions! Please read our [Contributing Guide](CONTRIBUTING.md) before submitting PRs.

### Before Contributing

1. **Read the docs**: Start with [`docs/START-HERE.md`](docs/START-HERE.md)
2. **Understand the architecture**: Review [Architecture Guidelines](docs/10_architecture/02_architecture-guidelines.md)
3. **Follow ubiquitous language**: Use terms from [Domain Model](docs/20_domain/01_domain-ubiquitous-language.md)
4. **Write tests**: All PRs must include unit, integration, and API tests
5. **Update docs**: Keep documentation in sync with code

### Development Workflow

```bash
# Create feature branch
git checkout -b feature/f0001-sign-in-with-google

# Make changes, write tests
dotnet test

# Commit with meaningful message
git commit -m "feat(identity): implement Google Sign-In (f0001)"

# Push and create PR
git push origin feature/f0001-sign-in-with-google
```

**Naming Convention**: Use feature IDs from `docs/40_features/` (e.g., `f0001`, `f0002`)

---

## 📊 Project Status

### Current Phase: **Foundation (Months 1-2)**

- [x] Project setup and documentation
- [ ] IdentityService (Layered architecture)
- [ ] ProfileService (Onion architecture)
- [ ] Basic GraphQL Gateway
- [ ] Local dev environment fully functional

### Roadmap

- **Phase 1 (Months 1-2)**: Foundation - Identity, Profile, Gateway
- **Phase 2 (Months 3-4)**: Core Matching - Matching, Scheduling, Notifications, SAGA
- **Phase 3 (Months 5-6)**: Enhancements - Real-time, Verification, Payments, Frontend
- **Phase 4 (Months 7-9)**: Cloud & Scale - Azure deployment, monitoring, performance
- **Phase 5 (Months 10-12)**: Mobile & Growth - MAUI apps, analytics, iterations

See [Roadmap](docs/00_overview/02_roadmap-feature-index.md) for detailed feature list.

---

## 📜 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

- **Inspiration**: Modern dating apps (Tinder, Bumble, Hinge) - we're building a privacy-focused alternative
- **Architecture**: DDD patterns from Eric Evans, Chris Richardson's Microservices Patterns
- **.NET Community**: For excellent libraries and resources

---

## 📞 Contact & Support

- **Issues**: [GitHub Issues](https://github.com/yourusername/QuietMatch/issues)
- **Discussions**: [GitHub Discussions](https://github.com/yourusername/QuietMatch/discussions)
- **Email**: contact@quietmatch.com (placeholder)

---

**Built with ❤️ using .NET 8, designed for learning, and committed to privacy.**

**Ready to dive in?** → Start with [`docs/START-HERE.md`](docs/START-HERE.md)
