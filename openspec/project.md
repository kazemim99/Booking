# Project Context

## Purpose
Booksy is a modular monolith service booking and catalog platform that enables service providers to register their businesses, manage services, staff, and business operations, while allowing customers to discover and book services. The system follows Domain-Driven Design principles with a focus on maintainability, scalability, and reliability through event-driven architecture.

## Tech Stack

### Backend
- **Language**: C# with .NET 9.0
- **Framework**: ASP.NET Core
- **Database**: PostgreSQL with Entity Framework Core 9.0
- **Message Broker**: RabbitMQ with CAP (Consistent Asynchronous Pattern) for event-driven communication
- **Cache**: Redis (StackExchange.Redis)
- **API Gateway**: Custom Booksy.Gateway implementation
- **Logging**: Serilog with Seq integration for structured logging
- **Monitoring & Observability**:
  - OpenTelemetry for distributed tracing (Jaeger exporter)
  - Prometheus for metrics
  - Application Insights (optional)
  - Sentry for error tracking
- **Documentation**: Swagger/OpenAPI (Swashbuckle)
- **Dependency Injection**: Autofac
- **Mapping**: AutoMapper
- **Validation**: FluentValidation
- **Mediation**: MediatR 13.0.0
- **API Versioning**: Microsoft.AspNetCore.Mvc.Versioning 5.1.0

### Frontend
- **Framework**: Vue 3 (Composition API)
- **Build Tool**: Vite 7.1.7
- **Language**: TypeScript 5.9
- **Package Manager**: npm
- **State Management**: Pinia 3.0.3
- **Routing**: Vue Router 4.5.1
- **Internationalization**: vue-i18n 10.0.8
- **UI Testing**: Vitest 3.2.4 (unit), Cypress 15.3.0 (E2E)
- **Code Quality**: ESLint with Vue plugin, Prettier 3.6.2
- **Maps**: Neshan Maps Platform (OpenLayers)

### DevOps & Infrastructure
- **Containerization**: Docker with Docker Compose
- **Observability Stack**: Seq, pgAdmin, Prometheus, Grafana, Jaeger

## Project Conventions

### Code Style

#### Backend (C#)
- **Nullable Reference Types**: Enabled project-wide for null-safety
- **Implicit Usings**: Enabled to reduce boilerplate
- **Warnings as Errors**: Enabled to enforce code quality
- **Documentation XML**: Generated for public APIs
- **Naming Conventions**:
  - PascalCase for classes, methods, properties
  - camelCase for parameters and local variables
  - Interface names prefixed with `I`
  - Database naming via EFCore.NamingConventions (snake_case in DB)

#### Frontend (TypeScript/Vue)
- **Linting**: ESLint with Vue-specific rules
- **Formatting**: Prettier for consistent code style
- **Component Structure**: Composition API with `<script setup>` syntax
- **Module Organization**: Feature-based modules with views, components, stores, and types

### Architecture Patterns

#### Domain-Driven Design (DDD)
- **Bounded Contexts**: Separate contexts for ServiceCatalog and UserManagement
- **Layered Architecture**:
  - **Domain Layer**: Aggregates, Entities, Value Objects, Domain Events
  - **Application Layer**: Commands, Queries, Handlers (CQRS pattern)
  - **Infrastructure Layer**: EF Core DbContext, Repositories, External Services
  - **API Layer**: Controllers, DTOs, API versioning
- **Key Patterns**:
  - Aggregate Root pattern with `AggregateRoot<TId>` base class
  - Repository pattern for data access abstraction
  - Unit of Work pattern for transaction management
  - Domain Events for intra-aggregate communication
  - Integration Events for cross-context communication

#### Event-Driven Architecture
- **CAP Framework**: Outbox pattern for reliable event publishing
- **Event Flow**:
  1. Domain event raised in aggregate
  2. Domain event handler maps to integration event
  3. CAP publisher stores in outbox table (same DB transaction)
  4. CAP background process publishes to RabbitMQ
  5. Consumer processes via [CapSubscribe] handlers
