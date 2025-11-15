# Booksy Implementation Priority Roadmap
**Decision Date:** 2025-11-15
**Approvers:** Product Director & CTO
**Planning Horizon:** 16 weeks (4 months)
**Team Capacity:** Assumed 2 backend developers + 2 frontend developers

---

## Executive Summary

Based on business impact analysis, development capacity assessment, and technical debt considerations, we're prioritizing improvements using the **RICE Framework** (Reach, Impact, Confidence, Effort).

**Strategic Focus:**
1. **Fix critical blocking issues** preventing current users from completing bookings (Weeks 1-2)
2. **Implement high-ROI backend foundations** enabling future features (Weeks 3-6)
3. **Build conversion-driving frontend features** with measurable KPIs (Weeks 7-12)
4. **Polish and optimize** for scale and accessibility (Weeks 13-16)

---

## Prioritization Framework

### RICE Scoring Formula
```
RICE Score = (Reach × Impact × Confidence) / Effort

Reach: % of users affected (1-10)
Impact: Business impact (1=minimal, 3=high, 5=massive)
Confidence: Data certainty (0.5=low, 0.8=medium, 1.0=high)
Effort: Person-weeks required (1-20)
```

---

## Priority Matrix: All Improvements Ranked

| Rank | Improvement | Reach | Impact | Confidence | Effort | **RICE Score** | Phase |
|------|-------------|-------|--------|------------|--------|----------------|-------|
| 1 | Provider Availability API | 10 | 5 | 1.0 | 3 | **16.7** | P1 |
| 2 | Smart Calendar with Availability Heatmap | 9 | 5 | 0.8 | 4 | **9.0** | P2 |
| 3 | Mobile Sticky Booking Bar | 8 | 4 | 0.8 | 2 | **12.8** | P2 |
| 4 | Hero Section with Contextual Search | 10 | 4 | 0.8 | 3 | **10.7** | P2 |
| 5 | Provider Search API with Filtering | 10 | 4 | 1.0 | 4 | **10.0** | P1 |
| 6 | Booking Creation API (Concurrency Safe) | 10 | 5 | 1.0 | 5 | **10.0** | P1 |
| 7 | Progressive Location Onboarding | 7 | 3 | 0.8 | 2 | **8.4** | P2 |
| 8 | Icon-Enhanced Category Cards | 8 | 3 | 1.0 | 2 | **12.0** | P2 |
| 9 | Reviews & Ratings System | 6 | 4 | 0.8 | 6 | **3.2** | P3 |
| 10 | Dynamic Trust Signals | 5 | 3 | 0.5 | 2 | **3.8** | P3 |
| 11 | Simplified Footer Redesign | 3 | 2 | 1.0 | 1 | **6.0** | P4 |
| 12 | Contextual Blog Content | 2 | 2 | 0.8 | 3 | **1.1** | P4 |
| 13 | Calendar Accessibility (WCAG 2.2) | 4 | 4 | 0.8 | 4 | **3.2** | P3 |
| 14 | Full Accessibility Audit & Fixes | 4 | 3 | 0.8 | 6 | **1.6** | P4 |

---

## Phase 1: Critical Path - Backend Foundations (Weeks 1-6)
**Goal:** Build the API infrastructure that unblocks all frontend work
**Team:** 2 Backend Devs (Full-time), 1 Frontend Dev (Documentation)

### Week 1-2: Seed Data Enhancement

**Priority: P0 (Blocker)**

#### Tasks
1. **Review Availability Seeder** (3 days)
   - Implement 90-day rolling window availability generation
   - Add realistic availability patterns (peak hours, fully booked days)
   - Persian calendar integration for Iranian holidays

   ```csharp
   // Priority implementation
   public class AvailabilitySeeder
   {
       public async Task GenerateRealisticAvailabilityAsync(Guid providerId, int daysAhead = 90)
       {
           var businessHours = await GetBusinessHoursAsync(providerId);
           var holidays = await GetIranianHolidaysAsync();

           for (int day = 0; day < daysAhead; day++)
           {
               var date = DateTime.UtcNow.Date.AddDays(day);

               // Skip Iranian holidays (Friday + official holidays)
               if (date.DayOfWeek == DayOfWeek.Friday || holidays.Contains(date))
               {
                   continue;
               }

               await GenerateDailySlotsWithPatterns(providerId, date, businessHours);
           }
       }

       private async Task GenerateDailySlotsWithPatterns(Guid providerId, DateTime date, BusinessHours hours)
       {
           // Morning (9-11): 60% available
           // Lunch (12-14): 40% available (high demand)
           // Afternoon (14-17): 70% available
           // Evening (18-20): 30% available (high demand)

           var currentTime = hours.OpenTime;
           while (currentTime < hours.CloseTime)
           {
               var availabilityChance = GetAvailabilityChanceByHour(currentTime.Hour);
               var status = Random.Shared.NextDouble() < availabilityChance
                   ? AvailabilityStatus.Available
                   : AvailabilityStatus.Booked;

               await CreateSlotAsync(providerId, date, currentTime, status);
               currentTime = currentTime.AddMinutes(30);
           }
       }
   }
   ```

2. **Reviews & Ratings Seeder** (2 days)
   - Generate reviews for 60% of completed bookings
   - Realistic rating distribution (50% 5-star, 25% 4-star, etc.)
   - Persian review comments (50+ variations)

3. **Provider Statistics Calculator** (2 days)
   - Total bookings count
   - Average rating calculation
   - Review count aggregation
   - Response time metrics

**Deliverables:**
- ✅ Realistic availability data for testing calendar UI
- ✅ Review data for provider profiles
- ✅ Statistics for search ranking

**Success Metrics:**
- 20 providers with 90 days of availability each
- 300+ reviews across providers
- All providers have accurate statistics

---

### Week 3-4: Core Booking APIs

**Priority: P0 (Blocker)**

#### Task 1: Provider Availability API (5 days)

```csharp
// File: src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Controllers/V1/AvailabilityController.cs

[ApiController]
[Route("api/v1/providers/{providerId}/availability")]
public class AvailabilityController : ControllerBase
{
    [HttpGet]
    [ResponseCache(Duration = 300)] // 5-minute cache
    public async Task<ActionResult<AvailabilityResponse>> GetAvailability(
        [FromRoute] Guid providerId,
        [FromQuery] DateTime date,
        [FromQuery] Guid? serviceId = null,
        [FromQuery] Guid? staffId = null)
    {
        var query = new GetProviderAvailabilityQuery
        {
            ProviderId = providerId,
            Date = date,
            ServiceId = serviceId,
            StaffId = staffId
        };

        var result = await _mediator.Send(query);

        return Ok(new AvailabilityResponse
        {
            ProviderId = providerId,
            Date = date,
            IsOpen = result.IsOpen,
            AvailableSlots = result.Slots,
            AvailabilitySummary = new
            {
                TotalSlots = result.TotalSlots,
                AvailableSlots = result.AvailableCount,
                BookedSlots = result.BookedCount,
                AvailabilityRate = result.AvailabilityRate
            }
        });
    }
}
```

