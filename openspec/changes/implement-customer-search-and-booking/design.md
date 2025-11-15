# Technical Design: Customer Search and Booking

## Architecture Overview

This feature follows the existing architecture patterns:
- **Backend**: Clean Architecture + CQRS + DDD
- **Frontend**: Vue 3 Composition API + TypeScript + Pinia
- **Communication**: RESTful APIs with JWT authentication

---

## Frontend Architecture

### Component Hierarchy

```
CustomerLayout.vue
├── CustomerDashboardView.vue
├── ProviderListView.vue (Browse/Search)
│   ├── SearchFilters.vue
│   ├── ProviderCard.vue (multiple)
│   └── PaginationControls.vue
├── ProviderDetailView.vue
│   ├── ProviderHeader.vue
│   ├── ServicesSection.vue
│   │   └── ServiceCard.vue (multiple)
│   ├── StaffSection.vue
│   │   └── StaffCard.vue (multiple)
│   ├── GallerySection.vue
│   └── MapLocation.vue
├── BookingWizardView.vue
│   ├── WizardStepper.vue (progress indicator)
│   ├── Step1: ServiceSelector.vue
│   │   ├── ServiceOptionCard.vue
│   │   └── StaffSelector.vue
│   ├── Step2: DateTimeSelector.vue
│   │   ├── PersianCalendar.vue
│   │   └── TimeSlotPicker.vue
│   ├── Step3: CustomerDetailsForm.vue
│   ├── Step4: BookingSummary.vue
│   └── Step5: PaymentView.vue
├── BookingConfirmation.vue
├── MyBookingsView.vue
│   ├── BookingFilters.vue
│   └── BookingCard.vue (multiple)
└── BookingDetailView.vue
    ├── BookingInfo.vue
    ├── BookingActions.vue
    └── BookingTimeline.vue
```

### State Management (Pinia Stores)

#### searchStore.ts
```typescript
interface SearchStore {
  // State
  searchQuery: string;
  filters: {
    category: string[];
    location: { lat: number; lng: number; radius: number } | null;
    rating: number | null;
    priceRange: { min: number; max: number } | null;
    openNow: boolean;
  };
  results: Provider[];
  totalResults: number;
  currentPage: number;
  isLoading: boolean;
  error: string | null;

  // Actions
  searchProviders(query: string): Promise<void>;
  applyFilters(filters: Filters): Promise<void>;
  loadMore(): Promise<void>;
  clearFilters(): void;
  setLocation(lat: number, lng: number): Promise<void>;
}
```

#### bookingStore.ts
```typescript
interface BookingStore {
  // State
  currentStep: number;
  provider: Provider | null;
  selectedService: Service | null;
  selectedOptions: ServiceOption[];
  selectedStaff: Staff | null;
  selectedDate: string | null; // ISO format
  selectedTimeSlot: TimeSlot | null;
  customerDetails: CustomerDetails;
  customerNotes: string;
  availableSlots: TimeSlot[];
  totalPrice: number;
  depositAmount: number;
  bookingId: string | null;
  paymentStatus: 'pending' | 'processing' | 'completed' | 'failed';

  // Computed
  canProceedToNextStep: ComputedRef<boolean>;
  pricingBreakdown: ComputedRef<PriceBreakdown>;

  // Actions
  setProvider(provider: Provider): void;
  selectService(service: Service): void;
  toggleOption(option: ServiceOption): void;
  selectStaff(staff: Staff): void;
  selectDate(date: string): Promise<void>; // Loads available slots
  selectTimeSlot(slot: TimeSlot): void;
  updateCustomerDetails(details: CustomerDetails): void;
  nextStep(): void;
  previousStep(): void;
  createBooking(): Promise<string>; // Returns booking ID
  processPayment(): Promise<void>;
  confirmBooking(paymentAuthority: string): Promise<void>;
  resetWizard(): void;

  // Persistence
  saveToSession(): void; // Save to sessionStorage
  restoreFromSession(): void; // Restore on page reload
}
```

#### customerStore.ts
```typescript
interface CustomerStore {
  // State
  profile: CustomerProfile | null;
  bookings: Booking[];
  favorites: Provider[];
  isAuthenticated: boolean;

  // Actions
  loadProfile(): Promise<void>;
  loadBookings(filter?: BookingFilter): Promise<void>;
  loadFavorites(): Promise<void>;
  addFavorite(providerId: string): Promise<void>;
  removeFavorite(providerId: string): Promise<void>;
  cancelBooking(bookingId: string, reason: string): Promise<void>;
  rescheduleBooking(bookingId: string, newSlot: TimeSlot): Promise<void>;
}
```

