---
sidebar_position: 1
---

# Architecture Overview

Booksy is built using **Domain-Driven Design (DDD)** principles with a focus on clean architecture and separation of concerns.

## System Architecture

```
┌─────────────────────────────────────────────────────────┐
│                   Presentation Layer                     │
│                                                          │
│   ┌──────────────────┐      ┌──────────────────┐       │
│   │  UserManagement  │      │  ServiceCatalog  │       │
│   │       API        │      │       API        │       │
│   │   (Port 5000)    │      │   (Port 5010)    │       │
│   └──────────────────┘      └──────────────────┘       │
└─────────────────────────────────────────────────────────┘
           │                          │
           │                          │
           ▼                          ▼
┌─────────────────────────────────────────────────────────┐
│                  Application Layer                       │
│                                                          │
│   Commands, Queries, Event Handlers (MediatR)           │
│   DTOs, Validation, Application Services                │
└─────────────────────────────────────────────────────────┘
           │                          │
           │                          │
           ▼                          ▼
┌─────────────────────────────────────────────────────────┐
│                   Domain Layer                           │
│                                                          │
│   Entities, Value Objects, Aggregates, Domain Events    │
│   Domain Services, Business Logic                       │
└─────────────────────────────────────────────────────────┘
           │                          │
           │                          │
           ▼                          ▼
┌─────────────────────────────────────────────────────────┐
│                Infrastructure Layer                      │
│                                                          │
│   PostgreSQL, Redis, External APIs, File Storage        │
│   Event Bus, Email Service, SMS Service                 │
└─────────────────────────────────────────────────────────┘
```

## Bounded Contexts

### 1. UserManagement

**Responsibility**: User authentication, authorization, and profile management

**Key Aggregates**:
- `User` - User account with authentication
- `Customer` - Customer-specific data and preferences
- `PhoneVerification` - OTP verification sessions

**Services**:
- Authentication (JWT)
- Phone verification (OTP)
- User registration
- Profile management

### 2. ServiceCatalog

**Responsibility**: Service providers, bookings, and payments

**Key Aggregates**:
- `Provider` - Service provider with business profile
- `Service` - Bookable service
- `Booking` - Appointment booking
- `Payment` - Payment transaction
- `Payout` - Provider payout

**Services**:
- Provider onboarding
- Service catalog
- Booking management
- Payment processing (ZarinPal)
- Availability management

## Design Patterns

### CQRS (Command Query Responsibility Segregation)

Using **MediatR** to separate read and write operations:

```csharp
// Command (Write)
public class CreateBookingCommand : IRequest<CreateBookingResult>
{
    public Guid CustomerId { get; set; }
    public Guid ServiceId { get; set; }
    public DateTime BookingTime { get; set; }
}

// Query (Read)
public class GetBookingByIdQuery : IRequest<BookingDto>
{
    public Guid BookingId { get; set; }
}
```

### Repository Pattern

Abstraction for data access:

```csharp
public interface IRepository<T> where T : AggregateRoot
{
    Task<T?> GetByIdAsync(Guid id);
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
}
```

### Domain Events

For cross-context communication:

```csharp
public class ProviderRegisteredEvent : IntegrationEvent
{
    public Guid ProviderId { get; set; }
    public string Email { get; set; }
    public string BusinessName { get; set; }
}
```

## Technology Stack

### Backend
- **.NET 9.0** - Runtime
- **ASP.NET Core** - Web framework
- **Entity Framework Core** - ORM
- **MediatR** - CQRS implementation
- **FluentValidation** - Input validation
- **AutoMapper** - Object mapping

### Database
- **PostgreSQL 14+** - Primary database
- **Redis 7+** - Caching and rate limiting

### Security
- **JWT** - Authentication tokens
- **ASP.NET Core Identity** - User management
- **Rate Limiting** - API protection

### DevOps
- **Docker** - Containerization
- **Serilog** - Structured logging
- **Swagger/OpenAPI** - API documentation

### Integration
- **ZarinPal** - Payment gateway
- **SMS Provider** - OTP delivery
- **Email Service** - Notifications

## Data Flow

### Example: Creating a Booking

```
1. Client → POST /api/v1/bookings
            ↓
2. Controller → Validates request
            ↓
3. Controller → Dispatches CreateBookingCommand (MediatR)
            ↓
4. CommandHandler → Validates business rules
            ↓
5. CommandHandler → Creates Booking aggregate
            ↓
6. Repository → Saves to database
            ↓
7. Domain Event → BookingCreatedEvent published
            ↓
8. Event Handler → Sends confirmation email
            ↓
9. Response → Returns BookingDto to client
```

## Security Architecture

### Authentication Flow

```
1. User → Login with credentials
2. API → Validates credentials
3. API → Generates JWT access + refresh tokens
4. User → Stores tokens
5. User → Includes access token in Authorization header
6. API → Validates token on each request
7. Token expires → Use refresh token to get new access token
```

### Authorization

- **Role-based** - Admin, Provider, Customer
- **Policy-based** - Custom authorization policies
- **Resource-based** - Owner-only access to resources

## Scalability Considerations

- **Stateless APIs** - Horizontal scaling
- **Redis caching** - Reduce database load
- **Async operations** - Non-blocking I/O
- **Rate limiting** - Prevent abuse
- **Database indexing** - Query optimization

## Further Reading

- [Domain Model](./domain-model) - Entity and value object details
- [Infrastructure](./infrastructure) - Database and external services
- [Bounded Contexts](./bounded-contexts) - Detailed context breakdown