**Query Handler:**
```csharp
public class GetProviderAvailabilityQueryHandler : IRequestHandler<GetProviderAvailabilityQuery, AvailabilityResult>
{
    public async Task<AvailabilityResult> Handle(GetProviderAvailabilityQuery request, CancellationToken cancellationToken)
    {
        // Get business hours for the day
        var businessHours = await _context.BusinessHours
            .FirstOrDefaultAsync(bh => bh.ProviderId == request.ProviderId
                && bh.DayOfWeek == request.Date.DayOfWeek, cancellationToken);

        if (businessHours == null || businessHours.IsClosed)
        {
            return new AvailabilityResult { IsOpen = false };
        }

        // Get availability slots
        var slots = await _context.ProviderAvailability
            .Where(a => a.ProviderId == request.ProviderId && a.Date.Date == request.Date.Date)
            .OrderBy(a => a.StartTime)
            .Select(a => new TimeSlot
            {
                StartTime = a.StartTime.ToString("HH:mm"),
                EndTime = a.EndTime.ToString("HH:mm"),
                IsAvailable = a.Status == AvailabilityStatus.Available,
                StaffId = a.StaffId,
                StaffName = a.Staff.FirstName + " " + a.Staff.LastName
            })
            .ToListAsync(cancellationToken);

        return new AvailabilityResult
        {
            IsOpen = true,
            Slots = slots,
            TotalSlots = slots.Count,
            AvailableCount = slots.Count(s => s.IsAvailable),
            BookedCount = slots.Count(s => !s.IsAvailable)
        };
    }
}
```

**Testing Requirements:**
- Unit tests for query handler
- Integration tests for API endpoint
- Load testing (100 concurrent requests)

---

#### Task 2: Booking Creation API with Concurrency Control (5 days)

```csharp
// File: src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Controllers/V1/BookingsController.cs

[ApiController]
[Route("api/v1/bookings")]
public class BookingsController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<BookingConfirmationResponse>> CreateBooking(
        [FromBody] CreateBookingRequest request)
    {
        var command = new CreateBookingCommand
        {
            ProviderId = request.ProviderId,
            ServiceId = request.ServiceId,
            StaffId = request.StaffId,
            DateTime = request.DateTime,
            Customer = request.Customer,
            Notes = request.Notes
        };

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        return CreatedAtAction(
            nameof(GetBooking),
            new { id = result.BookingId },
            result.ConfirmationResponse);
    }
}
```

**Command Handler with Concurrency Control:**
```csharp
public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, BookingResult>
{
    public async Task<BookingResult> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
    {
        // Use database transaction with row-level locking
        await using var transaction = await _context.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            // 1. Lock availability slot
            var availability = await _context.ProviderAvailability
                .FirstOrDefaultAsync(a =>
                    a.ProviderId == request.ProviderId &&
                    a.Date.Date == request.DateTime.Date &&
                    a.StartTime == TimeOnly.FromDateTime(request.DateTime),
                    cancellationToken);

            if (availability == null || availability.Status != AvailabilityStatus.Available)
            {
                return BookingResult.Failure("این زمان دیگر در دسترس نیست");
            }

            // 2. Create booking
            var booking = Booking.Create(
                ProviderId.From(request.ProviderId),
                ServiceId.From(request.ServiceId),
                CustomerId.From(request.Customer.Id),
                request.DateTime,
                request.Notes
            );

            _context.Bookings.Add(booking);

            // 3. Update availability status
            availability.Status = AvailabilityStatus.Booked;
            availability.BookingId = booking.Id;

            // 4. Save changes
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // 5. Send notifications (async, non-blocking)
            _ = Task.Run(() => _notificationService.SendBookingConfirmationAsync(booking.Id));

            return BookingResult.Success(booking.Id, GenerateConfirmationResponse(booking));
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return BookingResult.Failure("کاربر دیگری همین الان این زمان را رزرو کرد. لطفا زمان دیگری انتخاب کنید");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error creating booking");
            return BookingResult.Failure("خطا در ایجاد رزرو. لطفا دوباره تلاش کنید");
        }
    }
}
```

**Testing Requirements:**
- Concurrency tests (2+ users booking same slot)
- Transaction rollback tests
- Notification integration tests

---

### Week 5-6: Provider Search API

**Priority: P0 (Blocker)**

#### Implementation

```csharp
// File: src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Controllers/V1/ProvidersController.cs

[HttpGet("search")]
[ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "*" })]
public async Task<ActionResult<ProviderSearchResponse>> Search(
    [FromQuery] string? service = null,
    [FromQuery] string? city = null,
    [FromQuery] DateTime? date = null,
    [FromQuery] decimal? minRating = null,
    [FromQuery] string? priceRange = null,
    [FromQuery] string sortBy = "rating",
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20)
{
    var query = new SearchProvidersQuery
    {
        ServiceCategory = service,
        City = city,
        Date = date,
        MinRating = minRating,
        PriceRange = priceRange,
        SortBy = sortBy,
        Page = page,
        PageSize = pageSize
    };

    var result = await _mediator.Send(query);
    return Ok(result);
}
```