### Services Layer (API Integration)

#### providerService.ts
```typescript
class ProviderService {
  async searchProviders(params: SearchParams): Promise<ProviderSearchResult> {
    // GET /api/v1.0/providers/search
  }

  async getProviderById(id: string): Promise<Provider> {
    // GET /api/v1.0/providers/{id}
  }

  async searchByLocation(lat: number, lng: number, radius: number): Promise<Provider[]> {
    // GET /api/v1.0/providers/by-location
  }

  async getProviderServices(providerId: string): Promise<Service[]> {
    // GET /api/v1.0/services/provider/{providerId}
  }

  async getProviderGallery(providerId: string): Promise<GalleryImage[]> {
    // GET /api/v1.0/providers/{id}/gallery
  }
}
```

#### bookingService.ts
```typescript
class BookingService {
  async getAvailableSlots(params: AvailabilityParams): Promise<TimeSlot[]> {
    // GET /api/v1.0/bookings/available-slots
    // Query params: providerId, serviceId, staffId?, date
  }

  async createBooking(data: CreateBookingRequest): Promise<Booking> {
    // POST /api/v1.0/bookings
  }

  async getMyBookings(customerId: string, filter?: BookingFilter): Promise<Booking[]> {
    // GET /api/v1.0/bookings/my-bookings
  }

  async getBookingById(id: string): Promise<Booking> {
    // GET /api/v1.0/bookings/{id}
  }

  async cancelBooking(id: string, reason: string): Promise<void> {
    // POST /api/v1.0/bookings/{id}/cancel
  }

  async rescheduleBooking(id: string, newTime: TimeSlot): Promise<Booking> {
    // POST /api/v1.0/bookings/{id}/reschedule
  }

  async confirmBooking(id: string, paymentData: PaymentData): Promise<void> {
    // POST /api/v1.0/bookings/{id}/confirm
  }
}
```

#### paymentService.ts
```typescript
class PaymentService {
  async initializePayment(bookingId: string, amount: number): Promise<PaymentInitResponse> {
    // POST /api/v1.0/payments/initialize
    // Returns ZarinPal gateway URL and authority
  }

  async verifyPayment(authority: string, status: string): Promise<PaymentVerification> {
    // POST /api/v1.0/payments/verify
  }

  async getPaymentStatus(bookingId: string): Promise<PaymentStatus> {
    // GET /api/v1.0/payments/status/{bookingId}
  }
}
```

### Utilities

#### dateUtils.ts
```typescript
// Persian/Jalali calendar conversions
export function gregorianToJalali(date: Date): JalaliDate;
export function jalaliToGregorian(jalali: JalaliDate): Date;
export function formatJalaliDate(date: Date, format: string): string;
export function getJalaliMonthName(month: number): string;
export function getJalaliDayName(dayOfWeek: number): string;
export function isJalaliWeekend(date: JalaliDate): boolean;

// Time formatting
export function formatTime(date: Date, use24Hour: boolean): string;
export function parseTimeSlot(startTime: string, duration: number): TimeSlot;

// Timezone handling
export function toTehranTime(utcDate: Date): Date;
export function toUTC(tehranDate: Date): Date;
```

#### validationUtils.ts
```typescript
export function validateIranianPhone(phone: string): boolean;
export function validateEmail(email: string): boolean;
export function sanitizeInput(input: string): string;
export function validateBookingData(data: BookingData): ValidationResult;
```

---

## Backend Architecture

### CQRS Pattern

#### Queries (Read Operations)

**GetAvailableTimeSlotsQuery**
```csharp
public record GetAvailableTimeSlotsQuery(
    ProviderId ProviderId,
    ServiceId ServiceId,
    Guid? StaffId,
    DateOnly Date
) : IQuery<List<TimeSlotDto>>;

public class GetAvailableTimeSlotsHandler : IQueryHandler<GetAvailableTimeSlotsQuery, List<TimeSlotDto>>
{
    // 1. Get provider's business hours for the date
    // 2. Get service duration + prep + buffer times
    // 3. Get existing bookings for the date
    // 4. If staffId specified, get staff working hours
    // 5. Calculate available slots by excluding:
    //    - Times outside business hours
    //    - Existing bookings + buffer times
    //    - Staff unavailability
    //    - Provider holidays/time off
    // 6. Return available slots with start/end times
}
```

