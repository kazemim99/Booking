# Customer Profile Page - Design Document

## Context

The Booksy platform has matured with comprehensive provider-facing features (dashboard, registration, business management) but lacks equivalent sophistication in the customer experience. Customers currently have basic authentication and booking capabilities but no unified interface for account management, booking history, or personalization.

This design document outlines the technical approach for implementing a customer profile page that provides feature parity with the provider experience while maintaining architectural consistency with existing bounded contexts (DDD), CQRS patterns, and event-driven communication.

**Stakeholders:**
- Customers: Primary users needing account management
- Development Team: Must implement within existing architecture
- Support Team: Will see reduced support burden
- Product Team: Enabling future personalization features

**Constraints:**
- Must follow existing DDD/CQRS patterns
- Must support Persian RTL UI consistently
- Must maintain <2s page load performance
- Must be GDPR compliant (data export, deletion)
- Must integrate with existing authentication (phone-based OTP)

---

## Goals / Non-Goals

### Goals
1. **Unified Customer Dashboard**: Single entry point for all customer account management tasks
2. **Booking Management**: Comprehensive view of past, current, and upcoming appointments
3. **Personalization Foundation**: Store preferences, favorites, and settings for future recommendation features
4. **Self-Service**: Reduce support tickets by enabling customer self-management
5. **Trust Building**: Display verification status, stats, and transparency to build confidence
6. **Performance**: <2s initial load, lazy loading for non-critical sections
7. **Accessibility**: WCAG 2.1 AA compliance with full RTL support

### Non-Goals
1. **Social Features**: No customer-to-customer messaging or social network (future phase)
2. **Advanced Analytics**: No deep spending insights or predictive analytics (basic stats only)
3. **Subscription Management**: No recurring subscription plans (not in scope for v1)
4. **Multi-Account**: No family accounts or business customer accounts (v1 is individual only)
5. **Gamification**: No badges, achievements, or social sharing (basic loyalty points only)
6. **Provider Tools**: This is customer-only; no provider features in this change

---

## Decisions

### Decision 1: Bounded Context Placement

**Options Considered:**

**A. New CustomerManagement Bounded Context**
- Pros: Clean separation, dedicated domain, independent scaling
- Cons: Additional infrastructure, cross-context queries complexity

**B. Extend UserManagement Bounded Context**
- Pros: Leverages existing auth, simpler queries, shared user data
- Cons: Context becomes larger, potential coupling

**C. Extend ServiceCatalog Bounded Context**
- Pros: Close to booking data, easier history queries
- Cons: ServiceCatalog already large, conceptual mismatch

**Decision: Option B - Extend UserManagement Bounded Context**

**Rationale:**
- Customer profile is fundamentally an extension of user identity
- UserManagement already handles authentication, roles, and basic user data
- Avoids cross-context complexity for frequently accessed data (name, email, phone)
- Booking history can use cross-context queries (CustomerManagement → ServiceCatalog)
- Allows gradual growth without immediate infrastructure overhead
- Can be extracted to separate context later if needed

**Implementation:**
```
UserManagement/
├── Domain/
│   ├── Aggregates/
│   │   ├── User/ (existing)
│   │   ├── CustomerProfile/ (NEW)
│   │   └── FavoriteProvider/ (NEW)
│   ├── ValueObjects/
│   │   └── CustomerPreferences/ (NEW)
├── Application/
│   ├── Commands/
│   │   ├── UpdateCustomerProfile/
│   │   ├── AddFavoriteProvider/
│   │   └── UpdateNotificationPreferences/
│   ├── Queries/
│   │   ├── GetCustomerProfile/
│   │   ├── GetCustomerStats/
│   │   └── GetFavoriteProviders/
```

### Decision 2: Booking History Query Strategy

**Options Considered:**

**A. Direct Database Queries**
- Pros: Simple, fast for small datasets
- Cons: Violates bounded context boundaries, tight coupling

**B. Integration Events for Caching**
- Pros: Maintains boundaries, enables caching
- Cons: Eventual consistency, cache invalidation complexity

**C. API-to-API Calls with Orchestration**
- Pros: Clean boundaries, real-time data
- Cons: Network overhead, latency

**Decision: Option B - Integration Events with Read Model**

**Rationale:**
- Booking history rarely changes after completion (immutable historical data)
- Eventual consistency acceptable for historical views
- ServiceCatalog publishes `BookingCreatedEvent`, `BookingCompletedEvent`, etc.
- UserManagement subscribes and maintains a `CustomerBookingHistory` read model
- Optimized for read-heavy workload (customers viewing history)
- Enables advanced filtering/sorting without cross-context joins