**Query Handler:**
```csharp
public class SearchProvidersQueryHandler : IRequestHandler<SearchProvidersQuery, ProviderSearchResponse>
{
    public async Task<ProviderSearchResponse> Handle(SearchProvidersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Providers
            .Include(p => p.Profile)
            .Include(p => p.Location)
            .Where(p => p.Status == ProviderStatus.Active);

        // Apply filters
        if (!string.IsNullOrEmpty(request.City))
        {
            query = query.Where(p => p.Location.City.Name == request.City);
        }

        if (!string.IsNullOrEmpty(request.ServiceCategory))
        {
            query = query.Where(p => p.Services.Any(s =>
                s.Category.ToString() == request.ServiceCategory &&
                s.Status == ServiceStatus.Active));
        }

        if (request.MinRating.HasValue)
        {
            query = query.Where(p => p.Statistics.AverageRating >= request.MinRating.Value);
        }

        if (!string.IsNullOrEmpty(request.PriceRange))
        {
            query = query.Where(p => p.Profile.PriceRange == request.PriceRange);
        }

        // Apply sorting
        query = request.SortBy switch
        {
            "rating" => query.OrderByDescending(p => p.Statistics.AverageRating),
            "price" => query.OrderBy(p => p.Profile.AveragePriceIRR),
            "popularity" => query.OrderByDescending(p => p.Statistics.TotalBookings),
            "distance" => query, // TODO: Implement geospatial query
            _ => query.OrderByDescending(p => p.Statistics.AverageRating)
        };

        // Get total count (before pagination)
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var providers = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new ProviderSearchResult
            {
                Id = p.Id.Value,
                BusinessName = p.Profile.BusinessName,
                City = p.Location.City.Name,
                District = p.Location.District,
                Rating = p.Statistics.AverageRating,
                ReviewCount = p.Statistics.ReviewCount,
                TotalBookings = p.Statistics.TotalBookings,
                PriceRange = p.Profile.PriceRange,
                IsVerified = p.Profile.IsVerified,
                ThumbnailImage = p.Profile.ProfileImageUrl,
                Specialties = p.Services
                    .Where(s => s.IsPopular)
                    .Select(s => s.Name)
                    .Take(3)
                    .ToArray()
            })
            .ToListAsync(cancellationToken);

        return new ProviderSearchResponse
        {
            Results = providers,
            Pagination = new PaginationInfo
            {
                CurrentPage = request.Page,
                PageSize = request.PageSize,
                TotalResults = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
            }
        };
    }
}
```

**Caching Strategy:**
```csharp
// Add to Startup.cs
services.AddResponseCaching();
services.AddMemoryCache();

// Custom cache key generator
public class SearchCacheKeyGenerator
{
    public static string Generate(SearchProvidersQuery query)
    {
        return $"provider-search:{query.City}:{query.ServiceCategory}:{query.MinRating}:{query.SortBy}:{query.Page}";
    }
}
```

**Deliverables:**
- ✅ Search API with filtering
- ✅ Response caching (5-minute duration)
- ✅ Pagination support
- ✅ Multiple sort options

---

### Week 6: API Documentation & Testing

**Tasks:**
1. **Swagger Documentation** (2 days)
   - Add XML comments to all endpoints
   - Include request/response examples
   - Add authentication requirements

2. **Integration Tests** (3 days)
   - Search API tests (filtering, sorting, pagination)
   - Availability API tests (date ranges, edge cases)
   - Booking API tests (concurrency, validation)

**Deliverables:**
- ✅ Complete API documentation at `/swagger`
- ✅ 95%+ test coverage for new endpoints
- ✅ Load testing results (100+ concurrent users)

---

## Phase 2: High-ROI Frontend Features (Weeks 7-12)
**Goal:** Build conversion-driving UI features with measurable KPIs
**Team:** 2 Frontend Devs (Full-time), 1 Backend Dev (Support)

### Week 7-8: Smart Calendar with Availability Heatmap

**Priority: P1 (High Impact)**

**Business Impact:** 30-35% reduction in booking abandonment

#### Implementation

**Component Architecture:**
```
JalaliCalendar.vue (existing)
  ├─ AvailabilityHeatmap.vue (NEW)
  │   ├─ Fetches availability from API
  │   ├─ Color-codes dates (green/yellow/gray)
  │   └─ Shows "Next available" suggestion
  └─ TimeSlotPicker.vue (ENHANCED)
      ├─ Displays available slots for selected date
      ├─ Shows popular time indicator
      └─ Real-time availability updates
```

**File: `booksy-frontend/src/shared/components/calendar/AvailabilityHeatmap.vue`**

```vue
<template>
  <div class="availability-heatmap">
    <!-- Calendar with color-coded availability -->
    <JalaliCalendar
      v-model="selectedDate"
      :min-date="minDate"
      :max-date="maxDate"
      :day-class="getDayClass"
      @update:model-value="handleDateSelect"
    >
      <template #day="{ date, jalaliDate }">
        <div class="day-cell" :class="getAvailabilityClass(date)">
          <span class="day-number">{{ jalaliDate.day }}</span>
          <span class="availability-indicator" :title="getAvailabilityTooltip(date)">
            <span v-if="isFullyBooked(date)" class="icon-blocked">🚫</span>
            <span v-else-if="isLimitedAvailability(date)" class="icon-limited">⚠️</span>
            <span v-else class="icon-available">✓</span>
          </span>
        </div>
      </template>
    </JalaliCalendar>

    <!-- Next Available Quick Select -->
    <div v-if="nextAvailableDate" class="next-available">
      <button @click="selectNextAvailable" class="btn-next-available">
        <span class="icon">⚡</span>
        <span class="text">
          بعدی در دسترس: {{ formatJalaliDate(nextAvailableDate) }}
        </span>
      </button>
    </div>

    <!-- Time Slot Picker (shown after date selection) -->
    <TimeSlotPicker
      v-if="selectedDate"
      :provider-id="providerId"
      :date="selectedDate"
      :service-id="serviceId"
      @slot-select="handleSlotSelect"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useAvailability } from '@/modules/booking/composables/useAvailability'
import JalaliCalendar from './JalaliCalendar.vue'
import TimeSlotPicker from './TimeSlotPicker.vue'

interface Props {
  providerId: string
  serviceId?: string
  minDate?: Date
  maxDate?: Date
}

const props = withDefaults(defineProps<Props>(), {
  minDate: () => new Date(),
  maxDate: () => new Date(Date.now() + 90 * 24 * 60 * 60 * 1000) // 90 days ahead
})

const emit = defineEmits<{
  (e: 'booking-select', value: { date: Date; slot: TimeSlot }): void
}>()

const selectedDate = ref<Date | null>(null)

// Fetch availability data for date range
const {
  availability,
  loading,
  fetchAvailabilityRange,
  getAvailabilityForDate
} = useAvailability()

// Load availability on mount
await fetchAvailabilityRange(props.providerId, props.minDate, props.maxDate)

// Calculate next available date
const nextAvailableDate = computed(() => {
  const today = new Date()
  for (let i = 0; i < 90; i++) {
    const date = new Date(today.getTime() + i * 24 * 60 * 60 * 1000)
    const dayAvailability = getAvailabilityForDate(date)
    if (dayAvailability && dayAvailability.availableSlots > 0) {
      return date
    }
  }
  return null
})

// Availability classification
function getAvailabilityClass(date: Date): string {
  const dayAvailability = getAvailabilityForDate(date)
  if (!dayAvailability) return 'no-data'

  const availabilityRate = dayAvailability.availableSlots / dayAvailability.totalSlots

  if (availabilityRate === 0) return 'fully-booked'
  if (availabilityRate < 0.3) return 'limited-availability'
  return 'available'
}

function isFullyBooked(date: Date): boolean {
  const dayAvailability = getAvailabilityForDate(date)
  return dayAvailability?.availableSlots === 0
}

function isLimitedAvailability(date: Date): boolean {
  const dayAvailability = getAvailabilityForDate(date)
  if (!dayAvailability) return false
  const rate = dayAvailability.availableSlots / dayAvailability.totalSlots
  return rate > 0 && rate < 0.3
}

function getAvailabilityTooltip(date: Date): string {
  const dayAvailability = getAvailabilityForDate(date)
  if (!dayAvailability) return 'اطلاعاتی موجود نیست'

  if (dayAvailability.availableSlots === 0) {
    return 'تمام ساعات رزرو شده'
  }

  return `${dayAvailability.availableSlots} زمان در دسترس از ${dayAvailability.totalSlots}`
}

function selectNextAvailable() {
  if (nextAvailableDate.value) {
    selectedDate.value = nextAvailableDate.value
  }
}

function handleSlotSelect(slot: TimeSlot) {
  if (selectedDate.value) {
    emit('booking-select', { date: selectedDate.value, slot })
  }
}
</script>

<style scoped>
.availability-heatmap {
  direction: rtl;
}

.day-cell {
  position: relative;
  padding: 8px;
  border-radius: 8px;
  transition: all 0.2s;
}

.day-cell.available {
  background-color: rgba(34, 197, 94, 0.1); /* green-500/10 */
  border: 1px solid rgba(34, 197, 94, 0.3);
}

.day-cell.limited-availability {
  background-color: rgba(251, 191, 36, 0.1); /* yellow-500/10 */
  border: 1px solid rgba(251, 191, 36, 0.3);
}

.day-cell.fully-booked {
  background-color: rgba(156, 163, 175, 0.1); /* gray-400/10 */
  border: 1px solid rgba(156, 163, 175, 0.3);
  opacity: 0.6;
  cursor: not-allowed;
}

.availability-indicator {
  position: absolute;
  top: 4px;
  left: 4px;
  font-size: 10px;
}

.next-available {
  margin-top: 16px;
  text-align: center;
}

.btn-next-available {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 12px 24px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  border: none;
  border-radius: 12px;
  font-weight: 600;
  cursor: pointer;
  transition: transform 0.2s;
}

.btn-next-available:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 16px rgba(102, 126, 234, 0.3);
}
</style>
```