**SearchProvidersQuery**
```csharp
public record SearchProvidersQuery(
    string? SearchTerm,
    List<ProviderType>? Categories,
    Coordinates? Location,
    int? RadiusKm,
    decimal? MinRating,
    PriceRange? PriceRange,
    bool? OpenNow,
    int Page,
    int PageSize
) : IQuery<PagedResult<ProviderSearchDto>>;

public class SearchProvidersHandler : IQueryHandler<SearchProvidersQuery, PagedResult<ProviderSearchDto>>
{
    // 1. Build query with filters
    // 2. If location specified, calculate distances
    // 3. If openNow, check current time against business hours
    // 4. Apply pagination
    // 5. Project to ProviderSearchDto with:
    //    - Basic provider info
    //    - Distance (if location search)
    //    - Current status (open/closed)
    //    - Next available slot
    // 6. Return paged results
}
```

**GetCustomerBookingsQuery**
```csharp
public record GetCustomerBookingsQuery(
    UserId CustomerId,
    BookingStatus? Status,
    DateTime? FromDate,
    DateTime? ToDate,
    int Page,
    int PageSize
) : IQuery<PagedResult<BookingDto>>;
```

#### Commands (Write Operations)

**CreateBookingCommand**
```csharp
public record CreateBookingCommand(
    UserId CustomerId,
    ProviderId ProviderId,
    ServiceId ServiceId,
    Guid? StaffId,
    TimeSlot TimeSlot,
    List<Guid> SelectedOptions,
    string? CustomerNotes
) : ICommand<BookingId>;

public class CreateBookingHandler : ICommandHandler<CreateBookingCommand, BookingId>
{
    // 1. Validate slot is still available (optimistic locking)
    // 2. Get service and calculate total price
    // 3. Get provider's booking policy
    // 4. Create Booking aggregate
    // 5. Apply domain events:
    //    - BookingRequested
    //    - BookingPricingCalculated
    // 6. Save to repository
    // 7. Return BookingId
}
```

**ConfirmBookingCommand**
```csharp
public record ConfirmBookingCommand(
    BookingId BookingId,
    PaymentAuthority Authority,
    PaymentStatus PaymentStatus
) : ICommand;

public class ConfirmBookingHandler : ICommandHandler<ConfirmBookingCommand>
{
    // 1. Load booking aggregate
    // 2. Verify payment with payment gateway
    // 3. If successful:
    //    - booking.ConfirmBooking(paymentInfo)
    //    - Raise BookingConfirmed event
    //    - Send confirmation email
    // 4. If failed:
    //    - Raise PaymentFailed event
    //    - Keep booking in Requested status
}
```

**CancelBookingCommand**
```csharp
public record CancelBookingCommand(
    BookingId BookingId,
    UserId CustomerId,
    string Reason
) : ICommand<RefundAmount>;

public class CancelBookingHandler : ICommandHandler<CancelBookingCommand, RefundAmount>
{
    // 1. Load booking aggregate
    // 2. Verify customer owns the booking
    // 3. Check cancellation policy
    // 4. Calculate refund amount
    // 5. booking.Cancel(reason, refundAmount)
    // 6. Raise BookingCancelled event
    // 7. Process refund
    // 8. Return refund amount
}
```

### Domain Events

```csharp
public record BookingRequested(BookingId BookingId, UserId CustomerId, ProviderId ProviderId, TimeSlot TimeSlot);
public record BookingConfirmed(BookingId BookingId, PaymentInfo PaymentInfo);
public record BookingCancelled(BookingId BookingId, string Reason, RefundAmount RefundAmount);
public record BookingRescheduled(BookingId BookingId, TimeSlot OldTimeSlot, TimeSlot NewTimeSlot);
public record PaymentProcessed(BookingId BookingId, PaymentInfo PaymentInfo);
public record PaymentFailed(BookingId BookingId, string Reason);
```

### Event Handlers (for Notifications)

```csharp
public class BookingConfirmedEventHandler : IEventHandler<BookingConfirmed>
{
    public async Task Handle(BookingConfirmed @event)
    {
        // 1. Load booking details
        // 2. Send confirmation email to customer
        // 3. Send notification to provider
        // 4. Schedule appointment reminders
    }
}

public class BookingCancelledEventHandler : IEventHandler<BookingCancelled>
{
    public async Task Handle(BookingCancelled @event)
    {
        // 1. Load booking details
        // 2. Send cancellation email to customer
        // 3. Notify provider of cancellation
        // 4. Cancel scheduled reminders
        // 5. Process refund
    }
}
```

