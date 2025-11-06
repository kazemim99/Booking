# Booksy Platform - Technical Documentation

> **Living Document**: This documentation is continuously updated as the project evolves. Last updated: 2025-11-05

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Business Domain Overview](#business-domain-overview)
3. [Architecture Overview](#architecture-overview)
4. [Domain Model Deep Dive](#domain-model-deep-dive)
5. [API Architecture](#api-architecture)
6. [Data Architecture](#data-architecture)
7. [Testing Strategy](#testing-strategy)
8. [Recent Changes & Fixes](#recent-changes--fixes)
9. [Known Issues & Limitations](#known-issues--limitations)
10. [Future Development](#future-development)
11. [Developer Guidelines](#developer-guidelines)

---

## Executive Summary

### What is Booksy?

Booksy is a comprehensive service booking platform built with Domain-Driven Design (DDD) principles, enabling service providers (beauty salons, healthcare, fitness, consulting, etc.) to manage their business profiles, services, staff, and customer bookings.

### Technology Stack

**Backend**: .NET 8.0, EF Core 9.0, PostgreSQL, CQRS with MediatR, JWT Authentication
**Frontend**: Vue 3, TypeScript, Pinia, Vite, Tailwind CSS
**Testing**: xUnit, FluentAssertions, WebApplicationFactory
**Infrastructure**: Docker, GitHub Actions (CI/CD), Azure (planned)

### Current Status

- **Phase 1 (MVP)**: ✅ Completed - Provider/Service management, Authentication, Provider Portal UI
- **Phase 2 (Booking System)**: ✅ Completed - Booking domain model, API controllers, integration tests
- **Phase 3 (Payment & Financial System)**: ✅ Completed - Payment processing, Payout management, Financial reporting
- **Phase 4 (Customer Portal)**: 📋 Planned
- **Phase 5 (Advanced Features)**: 📋 Planned

---

## Business Domain Overview

### Core Business Concepts

#### 1. Service Providers
Organizations or individuals offering services to customers. Types include:
- **Individual**: Single-person operations (freelancers, consultants)
- **Business**: Multi-staff operations (salons, clinics, gyms)

**Provider Lifecycle:**
```
Drafted → PendingVerification → Verified → Active → (Suspended/Deactivated)
```

#### 2. Services
Offerings provided by providers with specific pricing, duration, and booking policies.

**Service Categories:**
- Beauty (Hair, Nails, Makeup, Spa)
- Healthcare (Medical, Dental, Mental Health)
- Fitness (Personal Training, Classes)
- Consulting (Business, Legal, Financial)
- Home Services (Cleaning, Repairs)

**Service Lifecycle:**
```
Draft → Active ↔ Inactive → Archived
```

#### 3. Staff
Team members who can deliver services for a provider.

**Key Concepts:**
- **Qualified Staff**: Staff members trained to deliver specific services
- **Staff Schedules**: Working hours, breaks, holidays
- **Staff Roles**: Regular staff, managers, administrators

#### 4. Bookings (Appointments)
Customer requests to receive a service at a specific time.

**Booking Workflow:**
```
Requested → Confirmed → InProgress → Completed
         ↓
    Cancelled / Rescheduled / NoShow
```

#### 5. Booking Policies
Rules governing how bookings can be made, cancelled, and rescheduled:
- Minimum/maximum advance booking windows
- Cancellation windows and fees
- Rescheduling permissions
- Deposit requirements

#### 6. Payments & Financial Management **[NEW]**
Comprehensive payment processing and provider payout system.

**Payment Lifecycle:**
```
Pending → Processing → Paid → (Refunded/PartiallyRefunded)
                    ↓
                 Failed
```

**Payment Methods:**
- CreditCard (primary)
- BankTransfer
- Wallet (digital wallets)
- Cash
- Other

**Key Concepts:**
- **Payment Processing**: Charge customers for bookings
- **Refund Processing**: Handle cancellations with policy-based refunds
- **Commission Calculation**: Platform fees (percentage, fixed, or mixed)
- **Tax Calculation**: Support for inclusive/exclusive tax rates
- **Payout Management**: Automated provider payments
- **Financial Reporting**: Provider earnings, platform revenue

**Payout Lifecycle:**
```
Pending → Scheduled → Processing → Paid
                  ↓              ↓
              Cancelled      Failed/OnHold
```

**Refund Policies:**
- **Flexible**: Full refund >24h, 50% <24h
- **Moderate**: Full refund >48h, 50% 24-48h, 25% <24h
- **Strict**: Full refund >72h, 50% <72h, no refund <24h
- **NoRefunds**: No refunds allowed
- **Custom**: Configurable windows and percentages

---

## Architecture Overview

### Design Principles

Booksy follows **Clean Architecture** and **Domain-Driven Design (DDD)**:

1. **Bounded Contexts**: Separate business domains with clear boundaries
2. **Aggregate Roots**: Transaction consistency boundaries
3. **CQRS**: Command-Query separation for scalability
4. **Domain Events**: Asynchronous cross-context communication
5. **Repository Pattern**: Data access abstraction
6. **Dependency Inversion**: Core domain independent of infrastructure

### Bounded Contexts

#### ServiceCatalog Context (Primary)
**Location**: `/src/BoundedContexts/ServiceCatalog/`

**Responsibilities:**
- Provider registration and profile management
- Service catalog management (CRUD, search, filtering)
- Staff management (scheduling, availability)
- Business hours and holiday management
- **Booking management** (newly added)
- **Availability management** (newly added)

**Aggregates:**
- `Provider` (Aggregate Root) - Manages providers, staff, business hours
- `Service` (Aggregate Root) - Manages service offerings, pricing, policies
- `Booking` (Aggregate Root) - Manages customer appointments **[NEW]**

**Layers:**
```
Booksy.ServiceCatalog.Api/              # Controllers, Middleware, Configuration
Booksy.ServiceCatalog.Application/      # Commands, Queries, Handlers, DTOs
Booksy.ServiceCatalog.Domain/           # Entities, Value Objects, Events
Booksy.ServiceCatalog.Infrastructure/   # DbContext, Repositories, External Services
```

#### UserManagement Context
**Location**: `/src/UserManagement/`

**Responsibilities:**
- User authentication (email/password, phone verification)
- JWT token generation and validation
- Role-based access control (Customer, Provider, Admin)
- Refresh token rotation
- Cross-context provider claims

**Aggregates:**
- `User` (Aggregate Root) - User identity, roles, verification

#### Future Contexts (Planned)
- **Payment Context**: Payment processing, transactions, invoicing
- **Notification Context**: Email, SMS, push notifications
- **Reviews Context**: Ratings, reviews, feedback

### Cross-Context Integration

**Current Implementation:**
- REST API integration between contexts
- JWT tokens carry provider claims (`providerId`, `provider_status`)
- UserManagement queries ServiceCatalog for provider info
- Eventual consistency through domain events

**Planned:**
- Message broker integration (RabbitMQ/Azure Service Bus)
- Saga pattern for distributed transactions
- Event sourcing for audit trails

### Technology Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    Frontend (Vue 3)                     │
│          Provider Portal | Customer Portal (planned)     │
└─────────────────────┬───────────────────────────────────┘
                      │ HTTP/REST
┌─────────────────────▼───────────────────────────────────┐
│              API Gateway (YARP) [Planned]               │
└─────┬───────────────────────────────────────┬───────────┘
      │                                       │
┌─────▼──────────────┐            ┌───────────▼──────────┐
│ ServiceCatalog API │            │ UserManagement API   │
│    Port: 7002      │            │    Port: 7001        │
└─────┬──────────────┘            └───────────┬──────────┘
      │                                       │
┌─────▼──────────────┐            ┌───────────▼──────────┐
│   PostgreSQL DB    │            │   PostgreSQL DB      │
│ ServiceCatalog     │            │ UserManagement       │
└────────────────────┘            └──────────────────────┘
      │                                       │
      └───────────────┬───────────────────────┘
                      │
            ┌─────────▼─────────┐
            │  Message Broker   │
            │ (RabbitMQ/Redis)  │
            │    [Planned]      │
            └───────────────────┘
```

---

## Domain Model Deep Dive

### ServiceCatalog Domain

#### Provider Aggregate

**File**: `/src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/Aggregates/ProviderAggregate/Provider.cs`

**Core Properties:**
```csharp
public sealed class Provider : AggregateRoot<ProviderId>
{
    // Identity
    public UserId OwnerId { get; private set; }
    public BusinessProfile Profile { get; private set; }

    // Status & Type
    public ProviderStatus Status { get; private set; }
    public ProviderType ProviderType { get; private set; }

    // Contact & Location
    public ContactInfo ContactInfo { get; private set; }
    public BusinessAddress Address { get; private set; }

    // Settings
    public bool RequiresApproval { get; private set; }
    public bool AllowOnlineBooking { get; private set; }
    public bool OffersMobileServices { get; private set; }

    // Collections
    public IReadOnlyList<Staff> Staff { get; }
    public IReadOnlyList<Service> Services { get; }
    public IReadOnlyList<BusinessHours> BusinessHours { get; }
    public IReadOnlyList<HolidaySchedule> Holidays { get; }
    public IReadOnlyList<ExceptionSchedule> Exceptions { get; }
}
```

**Key Business Methods:**
- `CreateDraft()` - Progressive registration (draft status)
- `CreateFullyRegistered()` - Complete registration (pending verification)
- `CompleteRegistration()` - Mark registration as complete
- `Verify()` - Verify provider (admin action)
- `Activate()` - Activate provider for online booking
- `Deactivate()` - Suspend provider operations
- `UpdateBusinessInfo()`, `UpdateLocation()`, `UpdateContactInfo()`
- `AddStaff()`, `RemoveStaff()`, `UpdateStaff()`
- `SetBusinessHours()`, `AddHoliday()`, `AddException()`

**Domain Events:**
- `ProviderRegisteredEvent`
- `ProviderVerifiedEvent`
- `ProviderActivatedEvent`
- `ProviderDeactivatedEvent`
- `StaffAddedEvent`
- `StaffRemovedEvent`

#### Service Aggregate

**File**: `/src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/Aggregates/ServiceAggregate/Service.cs`

**Core Properties:**
```csharp
public sealed class Service : AggregateRoot<ServiceId>
{
    // Identity
    public ProviderId ProviderId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }

    // Categorization
    public ServiceCategory Category { get; private set; }
    public ServiceType Type { get; private set; }

    // Pricing & Duration
    public Price BasePrice { get; private set; }
    public Duration Duration { get; private set; }
    public Duration? PreparationTime { get; private set; }
    public Duration? BufferTime { get; private set; }

    // Status & Settings
    public ServiceStatus Status { get; private set; }
    public bool RequiresDeposit { get; private set; }
    public decimal DepositPercentage { get; private set; }
    public bool AllowOnlineBooking { get; private set; }

    // Booking Rules
    public int? MaxAdvanceBookingDays { get; private set; }
    public int? MinAdvanceBookingHours { get; private set; }
    public int? MaxConcurrentBookings { get; private set; }
    public BookingPolicy? BookingPolicy { get; private set; } // NEW: Comprehensive policy

    // Collections
    public IReadOnlyList<ServiceOption> Options { get; }
    public IReadOnlyList<PriceTier> PriceTiers { get; }
    public IReadOnlyList<Guid> QualifiedStaff { get; }
}
```

**Key Business Methods:**
- `Create()` - Factory method for new services
- `UpdateBasicInfo()`, `UpdatePricing()`, `UpdateDuration()`
- `Activate()`, `Deactivate()`, `Archive()`
- `AddOption()`, `RemoveOption()` - Service add-ons
- `AddPriceTier()`, `RemovePriceTier()` - Tiered pricing
- `AddQualifiedStaff()`, `RemoveQualifiedStaff()` - Staff assignments
- `SetBookingPolicy()` - **NEW**: Set comprehensive booking rules
- `EnableDeposit()`, `DisableDeposit()`
- `CanBeBooked()` - Validation query
- `GetTotalDuration()` - Including prep/buffer time
- `CalculateDepositAmount()`

**Domain Events:**
- `ServiceCreatedEvent`
- `ServiceActivatedEvent`
- `ServiceDeactivatedEvent`
- `ServiceUpdatedEvent`

#### Booking Aggregate **[NEW]**

**File**: `/src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/Aggregates/BookingAggregate/Booking.cs`

**Core Properties:**
```csharp
public sealed class Booking : AggregateRoot<BookingId>
{
    // Identity
    public UserId CustomerId { get; private set; }
    public ProviderId ProviderId { get; private set; }
    public ServiceId ServiceId { get; private set; }
    public Guid StaffId { get; private set; }

    // Booking Details
    public TimeSlot TimeSlot { get; private set; }
    public Duration Duration { get; private set; }
    public BookingStatus Status { get; private set; }

    // Pricing & Payment
    public Price TotalPrice { get; private set; }
    public PaymentInfo PaymentInfo { get; private set; }

    // Policy & Rules
    public BookingPolicy Policy { get; private set; }

    // Additional Information
    public string? CustomerNotes { get; private set; }
    public string? StaffNotes { get; private set; }
    public string? CancellationReason { get; private set; }

    // Timestamps
    public DateTime RequestedAt { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? RescheduledAt { get; private set; }

    // Rescheduling Chain
    public BookingId? PreviousBookingId { get; private set; }
    public BookingId? RescheduledToBookingId { get; private set; }

    // Collections
    public IReadOnlyList<BookingHistoryEntry> History { get; }
}
```

**Key Business Methods:**
- `CreateBookingRequest()` - Factory method
- `Confirm()` - Confirm after validation/payment
- `Cancel()` - Cancel with optional fee
- `Complete()` - Mark booking as completed
- `MarkAsNoShow()` - Customer didn't show up
- `MarkAsInProgress()` - Service started
- `Reschedule()` - Create new booking and link
- `RecordPaymentIntent()`, `ConfirmDeposit()`, `ConfirmFullPayment()`
- `ProcessRefund()`

**Business Rules (enforced via `IBusinessRule`):**
- `BookingCanOnlyBeConfirmedFromRequestedStateRule`
- `DepositMustBePaidBeforeConfirmationRule`
- `BookingMustBeWithinValidTimeWindowRule`
- `CancellationMustBeWithinPolicyWindowRule`
- `OnlyConfirmedBookingsCanBeCompletedRule`
- `BookingCannotBeRescheduledIfStartedRule`

**Domain Events:**
- `BookingRequestedEvent`
- `BookingConfirmedEvent`
- `BookingCancelledEvent`
- `BookingCompletedEvent`
- `BookingRescheduledEvent`
- `BookingNoShowEvent`

#### Payment Aggregate **[NEW]**

**File**: `/src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/Aggregates/PaymentAggregate/Payment.cs`

**Core Properties:**
```csharp
public sealed class Payment : AggregateRoot<PaymentId>
{
    // Identity & References
    public BookingId? BookingId { get; private set; }
    public UserId CustomerId { get; private set; }
    public ProviderId ProviderId { get; private set; }

    // Amounts
    public Money Amount { get; private set; }
    public Money PaidAmount { get; private set; }
    public Money RefundedAmount { get; private set; }

    // Status & Method
    public PaymentStatus Status { get; private set; }
    public PaymentMethod Method { get; private set; }

    // Gateway Integration
    public string? PaymentIntentId { get; private set; }
    public string? PaymentMethodId { get; private set; }
    public string? Description { get; private set; }
    public string? FailureReason { get; private set; }

    // Timestamps
    public DateTime CreatedAt { get; private set; }
    public DateTime? AuthorizedAt { get; private set; }
    public DateTime? CapturedAt { get; private set; }
    public DateTime? RefundedAt { get; private set; }
    public DateTime? FailedAt { get; private set; }

    // Additional Data
    public Dictionary<string, object> Metadata { get; private set; }

    // Collections
    public IReadOnlyList<Transaction> Transactions { get; }
}
```

**Key Business Methods:**
- `CreateForBooking()` - Create payment for booking
- `CreateDirect()` - Create direct payment (no booking)
- `ProcessCharge()` - Process immediate charge
- `Authorize()` - Authorize funds (hold)
- `Capture()` - Capture authorized payment
- `PartialCapture()` - Capture partial amount
- `Refund()` - Full refund
- `PartialRefund()` - Partial refund
- `MarkAsFailed()` - Mark as failed with reason
- `AddTransaction()` - Record transaction

**Domain Events:**
- `PaymentCreatedEvent`
- `PaymentAuthorizedEvent`
- `PaymentCapturedEvent`
- `PaymentRefundedEvent`
- `PaymentFailedEvent`

#### Payout Aggregate **[NEW]**

**File**: `/src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/Aggregates/PayoutAggregate/Payout.cs`

**Core Properties:**
```csharp
public sealed class Payout : AggregateRoot<PayoutId>
{
    // Identity
    public ProviderId ProviderId { get; private set; }

    // Amounts
    public Money GrossAmount { get; private set; }
    public Money CommissionAmount { get; private set; }
    public Money NetAmount { get; private set; }

    // Period
    public DateTime PeriodStart { get; private set; }
    public DateTime PeriodEnd { get; private set; }

    // Status
    public PayoutStatus Status { get; private set; }
    public DateTime? ScheduledAt { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public string? FailureReason { get; private set; }

    // Gateway Integration
    public string? ExternalPayoutId { get; private set; }
    public DateTime? ArrivalDate { get; private set; }

    // Collections
    public IReadOnlyList<PaymentId> PaymentIds { get; }
}
```

**Key Business Methods:**
- `Create()` - Create pending payout
- `Schedule()` - Schedule for execution
- `MarkAsProcessing()` - Start processing
- `MarkAsPaid()` - Mark as paid with external ID
- `MarkAsFailed()` - Mark as failed with reason
- `Cancel()` - Cancel pending payout
- `PutOnHold()` - Place on hold for review
- `ReleaseFromHold()` - Release from hold

**Domain Events:**
- `PayoutCreatedEvent`
- `PayoutScheduledEvent`
- `PayoutProcessingEvent`
- `PayoutPaidEvent`
- `PayoutFailedEvent`
- `PayoutCancelledEvent`

#### Value Objects

**Core Value Objects** (`/src/Core/Booksy.Core.Domain/ValueObjects/`):
- `Money` - Amount + Currency (3-letter code)
- `Price` - Alias for Money in service pricing
- `Duration` - Minutes with validation
- `Email` - Validated email address
- `PhoneNumber` - Validated phone with country code

**ServiceCatalog Value Objects** (`/Domain/ValueObjects/`):
- `ProviderId`, `ServiceId`, `BookingId`, `UserId` - Strongly-typed IDs
- `BusinessProfile` - Business name, description, image
- `ContactInfo` - Phone, email, website
- `BusinessAddress` - Street, city, state, country, postal code, coordinates
- `ServiceCategory` - Name, description, icon
- `TimeSlot` - StartTime, EndTime with validation
- `BookingPolicy` **[NEW]** - Comprehensive booking rules:
  ```csharp
  public sealed class BookingPolicy : ValueObject
  {
      public int MinAdvanceBookingHours { get; }
      public int MaxAdvanceBookingDays { get; }
      public int CancellationWindowHours { get; }
      public decimal CancellationFeePercentage { get; }
      public bool AllowRescheduling { get; }
      public int RescheduleWindowHours { get; }
      public bool RequireDeposit { get; }
      public decimal DepositPercentage { get; }

      // Static presets
      public static BookingPolicy Default { get; }
      public static BookingPolicy Flexible { get; }
      public static BookingPolicy Strict { get; }

      // Query methods
      public bool CanCancelWithoutFee(DateTime bookingTime, DateTime currentTime)
      public bool CanReschedule(DateTime bookingTime, DateTime currentTime)
      public bool IsWithinBookingWindow(DateTime bookingTime, DateTime currentTime)
      public Money CalculateDepositAmount(Money totalPrice)
      public Money CalculateCancellationFee(Money totalPrice)
  }
  ```
- `PaymentInfo` - Total, deposit, paid amounts, status, payment intent IDs

**Financial Value Objects** (`/Domain/ValueObjects/`) **[NEW]**:
- `PaymentId`, `PayoutId` - Strongly-typed IDs
- `CommissionRate` - Platform commission configuration:
  ```csharp
  public sealed class CommissionRate : ValueObject
  {
      public CommissionType Type { get; }  // Percentage, Fixed, Mixed
      public decimal PercentageRate { get; }
      public Money? FixedAmount { get; }

      // Static factory methods
      public static CommissionRate Percentage(decimal rate)
      public static CommissionRate Fixed(Money amount)
      public static CommissionRate Mixed(decimal percentage, Money fixedAmount)

      // Calculation
      public Money CalculateCommission(Money amount)
  }
  ```
- `TaxRate` - Tax calculation support:
  ```csharp
  public sealed class TaxRate : ValueObject
  {
      public decimal Percentage { get; }
      public string TaxName { get; }
      public string TaxCode { get; }
      public bool IsInclusive { get; }

      // Calculation methods
      public Money CalculateTaxAmount(Money amount)
      public Money CalculateTotalWithTax(Money baseAmount)
      public Money CalculateBaseAmount(Money totalAmount)
  }
  ```
- `RefundPolicy` - Refund calculation rules:
  ```csharp
  public sealed class RefundPolicy : ValueObject
  {
      public bool AllowRefunds { get; }
      public int FullRefundWindowHours { get; }
      public int PartialRefundWindowHours { get; }
      public decimal PartialRefundPercentage { get; }
      public decimal CancellationFeePercentage { get; }
      public bool RefundProcessingFees { get; }

      // Static presets
      public static RefundPolicy Flexible { get; }
      public static RefundPolicy Moderate { get; }
      public static RefundPolicy Strict { get; }
      public static RefundPolicy NoRefunds { get; }

      // Calculation
      public Money CalculateRefundAmount(Money paidAmount, DateTime bookingTime, DateTime currentTime)
  }
  ```

#### Enums

**Provider Domain:**
- `ProviderStatus`: Drafted, PendingVerification, Verified, Active, Suspended, Deactivated
- `ProviderType`: Individual, Business

**Service Domain:**
- `ServiceStatus`: Draft, Active, Inactive, Archived
- `ServiceType`: OnSite, AtHome, Online, Hybrid

**Booking Domain:**
- `BookingStatus`: Requested, Confirmed, InProgress, Completed, Cancelled, Rescheduled, NoShow
- `PaymentStatus`: Pending, DepositPaid, FullyPaid, PartiallyRefunded, FullyRefunded, Failed

**Payment & Financial Domain** **[NEW]**:
- `PaymentMethod`: CreditCard, BankTransfer, Wallet, Cash, Other
- `PaymentStatus`: Pending, PartiallyPaid, Paid, Failed, Refunded, PartiallyRefunded
- `PayoutStatus`: Pending, Scheduled, Processing, Paid, Failed, Cancelled, OnHold
- `CommissionType`: Percentage, Fixed, Mixed
- `RefundReason`: CustomerCancellation, ProviderCancellation, ServiceNotDelivered, Dispute, Other

---

## API Architecture

### ServiceCatalog API

**Base URL**: `https://localhost:7002/api/v1`
**API Versioning**: URL-based (`/api/v{version}/`)
**Authentication**: JWT Bearer tokens

#### Provider Endpoints

**Registration:**
```http
POST   /api/v1/providers/register            # Progressive registration (draft)
POST   /api/v1/providers/register-full       # Complete registration
POST   /api/v1/providers/{id}/complete       # Complete progressive registration
```

**Management:**
```http
GET    /api/v1/providers/{id}                # Get provider by ID
GET    /api/v1/providers/by-owner/{ownerId}  # Get provider by owner
GET    /api/v1/providers/search              # Search with filters
POST   /api/v1/providers/{id}/activate       # Activate (Admin)
POST   /api/v1/providers/{id}/deactivate     # Deactivate (Admin)
PUT    /api/v1/providers/{id}/business-info  # Update business profile
PUT    /api/v1/providers/{id}/location       # Update address
PUT    /api/v1/providers/{id}/contact        # Update contact info
PUT    /api/v1/providers/{id}/settings       # Update settings
PUT    /api/v1/providers/{id}/working-hours  # Set business hours
```

**Staff Management:**
```http
GET    /api/v1/providers/{id}/staff          # Get all staff
POST   /api/v1/providers/{id}/staff          # Add staff member
PUT    /api/v1/providers/{id}/staff/{staffId} # Update staff
DELETE /api/v1/providers/{id}/staff/{staffId} # Remove staff
POST   /api/v1/providers/{id}/staff/{staffId}/activate   # Activate staff
POST   /api/v1/providers/{id}/staff/{staffId}/deactivate # Deactivate staff
```

#### Service Endpoints

**Service Management:**
```http
GET    /api/v1/services/{id}                 # Get service details
GET    /api/v1/services/search               # Search services
GET    /api/v1/services/provider/{providerId} # Get provider services
POST   /api/v1/services                      # Create service
PUT    /api/v1/services/{id}                 # Update service
DELETE /api/v1/services/{id}                 # Delete service
POST   /api/v1/services/{id}/activate        # Activate service
POST   /api/v1/services/{id}/deactivate      # Deactivate service
POST   /api/v1/services/bulk-activate        # Bulk activate
POST   /api/v1/services/bulk-deactivate      # Bulk deactivate
POST   /api/v1/services/bulk-delete          # Bulk delete
```

#### Booking Endpoints **[NEW]**

**File**: `/Api/Controllers/BookingsController.cs`

**Booking Management:**
```http
POST   /api/v1/bookings                      # Create booking request
       [Authorize]
       Body: CreateBookingRequest
       Response: 201 Created with BookingResponse

GET    /api/v1/bookings/{id}                 # Get booking details
       [Authorize]
       Response: BookingDetailsResponse

GET    /api/v1/bookings/my-bookings          # Get customer's bookings
       [Authorize]
       Query: status, startDate, endDate, page, pageSize
       Response: PagedList<BookingResponse>

GET    /api/v1/bookings/provider/{providerId} # Get provider bookings
       [Authorize(Policy = "ProviderOrAdmin")]
       Query: status, staffId, startDate, endDate, page, pageSize
       Response: PagedList<BookingResponse>
```

**Booking Actions:**
```http
POST   /api/v1/bookings/{id}/confirm         # Confirm booking
       [Authorize(Policy = "ProviderOrAdmin")]
       Body: ConfirmBookingRequest { PaymentIntentId, StaffNotes }
       Response: 200 OK with BookingResponse

POST   /api/v1/bookings/{id}/cancel          # Cancel booking
       [Authorize]
       Body: CancelBookingRequest { Reason }
       Response: 200 OK with BookingResponse

POST   /api/v1/bookings/{id}/reschedule      # Reschedule booking
       [Authorize]
       Body: RescheduleBookingRequest { NewStartTime, NewStaffId, Reason }
       Response: 200 OK with RescheduleBookingResult

POST   /api/v1/bookings/{id}/complete        # Mark as completed
       [Authorize(Policy = "ProviderOrAdmin")]
       Body: CompleteBookingRequest { StaffNotes, ActualEndTime }
       Response: 200 OK with BookingResponse

POST   /api/v1/bookings/{id}/no-show         # Mark as no-show
       [Authorize(Policy = "ProviderOrAdmin")]
       Body: MarkNoShowRequest { Notes }
       Response: 200 OK with BookingResponse
```

#### Availability Endpoints **[NEW]**

**File**: `/Api/Controllers/AvailabilityController.cs`

```http
GET    /api/v1/availability/slots            # Get available time slots
       [AllowAnonymous]
       Query: providerId, serviceId, staffId, date, durationMinutes, count
       Response: List<AvailableSlotResponse>

GET    /api/v1/availability/check            # Check if slot is available
       [AllowAnonymous]
       Query: providerId, serviceId, staffId, startTime
       Response: { IsAvailable: bool, Reason: string }

GET    /api/v1/availability/dates            # Get available dates in range
       [AllowAnonymous]
       Query: providerId, serviceId, staffId, startDate, endDate
       Response: List<DateTime>
```

#### Payment Endpoints **[NEW]**

**File**: `/Api/Controllers/V1/PaymentsController.cs`

**Payment Processing:**
```http
POST   /api/v1/payments                      # Process payment
       [Authorize]
       Body: ProcessPaymentRequest { BookingId?, Amount, Currency, PaymentMethod, PaymentMethodId, Description, Metadata }
       Response: 201 Created with PaymentResponse

POST   /api/v1/payments/{id}/capture         # Capture authorized payment
       [Authorize(Policy = "ProviderOrAdmin")]
       Body: CapturePaymentRequest { AmountToCapture? }
       Response: 200 OK with PaymentResponse

POST   /api/v1/payments/{id}/refund          # Refund payment
       [Authorize]
       Body: RefundPaymentRequest { RefundAmount?, Reason, Notes }
       Response: 200 OK with RefundPaymentResponse

GET    /api/v1/payments/{id}                 # Get payment details
       [Authorize]
       Response: PaymentDetailsResponse

GET    /api/v1/payments/customer/{customerId} # Get customer payments
       [Authorize]
       Query: status, startDate, endDate, page, pageSize
       Response: PagedList<PaymentResponse>

POST   /api/v1/payments/calculate-pricing    # Calculate pricing with tax/commission
       [AllowAnonymous]
       Body: CalculatePricingRequest { Amount, Currency, ProviderId?, IncludeTax, IncludeCommission }
       Response: PricingCalculationResponse
```

#### Payout Endpoints **[NEW]**

**File**: `/Api/Controllers/V1/PayoutsController.cs`

**Payout Management:**
```http
POST   /api/v1/payouts                       # Create payout for provider
       [Authorize(Policy = "AdminOrFinance")]
       Body: CreatePayoutRequest { ProviderId, PeriodStart, PeriodEnd }
       Response: 201 Created with PayoutResponse

POST   /api/v1/payouts/{id}/execute          # Execute pending payout
       [Authorize(Policy = "AdminOrFinance")]
       Body: ExecutePayoutRequest { ConnectedAccountId? }
       Response: 200 OK with PayoutResponse

GET    /api/v1/payouts/{id}                  # Get payout details
       [Authorize]
       Response: PayoutDetailsResponse

GET    /api/v1/payouts/provider/{providerId} # Get provider payouts
       [Authorize(Policy = "ProviderOrAdmin")]
       Query: status, startDate, endDate, page, pageSize
       Response: PagedList<PayoutResponse>

GET    /api/v1/payouts/pending               # Get all pending payouts
       [Authorize(Policy = "AdminOrFinance")]
       Response: List<PayoutResponse>
```

#### Financial Reporting Endpoints **[NEW]**

**File**: `/Api/Controllers/V1/FinancialController.cs`

**Financial Reports:**
```http
GET    /api/v1/financial/provider-earnings/{providerId} # Provider earnings report
       [Authorize(Policy = "ProviderOrAdmin")]
       Query: startDate, endDate
       Response: ProviderEarningsResponse { GrossEarnings, NetEarnings, CommissionPaid, TaxPaid, PayoutsPaid }

GET    /api/v1/financial/provider-earnings/{providerId}/current-month # Current month earnings
       [Authorize(Policy = "ProviderOrAdmin")]
       Response: ProviderEarningsResponse

GET    /api/v1/financial/provider-earnings/{providerId}/previous-month # Previous month earnings
       [Authorize(Policy = "ProviderOrAdmin")]
       Response: ProviderEarningsResponse
```

### CQRS Implementation

**Commands** (`/Application/Commands/`):
- Represent state-changing operations
- Validated with FluentValidation
- Handled by CommandHandlers
- Return Result objects (success/failure)
- Raise domain events

**Example Command Flow:**
```
CreateBookingCommand
  ↓
CreateBookingCommandValidator (FluentValidation)
  ↓
CreateBookingCommandHandler
  ↓ (validates business rules)
  ↓ (calls domain methods)
  ↓ (saves to repository)
  ↓ (publishes domain events)
  ↓
CreateBookingResult
```

**Queries** (`/Application/Queries/`):
- Read-only operations
- No side effects
- Can query read models directly
- Can bypass domain layer for performance

**Request/Response DTOs** (`/Api/Models/`):
- Request models: Input validation, binding
- Response models: Shaped for client consumption
- ViewModels: Read-optimized projections

---

## Data Architecture

### Database Schema

**Database**: PostgreSQL 15+ (production), SQL Server (development)
**Schema**: `ServiceCatalog` schema for all tables
**Migrations**: EF Core Code-First migrations

#### Provider Tables

**Providers** (`ServiceCatalog.Providers`):
```sql
PRIMARY KEY: Id (Guid)
FOREIGN KEY: OwnerId → Users.Id

Key Columns:
- OwnerId, Status, ProviderType
- BusinessName, Description, ProfileImageUrl
- Email, PhoneNumber, Website
- AddressLine1, AddressLine2, City, StateProvince, PostalCode, Country
- Latitude, Longitude
- RequiresApproval, AllowOnlineBooking, OffersMobileServices
- RegisteredAt, ActivatedAt, VerifiedAt, LastActiveAt
- AverageRating (computed)

Indexes:
- IX_Providers_OwnerId
- IX_Providers_Status
- IX_Providers_Status_AllowOnlineBooking
```

**Staff** (`ServiceCatalog.Staff`):
```sql
PRIMARY KEY: Id (Guid)
FOREIGN KEY: ProviderId → Providers.Id

Key Columns:
- ProviderId, FirstName, LastName, Email, PhoneNumber
- Role, Status, IsManager
- Bio, ProfileImageUrl
- SpecialtyTags (jsonb)

Indexes:
- IX_Staff_ProviderId
- IX_Staff_Status
```

**StaffSchedules** (`ServiceCatalog.StaffSchedules`):
```sql
PRIMARY KEY: Id (Guid)
FOREIGN KEY: StaffId → Staff.Id

Key Columns:
- StaffId, DayOfWeek
- StartTime, EndTime
- IsActive

Indexes:
- IX_StaffSchedules_StaffId_DayOfWeek
```

#### Service Tables

**Services** (`ServiceCatalog.Services`):
```sql
PRIMARY KEY: Id (Guid)
FOREIGN KEY: ProviderId → Providers.Id

Key Columns:
- ProviderId, Name, Description, Type, Status
- CategoryName, CategoryDescription, CategoryIconUrl
- BasePriceAmount, BasePriceCurrency
- DurationMinutes, PreparationTimeMinutes, BufferTimeMinutes
- RequiresDeposit, DepositPercentage
- AllowOnlineBooking, AvailableAtLocation, AvailableAsMobile
- MaxAdvanceBookingDays, MinAdvanceBookingHours, MaxConcurrentBookings
- BookingPolicyMinAdvanceBookingHours, BookingPolicyMaxAdvanceBookingDays
- BookingPolicyCancellationWindowHours, BookingPolicyCancellationFeePercentage
- BookingPolicyAllowRescheduling, BookingPolicyRescheduleWindowHours
- BookingPolicyRequireDeposit, BookingPolicyDepositPercentage
- QualifiedStaff (jsonb array)
- Metadata (jsonb)
- ImageUrl
- CreatedAt, ActivatedAt

Indexes:
- IX_Services_ProviderId
- IX_Services_Status
- IX_Services_Type
- IX_Services_ProviderId_Name (UNIQUE)
- IX_Services_Status_AllowOnlineBooking
```

**ServiceOptions** (`ServiceCatalog.ServiceOptions`):
```sql
PRIMARY KEY: Id (Guid)
FOREIGN KEY: ServiceId → Services.Id

Key Columns:
- ServiceId, Name, Description
- AdditionalPriceAmount, AdditionalPriceCurrency
- AdditionalDurationMinutes
- IsActive, SortOrder

Indexes:
- IX_ServiceOptions_ServiceId
```

**ServicePriceTiers** (`ServiceCatalog.ServicePriceTiers`):
```sql
PRIMARY KEY: Id (Guid)
FOREIGN KEY: ServiceId → Services.Id (owned entity)

Key Columns:
- ServiceId, Name, Description
- Price, Currency
- IsDefault, IsActive, SortOrder
- Attributes (jsonb)

Indexes:
- IX_ServicePriceTiers_ServiceId_IsActive
```

#### Booking Tables **[NEW]**

**Bookings** (`ServiceCatalog.Bookings`):
```sql
PRIMARY KEY: BookingId (Guid)
FOREIGN KEYS:
- CustomerId → Users.Id
- ProviderId → Providers.Id
- ServiceId → Services.Id
- StaffId → Staff.Id

Key Columns:
- BookingId, CustomerId, ProviderId, ServiceId, StaffId
- StartTime, EndTime, DurationMinutes
- Status
- TotalPriceAmount, TotalPriceCurrency
- PaymentTotalAmount, PaymentCurrency
- DepositAmount, DepositCurrency
- PaidAmount, PaidCurrency
- RefundedAmount, RefundedCurrency
- PaymentStatus, PaymentIntentId, DepositPaymentIntentId, RefundId
- PaidAt, RefundedAt
- PolicyMinAdvanceBookingHours, PolicyMaxAdvanceBookingDays
- PolicyCancellationWindowHours, PolicyCancellationFeePercentage
- PolicyAllowRescheduling, PolicyRescheduleWindowHours
- PolicyRequireDeposit, PolicyDepositPercentage
- CustomerNotes, StaffNotes, CancellationReason
- RequestedAt, ConfirmedAt, CancelledAt, CompletedAt, RescheduledAt
- PreviousBookingId, RescheduledToBookingId
- Version (concurrency token)

Indexes:
- IX_Bookings_CustomerId
- IX_Bookings_ProviderId
- IX_Bookings_ServiceId
- IX_Bookings_StaffId
- IX_Bookings_Status
- IX_Bookings_StaffId_Status
- IX_Bookings_Availability (filtered: Status IN ('Requested', 'Confirmed'))
```

**BookingHistory** (`ServiceCatalog.BookingHistory`):
```sql
PRIMARY KEY: Id (Guid)
FOREIGN KEY: BookingId → Bookings.BookingId

Key Columns:
- BookingId, Description, Status, OccurredAt

No explicit indexes (owned collection)
```

### EF Core Configuration

**Design-Time DbContext Factory** **[NEW]**:
```csharp
// File: /Infrastructure/Persistence/ServiceCatalogDbContextFactory.cs
public class ServiceCatalogDbContextFactory : IDesignTimeDbContextFactory<ServiceCatalogDbContext>
{
    // Resolves dependency injection issues for migrations
    // Provides mock implementations of ICurrentUserService and IDateTimeProvider
}
```

**Key Configuration Patterns:**

**Owned Entities** (`.OwnsOne()`, `.OwnsMany()`):
- `Service.Category` - Flattened to Service table
- `Service.BasePrice` - Flattened to Service table
- `Service.PriceTiers` - Separate table with foreign key
- `Service.BookingPolicy` - Flattened to Service table **[FIXED]**
- `Booking.TimeSlot`, `Booking.TotalPrice`, `Booking.PaymentInfo`, `Booking.Policy` - Flattened
- `Booking.History` - Separate table

**Value Object Conversions** (`.HasConversion()`):
- Strongly-typed IDs (ProviderId, ServiceId, etc.) → Guid
- Duration → int (minutes)
- Enums → string (for readability)

**JSON Columns** (`.HasColumnType("jsonb")`):
- Metadata dictionaries
- Staff specialties
- Price tier attributes
- QualifiedStaff list

**Backing Fields** (`.HasField("_fieldName")`):
- Collections in aggregates (Staff, Services, etc.)
- Encapsulation of domain invariants

### Migrations

**Migration Commands:**
```bash
# Add new migration
dotnet ef migrations add MigrationName \
  --project src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure

# Update database
dotnet ef database update \
  --project src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure

# Generate SQL script
dotnet ef migrations script \
  --project src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure \
  --output migration.sql
```

**Migration Status:**
- ✅ Provider aggregate migrations applied
- ✅ Service aggregate migrations applied
- 🚧 **Booking aggregate migration pending** (ready to run after recent fixes)

---

## Testing Strategy

### Test Projects

**ServiceCatalog Integration Tests**:
- **Location**: `/tests/Booksy.ServiceCatalog.IntegrationTests/`
- **Framework**: xUnit, FluentAssertions, WebApplicationFactory
- **Coverage**: API endpoints, CQRS handlers, database interactions

**UserManagement Tests**:
- **Location**: `/tests/Booksy.UserManagement.Tests/`
- **Coverage**: Authentication, user management

### Integration Test Infrastructure

**Base Test Class**:
```csharp
public abstract class BaseIntegrationTest : IAsyncLifetime
{
    protected HttpClient Client { get; }
    protected WebApplicationFactory<Program> Factory { get; }
    protected ServiceCatalogDbContext DbContext { get; }

    // Helper methods
    protected void AuthenticateAsUser(Guid userId, string email, string role = "Customer")
    protected void AuthenticateAsProvider(Guid providerId, Guid userId)
    protected Task<T> GetAsync<T>(string url)
    protected Task<HttpResponseMessage> PostAsJsonAsync<TRequest>(string url, TRequest data)
    protected Task<TResponse> PostAsJsonAsync<TRequest, TResponse>(string url, TRequest data)
    // ... more helpers
}
```

**Test Database**:
- In-memory SQLite database
- Created fresh for each test
- Automatic migrations applied
- Cleaned up after test

### Test Coverage

#### Provider Tests **[Existing]**
- ✅ Provider registration (simple and full)
- ✅ Provider retrieval
- ✅ Provider search and filtering
- ✅ Provider activation/deactivation
- ✅ Business info updates
- ✅ Staff management

#### Service Tests **[Existing]**
- ✅ Service creation
- ✅ Service updates
- ✅ Service activation/deactivation
- ✅ Service search
- ✅ Bulk operations

#### Booking Tests **[NEW - 16 tests]**
**File**: `/tests/Booksy.ServiceCatalog.IntegrationTests/Bookings/BookingsControllerTests.cs`

```csharp
// Creation
✅ CreateBooking_WithValidData_ShouldReturn201Created
✅ CreateBooking_WithInvalidServiceId_ShouldReturn400BadRequest

// Retrieval
✅ GetBookingById_WithValidId_ShouldReturn200Ok
✅ GetBookingById_WithNonExistentId_ShouldReturn404NotFound

// Customer Bookings
✅ GetMyBookings_AsCustomer_ShouldReturnCustomerBookingsOnly
✅ GetMyBookings_WithStatusFilter_ShouldReturnFilteredResults

// Provider Bookings
✅ GetProviderBookings_AsProvider_ShouldReturnProviderBookingsOnly
✅ GetProviderBookings_WithStaffFilter_ShouldReturnStaffBookings
✅ GetProviderBookings_AsNonOwner_ShouldReturn403Forbidden

// Confirmation
✅ ConfirmBooking_AsProvider_ShouldReturn200Ok
✅ ConfirmBooking_AsCustomer_ShouldReturn403Forbidden

// Cancellation
✅ CancelBooking_AsCustomer_ShouldReturn200Ok
✅ CancelBooking_AsProvider_ShouldReturn200Ok

// Completion
✅ CompleteBooking_AsProvider_ShouldReturn200Ok
✅ CompleteBooking_AsCustomer_ShouldReturn403Forbidden

// No-Show
✅ MarkAsNoShow_AsProvider_ShouldReturn200Ok
```

#### Availability Tests **[NEW - 13 tests]**
**File**: `/tests/Booksy.ServiceCatalog.IntegrationTests/Availability/AvailabilityControllerTests.cs`

```csharp
// Time Slot Queries
✅ GetAvailableSlots_WithValidRequest_ShouldReturnAvailableSlots
✅ GetAvailableSlots_WithNoAvailability_ShouldReturnEmptyList
✅ GetAvailableSlots_OutsideBusinessHours_ShouldReturnEmptyList
✅ GetAvailableSlots_WithExistingBooking_ShouldExcludeBookedSlot

// Slot Availability Check
✅ CheckSlotAvailability_WithAvailableSlot_ShouldReturnTrue
✅ CheckSlotAvailability_WithBookedSlot_ShouldReturnFalse
✅ CheckSlotAvailability_OutsideBusinessHours_ShouldReturnFalse

// Date Queries
✅ GetAvailableDates_WithinRange_ShouldReturnDates
✅ GetAvailableDates_AllDaysBooked_ShouldReturnEmptyList

// Validation
✅ GetAvailableSlots_WithMissingProviderId_ShouldReturn400
✅ GetAvailableSlots_WithMissingServiceId_ShouldReturn400
✅ CheckSlotAvailability_WithInvalidData_ShouldReturn400
✅ GetAvailableDates_WithInvalidDateRange_ShouldReturn400
```

**Total Test Count**: 29 integration tests for Booking/Availability **[NEW]**

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/Booksy.ServiceCatalog.IntegrationTests/

# Run specific test class
dotnet test --filter "FullyQualifiedName~BookingsControllerTests"

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverageReportFormat=html
```

---

## Recent Changes & Fixes

### Session: 2025-11-05 - Booking System Implementation & Fixes

#### 1. Booking Domain Model (Completed ✅)
**Commits**: Multiple commits leading to merge

**What was added:**
- `Booking` aggregate root with complete lifecycle
- `BookingPolicy` value object with comprehensive rules
- `TimeSlot`, `PaymentInfo` value objects
- `BookingHistoryEntry` entity for audit trail
- Domain events for all state transitions
- Business rules enforcement via `IBusinessRule` pattern

**Key files:**
- `/Domain/Aggregates/BookingAggregate/Booking.cs`
- `/Domain/ValueObjects/BookingPolicy.cs`
- `/Domain/ValueObjects/TimeSlot.cs`
- `/Domain/ValueObjects/PaymentInfo.cs`
- `/Domain/Enums/BookingStatus.cs`
- `/Domain/Enums/PaymentStatus.cs`

#### 2. Booking Commands & Handlers (Completed ✅)

**Commands implemented:**
- `CreateBookingCommand` - Create booking request
- `ConfirmBookingCommand` - Confirm with payment
- `CancelBookingCommand` - Cancel with optional fee
- `CompleteBookingCommand` - Mark as completed
- `RescheduleBookingCommand` - **[NEW]** Reschedule to new time
- `MarkNoShowCommand` - **[NEW]** Mark customer no-show

**Key patterns:**
- FluentValidation for input validation
- Repository pattern for data access
- Domain event publishing
- Result pattern for error handling
- Idempotency support (IdempotencyKey)

**Location**: `/Application/Commands/Bookings/`

#### 3. Availability Management (Completed ✅)

**Service**: `AvailabilityService.cs`

**Responsibilities:**
- Generate available time slots based on:
  - Provider business hours
  - Staff schedules
  - Existing bookings
  - Service duration (including prep/buffer)
  - Booking policy constraints
- Check slot availability
- Find available dates in range
- Validate booking constraints

**Key methods:**
```csharp
Task<List<AvailableSlot>> GetAvailableSlotsAsync(...)
Task<bool> IsSlotAvailableAsync(...)
Task<List<DateTime>> GetAvailableDatesAsync(...)
Task<AvailabilityValidationResult> ValidateBookingConstraintsAsync(...)
```

**Location**: `/Application/Services/AvailabilityService.cs`

#### 4. Booking API Controllers (Completed ✅)

**BookingsController**: 9 endpoints
- POST `/bookings` - Create booking
- GET `/bookings/{id}` - Get booking details
- GET `/bookings/my-bookings` - Customer's bookings
- GET `/bookings/provider/{providerId}` - Provider's bookings
- POST `/bookings/{id}/confirm` - Confirm booking
- POST `/bookings/{id}/cancel` - Cancel booking
- POST `/bookings/{id}/reschedule` - Reschedule booking **[NEW]**
- POST `/bookings/{id}/complete` - Mark completed
- POST `/bookings/{id}/no-show` - Mark no-show **[NEW]**

**AvailabilityController**: 3 endpoints
- GET `/availability/slots` - Get available slots
- GET `/availability/check` - Check slot availability
- GET `/availability/dates` - Get available dates

**Location**:
- `/Api/Controllers/BookingsController.cs`
- `/Api/Controllers/AvailabilityController.cs`

#### 5. Request/Response DTOs (Completed ✅)

**Request Models** (7 files):
- `CreateBookingRequest`
- `ConfirmBookingRequest`
- `CancelBookingRequest`
- `RescheduleBookingRequest` **[NEW]**
- `CompleteBookingRequest`
- `MarkNoShowRequest` **[NEW]**
- `GetAvailableSlotsRequest`

**Response Models** (6 files):
- `BookingResponse` - Summary info
- `BookingDetailsResponse` - Full details with nested objects
- `PaymentInfoResponse`
- `BookingPolicyResponse`
- `BookingHistoryEntryResponse`
- `AvailableSlotResponse`

**Location**: `/Api/Models/Bookings/`, `/Api/Models/Availability/`

#### 6. Integration Tests (Completed ✅)

**BookingsControllerTests.cs**: 16 tests covering all endpoints
**AvailabilityControllerTests.cs**: 13 tests covering availability logic

**Test Infrastructure Improvements:**
- Helper methods for creating test providers with services
- Helper methods for creating test bookings
- Authentication setup for customer/provider roles
- Business hours configuration helpers

**Location**: `/tests/Booksy.ServiceCatalog.IntegrationTests/`

#### 7. Critical Bug Fixes

##### Fix #1: AvailabilityService Parameter Type Error
**Commit**: `4e6009d`
**File**: `AvailabilityService.cs:219`

**Problem**: After merge from master, parameter type was incorrect
```csharp
// WRONG (introduced in merge)
public async Task<AvailabilityValidationResult> ValidateBookingConstraintsAsync(
    Provider provider,
    Service service,
    DayOfWeek startTime,  // ❌ Should be DateTime
    CancellationToken cancellationToken = default)

// FIXED
public async Task<AvailabilityValidationResult> ValidateBookingConstraintsAsync(
    Provider provider,
    Service service,
    DateTime startTime,  // ✅ Correct
    CancellationToken cancellationToken = default)
```

**Impact**: Build failure, CreateBookingCommandHandler couldn't compile

##### Fix #2: Service Status Logic Inversion
**Commit**: `4e6009d`
**File**: `AvailabilityService.cs:237`

**Problem**: Logic was inverted
```csharp
// WRONG
if (service.Status == ServiceStatus.Active)  // ❌ Wrong condition
{
    errors.Add("Service is not active");
}

// FIXED
if (service.Status != ServiceStatus.Active)  // ✅ Correct
{
    errors.Add("Service is not active");
}
```

**Impact**: Active services were incorrectly rejected

##### Fix #3: TimeRange Class Missing
**Commit**: `2638a24`
**Files**: `BookingsControllerTests.cs`, `AvailabilityControllerTests.cs`

**Problem**: Tests used non-existent `Domain.ValueObjects.TimeRange` class
```csharp
// WRONG (assumed TimeRange existed)
provider.SetBusinessHours(new Domain.ValueObjects.BusinessHours(
    new Dictionary<DayOfWeek, Domain.ValueObjects.TimeRange> {
        { DayOfWeek.Monday, TimeRange.Create(TimeSpan.FromHours(9), TimeSpan.FromHours(17)) }
    }
));

// FIXED (using actual API)
provider.SetBusinessHours(new Dictionary<DayOfWeek, (TimeOnly? Open, TimeOnly? Close)>
{
    { DayOfWeek.Monday, (TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)),
                         TimeOnly.FromTimeSpan(TimeSpan.FromHours(17))) }
});
```

**Impact**: Integration tests wouldn't compile

##### Fix #4: DbContext Design-Time Creation
**Commit**: `8e2b71c`
**File**: **[NEW]** `ServiceCatalogDbContextFactory.cs`

**Problem**: EF Core migrations couldn't create DbContext at design time
```
Error: Unable to create a 'DbContext' of type 'ServiceCatalogDbContext'.
The exception 'Nullable object must have a value.'
```

**Root Cause**: `ServiceCatalogDbContext` requires `ICurrentUserService` and `IDateTimeProvider` dependencies which aren't available during `dotnet ef migrations add`

**Solution**: Created `IDesignTimeDbContextFactory<ServiceCatalogDbContext>`
```csharp
public class ServiceCatalogDbContextFactory : IDesignTimeDbContextFactory<ServiceCatalogDbContext>
{
    public ServiceCatalogDbContext CreateDbContext(string[] args)
    {
        // Read connection string from appsettings.json
        // Create mock implementations of dependencies
        // Return fully configured DbContext
    }
}
```

**Additional Changes**:
- Added `appsettings.json` to Infrastructure project
- Added NuGet packages:
  - Microsoft.EntityFrameworkCore.SqlServer (9.0.4)
  - Microsoft.EntityFrameworkCore.Tools (9.0.4)
  - Microsoft.Extensions.Configuration.Json (9.0.4)
  - Microsoft.Extensions.Configuration.EnvironmentVariables (9.0.4)
- Removed Migrations folder from `.csproj` exclusion

**Impact**: Unblocked running migrations

##### Fix #5: Service.BookingPolicy EF Core Configuration
**Commit**: `1d022a1` **[MOST RECENT]**
**File**: `ServiceConfiguration.cs`

**Problem**: Navigation property error during migration
```
Error: Unable to determine the relationship represented by navigation
'Service.BookingPolicy' of type 'BookingPolicy'
```

**Root Cause**: `Service.BookingPolicy` value object had no EF Core mapping configuration

**Solution**: Added `.OwnsOne()` configuration in `ServiceConfiguration.cs`
```csharp
// Added lines 124-160
builder.OwnsOne(s => s.BookingPolicy, policy =>
{
    policy.Property(p => p.MinAdvanceBookingHours)
        .HasColumnName("BookingPolicyMinAdvanceBookingHours")
        .IsRequired();

    policy.Property(p => p.MaxAdvanceBookingDays)
        .HasColumnName("BookingPolicyMaxAdvanceBookingDays")
        .IsRequired();

    // ... 6 more properties
});
```

**Pattern**: Followed same configuration as `BookingConfiguration.cs` for `Booking.Policy`

**Impact**: Allows migration to succeed (now ready to run)

### Session: 2025-11-06 - Payment & Financial System Implementation **[NEW]**

#### 1. Payment & Financial Domain (Completed ✅)
**Commits**: `9738200` (fix builes), previous implementation commits

**What was added:**
- `Payment` aggregate root with complete payment lifecycle
- `Payout` aggregate root with provider payment processing
- `CommissionRate`, `TaxRate`, `RefundPolicy` value objects
- Financial calculation services
- Payment gateway integration abstractions
- Integration event handlers for booking-payment flow

**Key files:**
- `/Domain/Aggregates/PaymentAggregate/Payment.cs` - Payment processing
- `/Domain/Aggregates/PayoutAggregate/Payout.cs` - Provider payouts
- `/Domain/ValueObjects/CommissionRate.cs` - Commission calculations
- `/Domain/ValueObjects/TaxRate.cs` - Tax calculations
- `/Domain/ValueObjects/RefundPolicy.cs` - Refund rules
- `/Domain/Enums/PaymentMethod.cs` - **CRITICAL**: Changed `Card` → `CreditCard`

#### 2. Payment Commands & Handlers (Completed ✅)

**Commands implemented:**
- `ProcessPaymentCommand` - Process customer payment
- `CapturePaymentCommand` - Capture authorized payment
- `RefundPaymentCommand` - Process refund
- `CreatePayoutCommand` - Create provider payout
- `ExecutePayoutCommand` - Execute payout via gateway

**Queries implemented:**
- `GetPaymentDetailsQuery` - Get payment with transactions
- `GetCustomerPaymentsQuery` - Customer payment history
- `GetProviderEarningsQuery` - Provider financial report
- `CalculatePricingQuery` - Price calculation with tax/commission

#### 3. Payment & Financial API (Completed ✅)

**Controllers created:**
- `PaymentsController` - 6 endpoints
- `PayoutsController` - 5 endpoints
- `FinancialController` - 3 endpoints

**Total**: 14 new RESTful endpoints

#### 4. Integration Event Handlers (Completed ✅)

**Booking-Payment Flow Integration:**
- `BookingConfirmedPaymentIntegrationEventHandler` - Validate payment on booking confirmation
- `BookingCancelledRefundIntegrationEventHandler` - Auto-refund on cancellation
- `BookingCompletedIntegrationEventHandler` - Trigger payout on completion

#### 5. Comprehensive Testing (Completed ✅)

**Test Coverage: 117 Tests Total**

**Domain Unit Tests (70 tests):**
- `PaymentAggregateTests` - 19 tests
- `PayoutAggregateTests` - 17 tests
- `CommissionRateTests` - 12 tests
- `TaxRateTests` - 16 tests
- `RefundPolicyTests` - 20 tests
- `ProcessPaymentCommandHandlerTests` - 6 tests

**API Integration Tests (47 tests):**
- `PaymentsControllerTests` - 20 tests
- `PayoutsControllerTests` - 16 tests
- `FinancialControllerTests` - 11 tests

**Test Infrastructure:**
- Created `CreateTestProviderWithServicesAsync()` helper
- Created `GetFirstServiceForProviderAsync()` helper
- Enhanced `ServiceCatalogIntegrationTestBase` with financial test setup

#### 6. Critical Build Fixes & Lessons Learned **[IMPORTANT]**

**⚠️ CRITICAL PATTERNS TO REMEMBER:**

##### Fix #1: Method Signatures vs Property Names
**Problem**: Tests and controllers referenced non-existent properties/methods
**Root Cause**: Assumed API without checking actual implementation

**Examples:**
```csharp
// ❌ WRONG - Assumed these existed
TaxRate.Name         → ✅ CORRECT: TaxRate.TaxName
TaxRate.Code         → ✅ CORRECT: TaxRate.TaxCode
Payout.GetPaymentIds() → ✅ CORRECT: Payout.PaymentIds (property)
Payment.GetDomainEvents() → ✅ CORRECT: Payment.DomainEvents (property)
Payout.ScheduledDate → ✅ CORRECT: Payout.ScheduledAt
Payout.ProcessedAt   → ✅ CORRECT: Payout.PaidAt
Payout.CompletedAt   → ✅ CORRECT: Payout.PaidAt
Payout.PlaceOnHold() → ✅ CORRECT: Payout.PutOnHold()
```

**Lesson**: ALWAYS read the actual domain class before writing tests or handlers!

##### Fix #2: Enum Values
**Problem**: Tests used non-existent enum values
**Root Cause**: Assumed common naming conventions

**Examples:**
```csharp
// ❌ WRONG - Assumed these existed
PaymentMethod.CreditCard → ✅ FIXED IN DOMAIN: Changed Card → CreditCard (commit 9738200)
PaymentMethod.Card       → ❌ OLD VALUE (was changed)
RefundReason.CustomerRequest → ✅ CORRECT: RefundReason.CustomerCancellation
PaymentStatus.Authorized → ✅ CORRECT: PaymentStatus.PartiallyPaid (or redesign)
PayoutStatus.Scheduled   → ✅ CORRECT: PayoutStatus.Pending
```

**Lesson**: Check enum definitions in `/Domain/Enums/` before usage!

##### Fix #3: Result Record Properties
**Problem**: Controllers expected `IsSuccessful` and `ErrorMessage` properties on Result records
**Root Cause**: Initial implementation didn't include these properties

**Solution Applied (commit 9738200):**
```csharp
// All Result records now include:
public sealed record ProcessPaymentResult(
    ...existing properties...,
    bool IsSuccessful,      // ✅ Added
    string? ErrorMessage);  // ✅ Added

// Applied to:
// - ProcessPaymentResult
// - CapturePaymentResult
// - RefundPaymentResult
// - ExecutePayoutResult
```

**Lesson**: Design Result records with error handling properties from the start!

##### Fix #4: Command Parameter Names
**Problem**: Controllers used wrong parameter names in command construction
**Root Cause**: Didn't check actual record constructor signature

**Examples:**
```csharp
// ❌ WRONG in controllers
new ProcessPaymentCommand(
    PaymentMethod: "CreditCard",    // Wrong parameter name
    CaptureImmediately: true,       // Wrong - doesn't exist
    ...
);

// ✅ CORRECT (actual signature)
new ProcessPaymentCommand(
    Method: PaymentMethod.CreditCard,  // Correct parameter + enum
    ...  // CaptureImmediately removed
);

// ❌ WRONG
new CapturePaymentCommand(
    Amount: request.Amount,  // Wrong - doesn't exist
    ...
);

// ✅ CORRECT
new CapturePaymentCommand(
    AmountToCapture: request.Amount,  // Correct parameter name
    ...
);
```

**Lesson**: ALWAYS check record constructors before calling them!

##### Fix #5: Type Conversions
**Problem**: Type mismatches in metadata and parameters

**Examples:**
```csharp
// ❌ WRONG - Type mismatch
Dictionary<string, string> → Dictionary<string, object>  // Metadata
decimal? → decimal  // RefundAmount parameter

// ✅ FIXED with conversions
Metadata: request.Metadata?.ToDictionary(
    kvp => kvp.Key,
    kvp => (object)kvp.Value)
```

**Lesson**: Check property/parameter types carefully!

##### Fix #6: Extension Methods
**Problem**: Used non-existent extension method
**Root Cause**: Assumed common extension existed

**Example:**
```csharp
// ❌ WRONG - Method doesn't exist
User.GetUserId()

// ✅ FIXED - Added extension method
public static string? GetUserId(this ClaimsPrincipal user)
{
    return user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? user?.FindFirst("sub")?.Value;
}
```

**Lesson**: Verify extension methods exist or create them in `/Extensions/`!

##### Fix #7: Value Object Factory Methods
**Problem**: Used wrong factory method names

**Example:**
```csharp
// ❌ WRONG
RefundPolicy.Custom(params...)

// ✅ CORRECT
RefundPolicy.Create(params...)
```

**Lesson**: Check static factory method names in value objects!

#### 7. Build Fix Strategy (Process to Follow)

**When compilation errors occur:**

1. **Read First, Fix Second**:
   ```bash
   # ALWAYS read the actual implementation first
   cat Domain/Aggregates/MyAggregate/MyAggregate.cs
   cat Domain/ValueObjects/MyValueObject.cs
   cat Domain/Enums/MyEnum.cs
   ```

2. **Check Method Signatures**:
   - Parameter names (e.g., `Method` not `PaymentMethod`)
   - Parameter types (e.g., `PaymentMethod` enum not `string`)
   - Return types (e.g., properties not methods)

3. **Verify Property Names**:
   - Exact casing (e.g., `TaxName` not `Name`)
   - Full names (e.g., `ScheduledAt` not `ScheduledDate`)
   - Property vs Method (e.g., `PaymentIds` not `GetPaymentIds()`)

4. **Test Enum Values**:
   - Check actual enum file in `/Domain/Enums/`
   - Don't assume common names (e.g., `CreditCard` vs `Card`)

5. **Systematic Fixes**:
   ```bash
   # Use sed for batch fixes across test files
   sed -i 's/\.OldName/\.NewName/g' **/*Tests.cs

   # But verify each file afterward!
   ```

6. **Incremental Compilation**:
   ```bash
   # Build frequently to catch errors early
   dotnet build
   ```

#### 8. Developer Guidelines Updates

**When implementing new aggregates:**

✅ **DO**:
- Read actual domain classes before writing handlers
- Check exact property and method names
- Verify enum values in enum definition files
- Include `IsSuccessful` and `ErrorMessage` in Result records
- Use strongly-typed parameter names
- Create extension methods when needed (in `/Extensions/`)

❌ **DON'T**:
- Assume property/method names without checking
- Assume enum values follow conventions
- Use generic parameter names (e.g., `Data` instead of specific name)
- Create methods without checking if properties exist
- Batch-fix without verification

**Golden Rule**: *"Read the actual code before writing code that calls it!"*

---

## Known Issues & Limitations

### Current Limitations

#### 1. Booking System (In Development)
- **Migration Not Applied**: Booking tables don't exist in database yet
  - **Status**: Migration ready to run after fix #5
  - **Action Required**: Run `dotnet ef migrations add AddBookingSystem` and `dotnet ef database update`
- **Payment Integration**: Placeholder only, no actual payment gateway
- **Notifications**: No email/SMS notifications on booking actions
- **Recurring Bookings**: Not supported yet
- **Waitlist**: Not implemented

#### 2. Search & Discovery
- **Full-Text Search**: Basic SQL LIKE queries, not optimized
  - **Planned**: Elasticsearch or PostgreSQL full-text search
- **Geolocation Search**: Location data stored but not indexed
  - **Planned**: PostGIS extension for spatial queries

#### 3. Security & Performance
- **Rate Limiting**: Configuration exists but not enforced
- **Caching**: Redis configured but not implemented
- **Message Broker**: RabbitMQ configured but not used
- **API Gateway**: Not implemented (direct API calls)

#### 4. Frontend
- **Customer Portal**: Not implemented (only provider portal exists)
- **Mobile App**: Not started
- **Real-time Updates**: No WebSocket/SignalR integration

#### 5. Cross-Cutting Concerns
- **Logging**: Basic console logging only
  - **Planned**: Structured logging with Serilog, Seq
- **Monitoring**: No APM or health checks
  - **Planned**: Application Insights, Prometheus
- **Distributed Tracing**: Not implemented
  - **Planned**: OpenTelemetry

### Known Technical Debt

#### 1. Command Handler Inconsistencies
**Issue**: Some command handlers bypass domain methods and manipulate state directly
**Example**: Early handlers before domain methods were added
**Impact**: Business rules might not be enforced consistently
**Resolution**: Refactor handlers to always use aggregate methods

#### 2. Validation Duplication
**Issue**: Some validation exists in both FluentValidation and domain layer
**Impact**: Maintenance burden, potential inconsistencies
**Resolution**: Move business rule validation to domain, keep input validation in FluentValidation

#### 3. ViewModel Inconsistencies
**Issue**: Some ViewModels access properties that don't exist or use wrong names
**Example**: Past issues with `Service.BookingPolicy` properties
**Impact**: Runtime errors
**Resolution**: Generate ViewModels from DTOs, use AutoMapper profiles

#### 4. Integration Test Data Setup
**Issue**: Test data creation is verbose and repetitive
**Impact**: Hard to write and maintain tests
**Resolution**: Create builder pattern for test data (Object Mother pattern)

#### 5. Strongly-Typed IDs in API
**Issue**: API uses `Guid` instead of strongly-typed IDs
**Impact**: Loss of type safety at API boundary
**Resolution**: Custom JSON converters for value objects

---

## Future Development

### Phase 2: Booking System (Current Phase 🚧)

#### Completed in This Session ✅
- ✅ Booking domain model
- ✅ Booking commands and handlers
- ✅ Availability management
- ✅ Booking API endpoints
- ✅ Integration tests (29 tests)

#### Remaining Tasks 📋
- ⬜ Apply booking migrations to database
- ⬜ Frontend booking UI for customers
- ⬜ Frontend booking management UI for providers
- ⬜ Email/SMS notification system
- ⬜ Payment gateway integration (Stripe/PayPal)
- ⬜ Recurring bookings
- ⬜ Waitlist functionality
- ⬜ Booking reminders

### Phase 3: Customer Portal (Planned 📋)

#### User Stories
1. As a customer, I want to search for services by location
2. As a customer, I want to view provider profiles and reviews
3. As a customer, I want to book appointments online
4. As a customer, I want to manage my bookings
5. As a customer, I want to leave reviews

#### Technical Tasks
- Customer registration and profile management
- Service discovery and search UI
- Provider profile pages
- Reviews and ratings domain model
- Favorite providers
- Customer booking history

### Phase 4: Payment Integration (Planned 📋)

#### Scope
- Payment gateway integration (Stripe recommended)
- Deposit collection at booking time
- Full payment processing
- Refund processing for cancellations
- Invoice generation
- Payment history

#### Technical Components
- Payment bounded context (separate microservice)
- Webhook handling for payment events
- Saga pattern for booking + payment coordination
- PCI compliance considerations

### Phase 5: Notifications (Planned 📋)

#### Channels
- Email notifications (SendGrid/AWS SES)
- SMS notifications (Twilio/AWS SNS)
- Push notifications (mobile app)
- In-app notifications

#### Notification Types
- Booking confirmation
- Booking reminder (24h, 1h before)
- Booking cancelled
- Booking rescheduled
- Payment receipt
- Review request

### Phase 6: Advanced Features (Planned 📋)

#### Features
- Real-time availability updates (SignalR)
- Calendar integration (Google Calendar, iCal)
- Analytics dashboard for providers
- Reporting system
- Multi-location support
- Inventory management (products)
- Loyalty programs
- Gift cards

#### Infrastructure
- API Gateway (YARP)
- Message broker (RabbitMQ/Azure Service Bus)
- Caching (Redis)
- CDN for images
- Multi-region deployment
- Kubernetes orchestration

---

## Developer Guidelines

### Getting Started

#### Prerequisites
- .NET 8.0 SDK
- PostgreSQL 15+ or SQL Server 2019+
- Node.js 20+ (for frontend)
- Docker (optional, for local services)

#### Initial Setup

1. **Clone repository**:
```bash
git clone <repo-url>
cd Booking
```

2. **Configure database**:
```bash
# ServiceCatalog
cd src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api
# Edit appsettings.Development.json

# UserManagement
cd src/UserManagement/Booksy.UserManagement.API
# Edit appsettings.Development.json
```

3. **Run migrations**:
```bash
# ServiceCatalog
dotnet ef database update \
  --project src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure \
  --startup-project src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api

# UserManagement
dotnet ef database update \
  --project src/UserManagement/Booksy.UserManagement.Infrastructure \
  --startup-project src/UserManagement/Booksy.UserManagement.API
```

4. **Run APIs**:
```bash
# Terminal 1: ServiceCatalog
cd src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api
dotnet run

# Terminal 2: UserManagement
cd src/UserManagement/Booksy.UserManagement.API
dotnet run
```

5. **Run frontend**:
```bash
cd booksy-frontend
npm install
npm run dev
```

### Development Workflow

#### Adding a New Feature

**Step 1: Design**
- Identify which bounded context owns the feature
- Design domain model (aggregates, entities, value objects)
- Define domain events
- Consider cross-context integration

**Step 2: Domain Layer**
```csharp
// 1. Create entities/aggregates in /Domain/Aggregates/
public sealed class MyAggregate : AggregateRoot<MyAggregateId>
{
    // Private fields for collections
    private readonly List<MyEntity> _entities = new();

    // Public properties (private setters)
    public SomeValueObject Property { get; private set; }

    // Factory method
    public static MyAggregate Create(...)
    {
        var aggregate = new MyAggregate { ... };
        aggregate.RaiseDomainEvent(new MyAggregateCreatedEvent(...));
        return aggregate;
    }

    // Business methods
    public void DoSomething()
    {
        // Validate invariants
        // Mutate state
        // Raise events
    }
}

// 2. Create value objects in /Domain/ValueObjects/
public sealed class MyValueObject : ValueObject
{
    public string Value { get; }

    private MyValueObject(string value)
    {
        // Validate
        Value = value;
    }

    public static MyValueObject Create(string value) => new(value);

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }
}

// 3. Create domain events in /Domain/Events/
public sealed record MyAggregateCreatedEvent(
    MyAggregateId Id,
    DateTime OccurredAt) : IDomainEvent;
```

**Step 3: Application Layer**
```csharp
// 1. Create command in /Application/Commands/
public sealed record MyCommand(
    string SomeParameter,
    Guid? IdempotencyKey = null) : ICommand<MyResult>;

// 2. Create validator
public sealed class MyCommandValidator : AbstractValidator<MyCommand>
{
    public MyCommandValidator()
    {
        RuleFor(x => x.SomeParameter)
            .NotEmpty()
            .MaximumLength(100);
    }
}

// 3. Create handler
public sealed class MyCommandHandler : ICommandHandler<MyCommand, MyResult>
{
    private readonly IMyAggregateRepository _repository;

    public async Task<Result<MyResult>> Handle(
        MyCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Validate business rules
        // 2. Load aggregate (if updating)
        // 3. Execute domain method
        var aggregate = MyAggregate.Create(...);

        // 4. Save
        await _repository.AddAsync(aggregate, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        // 5. Return result
        return Result<MyResult>.Success(new MyResult(...));
    }
}

// 4. Create result
public sealed record MyResult(Guid Id, string Status);
```

**Step 4: Infrastructure Layer**
```csharp
// 1. Create repository interface in /Application/Interfaces/
public interface IMyAggregateRepository : IRepository<MyAggregate>
{
    Task<MyAggregate?> GetByIdAsync(MyAggregateId id, CancellationToken ct);
    // ... other queries
}

// 2. Implement repository in /Infrastructure/Persistence/Repositories/
public sealed class MyAggregateRepository :
    BaseRepository<MyAggregate>,
    IMyAggregateRepository
{
    public MyAggregateRepository(ServiceCatalogDbContext context)
        : base(context) { }

    public async Task<MyAggregate?> GetByIdAsync(
        MyAggregateId id,
        CancellationToken ct)
    {
        return await _context.MyAggregates
            .Include(x => x.Entities)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }
}

// 3. Configure EF Core in /Infrastructure/Persistence/Configurations/
public sealed class MyAggregateConfiguration : IEntityTypeConfiguration<MyAggregate>
{
    public void Configure(EntityTypeBuilder<MyAggregate> builder)
    {
        builder.ToTable("MyAggregates", "ServiceCatalog");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(
                id => id.Value,
                value => MyAggregateId.From(value));

        // Configure value objects
        builder.OwnsOne(x => x.Property, prop =>
        {
            prop.Property(p => p.Value)
                .HasColumnName("PropertyValue")
                .IsRequired();
        });

        // Configure collections
        builder.HasMany(x => x.Entities)
            .WithOne()
            .HasForeignKey("MyAggregateId");
    }
}
```

**Step 5: API Layer**
```csharp
// 1. Create request/response DTOs in /Api/Models/
public sealed class CreateMyAggregateRequest
{
    [Required]
    [MaxLength(100)]
    public string SomeParameter { get; set; } = default!;
}

public sealed class MyAggregateResponse
{
    public Guid Id { get; set; }
    public string Status { get; set; } = default!;
}

// 2. Create controller in /Api/Controllers/
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class MyAggregatesController : ControllerBase
{
    private readonly ISender _mediator;

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(
        [FromBody] CreateMyAggregateRequest request,
        CancellationToken cancellationToken)
    {
        var command = new MyCommand(request.SomeParameter);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        var response = new MyAggregateResponse
        {
            Id = result.Value.Id,
            Status = result.Value.Status
        };

        return CreatedAtAction(
            nameof(GetById),
            new { id = response.Id },
            response);
    }
}
```

**Step 6: Add Migration**
```bash
dotnet ef migrations add AddMyAggregate \
  --project src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure \
  --startup-project src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api

# Review generated migration, then apply
dotnet ef database update \
  --project src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure \
  --startup-project src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api
```

**Step 7: Write Tests**
```csharp
// Integration test in /tests/Booksy.ServiceCatalog.IntegrationTests/
public class MyAggregatesControllerTests : BaseIntegrationTest
{
    [Fact]
    public async Task Create_WithValidData_ShouldReturn201Created()
    {
        // Arrange
        AuthenticateAsUser(Guid.NewGuid(), "user@test.com");
        var request = new CreateMyAggregateRequest
        {
            SomeParameter = "Valid Value"
        };

        // Act
        var response = await PostAsJsonAsync<CreateMyAggregateRequest, MyAggregateResponse>(
            "/api/v1/myaggregates",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Data!.Id.Should().NotBeEmpty();
    }
}
```

### Coding Standards

#### Naming Conventions
- **Aggregates**: PascalCase, singular (e.g., `Provider`, `Booking`)
- **Entities**: PascalCase, singular (e.g., `Staff`, `ServiceOption`)
- **Value Objects**: PascalCase, descriptive (e.g., `BookingPolicy`, `TimeSlot`)
- **Commands**: PascalCase, verb + noun (e.g., `CreateBookingCommand`)
- **Events**: PascalCase, past tense (e.g., `BookingCreatedEvent`)
- **Interfaces**: Prefix with `I` (e.g., `IBookingRepository`)
- **Private fields**: Prefix with `_` (e.g., `_bookings`)

#### DDD Patterns

**Always:**
- ✅ Encapsulate state (private setters)
- ✅ Use factory methods instead of public constructors
- ✅ Validate invariants in domain methods
- ✅ Raise domain events for state changes
- ✅ Use value objects for concepts with identity
- ✅ Use strongly-typed IDs

**Never:**
- ❌ Public setters on entities
- ❌ Anemic domain models (just getters/setters)
- ❌ Business logic in application layer
- ❌ Direct manipulation of collections
- ❌ Primitive obsession (use value objects)

#### Error Handling

**Domain Layer**: Throw domain exceptions
```csharp
if (condition)
    throw new InvalidBookingException("Reason");
```

**Application Layer**: Return Result pattern
```csharp
if (!validation.IsValid)
    return Result<T>.Failure("Validation failed");

return Result<T>.Success(data);
```

**API Layer**: Return appropriate HTTP status codes
```csharp
if (result.IsFailure)
    return BadRequest(new { error = result.Error });

return Ok(result.Value);
```

### Git Workflow

**Branch Naming**:
- `feature/booking-system` - New features
- `bugfix/availability-parameter-type` - Bug fixes
- `refactor/provider-aggregate` - Refactoring
- `docs/update-readme` - Documentation

**Commit Messages** (Conventional Commits):
```
feat: add booking cancellation with fee calculation

- Implement CancelBookingCommand and handler
- Add cancellation fee calculation based on policy
- Raise BookingCancelledEvent
- Add integration tests

BREAKING CHANGE: CancelBooking now requires cancellation reason
```

**Pull Request Process**:
1. Create feature branch from `develop`
2. Implement with tests
3. Ensure all tests pass
4. Update documentation
5. Submit PR with description
6. Address review feedback
7. Squash commits and merge

---

## Appendix

### Useful Commands

**Build:**
```bash
dotnet build
dotnet build --configuration Release
```

**Test:**
```bash
dotnet test
dotnet test --filter "FullyQualifiedName~BookingsControllerTests"
dotnet test /p:CollectCoverage=true
```

**Migrations:**
```bash
# Add migration
dotnet ef migrations add MigrationName \
  --project src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure \
  --startup-project src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api

# Update database
dotnet ef database update \
  --project src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure \
  --startup-project src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api

# Remove last migration
dotnet ef migrations remove \
  --project src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure

# Generate SQL script
dotnet ef migrations script \
  --project src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure \
  --output migration.sql
```

**Run:**
```bash
# ServiceCatalog API
dotnet run --project src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api

# UserManagement API
dotnet run --project src/UserManagement/Booksy.UserManagement.API

# With specific environment
dotnet run --project src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api \
  --environment Production
```

**Frontend:**
```bash
cd booksy-frontend
npm run dev          # Development server
npm run build        # Production build
npm run preview      # Preview production build
npm run test:unit    # Unit tests
npm run test:e2e     # E2E tests
```

### Key File Locations Reference

**Domain Models:**
- Providers: `/src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/Aggregates/ProviderAggregate/`
- Services: `/src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/Aggregates/ServiceAggregate/`
- Bookings: `/src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/Aggregates/BookingAggregate/`
- Value Objects: `/src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/ValueObjects/`
- Events: `/src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/Events/`

**Application Layer:**
- Commands: `/src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Commands/`
- Queries: `/src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Queries/`
- Services: `/src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Services/`

**Infrastructure:**
- DbContext: `/src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Persistence/ServiceCatalogDbContext.cs`
- Configurations: `/src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Persistence/Configurations/`
- Repositories: `/src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Persistence/Repositories/`
- Migrations: `/src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Migrations/`

**API:**
- Controllers: `/src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Controllers/`
- DTOs: `/src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Models/`
- Configuration: `/src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/appsettings.json`

**Tests:**
- Integration Tests: `/tests/Booksy.ServiceCatalog.IntegrationTests/`
- Test Infrastructure: `/tests/Booksy.Tests.Common/`

### External Resources

**Documentation:**
- Clean Architecture: https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html
- DDD Reference: https://www.domainlanguage.com/ddd/reference/
- EF Core: https://learn.microsoft.com/en-us/ef/core/

**Libraries:**
- MediatR: https://github.com/jbogard/MediatR
- FluentValidation: https://fluentvalidation.net/
- FluentAssertions: https://fluentassertions.com/

---

## Document Revision History

| Date       | Version | Changes                                           | Author       |
|------------|---------|---------------------------------------------------|--------------|
| 2025-11-05 | 1.0.0   | Initial comprehensive documentation created       | Claude (AI)  |
|            |         | Covers: Architecture, Domain Model, Booking System|              |
|            |         | Recent fixes, Testing, Developer Guidelines       |              |

**Next Update**: After Booking migrations are applied and tested

---

*This is a living document. Update this file whenever significant changes are made to the architecture, domain model, or development processes.*