**Composable: `useAvailability.ts`**

```typescript
// File: booksy-frontend/src/modules/booking/composables/useAvailability.ts

import { ref, Ref } from 'vue'
import { availabilityService } from '../services/availability.service'

interface DayAvailability {
  date: Date
  totalSlots: number
  availableSlots: number
  bookedSlots: number
  availabilityRate: number
}

export function useAvailability() {
  const availability = ref<Map<string, DayAvailability>>(new Map())
  const loading = ref(false)
  const error = ref<string | null>(null)

  async function fetchAvailabilityRange(
    providerId: string,
    startDate: Date,
    endDate: Date
  ) {
    loading.value = true
    error.value = null

    try {
      // Fetch availability for date range (single API call)
      const response = await availabilityService.getAvailabilityRange(
        providerId,
        startDate,
        endDate
      )

      // Store in map for quick lookup
      availability.value.clear()
      response.availability.forEach((day) => {
        const key = day.date.toISOString().split('T')[0]
        availability.value.set(key, day)
      })
    } catch (e) {
      error.value = 'خطا در دریافت اطلاعات موجودی'
      console.error(e)
    } finally {
      loading.value = false
    }
  }

  function getAvailabilityForDate(date: Date): DayAvailability | undefined {
    const key = date.toISOString().split('T')[0]
    return availability.value.get(key)
  }

  return {
    availability,
    loading,
    error,
    fetchAvailabilityRange,
    getAvailabilityForDate
  }
}
```

**Testing Requirements:**
- Visual regression testing (calendar colors)
- Accessibility testing (keyboard navigation, screen readers)
- Performance testing (90-day range load)

**Success Metrics:**
- Time to select date: < 5 seconds (vs. 15+ seconds without heatmap)
- "No availability" bounce rate: -30%
- Booking flow completion: +25%

---

### Week 9-10: Mobile Sticky Booking Bar + Hero Search

**Priority: P1 (High Impact)**

**Business Impact:** 25-30% increase in mobile conversion

#### Task 1: Mobile Sticky Booking Bar (3 days)

**File: `booksy-frontend/src/shared/components/layout/MobileBookingBar.vue`**

```vue
<template>
  <Teleport to="body">
    <div
      v-if="showBar"
      class="mobile-booking-bar"
      :class="{ 'has-draft': hasDraftBooking }"
    >
      <!-- Draft Booking State -->
      <div v-if="hasDraftBooking" class="booking-draft">
        <div class="draft-info">
          <span class="icon">📋</span>
          <div class="draft-text">
            <p class="draft-title">رزرو در حال انجام</p>
            <p class="draft-details">
              {{ draftBooking.serviceName }} - {{ draftBooking.dateTime }}
            </p>
          </div>
        </div>
        <button @click="continueDraftBooking" class="btn-continue">
          ادامه رزرو
        </button>
      </div>

      <!-- Quick Actions (when no draft) -->
      <nav v-else class="quick-actions">
        <button @click="openSearch" class="action-btn">
          <span class="icon">🔍</span>
          <span class="label">جستجو</span>
        </button>

        <button @click="openCalendar" class="action-btn">
          <span class="icon">📅</span>
          <span class="label">تقویم</span>
        </button>

        <button @click="openCategories" class="action-btn">
          <span class="icon">🏷️</span>
          <span class="label">دسته‌ها</span>
        </button>
      </nav>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { useBookingDraft } from '@/modules/booking/composables/useBookingDraft'

const router = useRouter()
const { draftBooking, hasDraft } = useBookingDraft()

const showBar = computed(() => {
  // Show on mobile devices only
  return window.innerWidth < 768
})

const hasDraftBooking = computed(() => hasDraft.value)

function continueDraftBooking() {
  router.push({ name: 'BookingConfirm', params: { id: draftBooking.value.id } })
}

function openSearch() {
  router.push({ name: 'Search' })
}

function openCalendar() {
  router.push({ name: 'Calendar' })
}

function openCategories() {
  router.push({ name: 'Categories' })
}
</script>

<style scoped>
.mobile-booking-bar {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  background: white;
  border-top: 1px solid #e5e7eb;
  box-shadow: 0 -4px 12px rgba(0, 0, 0, 0.08);
  z-index: 1000;
  padding: 12px 16px;
  padding-bottom: calc(12px + env(safe-area-inset-bottom)); /* iOS safe area */
}

/* Thumb zone optimization - buttons in lower third */
.quick-actions {
  display: flex;
  justify-content: space-around;
  gap: 8px;
}

.action-btn {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
  padding: 12px 8px;
  background: transparent;
  border: none;
  border-radius: 12px;
  cursor: pointer;
  transition: background 0.2s;
}

.action-btn:active {
  background: rgba(139, 92, 246, 0.1);
}

.action-btn .icon {
  font-size: 24px;
}

.action-btn .label {
  font-size: 12px;
  color: #6b7280;
  font-weight: 500;
}

/* Draft booking state */
.booking-draft {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.draft-info {
  display: flex;
  align-items: center;
  gap: 12px;
  flex: 1;
}

.draft-text {
  flex: 1;
}

.draft-title {
  font-weight: 600;
  font-size: 14px;
  color: #111827;
  margin: 0 0 4px 0;
}

.draft-details {
  font-size: 12px;
  color: #6b7280;
  margin: 0;
}

.btn-continue {
  padding: 10px 20px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  border: none;
  border-radius: 8px;
  font-weight: 600;
  font-size: 14px;
  white-space: nowrap;
  cursor: pointer;
}
</style>
```

