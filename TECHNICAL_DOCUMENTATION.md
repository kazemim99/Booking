# Booksy Platform - Comprehensive Technical Documentation

> **Living Document**: This documentation consolidates all technical documentation from the project. Last updated: 2025-11-09

---

## Document Navigation

- [Overview](#overview)
- [Architecture & Patterns](#architecture--patterns)
- [Bounded Contexts](#bounded-contexts)
- [Dependency Injection Configuration](#dependency-injection-configuration)
- [Entity Framework Core Configuration](#entity-framework-core-configuration)
- [Payment Gateway Integration](#payment-gateway-integration)
- [Notification Services](#notification-services)
- [Setup & Configuration Guides](#setup--configuration-guides)
- [Troubleshooting Guide](#troubleshooting-guide)
- [Recent Fixes & Updates](#recent-fixes--updates)

---

## Overview

Booksy is a comprehensive booking management platform built using Domain-Driven Design (DDD) principles with a microservices architecture. The platform enables service providers to manage appointments, services, staff, and customer relationships.

### Tech Stack

- **Backend**: .NET 8 / ASP.NET Core
- **Frontend**: Next.js 14 with TypeScript
- **Database**: PostgreSQL with Entity Framework Core
- **Architecture**: DDD with CQRS and Event Sourcing
- **Event Bus**: CAP with RabbitMQ
- **Caching**: Redis (optional)
- **Real-time**: SignalR

---

## Architecture & Patterns

### Domain-Driven Design (DDD)

The solution follows strategic and tactical DDD patterns:

**Strategic Patterns:**
- Bounded Contexts for separation of concerns
- Shared Kernel for common value objects
- Anti-Corruption Layer for external integrations

**Tactical Patterns:**
- Aggregates and Aggregate Roots
- Value Objects for primitive obsession prevention
- Domain Events for cross-aggregate communication
- Repository pattern for data access abstraction

### CQRS (Command Query Responsibility Segregation)

- **Commands**: Handled by `ICommandHandler<TCommand, TResult>`
- **Queries**: Handled by `IQueryHandler<TQuery, TResult>`
- **MediatR**: Used for command and query dispatching

### Event-Driven Architecture

- **Domain Events**: Published within aggregates
- **Integration Events**: Cross-context communication via CAP
- **Event Handlers**: Scoped handlers for domain and integration events

---

## Bounded Contexts

### 1. UserManagement Context

**Purpose**: User authentication, authorization, profiles, and phone verification

**Key Aggregates:**
- `User`: Main user aggregate with email, password, profile
- `PhoneVerification`: OTP verification for phone numbers
- `Customer`: Customer-specific data and preferences

**Database Schema:** `user_management`

**Key Features:**
- User registration and authentication
- JWT token-based authentication
- Refresh token management
- Phone number verification via SMS
- Two-factor authentication
- User profile management

### 2. ServiceCatalog Context

**Purpose**: Service provider management, bookings, payments

**Key Aggregates:**
- `Provider`: Service provider entity
- `Service`: Services offered by providers
- `Booking`: Customer bookings and appointments
- `Payment`: Payment processing and tracking
- `Payout`: Provider payout management

**Database Schema:** `service_catalog`

**Key Features:**
- Provider onboarding and management
- Service catalog management
- Booking lifecycle management
- Multi-gateway payment processing
- Notification management (email, SMS, push, in-app)

---

## Dependency Injection Configuration

### ServiceCatalog Infrastructure Extensions

Located: `ServiceCatalog.Infrastructure/DependencyInjection/ServiceCatalogInfrastructureExtensions.cs`

#### Core Services Registration

```csharp
services.AddServiceCatalogInfrastructure(configuration);
```

**Includes:**
- DbContext with PostgreSQL
- Unit of Work pattern
- Repository implementations
- Domain and application services
- Notification services
- Payment services

#### Repository Registrations

```csharp
services.AddScoped<IProviderReadRepository, ProviderReadRepository>();
services.AddScoped<IProviderWriteRepository, ProviderWriteRepository>();
services.AddScoped<IServiceReadRepository, ServiceReadRepository>();
services.AddScoped<IServiceWriteRepository, ServiceWriteRepository>();
services.AddScoped<IBookingReadRepository, BookingReadRepository>();
services.AddScoped<IBookingWriteRepository, BookingWriteRepository>();
services.AddScoped<IPaymentReadRepository, PaymentReadRepository>();
services.AddScoped<IPaymentWriteRepository, PaymentWriteRepository>();
services.AddScoped<IPayoutWriteRepository, PayoutWriteRepository>();
services.AddScoped<INotificationReadRepository, NotificationReadRepository>();
services.AddScoped<INotificationWriteRepository, NotificationWriteRepository>();
```

#### Notification Services

```csharp
services.AddNotificationServices();
```

**Includes:**
- `ISendGridClient`: SendGrid email integration
- `IEmailNotificationService`: Email notifications
- `ISmsNotificationService`: SMS notifications (Rahyab)
- `IPushNotificationService`: Firebase push notifications
- `IInAppNotificationService`: SignalR-based in-app notifications

#### Payment Gateway Services

```csharp
services.AddPaymentServices(configuration);
```

**Registered Gateways:**
- ZarinPal (Iranian payment gateway)
- IDPay (Iranian payment gateway)
- Behpardakht (Bank Mellat)
- Parsian Bank
- Saman Bank
- Stripe (International)

**Services:**
- `IPaymentGatewayFactory`: Factory for creating gateway instances
- `IPaymentGateway`: Default gateway instance
- `IZarinPalService`: ZarinPal-specific service
- `IIDPayService`: IDPay-specific service
- `IBehpardakhtService`: Behpardakht-specific service

### UserManagement Infrastructure Extensions

Located: `UserManagement.Infrastructure/DependencyInjection/UserManagementInfrastructureExtensions.cs`

#### Repository Registrations

```csharp
services.AddScoped<IUserRepository, UserRepository>();
services.AddScoped<IUserQueryRepository, UserQueryRepository>();
services.AddScoped<IPhoneVerificationRepository, PhoneVerificationRepository>();
services.AddScoped<ICustomerRepository, CustomerRepository>();
```

#### External Services

```csharp
services.AddExternalServices(configuration);
```

#### SMS Notification Service

```csharp
// Domain-specific SMS service for phone verification
services.AddScoped<ISmsNotificationService, RahyabSmsNotificationService>();
services.AddHttpClient<RahyabSmsNotificationService>();
```

**Note**: UserManagement uses the same SMS service interface as ServiceCatalog but for phone verification purposes.

---

## Entity Framework Core Configuration

### Key Patterns

#### 1. Value Object Conversion

```csharp
builder.Property(u => u.Id)
    .HasConversion(
        id => id.Value,
        value => UserId.From(value))
    .HasColumnName("id");
```

#### 2. Owned Entity Configuration

```csharp
builder.OwnsOne(u => u.PhoneNumber, pn =>
{
    pn.Property(p => p.Value)
        .HasColumnName("phone_number")
        .HasMaxLength(20);

    pn.Property(p => p.CountryCode)
        .HasColumnName("country_code")
        .HasMaxLength(5);

    pn.Property(p => p.NationalNumber)
        .HasColumnName("national_number")
        .HasMaxLength(15);

    // Index must be configured inside OwnsOne block
    pn.HasIndex(p => p.Value)
        .HasDatabaseName("ix_table_phone_number");
});
```

**Important**: Indexes on owned entity properties must be configured within the `OwnsOne` block using the property builder.

#### 3. One-to-One Relationships with Value Object FK

```csharp
// In User Configuration
builder.HasOne(u => u.Profile)
    .WithOne()
    .HasForeignKey<UserProfile>(p => p.UserId)  // Use lambda, not string
    .OnDelete(DeleteBehavior.Cascade);

// In UserProfile Configuration
builder.Property(up => up.UserId)
    .HasConversion(
        id => id.Value,
        value => UserId.From(value))
    .HasColumnName("user_id")
    .IsRequired();
```

**Important**: When the FK property is a value object, configure it using lambda expressions, not string-based shadow properties.

#### 4. PhoneNumber Value Object Requirements

For EF Core to properly materialize value objects, they need:

```csharp
public sealed class PhoneNumber : ValueObject
{
    // Properties must have private setters (not get-only)
    public string Value { get; private set; }
    public string CountryCode { get; private set; }
    public string NationalNumber { get; private set; }

    // Parameterless constructor for EF Core
    private PhoneNumber()
    {
        Value = string.Empty;
        CountryCode = string.Empty;
        NationalNumber = string.Empty;
    }

    // Public factory methods
    public static PhoneNumber Create(string phoneNumber) { ... }
}
```

---

## Payment Gateway Integration

### Architecture

The payment gateway system uses the **Abstract Factory** pattern to support multiple payment providers.

### Configuration

**appsettings.json:**

```json
{
  "Payment": {
    "DefaultProvider": "ZarinPal",
    "ZarinPal": {
      "MerchantId": "your-merchant-id",
      "IsSandbox": true,
      "CallbackUrl": "https://localhost:7002/api/v1/payments/zarinpal/callback"
    },
    "IDPay": {
      "ApiKey": "your-api-key",
      "IsSandbox": true,
      "CallbackUrl": "https://localhost:7002/api/v1/payments/idpay/callback"
    },
    "Behpardakht": {
      "TerminalId": 0,
      "Username": "your-username",
      "Password": "your-password",
      "CallbackUrl": "https://localhost:7002/api/v1/payments/behpardakht/callback"
    }
  }
}
```

### Usage

```csharp
public class ProcessPaymentCommandHandler
{
    private readonly IPaymentGatewayFactory _gatewayFactory;

    public async Task<ProcessPaymentResult> Handle(...)
    {
        // Get specific gateway
        var gateway = _gatewayFactory.GetGateway("ZarinPal");

        // Or use default
        var defaultGateway = _gatewayFactory.GetDefaultGateway();

        var result = await gateway.ProcessPaymentAsync(...);
    }
}
```

---

## Notification Services

### Multi-Channel Support

The platform supports four notification channels:

1. **Email** - SendGrid
2. **SMS** - Rahyab (or Kavenegar for UserManagement)
3. **Push** - Firebase Cloud Messaging
4. **In-App** - SignalR

### SignalR Configuration

**Startup.cs:**

```csharp
// In ConfigureServices
services.AddSignalR();

// In Configure
app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<NotificationHub>("/hubs/notifications");
});
```

### Email Configuration (SendGrid)

**appsettings.json:**

```json
{
  "SendGrid": {
    "ApiKey": "your-sendgrid-api-key",
    "FromEmail": "noreply@booksy.com",
    "FromName": "Booksy"
  }
}
```

### SMS Configuration

**For ServiceCatalog (Rahyab):**

```json
{
  "Rahyab": {
    "ApiKey": "your-rahyab-api-key",
    "Sender": "10004346"
  }
}
```

**For UserManagement (Kavenegar):**

```json
{
  "Kavenegar": {
    "ApiKey": "your-kavenegar-api-key",
    "Sender": "10004346",
    "Enabled": true
  }
}
```

---

## Setup & Configuration Guides

### Prerequisites

- .NET 8 SDK
- PostgreSQL 14+
- Redis (optional, for caching)
- RabbitMQ (for event bus)
- Node.js 18+ (for frontend)

### Database Setup

1. **Create Databases:**

```sql
CREATE DATABASE booksy_usermanagement;
CREATE DATABASE booksy_servicecatalog;
```

2. **Update Connection Strings:**

**appsettings.json:**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=booksy;Username=postgres;Password=your_password",
    "UserManagement": "Host=localhost;Database=booksy_usermanagement;Username=postgres;Password=your_password",
    "ServiceCatalog": "Host=localhost;Database=booksy_servicecatalog;Username=postgres;Password=your_password",
    "Redis": "localhost:6379"
  }
}
```

3. **Run Migrations:**

```bash
# UserManagement
cd src/UserManagement/Booksy.UserManagement.API
dotnet ef database update --context UserManagementDbContext

# ServiceCatalog
cd src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api
dotnet ef database update --context ServiceCatalogDbContext
```

### Running the Application

```bash
# Backend - UserManagement
cd src/UserManagement/Booksy.UserManagement.API
dotnet run

# Backend - ServiceCatalog
cd src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api
dotnet run

# Frontend
cd booksy-frontend
npm install
npm run dev
```

---

## Troubleshooting Guide

### Common EF Core Issues

#### Issue: "column u0.CustomerId does not exist"

**Cause**: Shadow property configuration mismatch between relationship and property configuration.

**Solution**: Use lambda expressions instead of string-based shadow properties when the FK property exists on the entity:

```csharp
// ❌ Wrong
.HasForeignKey<UserProfile>("UserId")

// ✅ Correct
.HasForeignKey<UserProfile>(p => p.UserId)
```

#### Issue: "No suitable constructor was found for entity type 'PhoneNumber'"

**Cause**: Value object doesn't have a parameterless constructor or properties are get-only.

**Solution**: Add parameterless constructor and use `private set`:

```csharp
public class PhoneNumber
{
    public string Value { get; private set; }  // Not { get; }

    private PhoneNumber()  // Parameterless constructor
    {
        Value = string.Empty;
    }
}
```

#### Issue: "The expression 'v => v.PhoneNumber.Value' is not a valid member access expression"

**Cause**: Trying to create index on owned entity property from outside the `OwnsOne` block.

**Solution**: Configure indexes inside the `OwnsOne` block:

```csharp
builder.OwnsOne(v => v.PhoneNumber, pn =>
{
    pn.Property(p => p.Value)...;

    // ✅ Index here
    pn.HasIndex(p => p.Value)
        .HasDatabaseName("ix_phone_number");
});
```

### Dependency Injection Issues

#### Issue: "Unable to resolve service for type 'IXxxRepository'"

**Cause**: Repository not registered in DI container.

**Solution**: Add registration in infrastructure extensions:

```csharp
services.AddScoped<IXxxRepository, XxxRepository>();
```

#### Issue: "Unable to resolve service for type 'ISendGridClient'"

**Cause**: SendGrid client not registered.

**Solution**: Ensure `AddNotificationServices()` is called and API key is configured.

---

## Recent Fixes & Updates

### Session: 2025-11-09 - Dependency Injection & EF Core Fixes

**Commits: 10 total**

1. **Fix dependency injection issues for payment, payout, and notification services**
   - Registered payment/payout repositories
   - Configured SendGrid client
   - Added SignalR for in-app notifications

2. **Register payment gateway services and domain-specific SMS notification service**
   - Registered payment gateway factory
   - Configured ZarinPal service
   - Added domain-specific SMS service

3. **Register IDPay and Behpardakht payment gateway services**
   - Added IIDPayService and IBehpardakhtService
   - Configured settings from appsettings

4. **Register SMS notification service for UserManagement bounded context**
   - Added RahyabSmsNotificationService for phone verification
   - Shared SMS service between bounded contexts

5. **Fix EF Core configuration for PhoneVerification owned entity indexes**
   - Initial attempt to fix owned entity index configuration

6. **Fix owned entity index configuration in PhoneVerificationConfiguration**
   - Moved index configuration inside OwnsOne block
   - Removed composite index with mixed property types

7. **Fix EF Core constructor binding for PhoneNumber value object**
   - Added parameterless constructor
   - Changed properties from get-only to `private set`

8. **Remove non-existent customer_id shadow property from UserProfileConfiguration**
   - Removed CustomerId shadow property that didn't exist in database

9. **Fix UserId configuration in UserProfileConfiguration - use property not shadow**
   - Changed from shadow property to explicit property mapping
   - Added value object conversion

10. **Fix User-UserProfile relationship to use property instead of shadow FK**
    - Updated foreign key configuration to use lambda expression
    - Aligned both sides of relationship configuration

**Key Learnings:**
- Always use lambda expressions for FK configuration when property exists
- Owned entity indexes must be configured within `OwnsOne` block
- Value objects need parameterless constructors and settable properties for EF Core
- Shadow properties and explicit properties cannot be mixed in configurations

---

## Document Revision History

| Date       | Version | Changes                                           | Author       |
|------------|---------|---------------------------------------------------|--------------|
| 2025-11-09 | 3.0.0   | Comprehensive update with DI and EF Core fixes   | Claude (AI)  |
| 2025-11-06 | 2.0.0   | Consolidated all technical documentation          | Claude (AI)  |
| 2025-11-05 | 1.0.0   | Initial comprehensive documentation created       | Claude (AI)  |

---

*This is a living document. Update this file whenever significant changes are made to the architecture, domain model, or development processes.*
