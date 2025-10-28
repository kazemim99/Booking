# Booksy - Service Booking Platform

A modern, scalable service booking platform built with Domain-Driven Design (DDD) principles, enabling service providers to manage their business profiles, services, staff, and customer bookings.

---

## Table of Contents

- [Business Overview](#business-overview)
- [Architecture](#architecture)
- [Technology Stack](#technology-stack)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
- [Key Features](#key-features)
- [Development Roadmap](#development-roadmap)
- [Contributing](#contributing)

---

## Business Overview

### What is Booksy?

Booksy is a comprehensive service booking platform designed for service-based businesses such as:
- Beauty salons and barbershops
- Healthcare providers
- Fitness trainers and gyms
- Consulting services
- Home services (cleaning, repairs, etc.)

### Core Business Capabilities

#### For Service Providers
- **Provider Registration & Onboarding**: Multi-step registration with business profile setup
- **Service Management**: Create, update, and organize service offerings with pricing and duration
- **Staff Management**: Add team members, manage schedules, and track availability
- **Business Profile**: Manage business information, location, working hours, and gallery
- **Booking Management**: Accept, manage, and track customer appointments
- **Analytics & Insights**: Track business performance and customer engagement

#### For Customers
- **Service Discovery**: Search and filter services by category, location, and availability
- **Provider Profiles**: View detailed provider information, reviews, and ratings
- **Easy Booking**: Select services, choose time slots, and book appointments
- **Booking Management**: View, reschedule, or cancel appointments
- **Reviews & Ratings**: Share feedback on service experiences

---

## Architecture

### Design Principles

Booksy follows **Domain-Driven Design (DDD)** and **Clean Architecture** principles:

1. **Bounded Contexts**: Separate business domains with clear boundaries
2. **Aggregate Roots**: Consistent transaction boundaries for business entities
3. **CQRS Pattern**: Command Query Responsibility Segregation for scalability
4. **Event-Driven**: Domain events for cross-context communication
5. **Repository Pattern**: Abstraction over data access
6. **Dependency Inversion**: Core domain independent of infrastructure

### Bounded Contexts

#### 1. ServiceCatalog (Primary Context)
Manages service providers, their offerings, staff, and business profiles.

**Aggregates:**
- `Provider` (Aggregate Root)
  - Business Profile
  - Staff Collection
  - Services Collection
  - Working Hours
  - Location
  - Business Hours & Schedules

- `Service` (Aggregate Root)
  - Service Details
  - Pricing
  - Duration
  - Categories

**Key Responsibilities:**
- Provider registration and profile management
- Service catalog management
- Staff scheduling and availability
- Business hours and exception management

#### 2. UserManagement (Identity Context)
Handles authentication, authorization, and user identity.

**Aggregates:**
- `User` (Aggregate Root)
  - User Profile
  - Roles & Permissions
  - Phone Verification
  - Refresh Tokens

**Key Responsibilities:**
- User authentication (email/password, phone verification)
- JWT token generation and validation
- Role-based access control
- Cross-context user integration

#### 3. Booking (Planned)
Manages customer bookings and appointments.

**Aggregates (Planned):**
- `Booking` (Aggregate Root)
  - Customer Information
  - Service Selection
  - Time Slot
  - Status & History

#### 4. Payment (Planned)
Handles payment processing and transactions.

### Cross-Context Integration

**Current Implementation:**
- UserManagement queries ServiceCatalog for provider info
- JWT tokens include provider claims (providerId, provider_status)
- REST API integration between contexts
- Eventual consistency through domain events

**Planned:**
- Message-based integration (RabbitMQ/Azure Service Bus)
- Saga pattern for distributed transactions
- Event sourcing for audit trails

---

## Technology Stack

### Backend

#### Core Framework
- **.NET 8.0**: Latest LTS version of ASP.NET Core
- **C# 12**: Modern language features and nullable reference types

#### Architecture & Patterns
- **MediatR**: CQRS pattern implementation
- **FluentValidation**: Request validation
- **AutoMapper**: Object-object mapping
- **CAP (Planned)**: Distributed transaction and event bus

#### Data & Persistence
- **Entity Framework Core 8**: ORM and data access
- **PostgreSQL**: Primary database (production)
- **SQL Server**: Development database
- **Npgsql**: PostgreSQL provider

#### Authentication & Security
- **JWT Bearer Tokens**: Stateless authentication
- **ASP.NET Core Identity**: User management foundation
- **BCrypt**: Password hashing

#### API & Communication
- **REST API**: RESTful web services
- **Swagger/OpenAPI**: API documentation
- **YARP (Planned)**: API Gateway and reverse proxy

#### Testing
- **xUnit**: Unit and integration testing framework
- **FluentAssertions**: Readable test assertions
- **Moq**: Mocking framework
- **WebApplicationFactory**: Integration testing

#### Observability (Planned)
- **Serilog**: Structured logging
- **OpenTelemetry**: Distributed tracing
- **Prometheus**: Metrics collection
- **Grafana**: Metrics visualization
- **Seq/ELK**: Log aggregation and analysis

### Frontend

#### Core Framework
- **Vue 3**: Progressive JavaScript framework with Composition API
- **TypeScript**: Type-safe development
- **Vite**: Fast build tool and dev server

#### State & Routing
- **Pinia**: Vue state management
- **Vue Router**: Client-side routing
- **Vue I18n**: Internationalization (English, Persian/Farsi)

#### UI & Styling
- **Tailwind CSS (Planned)**: Utility-first CSS framework
- **Component Library (Planned)**: Custom design system
- **ECharts**: Data visualization and charts
- **Neshan Maps**: Map integration for location services

#### Development Tools
- **ESLint**: Code linting
- **Prettier**: Code formatting
- **Vitest**: Unit testing
- **Cypress**: E2E testing

### Infrastructure & DevOps (Planned)

#### Containerization
- **Docker**: Container runtime
- **Docker Compose**: Local development orchestration
- **Kubernetes (AKS)**: Production orchestration

#### CI/CD
- **GitHub Actions**: Automated workflows
- **Azure DevOps (Alternative)**: Build and release pipelines

#### Cloud Services (Azure)
- **Azure App Service**: Hosting
- **Azure Database for PostgreSQL**: Managed database
- **Azure Service Bus**: Message broker
- **Azure Blob Storage**: File storage
- **Azure Key Vault**: Secrets management
- **Azure Application Insights**: Monitoring

---

## Project Structure

```
Booksy/
├── src/
│   ├── APIGateway/
│   │   └── Booksy.Gateway/              # API Gateway (YARP)
│   │
│   ├── BoundedContexts/
│   │   ├── ServiceCatalog/
│   │   │   ├── Booksy.ServiceCatalog.Api/           # REST API
│   │   │   ├── Booksy.ServiceCatalog.Application/   # Use cases (CQRS)
│   │   │   ├── Booksy.ServiceCatalog.Domain/        # Domain model
│   │   │   └── Booksy.ServiceCatalog.Infrastructure/# Data access
│   │   │
│   │   ├── Booking/                     # (Planned)
│   │   └── Payment/                     # (Planned)
│   │
│   ├── UserManagement/
│   │   ├── Booksy.UserManagement.API/               # REST API
│   │   ├── Booksy.UserManagement.Application/       # Use cases
│   │   ├── Booksy.UserManagement.Domain/            # Domain model
│   │   └── Booksy.UserManagement.Infrastructure/    # Data access
│   │
│   ├── Core/
│   │   ├── Booksy.Core.Application/     # Shared application logic
│   │   └── Booksy.Core.Domain/          # Shared domain primitives
│   │
│   └── Infrastructure/
│       ├── Booksy.Infrastructure.Core/  # Shared infrastructure
│       ├── Booksy.Infrastructure.External/# External service integrations
│       └── Booksy.API/                  # Shared API utilities
│
├── booksy-frontend/
│   ├── src/
│   │   ├── core/                        # Core functionality
│   │   │   ├── api/                     # API clients
│   │   │   ├── router/                  # Routing configuration
│   │   │   └── stores/                  # Global stores (auth)
│   │   │
│   │   ├── modules/
│   │   │   ├── provider/                # Provider portal
│   │   │   │   ├── components/          # Provider components
│   │   │   │   ├── services/            # API services
│   │   │   │   ├── stores/              # Provider stores
│   │   │   │   ├── types/               # TypeScript types
│   │   │   │   └── views/               # Provider views
│   │   │   │
│   │   │   ├── customer/                # Customer portal (planned)
│   │   │   └── admin/                   # Admin portal (planned)
│   │   │
│   │   ├── shared/                      # Shared UI components
│   │   │   └── components/
│   │   │
│   │   └── locales/                     # i18n translations
│   │       ├── en.json                  # English
│   │       └── fa.json                  # Persian/Farsi
│   │
│   └── public/                          # Static assets
│
├── tests/
│   ├── Booksy.ServiceCatalog.IntegrationTests/
│   ├── Booksy.UserManagement.Tests/
│   └── Booksy.Tests.Common/             # Shared test utilities
│
└── openspec/                            # OpenSpec specifications
    ├── specs/                           # System specifications
    └── changes/                         # Change proposals
```

---

## Getting Started

### Prerequisites

**Backend:**
- .NET 8.0 SDK or later
- PostgreSQL 15+ or SQL Server 2019+
- Visual Studio 2022 / VS Code / Rider

**Frontend:**
- Node.js 20.19.0 or 22.12.0+
- npm or yarn

### Installation

#### 1. Clone the Repository
```bash
git clone https://github.com/your-org/booksy.git
cd booksy
```

#### 2. Backend Setup

**Update Connection Strings:**
```bash
# ServiceCatalog API
cd src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api
# Edit appsettings.Development.json with your database connection

# UserManagement API
cd src/UserManagement/Booksy.UserManagement.API
# Edit appsettings.Development.json with your database connection
```

**Run Migrations:**
```bash
# ServiceCatalog
cd src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure
dotnet ef database update

# UserManagement
cd src/UserManagement/Booksy.UserManagement.Infrastructure
dotnet ef database update
```

**Run APIs:**
```bash
# ServiceCatalog API (Port 7002)
cd src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api
dotnet run

# UserManagement API (Port 7001)
cd src/UserManagement/Booksy.UserManagement.API
dotnet run
```

#### 3. Frontend Setup

```bash
cd booksy-frontend
npm install
npm run dev
```

Frontend will be available at `http://localhost:5173`

### API Documentation

Once the APIs are running:
- ServiceCatalog API: `https://localhost:7002/swagger`
- UserManagement API: `https://localhost:7001/swagger`

---

## Key Features

### Implemented Features

#### Provider Management
- ✅ Provider registration (simple and full)
- ✅ Business profile management
- ✅ Location and address management
- ✅ Working hours configuration
- ✅ Provider status management (Draft, PendingVerification, Verified, Active)
- ✅ Provider search and filtering

#### Service Management
- ✅ Service CRUD operations
- ✅ Service categorization
- ✅ Pricing and duration management
- ✅ Service status (Draft, Active, Inactive, Archived)
- ✅ Service search and filtering
- ✅ Bulk operations (activate, deactivate, delete)

#### Staff Management
- ✅ Staff member management within provider aggregate
- ✅ Staff roles and permissions
- ✅ Staff activation/deactivation
- ✅ Staff schedules and working hours

#### Authentication & Authorization
- ✅ Email/password authentication
- ✅ Phone verification (OTP)
- ✅ JWT token-based authentication
- ✅ Role-based access control (Customer, Provider, Admin)
- ✅ Refresh token rotation
- ✅ Cross-context provider claims in JWT

#### User Interface
- ✅ Provider dashboard
- ✅ Service catalog management UI
- ✅ Provider settings UI
- ✅ Staff management UI
- ✅ Responsive design
- ✅ Multi-language support (English, Persian)

### Planned Features

#### Booking System
- 📋 Service booking flow
- 📋 Time slot availability
- 📋 Booking confirmation and notifications
- 📋 Booking cancellation and rescheduling
- 📋 Recurring bookings

#### Payment Integration
- 📋 Payment gateway integration
- 📋 Multiple payment methods
- 📋 Invoice generation
- 📋 Payment history

#### Customer Portal
- 📋 Customer registration and profile
- 📋 Service search and discovery
- 📋 Provider reviews and ratings
- 📋 Favorite providers
- 📋 Booking history

#### Advanced Features
- 📋 Real-time notifications (SignalR)
- 📋 Email notifications
- 📋 SMS notifications
- 📋 Calendar integration
- 📋 Analytics dashboard
- 📋 Reporting system
- 📋 Multi-location support
- 📋 Inventory management

#### Infrastructure
- 📋 API Gateway implementation
- 📋 Centralized logging
- 📋 Distributed tracing
- 📋 Performance monitoring
- 📋 Health checks
- 📋 Rate limiting
- 📋 Caching strategy

---

## Development Roadmap

### Phase 1: MVP (Completed ✅)
- ✅ Core domain models
- ✅ Provider and service management
- ✅ Basic authentication
- ✅ Provider portal UI
- ✅ Integration tests

### Phase 2: Booking System (In Progress 🚧)
- 📋 Booking domain model
- 📋 Availability management
- 📋 Booking workflow
- 📋 Customer booking UI

### Phase 3: Customer Portal (Planned 📋)
- 📋 Customer registration
- 📋 Service discovery
- 📋 Provider search
- 📋 Reviews and ratings

### Phase 4: Payments (Planned 📋)
- 📋 Payment gateway integration
- 📋 Transaction management
- 📋 Invoice generation

### Phase 5: Advanced Features (Planned 📋)
- 📋 Notifications system
- 📋 Analytics and reporting
- 📋 Mobile app development
- 📋 Third-party integrations

### Phase 6: Scale & Optimize (Planned 📋)
- 📋 Kubernetes deployment
- 📋 Performance optimization
- 📋 CDN integration
- 📋 Multi-region support

---

## API Endpoints

### ServiceCatalog API (`/api/v1`)

#### Providers
- `POST /providers/register` - Register new provider
- `POST /providers/register-full` - Full provider registration
- `GET /providers/{id}` - Get provider details
- `GET /providers/by-owner/{ownerId}` - Get provider by owner
- `GET /providers/search` - Search providers
- `POST /providers/{id}/activate` - Activate provider (Admin)

#### Provider Settings
- `PUT /providers/{id}/business-info` - Update business info
- `PUT /providers/{id}/location` - Update location
- `PUT /providers/{id}/working-hours` - Update working hours

#### Provider Services
- `GET /providers/{id}/services` - Get provider services
- `POST /providers/{id}/services` - Create service
- `PUT /providers/{id}/services/{serviceId}` - Update service
- `DELETE /providers/{id}/services/{serviceId}` - Delete service

#### Provider Staff
- `GET /providers/{id}/staff` - Get provider staff
- `POST /providers/{id}/staff` - Add staff member
- `PUT /providers/{id}/staff/{staffId}` - Update staff
- `DELETE /providers/{id}/staff/{staffId}` - Remove staff

#### Services
- `GET /services/{id}` - Get service details
- `GET /services/search` - Search services
- `GET /services/provider/{providerId}` - Get services by provider
- `POST /services/{id}/activate` - Activate service
- `POST /services/{id}/deactivate` - Deactivate service

### UserManagement API (`/api/v1`)

#### Authentication
- `POST /auth/login` - Email/password login
- `POST /auth/send-verification-code` - Send phone OTP
- `POST /auth/verify-code` - Verify phone OTP
- `POST /auth/refresh-token` - Refresh access token
- `POST /auth/logout` - Logout

---

## Testing

### Backend Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/Booksy.ServiceCatalog.IntegrationTests/

# Run with coverage
dotnet test /p:CollectCoverage=true
```

### Frontend Tests

```bash
cd booksy-frontend

# Unit tests
npm run test:unit

# E2E tests
npm run test:e2e

# E2E tests (headless)
npm run test:e2e:ci
```

---

## Contributing

### Branching Strategy

- `main` - Production-ready code
- `develop` - Development branch
- `feature/*` - Feature branches
- `bugfix/*` - Bug fix branches
- `hotfix/*` - Production hotfixes

### Commit Convention

Follow [Conventional Commits](https://www.conventionalcommits.org/):

```
feat: add booking cancellation
fix: resolve JWT token expiration issue
docs: update API documentation
refactor: improve provider aggregate methods
test: add integration tests for staff management
```

### Pull Request Process

1. Create feature branch from `develop`
2. Implement changes with tests
3. Ensure all tests pass
4. Update documentation if needed
5. Submit PR with clear description
6. Address review feedback
7. Merge after approval

---

## License

This project is proprietary and confidential.

---

## Contact & Support

For questions, issues, or contributions, please contact the development team.

**Project Repository:** [GitHub](https://github.com/your-org/booksy)
**Documentation:** [Docs](https://docs.booksy.com)
**Issue Tracker:** [GitHub Issues](https://github.com/your-org/booksy/issues)