**Implementation:**
```csharp
// ServiceCatalog publishes
public class BookingCompletedEvent : IntegrationEvent
{
    public Guid BookingId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid ProviderId { get; set; }
    public string ServiceName { get; set; }
    public DateTime StartTime { get; set; }
    public decimal TotalPrice { get; set; }
    // ... other fields
}

// UserManagement subscribes
[CapSubscribe("booking.completed")]
public async Task HandleBookingCompleted(BookingCompletedEvent @event)
{
    var history = new CustomerBookingHistoryEntry
    {
        BookingId = @event.BookingId,
        CustomerId = @event.CustomerId,
        // ... map event data
    };
    await _repository.AddHistoryEntry(history);
}
```

### Decision 3: Frontend State Management

**Options Considered:**

**A. Single Pinia Store (customer.store.ts)**
- Pros: Centralized, simple
- Cons: Large store, tight coupling

**B. Multiple Pinia Stores by Feature**
- Pros: Modular, lazy loading, clear boundaries
- Cons: More boilerplate

**C. Composition API Only (No Pinia)**
- Pros: Lightweight, Vue 3 native
- Cons: Harder to share state, no devtools

**Decision: Option B - Multiple Pinia Stores**

**Rationale:**
- Aligns with existing pattern (auth.store, provider.store exist)
- Enables lazy loading of store modules
- Clear separation of concerns (profile, bookings, favorites)
- Better TypeScript support with typed stores
- Vue devtools integration for debugging

**Implementation:**
```typescript
// stores/modules/customer-profile.store.ts
export const useCustomerProfileStore = defineStore('customerProfile', {
  state: () => ({
    profile: null as CustomerProfile | null,
    loading: false,
    error: null
  }),
  actions: {
    async fetchProfile() { /* ... */ },
    async updateProfile(data: UpdateProfileRequest) { /* ... */ }
  }
})

// stores/modules/customer-bookings.store.ts
export const useCustomerBookingsStore = defineStore('customerBookings', {
  state: () => ({
    upcomingBookings: [] as Booking[],
    historyPage: 1,
    historyData: [] as Booking[],
    filters: { /* ... */ }
  }),
  actions: {
    async fetchUpcoming() { /* ... */ },
    async fetchHistory(page: number) { /* ... */ }
  }
})

// stores/modules/customer-favorites.store.ts
export const useCustomerFavoritesStore = defineStore('customerFavorites', {
  state: () => ({
    favorites: [] as FavoriteProvider[],
    sortBy: 'recent' as SortOption
  }),
  actions: {
    async addFavorite(providerId: string) { /* ... */ },
    async removeFavorite(providerId: string) { /* ... */ }
  }
})
```

### Decision 4: Avatar Upload Strategy

**Options Considered:**

**A. Direct Upload to Backend**
- Pros: Simple, centralized control
- Cons: Backend bandwidth, processing overhead

**B. Client-Side Crop + S3 Direct Upload**
- Pros: Reduces backend load, faster
- Cons: S3 credentials management, security

**C. Client-Side Crop + Backend Pre-Signed URL**
- Pros: Secure, efficient, scalable
- Cons: More complex flow

**Decision: Option C - Client Crop + Backend Pre-Signed URL**

**Rationale:**
- Client crops/resizes image using `vue-advanced-cropper` or similar
- Client requests pre-signed S3 URL from backend
- Client uploads directly to S3 via pre-signed URL
- Client notifies backend of successful upload
- Backend validates and updates profile with S3 URL
- Reduces backend load, leverages CDN (CloudFront) for serving

**Flow:**
```
1. User selects image → Client validates (size, format)
2. Client shows crop UI → User crops
3. Client requests /api/v1/customers/avatar/upload-url
4. Backend generates S3 pre-signed URL (15min expiry)
5. Client uploads to S3 using pre-signed URL
6. Client calls /api/v1/customers/profile PATCH with S3 key
7. Backend validates S3 key exists, updates profile
8. Client displays avatar from CDN URL
```

### Decision 5: Loyalty Points Architecture

**Options Considered:**

**A. Simple Counter in Profile**
- Pros: Simple implementation
- Cons: No auditability, no transaction history

**B. Event-Sourced Points Ledger**
- Pros: Full audit trail, time-travel queries
- Cons: Complex, overkill for v1

**C. Transaction-Based Ledger with Balance Cache**
- Pros: Balance, audit trail, performant reads
- Cons: Moderate complexity

**Decision: Option C - Transaction Ledger with Cached Balance**