---

#### Task 2: Hero Section with Contextual Search (4 days)

**File: `booksy-frontend/src/views/HomeView.vue` (Enhanced)**

```vue
<template>
  <div class="home-view">
    <!-- Hero Section with Search -->
    <section class="hero">
      <div class="hero-content">
        <h1 class="hero-title">
          رزرو آنلاین نوبت در <span class="highlight">60 ثانیه</span>
        </h1>
        <p class="hero-subtitle">
          بهترین آرایشگاه‌ها، سالن‌های زیبایی و مراکز سلامت در شهر شما
        </p>

        <!-- Trust Metrics -->
        <div class="trust-metrics">
          <div class="metric">
            <span class="value">{{ providerCount.toLocaleString('fa') }}+</span>
            <span class="label">مرکز خدماتی</span>
          </div>
          <div class="metric">
            <span class="value">{{ monthlyBookings.toLocaleString('fa') }}</span>
            <span class="label">رزرو ماهانه</span>
          </div>
          <div class="metric">
            <span class="value">{{ averageRating.toFixed(1) }}★</span>
            <span class="label">امتیاز متوسط</span>
          </div>
        </div>

        <!-- Contextual Search -->
        <div class="search-container">
          <div class="search-box">
            <!-- Location Input -->
            <div class="search-input location-input">
              <span class="icon">📍</span>
              <input
                v-model="searchLocation"
                type="text"
                placeholder="شهر یا محله"
                @focus="showLocationSuggestions = true"
              />
            </div>

            <!-- Service Input -->
            <div class="search-input service-input">
              <span class="icon">💇</span>
              <input
                v-model="searchService"
                type="text"
                placeholder="نوع خدمات (مثلا کوتاهی مو)"
                @focus="showServiceSuggestions = true"
              />
            </div>

            <!-- Search Button -->
            <button @click="performSearch" class="btn-search">
              جستجو <span class="arrow">←</span>
            </button>
          </div>

          <!-- Autocomplete Suggestions -->
          <LocationSuggestions
            v-if="showLocationSuggestions"
            v-model="searchLocation"
            @select="handleLocationSelect"
          />

          <ServiceSuggestions
            v-if="showServiceSuggestions"
            v-model="searchService"
            @select="handleServiceSelect"
          />
        </div>

        <!-- Popular Searches -->
        <div class="popular-searches">
          <span class="label">جستجوهای پرطرفدار:</span>
          <button
            v-for="search in popularSearches"
            :key="search.id"
            @click="quickSearch(search)"
            class="quick-search-btn"
          >
            {{ search.label }}
          </button>
        </div>
      </div>
    </section>

    <!-- Category Cards (Icon-Enhanced) -->
    <section class="categories">
      <h2 class="section-title">دسته‌بندی خدمات</h2>
      <div class="category-grid">
        <CategoryCard
          v-for="category in categories"
          :key="category.id"
          :category="category"
          @click="navigateToCategory(category)"
        />
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useGeolocation } from '@/shared/composables/useGeolocation'
import CategoryCard from '@/components/CategoryCard.vue'

const router = useRouter()
const { requestLocation, userCity } = useGeolocation()

// Search state
const searchLocation = ref('')
const searchService = ref('')
const showLocationSuggestions = ref(false)
const showServiceSuggestions = ref(false)

// Statistics (from API)
const providerCount = ref(250)
const monthlyBookings = ref(15000)
const averageRating = ref(4.8)

// Popular searches
const popularSearches = ref([
  { id: 1, label: 'کوتاهی مو', service: 'haircut', city: null },
  { id: 2, label: 'مانیکور', service: 'manicure', city: null },
  { id: 3, label: 'ماساژ', service: 'massage', city: null }
])

onMounted(async () => {
  // Request location permission
  await requestLocation()
  if (userCity.value) {
    searchLocation.value = userCity.value
  }

  // Load statistics
  await loadStatistics()
})

async function loadStatistics() {
  // API call to get platform statistics
  const stats = await fetch('/api/v1/statistics').then(r => r.json())
  providerCount.value = stats.totalProviders
  monthlyBookings.value = stats.monthlyBookings
  averageRating.value = stats.averageRating
}

function performSearch() {
  router.push({
    name: 'ProviderSearch',
    query: {
      city: searchLocation.value,
      service: searchService.value
    }
  })
}

function quickSearch(search: any) {
  searchService.value = search.label
  performSearch()
}
</script>

<style scoped>
.hero {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  padding: 60px 20px;
  text-align: center;
}

.hero-title {
  font-size: 36px;
  font-weight: 800;
  margin: 0 0 16px 0;
}

.hero-title .highlight {
  color: #fbbf24; /* yellow-400 */
}

.trust-metrics {
  display: flex;
  justify-content: center;
  gap: 40px;
  margin: 32px 0;
}

.metric {
  display: flex;
  flex-direction: column;
}

.metric .value {
  font-size: 24px;
  font-weight: 700;
}

.metric .label {
  font-size: 14px;
  opacity: 0.9;
}

.search-box {
  display: flex;
  gap: 12px;
  background: white;
  padding: 8px;
  border-radius: 16px;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.15);
  max-width: 800px;
  margin: 0 auto;
}

.search-input {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 12px 16px;
  flex: 1;
  background: #f9fafb;
  border-radius: 12px;
}

.search-input input {
  border: none;
  background: transparent;
  flex: 1;
  font-size: 16px;
  color: #111827;
  direction: rtl;
}

.search-input input:focus {
  outline: none;
}

.btn-search {
  padding: 16px 32px;
  background: #111827;
  color: white;
  border: none;
  border-radius: 12px;
  font-weight: 600;
  font-size: 16px;
  cursor: pointer;
  white-space: nowrap;
  transition: transform 0.2s;
}

.btn-search:hover {
  transform: scale(1.05);
}

.popular-searches {
  margin-top: 24px;
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 8px;
  align-items: center;
}

.quick-search-btn {
  padding: 8px 16px;
  background: rgba(255, 255, 255, 0.2);
  border: 1px solid rgba(255, 255, 255, 0.3);
  color: white;
  border-radius: 20px;
  font-size: 14px;
  cursor: pointer;
  transition: background 0.2s;
}

.quick-search-btn:hover {
  background: rgba(255, 255, 255, 0.3);
}

/* Responsive */
@media (max-width: 768px) {
  .search-box {
    flex-direction: column;
  }

  .trust-metrics {
    gap: 20px;
  }

  .metric .value {
    font-size: 20px;
  }
}
</style>
```