### DTOs (Data Transfer Objects)

```csharp
public record ProviderSearchDto(
    Guid Id,
    string BusinessName,
    ProviderType Type,
    decimal AverageRating,
    int TotalReviews,
    string? LogoUrl,
    AddressDto Address,
    double? DistanceKm,
    string CurrentStatus, // "Open", "Closed", "Opens at X"
    TimeSlot? NextAvailableSlot
);

public record TimeSlotDto(
    DateTime StartTime,
    DateTime EndTime,
    int DurationMinutes,
    bool IsAvailable,
    Guid? StaffId,
    string? StaffName
);

public record BookingDto(
    Guid BookingId,
    ProviderSummaryDto Provider,
    ServiceSummaryDto Service,
    StaffSummaryDto? Staff,
    DateTime StartTime,
    DateTime EndTime,
    BookingStatus Status,
    PriceDto TotalPrice,
    PaymentDto? Payment,
    string? CustomerNotes,
    DateTime RequestedAt,
    DateTime? ConfirmedAt
);
```

---

## Availability Calculation Algorithm

### Inputs
- Provider ID
- Service ID (to get duration)
- Staff ID (optional)
- Date to check
- Customer timezone

### Process

1. **Get Provider Business Hours**
   ```csharp
   var businessHours = await _providerRepository.GetBusinessHours(providerId, date);
   if (businessHours == null || !businessHours.IsOpen)
       return EmptyList; // Provider closed on this day
   ```

2. **Get Service Details**
   ```csharp
   var service = await _serviceRepository.GetById(serviceId);
   var totalDuration = service.Duration + service.PreparationTime + service.BufferTime;
   ```

3. **Get Existing Bookings**
   ```csharp
   var existingBookings = await _bookingRepository
       .GetByProviderAndDate(providerId, date);
   ```

4. **Get Staff Availability** (if applicable)
   ```csharp
   if (staffId.HasValue)
   {
       var staffHours = await _staffRepository.GetWorkingHours(staffId.Value, date);
       // Use staff hours instead of provider hours
   }
   ```

5. **Generate Potential Slots**
   ```csharp
   var slots = new List<TimeSlot>();
   var currentTime = businessHours.OpenTime;
   var slotInterval = TimeSpan.FromMinutes(15); // 15-minute increments

   while (currentTime + totalDuration <= businessHours.CloseTime)
   {
       var slot = new TimeSlot(currentTime, currentTime + totalDuration);
       slots.Add(slot);
       currentTime += slotInterval;
   }
   ```

6. **Filter Out Unavailable Slots**
   ```csharp
   var availableSlots = slots.Where(slot =>
   {
       // Check if slot overlaps with any existing booking
       var hasOverlap = existingBookings.Any(booking =>
           slot.OverlapsWith(booking.TimeSlot));

       // Check if slot is in the past
       var isPast = slot.StartTime <= DateTime.UtcNow;

       // Check if slot exceeds max advance booking window
       var isTooFarAhead = slot.StartTime > DateTime.UtcNow.AddDays(provider.MaxAdvanceDays);

       return !hasOverlap && !isPast && !isTooFarAhead;
   }).ToList();
   ```

7. **Return Available Slots**
   ```csharp
   return availableSlots.Select(slot => new TimeSlotDto(
       StartTime: slot.StartTime,
       EndTime: slot.EndTime,
       DurationMinutes: (int)totalDuration.TotalMinutes,
       IsAvailable: true,
       StaffId: staffId,
       StaffName: staff?.Name
   )).ToList();
   ```

### Optimization Strategies

1. **Caching**: Cache provider business hours and staff schedules
2. **Indexing**: Add database indexes on `(ProviderId, Date)` for bookings
3. **Parallel Processing**: Calculate slots for multiple dates concurrently
4. **Pre-computation**: Generate slots for next 7 days nightly
5. **Pagination**: Return only next N available slots, load more on demand

---

## Payment Flow

### ZarinPal Integration Sequence