**Rationale:**
- `loyalty_transactions` table stores all point changes (earned, redeemed, expired)
- `customer_profiles.loyalty_points` stores cached current balance
- Transactional consistency: increment balance when transaction created
- Enables point expiration (scheduled job finds expiring points, creates deduction transaction)
- Provides transparency to customers (view transaction history)

**Schema:**
```sql
CREATE TABLE loyalty_transactions (
    id UUID PRIMARY KEY,
    customer_id UUID NOT NULL REFERENCES users(id),
    transaction_type VARCHAR(50) NOT NULL, -- 'earned', 'redeemed', 'expired', 'adjusted'
    points_amount INT NOT NULL, -- positive for earn, negative for redeem
    balance_after INT NOT NULL,
    reason VARCHAR(255), -- 'Booking completed', 'Redeemed for discount', etc.
    related_booking_id UUID,
    related_redemption_id UUID,
    expires_at TIMESTAMPTZ, -- for earned points
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_loyalty_customer_date ON loyalty_transactions(customer_id, created_at DESC);
CREATE INDEX idx_loyalty_expiring ON loyalty_transactions(expires_at) WHERE transaction_type = 'earned';
```

---

## Risks / Trade-offs

### Risk 1: Query Performance for Booking History
**Risk:** Customers with hundreds of bookings may experience slow page loads

**Mitigation:**
- Implement pagination (20 results per page)
- Create composite index on (customer_id, start_time DESC)
- Use cursor-based pagination for efficient deep paging
- Consider read-through cache in Redis for first page
- Monitor slow query log, optimize as needed

**Trade-off:** Eventual consistency (read model) vs real-time accuracy
- **Accept**: Historical data is inherently immutable, 1-2 second delay acceptable
- **Benefit**: Avoids cross-context synchronous calls

### Risk 2: Cross-Context Data Consistency
**Risk:** Favorite provider data out of sync if provider deleted

**Mitigation:**
- Subscribe to `ProviderDeletedEvent` in UserManagement
- Soft-delete favorite relationships (mark as inactive)
- Display "Provider no longer available" in UI
- Periodic cleanup job removes old inactive favorites (>90 days)

**Trade-off:** Eventual consistency vs immediate consistency
- **Accept**: Favorite marked inactive within 1-2 seconds of provider deletion
- **Benefit**: No distributed transactions, simpler error handling

### Risk 3: Avatar Upload Abuse
**Risk:** Users upload inappropriate images or excessively large files

**Mitigation:**
- Client-side validation: max 5MB, JPEG/PNG only
- Backend re-validation before pre-signed URL generation
- Image processing: resize to 256x256, strip EXIF data
- Content moderation: integrate AWS Rekognition for inappropriate content detection
- Rate limiting: max 5 avatar changes per hour per user

**Trade-off:** Manual moderation vs automated moderation
- **Accept**: Automated + manual review for flagged images
- **Benefit**: Scalable, reduces manual work

### Risk 4: GDPR Data Export Complexity
**Risk:** Customer data spread across multiple contexts makes export complex

**Mitigation:**
- Implement `DataExportService` in UserManagement
- Orchestrates calls to other contexts (ServiceCatalog for bookings, etc.)
- Generates ZIP file with JSON exports per domain
- Stores export in S3 with time-limited download link
- Background job processes export requests (avoid timeout)

**Trade-off:** Synchronous vs asynchronous export
- **Accept**: Async export with email notification when ready
- **Benefit**: Handles large datasets, avoids gateway timeouts

### Risk 5: Mobile Performance with Large Lists
**Risk:** Rendering hundreds of booking history items causes jank on mobile

**Mitigation:**
- Implement virtual scrolling (vue-virtual-scroller)
- Lazy load images with Intersection Observer
- Use `<KeepAlive>` for tab switching (avoid re-render)
- Debounce scroll events (100ms)
- Progressive image loading (blur placeholder → low-res → full)

**Trade-off:** Simple list vs virtual scrolling complexity
- **Accept**: Virtual scrolling for lists >50 items
- **Benefit**: Smooth 60fps scrolling even with 1000+ items

---

## Migration Plan

### Phase 1: Database Schema (Week 1)
1. Create migration for new tables (`customer_profiles`, `favorite_providers`, `loyalty_transactions`)
2. Seed existing users with default customer profile entries
3. Add indexes for query optimization
4. Deploy to staging, validate data migration

### Phase 2: Backend API (Week 2-3)
1. Implement CustomerProfile aggregate, repositories
2. Implement command handlers (UpdateProfile, AddFavorite, etc.)
3. Implement query handlers (GetProfile, GetFavorites, GetBookingHistory)
4. Add CustomersController with endpoints
5. Write integration tests for all endpoints
6. Document API in Swagger