**Deliverables:**
- ✅ Mobile sticky bar with thumb-zone optimization
- ✅ Hero section with geolocation-based search
- ✅ Trust metrics API endpoint
- ✅ Popular searches feature

**Success Metrics:**
- Mobile bounce rate: -15-20%
- Search engagement: +25-30%
- Time to first action: < 3 seconds

---

### Week 11-12: Icon-Enhanced Category Cards + Progressive Location

**Priority: P2 (Medium Impact)**

#### Task 1: Icon-Enhanced Category Cards (3 days)

**Component: `CategoryCard.vue`**

```vue
<template>
  <div class="category-card" @click="handleClick" @mouseenter="showPreview = true" @mouseleave="showPreview = false">
    <!-- Icon -->
    <div class="category-icon" :style="{ background: category.color }">
      <span class="icon">{{ category.icon }}</span>
    </div>

    <!-- Info -->
    <div class="category-info">
      <h3 class="category-name">{{ category.name }}</h3>
      <p class="category-count">{{ category.providerCount }}+ ارائه‌دهنده</p>
    </div>

    <!-- Hover Preview -->
    <Transition name="fade">
      <div v-if="showPreview" class="category-preview">
        <p class="preview-title">محبوب‌ترین خدمات:</p>
        <ul class="service-list">
          <li v-for="service in category.popularServices" :key="service">
            {{ service }}
          </li>
        </ul>
        <p class="price-range">
          محدوده قیمت: {{ formatPrice(category.minPrice) }} - {{ formatPrice(category.maxPrice) }} تومان
        </p>
      </div>
    </Transition>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'

interface Props {
  category: {
    id: string
    name: string
    icon: string
    color: string
    providerCount: number
    popularServices: string[]
    minPrice: number
    maxPrice: number
  }
}

const props = defineProps<Props>()
const emit = defineEmits<{ (e: 'click'): void }>()

const showPreview = ref(false)

function handleClick() {
  emit('click')
}

function formatPrice(price: number): string {
  return (price / 10).toLocaleString('fa')
}
</script>

<style scoped>
.category-card {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 20px;
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 16px;
  cursor: pointer;
  transition: all 0.3s;
  position: relative;
  overflow: hidden;
}

.category-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 12px 24px rgba(0, 0, 0, 0.1);
  border-color: #667eea;
}

.category-icon {
  width: 64px;
  height: 64px;
  border-radius: 16px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 32px;
}

.category-info {
  flex: 1;
}

.category-name {
  font-size: 18px;
  font-weight: 700;
  color: #111827;
  margin: 0 0 4px 0;
}

.category-count {
  font-size: 14px;
  color: #6b7280;
  margin: 0;
}

.category-preview {
  position: absolute;
  top: 100%;
  left: 0;
  right: 0;
  background: white;
  border: 1px solid #e5e7eb;
  border-top: none;
  border-radius: 0 0 16px 16px;
  padding: 16px;
  box-shadow: 0 8px 16px rgba(0, 0, 0, 0.1);
  z-index: 10;
}

.preview-title {
  font-weight: 600;
  font-size: 14px;
  color: #111827;
  margin: 0 0 8px 0;
}

.service-list {
  list-style: none;
  padding: 0;
  margin: 0 0 12px 0;
}

.service-list li {
  padding: 4px 0;
  font-size: 13px;
  color: #6b7280;
}

.service-list li::before {
  content: '✓ ';
  color: #10b981;
  font-weight: bold;
}

.price-range {
  font-size: 13px;
  color: #8b5cf6;
  font-weight: 600;
  margin: 0;
}

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.3s;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
```

**Category Data (with icons):**

```typescript
const categories = [
  {
    id: 'salon',
    name: 'آرایشگاه زنانه',
    icon: '💇‍♀️',
    color: 'linear-gradient(135deg, #f093fb 0%, #f5576c 100%)',
    providerCount: 85,
    popularServices: ['کوتاهی و فشن', 'رنگ مو', 'کراتینه'],
    minPrice: 800000,
    maxPrice: 5000000
  },
  {
    id: 'barbershop',
    name: 'آرایشگاه مردانه',
    icon: '💈',
    color: 'linear-gradient(135deg, #4facfe 0%, #00f2fe 100%)',
    providerCount: 65,
    popularServices: ['کوتاهی مو', 'اصلاح صورت', 'اصلاح ریش'],
    minPrice: 300000,
    maxPrice: 1500000
  },
  // ... more categories
]
```

---

#### Task 2: Progressive Location Onboarding (2 days)

**Composable: `useGeolocation.ts`**