```
Customer                Frontend              Backend               ZarinPal
   |                       |                     |                      |
   |-- Complete Booking -->|                     |                      |
   |                       |-- Create Booking -->|                      |
   |                       |                     |-- Save Booking -->DB |
   |                       |<-- Booking ID ------|                      |
   |                       |                     |                      |
   |                       |-- Init Payment ---->|                      |
   |                       |                     |-- Request Payment -->|
   |                       |                     |<-- Authority + URL --|
   |                       |<-- Gateway URL -----|                      |
   |                       |                     |                      |
   |-- Redirect to ZarinPal gateway ------------>|                      |
   |                       |                     |                      |
   |<------- Complete Payment on ZarinPal ------>|                      |
   |                       |                     |                      |
   |<------- Redirect to callback URL -----------|                      |
   |                       |                     |                      |
   |-- Callback with Authority & Status -------->|                      |
   |                       |-- Verify Payment -->|                      |
   |                       |                     |-- Verify Authority ->|
   |                       |                     |<-- Verification -----|
   |                       |                     |-- Update Booking -->DB
   |                       |                     |-- Send Email ------->|
   |                       |<-- Confirmation ----|                      |
   |<-- Show Success ------|                     |                      |
```

### Payment States

1. **Pending**: Booking created, payment not initiated
2. **Processing**: Redirected to ZarinPal, awaiting completion
3. **Verifying**: Returned from gateway, verifying with backend
4. **Completed**: Payment verified, booking confirmed
5. **Failed**: Payment failed or cancelled
6. **Refunded**: Payment refunded after cancellation

---

## Database Indexes (Performance)

```sql
-- Provider search optimization
CREATE INDEX idx_providers_location ON ServiceCatalog.Providers (Latitude, Longitude);
CREATE INDEX idx_providers_type_status ON ServiceCatalog.Providers (Type, Status);
CREATE INDEX idx_providers_rating ON ServiceCatalog.Providers (AverageRating DESC);

-- Service search optimization
CREATE INDEX idx_services_provider_category ON ServiceCatalog.Services (ProviderId, Category);
CREATE INDEX idx_services_price ON ServiceCatalog.Services (BasePrice);

-- Booking queries optimization
CREATE INDEX idx_bookings_customer_status ON ServiceCatalog.Bookings (CustomerId, Status, StartTime DESC);
CREATE INDEX idx_bookings_provider_date ON ServiceCatalog.Bookings (ProviderId, StartTime);
CREATE INDEX idx_bookings_status_date ON ServiceCatalog.Bookings (Status, StartTime);

-- Availability calculation optimization
CREATE INDEXIX idx_bookings_availability ON ServiceCatalog.Bookings (ProviderId, StartTime, EndTime)
WHERE Status IN ('Confirmed', 'Requested');
```

---

## Security Considerations

### Authentication & Authorization
- All booking APIs require JWT authentication
- Customer can only view/modify their own bookings
- Provider can view bookings for their business
- Admin can view all bookings

### Payment Security
- Never store credit card information
- Use HTTPS for all payment redirects
- Validate payment callbacks with backend verification
- Implement CSRF protection on payment callbacks

### Data Validation
- Sanitize all user inputs (search queries, notes)
- Validate all IDs to prevent unauthorized access
- Check booking ownership before cancellation/reschedule
- Prevent SQL injection with parameterized queries

### Rate Limiting
- Limit search requests: 100 per minute per IP
- Limit booking creation: 10 per hour per customer
- Limit availability checks: 60 per minute per customer

---

## Error Handling Strategy

### Frontend Error Handling

```typescript
try {
  await bookingService.createBooking(bookingData);
  // Success handling
} catch (error) {
  if (error.status === 401) {
    // Redirect to login
    router.push('/login');
  } else if (error.status === 400) {
    // Validation error - show to user
    showError(error.message);
  } else if (error.status === 409) {
    // Conflict (slot taken) - reload slots
    await reloadAvailableSlots();
    showError('این زمان دیگر موجود نیست. لطفا زمان دیگری انتخاب کنید.');
  } else if (error.status >= 500) {
    // Server error - show generic message
    showError('خطایی رخ داده است. لطفا دوباره تلاش کنید.');
  }
}
```

### Backend Error Handling

```csharp
public class BookingErrorHandler : IExceptionHandler
{
    public async Task<Result> Handle(Exception exception)
    {
        return exception switch
        {
            BookingNotFoundException => NotFound("Booking not found"),
            SlotNotAvailableException => Conflict("Time slot is no longer available"),
            PaymentFailedException ex => BadRequest($"Payment failed: {ex.Reason}"),
            ValidationException ex => BadRequest(ex.Errors),
            _ => InternalServerError("An error occurred")
        };
    }
}
```