### Phase 3: Frontend Core (Week 3-4)
1. Create customer module structure
2. Implement ProfilePage.vue with tab navigation
3. Implement stores (profile, bookings, favorites)
4. Implement API services
5. Add Persian translations (100+ keys)
6. Add routing and navigation menu item

### Phase 4: Frontend Components (Week 4-5)
1. ProfileHeader with avatar upload
2. UpcomingBookingsWidget
3. BookingsTab with history table
4. FavoritesTab with provider grid
5. SettingsPanel with preferences forms
6. Mobile responsive testing

### Phase 5: Integration & Polish (Week 5-6)
1. Integrate favorite providers with provider pages (add heart icon)
2. Connect booking history to rebooking flow
3. Implement notification preferences (connect to notification service)
4. Add loyalty points display (if loyalty feature exists)
5. Performance optimization (lazy loading, caching)
6. Accessibility audit (WCAG compliance)
7. Cross-browser testing (Chrome, Safari, Firefox on desktop/mobile)

### Phase 6: Deployment (Week 6)
1. Deploy backend to staging
2. Deploy frontend to staging
3. UAT testing with sample users
4. Performance testing (load test with 1000 concurrent users)
5. Security audit (OWASP top 10)
6. Deploy to production with feature flag
7. Monitor error rates, performance metrics
8. Gradual rollout (10% → 50% → 100%)

---

## Open Questions

1. **Loyalty Points Algorithm**: How are points earned? (Per booking amount, fixed per booking, tier-based?)
   - **Proposal**: 1 point per 10,000 تومان spent, tier multipliers (Silver 1.5x, Gold 2x)
   - **Decision needed from**: Product team

2. **Data Retention Policy**: How long to keep booking history?
   - **Proposal**: Indefinite history, archive bookings >3 years old to cold storage
   - **Decision needed from**: Legal/compliance team

3. **Avatar Image Moderation**: Automated only or manual review?
   - **Proposal**: Automated flagging (AWS Rekognition), manual review for flagged images
   - **Decision needed from**: Trust & Safety team

4. **Favorite Providers Limit**: Cap at N favorites?
   - **Proposal**: No limit, but show top 50 by default with "Load more"
   - **Decision needed from**: Product team

5. **Multi-Language Support**: English translation alongside Persian?
   - **Proposal**: Persian only for v1, English in Phase 2
   - **Decision needed from**: Product/Marketing team

6. **Payment Methods Storage**: Build custom or use Stripe's saved cards?
   - **Proposal**: Leverage Stripe Payment Methods API (tokenized, PCI compliant)
   - **Decision needed from**: Engineering lead

---

## Alternatives Considered

### Alternative 1: SPA vs Server-Side Rendering
**Considered**: Next.js/Nuxt for SSR/SSG
**Rejected**: Would require rewriting entire frontend; Vue 3 SPA with route-based code splitting is sufficient for performance; SEO not critical for auth-gated profile pages

### Alternative 2: GraphQL vs REST
**Considered**: GraphQL for flexible querying (especially for booking history filters)
**Rejected**: Team lacks GraphQL expertise; REST with well-designed endpoints sufficient; avoid introducing new tech stack complexity

### Alternative 3: Real-Time Updates via WebSockets
**Considered**: Live booking status updates on profile page
**Rejected**: Overkill for v1; standard polling or manual refresh acceptable; WebSockets add infrastructure complexity; can add later if needed

### Alternative 4: Native Mobile Apps vs PWA
**Considered**: React Native or Flutter for native customer mobile app
**Rejected**: PWA with excellent mobile responsiveness is more cost-effective; can leverage existing Vue 3 codebase; native apps can be Phase 2 if user demand exists

---

## Success Metrics

### Technical Metrics
- Profile page load time: <2 seconds (p95)
- Booking history query: <500ms (p95)
- Avatar upload: <5 seconds end-to-end
- Zero N+1 query problems (enforce in code review)
- <0.5% error rate for all customer endpoints

### User Experience Metrics
- Profile completion rate: >80% (users with avatar + full info)
- Favorites usage: >30% of users save at least 1 favorite
- Settings engagement: >50% of users visit settings within first week
- Booking history views: >60% of users view history after booking
- Support ticket reduction: 20% fewer account management tickets

### Business Metrics
- Customer retention: +10% month-over-month
- Rebooking rate: +15% (via favorites and history)
- Average session duration: +20%
- User satisfaction (NPS): +5 points