- **Message Broker**: RabbitMQ for async inter-service communication

#### CQRS (Command Query Responsibility Segregation)
- Commands for write operations (handled by MediatR)
- Queries for read operations
- FluentValidation for input validation
- Separate handlers for each command/query

#### Frontend Architecture
- **Module-Based**: Feature modules with dedicated views, components, stores, and types
- **State Management**: Pinia stores for reactive state
- **Composition Pattern**: Reusable composables in shared utilities
- **Type Safety**: Strong TypeScript typing throughout

### Testing Strategy

#### Backend Testing
- **Framework**: xUnit 2.9.2
- **Test Levels**:
  - **Unit Tests**: Domain logic and application handlers (no external dependencies)
  - **Integration Tests**: Database interactions with real DbContext
  - **Architecture Tests**: NetArchTest for validating layer dependencies and namespace conventions
- **Coverage**: Coverlet for code coverage reporting
- **Shared Utilities**: `Booksy.Tests.Commons` for test helpers

#### Frontend Testing
- **Unit Tests**: Vitest with Vue Test Utils 2.4.6
- **E2E Tests**: Cypress for end-to-end user flows
- **Test Data**: @faker-js/faker for generating test data
- **Coverage**: Built-in Vitest coverage tools

### Git Workflow
- **Main Branch**: `master`
- **Commit Convention**: Descriptive commit messages (recent examples show feature-focused commits)
- **Recent Work**: CAP event consistency, UI admin dashboard, provider registration

## Domain Context

### Core Domain Concepts

#### Providers (Aggregate Root)
- Business entities offering services
- Manage business profile (name, description, location)
- Own collections of Services, Staff, and BusinessHours
- Have ContactInfo and BusinessAddress (Value Objects)
- Support different ProviderTypes and ProviderStatus (Pending, Active, Inactive)
- Control booking settings (online booking enabled, mobile services, approval requirements)

#### Services
- Offered by providers with pricing and duration
- Searchable and browsable in service catalog

#### Staff
- Employees managed by providers
- Associated with service delivery

#### Users
- Customers who book services
- Providers who manage businesses
- Admins who manage the platform
- Role-based access control via UserManagement context

#### Bookings
- Customer reservations for services
- Integration with provider availability and business hours

### Bounded Contexts
1. **ServiceCatalog**: Provider registration, service management, staff, business hours
2. **UserManagement**: User roles, authentication, provider role assignment

## Important Constraints

### Technical Constraints
- Must maintain transactional consistency using CAP outbox pattern
- Database schema naming enforced via EFCore.NamingConventions
- API versioning required for all public endpoints
- Architecture constraints validated via NetArchTest in CI/CD
- Strong typing required (nullable reference types, no implicit anys in TypeScript)
- Warnings treated as errors in backend builds

### Quality Constraints
- All domain logic must be in Domain layer (no business logic in API/Infrastructure)
- All write operations must go through command handlers
- All database access must go through repositories
- All integration events must use CAP for reliability
- Health checks required for all external dependencies

### Performance Constraints
- Redis caching for frequently accessed data
- Async/await patterns for all I/O operations
- Connection pooling for database connections

## External Dependencies

### Infrastructure Services
- **PostgreSQL**: Primary data store (port 5432)
- **RabbitMQ**: Message broker for event-driven communication (port 5672, management UI on 15672)
- **Redis**: Distributed caching layer (port 6379)
- **Seq**: Structured logging aggregation and search (port 5341)
- **pgAdmin**: Database administration tool (port 5050)

### Third-Party Libraries & Services
- **Neshan Maps Platform**: Map integration for location services
- **JWT Authentication**: Token-based authentication for API access
- **Azure Key Vault**: Optional secrets management (Booksy.Configuration.KeyVault)

### Monitoring & Observability
- **Jaeger**: Distributed tracing backend
- **Prometheus**: Metrics collection
- **Application Insights**: Optional telemetry (Microsoft.ApplicationInsights)
- **Sentry**: Error tracking and monitoring