```typescript
// File: booksy-frontend/src/shared/composables/useGeolocation.ts

import { ref } from 'vue'

export function useGeolocation() {
  const userCity = ref<string | null>(null)
  const userCoordinates = ref<{ lat: number; lng: number } | null>(null)
  const permissionDenied = ref(false)
  const loading = ref(false)

  async function requestLocation() {
    loading.value = true

    try {
      const position = await new Promise<GeolocationPosition>((resolve, reject) => {
        navigator.geolocation.getCurrentPosition(resolve, reject)
      })

      userCoordinates.value = {
        lat: position.coords.latitude,
        lng: position.coords.longitude
      }

      // Reverse geocode to get city name
      const city = await reverseGeocode(userCoordinates.value)
      userCity.value = city

      // Store in localStorage
      localStorage.setItem('user_location', JSON.stringify({
        city,
        coordinates: userCoordinates.value,
        timestamp: Date.now()
      }))

      return { success: true, city }
    } catch (error) {
      permissionDenied.value = true
      return { success: false, error: 'دسترسی به موقعیت مکانی رد شد' }
    } finally {
      loading.value = false
    }
  }

  async function reverseGeocode(coords: { lat: number; lng: number }): Promise<string> {
    // Use Neshan API for Iran
    const response = await fetch(
      `https://api.neshan.org/v5/reverse?lat=${coords.lat}&lng=${coords.lng}`,
      {
        headers: {
          'Api-Key': import.meta.env.VITE_NESHAN_SERVICE_KEY
        }
      }
    )

    const data = await response.json()
    return data.city || data.state || 'تهران'
  }

  function loadSavedLocation() {
    const saved = localStorage.getItem('user_location')
    if (saved) {
      const { city, coordinates, timestamp } = JSON.parse(saved)

      // Location is valid for 7 days
      if (Date.now() - timestamp < 7 * 24 * 60 * 60 * 1000) {
        userCity.value = city
        userCoordinates.value = coordinates
        return true
      }
    }
    return false
  }

  return {
    userCity,
    userCoordinates,
    permissionDenied,
    loading,
    requestLocation,
    loadSavedLocation
  }
}
```

**Location Permission Modal:**

```vue
<template>
  <Modal v-model="show" :closable="true" @close="handleDeny">
    <div class="location-permission">
      <div class="icon">📍</div>
      <h2 class="title">دسترسی به موقعیت مکانی</h2>
      <p class="description">
        با اجازه دسترسی به موقعیت شما، می‌توانیم بهترین مراکز خدماتی نزدیک به شما را نمایش دهیم.
      </p>

      <div class="benefits">
        <div class="benefit">
          <span class="check">✓</span>
          <span>مراکز نزدیک به شما</span>
        </div>
        <div class="benefit">
          <span class="check">✓</span>
          <span>قیمت‌های محلی</span>
        </div>
        <div class="benefit">
          <span class="check">✓</span>
          <span>زمان رسیدن دقیق</span>
        </div>
      </div>

      <div class="actions">
        <button @click="handleAllow" class="btn-allow">
          اجازه دسترسی
        </button>
        <button @click="handleDeny" class="btn-deny">
          انتخاب دستی شهر
        </button>
      </div>

      <p class="privacy-note">
        <span class="icon">🔒</span>
        موقعیت شما ذخیره نمی‌شود و تنها برای جستجوی محلی استفاده می‌شود.
      </p>
    </div>
  </Modal>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useGeolocation } from '@/shared/composables/useGeolocation'
import Modal from '@/shared/components/Modal.vue'

const show = ref(true)
const { requestLocation } = useGeolocation()

const emit = defineEmits<{
  (e: 'allow', city: string): void
  (e: 'deny'): void
}>()

async function handleAllow() {
  const result = await requestLocation()
  if (result.success) {
    emit('allow', result.city!)
    show.value = false
  }
}

function handleDeny() {
  emit('deny')
  show.value = false
}
</script>

<style scoped>
.location-permission {
  text-align: center;
  padding: 32px;
  max-width: 480px;
}

.icon {
  font-size: 64px;
  margin-bottom: 16px;
}

.title {
  font-size: 24px;
  font-weight: 700;
  color: #111827;
  margin: 0 0 12px 0;
}

.description {
  font-size: 16px;
  color: #6b7280;
  line-height: 1.6;
  margin: 0 0 24px 0;
}

.benefits {
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin-bottom: 32px;
}

.benefit {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 12px 16px;
  background: #f3f4f6;
  border-radius: 8px;
  text-align: right;
}

.benefit .check {
  color: #10b981;
  font-weight: bold;
  font-size: 18px;
}