---

## Performance Targets

| Operation | Target | Optimization Strategy |
|-----------|--------|----------------------|
| Provider search | < 200ms | Database indexes, pagination |
| Provider details | < 150ms | Include related data in single query |
| Availability calculation | < 500ms | Caching, database indexes |
| Booking creation | < 1s | Optimistic locking, async events |
| Payment verification | < 2s | Timeout handling, retry logic |
| Page load (TTI) | < 2s | Code splitting, lazy loading |

---

## Testing Strategy

### Unit Tests
- Availability calculation algorithm
- Booking domain logic (create, cancel, reschedule)
- Date conversion utilities (Jalali ↔ Gregorian)
- Price calculation with options and deposits

### Integration Tests
- Complete booking flow (API → Domain → DB)
- Payment processing and verification
- Booking cancellation with refund
- Email sending on booking events

### E2E Tests (Cypress)
```typescript
describe('Complete Booking Flow', () => {
  it('should allow customer to search, select, and book', () => {
    cy.visit('/customer/browse');
    cy.get('[data-testid="search-input"]').type('Beauty Salon');
    cy.get('[data-testid="provider-card"]').first().click();
    cy.get('[data-testid="book-service-btn"]').first().click();

    // Step 1: Select service
    cy.get('[data-testid="service-option"]').first().check();
    cy.get('[data-testid="next-btn"]').click();

    // Step 2: Select date/time
    cy.get('[data-testid="calendar-date"]').contains('15').click();
    cy.get('[data-testid="time-slot"]').first().click();
    cy.get('[data-testid="next-btn"]').click();

    // Step 3: Customer details
    cy.get('[data-testid="customer-notes"]').type('Test booking');
    cy.get('[data-testid="next-btn"]').click();

    // Step 4: Review
    cy.get('[data-testid="confirm-booking-btn"]').click();

    // Step 5: Payment (mock)
    // ... payment simulation

    // Confirmation
    cy.get('[data-testid="booking-reference"]').should('exist');
  });
});
```

---

## Deployment Considerations

### Environment Variables

```env
# Frontend (.env.production)
VITE_API_BASE_URL=https://api.booksy.ir
VITE_ZARINPAL_MERCHANT_ID=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
VITE_CALLBACK_URL=https://booksy.ir/customer/payment/callback
VITE_GOOGLE_MAPS_API_KEY=xxxxxxxxxxxxxxxxxxxx

# Backend (appsettings.Production.json)
{
  "PaymentGateway": {
    "ZarinPal": {
      "MerchantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
      "SandboxMode": false
    }
  },
  "Notifications": {
    "Email": {
      "SmtpHost": "smtp.example.com",
      "SmtpPort": 587,
      "SenderEmail": "noreply@booksy.ir"
    },
    "Sms": {
      "Provider": "Kavenegar",
      "ApiKey": "xxxxxxxxxxxxxxxxxxxx"
    }
  }
}
```

### Feature Flags

```typescript
const features = {
  customerBooking: true,
  smsNotifications: false, // Enable later
  mapSearch: true,
  favoriteProviders: true,
};
```

---

## Monitoring & Analytics

### Key Metrics to Track
1. Search → Booking conversion rate
2. Average time to complete booking
3. Payment success rate
4. Booking cancellation rate
5. Most searched categories
6. Most booked time slots
7. API response times
8. Error rates by endpoint

### Logging

```csharp
_logger.LogInformation(
    "Booking created: BookingId={BookingId}, Customer={CustomerId}, Provider={ProviderId}, Service={ServiceId}, StartTime={StartTime}",
    bookingId, customerId, providerId, serviceId, startTime
);
```

---

## Future Enhancements

1. **Real-time Availability** - WebSocket updates for slot availability
2. **Recurring Bookings** - Weekly/monthly appointments
3. **Waitlist** - Join waitlist when no slots available
4. **Group Bookings** - Book for multiple people
5. **Package Deals** - Booking bundles with discounts
6. **Loyalty Program** - Points and rewards
7. **Push Notifications** - Mobile app notifications
8. **Video Consultations** - Virtual appointments
9. **AI Recommendations** - Suggest providers based on preferences
10. **Multi-language** - Support for English and other languages