.actions {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.btn-allow {
  padding: 16px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  border: none;
  border-radius: 12px;
  font-weight: 600;
  font-size: 16px;
  cursor: pointer;
}

.btn-deny {
  padding: 16px;
  background: transparent;
  color: #6b7280;
  border: 1px solid #d1d5db;
  border-radius: 12px;
  font-weight: 600;
  font-size: 16px;
  cursor: pointer;
}

.privacy-note {
  margin-top: 24px;
  font-size: 13px;
  color: #9ca3af;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
}
</style>
```

---

## Phase 3: Reviews & Trust Signals (Weeks 13-14)
**Goal:** Build social proof and trust mechanisms
**Team:** 1 Backend Dev, 1 Frontend Dev

### Week 13-14: Reviews & Ratings System

**Priority: P3 (Medium-Low Impact)**

**Database Schema:**

```sql
CREATE TABLE "ServiceCatalog"."Reviews" (
    "Id" UUID PRIMARY KEY,
    "ProviderId" UUID NOT NULL,
    "CustomerId" UUID NOT NULL,
    "BookingId" UUID NOT NULL,
    "Rating" DECIMAL(2,1) NOT NULL CHECK ("Rating" >= 1.0 AND "Rating" <= 5.0),
    "Comment" TEXT,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    "IsVerified" BOOLEAN NOT NULL DEFAULT TRUE,
    "HelpfulCount" INT NOT NULL DEFAULT 0,
    "ResponseFromProvider" TEXT NULL,
    "ResponseAt" TIMESTAMP WITH TIME ZONE NULL,

    CONSTRAINT "FK_Reviews_Providers" FOREIGN KEY ("ProviderId")
        REFERENCES "ServiceCatalog"."Providers"("Id"),
    CONSTRAINT "FK_Reviews_Customers" FOREIGN KEY ("CustomerId")
        REFERENCES "UserManagement"."Users"("Id"),
    CONSTRAINT "FK_Reviews_Bookings" FOREIGN KEY ("BookingId")
        REFERENCES "ServiceCatalog"."Bookings"("Id")
);

CREATE INDEX "IX_Reviews_ProviderId" ON "ServiceCatalog"."Reviews" ("ProviderId");
CREATE INDEX "IX_Reviews_Rating" ON "ServiceCatalog"."Reviews" ("Rating");
```

**API Endpoints:**

```http
GET  /api/v1/providers/{id}/reviews?page=1&sortBy=recent
POST /api/v1/reviews
POST /api/v1/reviews/{id}/helpful
POST /api/v1/reviews/{id}/response (Provider only)
```

---

## Phase 4: Polish & Accessibility (Weeks 15-16)
**Goal:** Production readiness and accessibility compliance
**Team:** 1 Frontend Dev (Full-time), QA support

### Week 15: Accessibility Audit & Fixes

**Tasks:**
1. **Calendar Accessibility** (3 days)
   - ARIA grid pattern implementation
   - Keyboard navigation (arrow keys)
   - Screen reader announcements

2. **Form Accessibility** (2 days)
   - Error message association
   - Autocomplete attributes
   - Focus management

**Deliverables:**
- ✅ WCAG 2.2 AA compliance
- ✅ Screen reader testing (NVDA, VoiceOver)
- ✅ Keyboard navigation testing

### Week 16: Performance Optimization & Launch Prep

**Tasks:**
1. **Performance Optimization** (2 days)
   - Image lazy loading
   - Code splitting
   - API response caching

2. **E2E Testing** (2 days)
   - Critical path tests (search → book → confirm)
   - Mobile booking flow
   - Error scenarios

3. **Documentation** (1 day)
   - API documentation finalization
   - Developer onboarding guide
   - Operations runbook

---

## Technical Decisions

### Backend

| Decision | Option Selected | Rationale |
|----------|----------------|-----------|
| **Caching** | Redis | Industry standard, supports distributed cache, pub/sub for invalidation |
| **Background Jobs** | Hangfire | .NET native, persistent jobs, dashboard UI, supports recurring tasks |
| **Real-time Updates** | Polling (Phase 1), SignalR (Phase 2+) | Start simple with 30s polling, add WebSocket when needed |

### Frontend

| Decision | Option Selected | Rationale |
|----------|----------------|-----------|
| **State Management** | Pinia | Vue 3 official, TypeScript support, dev tools integration |
| **Date Library** | Custom Jalali utils | Already implemented, no external dependency |
| **Testing** | Vitest + Cypress | Fast unit tests, reliable E2E testing |
| **Styling** | Scoped CSS (existing) | Already in use, consider Tailwind in Phase 3+ |

---

## Resource Allocation

### Team Capacity Assumptions

**Backend Team (2 developers):**
- Senior Backend Dev: 40 hrs/week
- Mid-level Backend Dev: 40 hrs/week
- **Total:** 80 hrs/week = 320 hrs/month

**Frontend Team (2 developers):**
- Senior Frontend Dev: 40 hrs/week
- Mid-level Frontend Dev: 40 hrs/week
- **Total:** 80 hrs/week = 320 hrs/month

### Phase Resource Distribution

| Phase | Backend Hours | Frontend Hours | Total Weeks |
|-------|---------------|----------------|-------------|
| **Phase 1** | 320 hrs (80%) | 80 hrs (20%) | 6 weeks |
| **Phase 2** | 120 hrs (30%) | 280 hrs (70%) | 6 weeks |
| **Phase 3** | 120 hrs (60%) | 80 hrs (40%) | 2 weeks |
| **Phase 4** | 40 hrs (20%) | 160 hrs (80%) | 2 weeks |

---

## Success Metrics & KPIs

### Phase 1 (Backend Foundations)
- ✅ API response time: < 200ms (p95)
- ✅ Availability API handles 100 concurrent requests
- ✅ Booking concurrency: 0% double-bookings in load testing
- ✅ Test coverage: > 90% for critical paths

### Phase 2 (Frontend Features)
- ✅ Mobile bounce rate: -15-20%
- ✅ Time to first booking action: < 5 seconds
- ✅ Calendar date selection: < 5 seconds
- ✅ Search engagement: +25-30%

### Phase 3 (Reviews & Trust)
- ✅ Review submission rate: 40-60% of completed bookings
- ✅ Provider response rate: > 70%
- ✅ Trust signal click-through: +10-15%

### Phase 4 (Polish & Accessibility)
- ✅ WCAG 2.2 AA compliance: 100%
- ✅ Lighthouse performance score: > 90
- ✅ Keyboard navigation success: 95%+
- ✅ Core Web Vitals: Pass all metrics

---

## Risk Mitigation

### High-Risk Items

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| **Concurrency bugs in booking** | Critical | Medium | Extensive load testing, transaction isolation |
| **Availability data performance** | High | Medium | Pre-calculate summaries, aggressive caching |
| **Mobile calendar UX issues** | High | Low | Early user testing, responsive design testing |
| **Accessibility regression** | Medium | Medium | Automated axe testing in CI/CD |

---

## Go/No-Go Decision Points

### After Phase 1 (Week 6)
**Decision:** Proceed to Phase 2 if:
- ✅ All 3 core APIs deployed to staging
- ✅ Load testing passes (100 concurrent users)
- ✅ Seed data generates successfully
- ✅ Zero critical bugs in API

### After Phase 2 (Week 12)
**Decision:** Proceed to Phase 3 if:
- ✅ Mobile booking completion rate > 5%
- ✅ Calendar interaction time < 10 seconds
- ✅ No P1 bugs in production
- ✅ User acceptance testing positive feedback

---

## Timeline Summary

```
Week 1-2   ████████░░░░░░░░░░░░  Seed Data Enhancement
Week 3-4   ░░░░░░░░████████░░░░  Availability & Booking APIs
Week 5-6   ░░░░░░░░░░░░████████  Provider Search API
Week 7-8   ██████░░░░░░░░░░░░░░  Smart Calendar + Heatmap
Week 9-10  ░░░░░░██████░░░░░░░░  Mobile Bar + Hero Search
Week 11-12 ░░░░░░░░░░░░██████░░  Category Cards + Location
Week 13-14 ░░░░██████░░░░░░░░░░  Reviews & Ratings
Week 15    ░░░░░░░░░░████░░░░░░  Accessibility Fixes
Week 16    ░░░░░░░░░░░░░░██████  Performance & Launch Prep

Legend:
████  Backend work
████  Frontend work
```

---

## Appendix: Deferred Improvements

These improvements are valuable but deprioritized for resource constraints:

1. **Simplified Footer Redesign** (P4)
   - Low user interaction with footer
   - Can be done in maintenance sprint

2. **Contextual Blog Content** (P4)
   - Content strategy not yet defined
   - Requires content team involvement

3. **Full Accessibility Audit** (P4)
   - Core calendar accessibility prioritized
   - Full audit in post-launch phase

4. **Dynamic Trust Signals** (P3)
   - Requires real-time booking stream
   - Better after user volume increases

---

## Next Steps (Immediate Actions)

### This Week
1. **Product Director:**
   - Review and approve this roadmap
   - Align with business goals for Q1 2026
   - Confirm success metrics

2. **CTO:**
   - Review technical decisions (Redis, Hangfire, etc.)
   - Allocate backend/frontend developer resources
   - Set up staging environment for Phase 1

3. **Development Team:**
   - Kick off Phase 1 Sprint 1 (Seed Data Enhancement)
   - Set up project tracking (JIRA/Linear)
   - Schedule daily standups

### Week 2
- Complete seed data enhancement
- Begin Availability API development
- Frontend team: Documentation review & API contract alignment

---

**Document Status:** ✅ Ready for Executive Review
**Prepared By:** Product Director & CTO
**Review Date:** 2025-11-15
**Next Review:** After Phase 1 completion (Week 6)
