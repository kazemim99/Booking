# Customer Profile Page - Implementation Tasks

## 1. Database Schema & Migrations

- [ ] 1.1 Create migration for `customer_profiles` table
  - [ ] Add columns: customer_id (FK to users), avatar_url, birth_date, gender, preferences_json, loyalty_points, tier, created_at, updated_at
  - [ ] Add unique index on customer_id
  - [ ] Create trigger to initialize profile for new users
- [ ] 1.2 Create migration for `favorite_providers` table
  - [ ] Add columns: id (PK), customer_id (FK), provider_id (FK), created_at, last_visited_at, total_bookings
  - [ ] Add composite unique index on (customer_id, provider_id)
  - [ ] Add index on customer_id for fast lookups
- [ ] 1.3 Create migration for `loyalty_transactions` table
  - [ ] Add columns: id (PK), customer_id (FK), transaction_type, points_amount, balance_after, reason, related_booking_id, expires_at, created_at
  - [ ] Add index on (customer_id, created_at DESC)
  - [ ] Add index on expires_at for expiration job
- [ ] 1.4 Create migration for `customer_booking_history` read model
  - [ ] Add columns: booking_id (PK), customer_id, provider_id, provider_name, service_name, staff_name, start_time, status, total_price, created_at
  - [ ] Add composite index on (customer_id, start_time DESC)
  - [ ] Add index on status for filtering
- [ ] 1.5 Seed existing users with default customer profiles
- [ ] 1.6 Test migrations on staging database
- [ ] 1.7 Verify rollback procedures

## 2. Backend - Domain Layer

- [ ] 2.1 Create CustomerProfile aggregate
  - [ ] CustomerProfile.cs with properties (FullName, Email, AvatarUrl, BirthDate, Gender, LoyaltyPoints, Tier)
  - [ ] UpdateProfile() method
  - [ ] UpdateAvatar() method
  - [ ] AddLoyaltyPoints() method
  - [ ] RedeemLoyaltyPoints() method
- [ ] 2.2 Create FavoriteProvider aggregate
  - [ ] FavoriteProvider.cs with properties (CustomerId, ProviderId, CreatedAt, LastVisitedAt, TotalBookings)
  - [ ] AddFavorite() method
  - [ ] RemoveFavorite() method
  - [ ] UpdateVisitStats() method
- [ ] 2.3 Create CustomerPreferences value object
  - [ ] NotificationSettings (SmsEnabled, EmailEnabled, PushEnabled)
  - [ ] PrivacySettings (ProfileVisibility, AllowRecommendations)
  - [ ] ReminderTiming enum
- [ ] 2.4 Create domain events
  - [ ] CustomerProfileUpdatedEvent
  - [ ] FavoriteProviderAddedEvent
  - [ ] FavoriteProviderRemovedEvent
  - [ ] LoyaltyPointsEarnedEvent
  - [ ] LoyaltyPointsRedeemedEvent

## 3. Backend - Application Layer (Commands)

- [ ] 3.1 Implement UpdateCustomerProfile command
  - [ ] UpdateCustomerProfileCommand.cs
  - [ ] UpdateCustomerProfileCommandValidator.cs (FluentValidation)
  - [ ] UpdateCustomerProfileCommandHandler.cs
  - [ ] Write unit tests
- [ ] 3.2 Implement UpdateCustomerAvatar command
  - [ ] UpdateCustomerAvatarCommand.cs
  - [ ] UpdateCustomerAvatarCommandHandler.cs (request S3 pre-signed URL)
  - [ ] Write unit tests
- [ ] 3.3 Implement AddFavoriteProvider command
  - [ ] AddFavoriteProviderCommand.cs
  - [ ] AddFavoriteProviderCommandValidator.cs
  - [ ] AddFavoriteProviderCommandHandler.cs
  - [ ] Write unit tests
- [ ] 3.4 Implement RemoveFavoriteProvider command
  - [ ] RemoveFavoriteProviderCommand.cs
  - [ ] RemoveFavoriteProviderCommandHandler.cs
  - [ ] Write unit tests
- [ ] 3.5 Implement UpdateNotificationPreferences command
  - [ ] UpdateNotificationPreferencesCommand.cs
  - [ ] UpdateNotificationPreferencesCommandHandler.cs
  - [ ] Write unit tests

## 4. Backend - Application Layer (Queries)

- [ ] 4.1 Implement GetCustomerProfile query
  - [ ] GetCustomerProfileQuery.cs
  - [ ] GetCustomerProfileQueryHandler.cs
  - [ ] CustomerProfileViewModel.cs (DTO)
  - [ ] Write unit tests
- [ ] 4.2 Implement GetUpcomingBookings query
  - [ ] GetUpcomingBookingsQuery.cs (top 3, sorted by date)
  - [ ] GetUpcomingBookingsQueryHandler.cs
  - [ ] UpcomingBookingViewModel.cs
  - [ ] Write unit tests
- [ ] 4.3 Implement GetCustomerBookingHistory query
  - [ ] GetCustomerBookingHistoryQuery.cs (with pagination, filters)
  - [ ] GetCustomerBookingHistoryQueryHandler.cs
  - [ ] BookingHistoryViewModel.cs
  - [ ] Write unit tests
- [ ] 4.4 Implement GetFavoriteProviders query
  - [ ] GetFavoriteProvidersQuery.cs
  - [ ] GetFavoriteProvidersQueryHandler.cs
  - [ ] FavoriteProviderViewModel.cs
  - [ ] Write unit tests
- [ ] 4.5 Implement GetCustomerStatistics query
  - [ ] GetCustomerStatisticsQuery.cs
  - [ ] GetCustomerStatisticsQueryHandler.cs (total bookings, total spent, loyalty points)
  - [ ] CustomerStatisticsViewModel.cs
  - [ ] Write unit tests
- [ ] 4.6 Implement GetLoyaltyTransactions query
  - [ ] GetLoyaltyTransactionsQuery.cs
  - [ ] GetLoyaltyTransactionsQueryHandler.cs
  - [ ] LoyaltyTransactionViewModel.cs
  - [ ] Write unit tests

## 5. Backend - Infrastructure Layer

- [ ] 5.1 Create CustomerProfile repository
  - [ ] ICustomerProfileRepository.cs interface
  - [ ] CustomerProfileRepository.cs implementation
  - [ ] CustomerProfileConfiguration.cs (EF Core)
- [ ] 5.2 Create FavoriteProvider repository
  - [ ] IFavoriteProviderRepository.cs interface
  - [ ] FavoriteProviderRepository.cs implementation
  - [ ] FavoriteProviderConfiguration.cs (EF Core)
- [ ] 5.3 Create LoyaltyTransaction repository
  - [ ] ILoyaltyTransactionRepository.cs interface
  - [ ] LoyaltyTransactionRepository.cs implementation
  - [ ] LoyaltyTransactionConfiguration.cs (EF Core)
- [ ] 5.4 Implement integration event handlers
  - [ ] BookingCompletedEventHandler.cs (add to history read model, award points)
  - [ ] BookingCancelledEventHandler.cs (update history status)
  - [ ] ProviderDeletedEventHandler.cs (soft-delete favorites)
- [ ] 5.5 Implement S3 service for avatar uploads
  - [ ] IAvatarStorageService.cs interface
  - [ ] S3AvatarStorageService.cs (generate pre-signed URLs)
  - [ ] Configure AWS S3 bucket and CDN

## 6. Backend - API Layer

- [ ] 6.1 Create CustomersController
  - [ ] GET /api/v1/customers/profile - Get customer profile
  - [ ] PATCH /api/v1/customers/profile - Update profile
  - [ ] POST /api/v1/customers/avatar/upload-url - Get S3 pre-signed URL
  - [ ] GET /api/v1/customers/bookings/upcoming - Get upcoming bookings
  - [ ] GET /api/v1/customers/bookings/history - Get booking history (paginated)
  - [ ] GET /api/v1/customers/favorites - Get favorite providers
  - [ ] POST /api/v1/customers/favorites/{providerId} - Add favorite
  - [ ] DELETE /api/v1/customers/favorites/{providerId} - Remove favorite
  - [ ] GET /api/v1/customers/statistics - Get customer stats
  - [ ] GET /api/v1/customers/loyalty/transactions - Get loyalty transactions
  - [ ] PATCH /api/v1/customers/preferences - Update notification preferences
- [ ] 6.2 Add request/response DTOs
  - [ ] UpdateCustomerProfileRequest.cs
  - [ ] CustomerProfileResponse.cs
  - [ ] UpcomingBookingResponse.cs
  - [ ] BookingHistoryResponse.cs
  - [ ] FavoriteProviderResponse.cs
- [ ] 6.3 Add validation attributes and filters
- [ ] 6.4 Document all endpoints in Swagger
- [ ] 6.5 Write integration tests for all endpoints

## 7. Frontend - Module Setup

- [ ] 7.1 Create customer module structure
  - [ ] Create `booksy-frontend/src/modules/customer/` directory
  - [ ] Create subdirectories: views/, components/, stores/, api/, types/
- [ ] 7.2 Set up routing
  - [ ] Add routes in `customer.routes.ts`:
    - `/customer/profile` (default to bookings tab)
    - `/customer/profile/bookings`
    - `/customer/profile/favorites`
    - `/customer/profile/history`
    - `/customer/profile/payments`
    - `/customer/profile/reviews`
    - `/customer/profile/settings`
  - [ ] Add auth guard to protect routes
  - [ ] Add profile link to main navigation

## 8. Frontend - Pinia Stores

- [ ] 8.1 Create customer-profile store
  - [ ] `stores/modules/customer-profile.store.ts`
  - [ ] State: profile, loading, error
  - [ ] Actions: fetchProfile, updateProfile, uploadAvatar
  - [ ] Getters: fullName, isVerified, loyaltyTier
- [ ] 8.2 Create customer-bookings store
  - [ ] `stores/modules/customer-bookings.store.ts`
  - [ ] State: upcomingBookings, historyPage, historyData, filters, loading
  - [ ] Actions: fetchUpcoming, fetchHistory, filterHistory, exportHistory
  - [ ] Getters: hasUpcoming, historyCount
- [ ] 8.3 Create customer-favorites store
  - [ ] `stores/modules/customer-favorites.store.ts`
  - [ ] State: favorites, sortBy, loading
  - [ ] Actions: fetchFavorites, addFavorite, removeFavorite, sortFavorites
  - [ ] Getters: favoriteIds, favoriteCount

## 9. Frontend - API Services

- [ ] 9.1 Create customer API service
  - [ ] `api/customer.service.ts`
  - [ ] getProfile()
  - [ ] updateProfile(data)
  - [ ] getAvatarUploadUrl()
  - [ ] uploadAvatarToS3(file, presignedUrl)
  - [ ] getUpcomingBookings()
  - [ ] getBookingHistory(page, filters)
  - [ ] getFavoriteProviders()
  - [ ] addFavoriteProvider(providerId)
  - [ ] removeFavoriteProvider(providerId)
  - [ ] getStatistics()
  - [ ] getLoyaltyTransactions()
  - [ ] updatePreferences(preferences)

## 10. Frontend - TypeScript Types

- [ ] 10.1 Create customer types
  - [ ] `types/customer.types.ts`
  - [ ] CustomerProfile interface
  - [ ] UpcomingBooking interface
  - [ ] BookingHistoryEntry interface
  - [ ] FavoriteProvider interface
  - [ ] CustomerStatistics interface
  - [ ] LoyaltyTransaction interface
  - [ ] NotificationPreferences interface
  - [ ] PrivacySettings interface

## 11. Frontend - Main Views

- [ ] 11.1 Implement ProfilePage.vue
  - [ ] Create layout with header, tabs, content area, sidebar
  - [ ] Implement tab navigation with URL sync
  - [ ] Add responsive breakpoints (mobile/tablet/desktop)
  - [ ] Implement loading states (skeletons)
  - [ ] Add error boundaries
- [ ] 11.2 Implement BookingsView.vue (default tab)
  - [ ] Display upcoming bookings widget at top
  - [ ] Add countdown timers with Persian numbers
  - [ ] Implement action buttons (Cancel, Reschedule, Details)
  - [ ] Add empty state for no bookings
- [ ] 11.3 Implement FavoritesView.vue
  - [ ] Grid layout (responsive columns)
  - [ ] Provider cards with stats
  - [ ] Sort dropdown (Recent, Rating, Distance, Visits)
  - [ ] Add/remove favorite toggle
  - [ ] Empty state with CTA
- [ ] 11.4 Implement HistoryView.vue
  - [ ] Data table with pagination
  - [ ] Filter panel (date range, provider, status, service)
  - [ ] Sort by columns (date, provider, price)
  - [ ] Export buttons (PDF, Excel)
  - [ ] Rebook action
- [ ] 11.5 Implement SettingsView.vue
  - [ ] Notification preferences section
  - [ ] Privacy settings section
  - [ ] Security section (2FA, active sessions)
  - [ ] Language & region section
  - [ ] Account actions (export data, delete account)

## 12. Frontend - Components

- [ ] 12.1 ProfileHeader.vue
  - [ ] Avatar display with upload button
  - [ ] Name, phone, email display
  - [ ] Verification badges
  - [ ] Edit button toggle
  - [ ] Inline edit form with validation
- [ ] 12.2 UpcomingBookingsWidget.vue
  - [ ] Booking card component
  - [ ] Countdown timer logic
  - [ ] Action buttons with modals
  - [ ] Empty state
- [ ] 12.3 BookingHistoryTable.vue
  - [ ] Table with sortable columns
  - [ ] Pagination controls (Persian numbers)
  - [ ] Filter chips display
  - [ ] Action buttons per row
- [ ] 12.4 FavoriteProviderCard.vue
  - [ ] Provider info display
  - [ ] Heart toggle icon
  - [ ] Quick book button
  - [ ] Distance calculation
- [ ] 12.5 AvatarUploadModal.vue
  - [ ] File picker trigger
  - [ ] Image crop interface (vue-advanced-cropper)
  - [ ] Upload progress indicator
  - [ ] Error handling
- [ ] 12.6 LoyaltyCard.vue
  - [ ] Points balance display
  - [ ] Tier badge with icon
  - [ ] Progress bar to next tier
  - [ ] Expiring points warning
  - [ ] Transaction history link
- [ ] 12.7 NotificationPreferencesForm.vue
  - [ ] Toggle switches for channels
  - [ ] Dropdown for reminder timing
  - [ ] Auto-save on change
- [ ] 12.8 PrivacySettingsForm.vue
  - [ ] Toggle switches for privacy options
  - [ ] Explanatory text for each option
- [ ] 12.9 ActiveSessionsList.vue
  - [ ] Session cards with device/location info
  - [ ] Logout button per session
  - [ ] Current session indicator
- [ ] 12.10 DataExportButton.vue
  - [ ] Confirmation dialog
  - [ ] Progress indicator
  - [ ] Success notification with email prompt
- [ ] 12.11 DeleteAccountButton.vue
  - [ ] Multi-step confirmation
  - [ ] Re-authentication prompt
  - [ ] Warning about consequences

## 13. Frontend - Styling (RTL)

- [ ] 13.1 Create profile-specific CSS
  - [ ] `styles/modules/profile.scss`
  - [ ] RTL-specific rules (flexbox direction, margins, padding)
  - [ ] Responsive grid layouts
  - [ ] Tab navigation styles
- [ ] 13.2 Add dark mode support (if applicable)
- [ ] 13.3 Test across breakpoints (320px, 768px, 1024px, 1440px)

## 14. Frontend - Internationalization

- [ ] 14.1 Add Persian translations
  - [ ] Update `core/i18n/locales/fa.json` with 100+ keys:
    - Profile header labels
    - Tab names
    - Form labels and placeholders
    - Button texts
    - Error messages
    - Empty state messages
    - Confirmation dialogs
    - Success/error toasts
- [ ] 14.2 Add English translations (if required)
  - [ ] Update `core/i18n/locales/en.json`

## 15. Frontend - State Management Integration

- [ ] 15.1 Connect ProfilePage to stores
  - [ ] Fetch profile on mount
  - [ ] Subscribe to profile updates
  - [ ] Handle loading/error states
- [ ] 15.2 Implement optimistic updates
  - [ ] Update local state immediately on user action
  - [ ] Revert on API error
- [ ] 15.3 Add caching strategy
  - [ ] Cache profile data (5 min)
  - [ ] Invalidate cache on update
  - [ ] Stale-while-revalidate pattern

## 16. Testing - Backend

- [ ] 16.1 Unit tests for domain aggregates
  - [ ] CustomerProfile aggregate tests
  - [ ] FavoriteProvider aggregate tests
- [ ] 16.2 Unit tests for command handlers
  - [ ] UpdateCustomerProfile handler test
  - [ ] AddFavoriteProvider handler test
  - [ ] RemoveFavoriteProvider handler test
- [ ] 16.3 Unit tests for query handlers
  - [ ] GetCustomerProfile handler test
  - [ ] GetUpcomingBookings handler test
  - [ ] GetBookingHistory handler test
- [ ] 16.4 Integration tests for API endpoints
  - [ ] Test all GET endpoints
  - [ ] Test all POST/PATCH/DELETE endpoints
  - [ ] Test authentication and authorization
  - [ ] Test validation errors
  - [ ] Test pagination and filtering
- [ ] 16.5 Test integration event handlers
  - [ ] BookingCompletedEvent → booking history update
  - [ ] ProviderDeletedEvent → favorite cleanup

## 17. Testing - Frontend

- [ ] 17.1 Unit tests for stores
  - [ ] customer-profile.store.spec.ts
  - [ ] customer-bookings.store.spec.ts
  - [ ] customer-favorites.store.spec.ts
- [ ] 17.2 Unit tests for components
  - [ ] ProfileHeader.spec.ts
  - [ ] UpcomingBookingsWidget.spec.ts
  - [ ] FavoriteProviderCard.spec.ts
  - [ ] AvatarUploadModal.spec.ts
- [ ] 17.3 Unit tests for API services
  - [ ] customer.service.spec.ts (mock axios)
- [ ] 17.4 E2E tests with Cypress
  - [ ] Profile page navigation
  - [ ] Profile edit flow
  - [ ] Avatar upload flow
  - [ ] Add/remove favorite provider
  - [ ] Booking history filtering
  - [ ] Settings update flow

## 18. Performance Optimization

- [ ] 18.1 Implement lazy loading
  - [ ] Route-based code splitting
  - [ ] Component-level lazy loading
  - [ ] Image lazy loading with Intersection Observer
- [ ] 18.2 Implement caching
  - [ ] Redis cache for profile data (5 min TTL)
  - [ ] Browser cache for static assets (CDN)
  - [ ] LocalStorage for user preferences
- [ ] 18.3 Optimize images
  - [ ] Convert avatars to WebP format
  - [ ] Implement responsive images (srcset)
  - [ ] Use CloudFront CDN for delivery
- [ ] 18.4 Database optimization
  - [ ] Add composite indexes for common queries
  - [ ] Analyze query execution plans
  - [ ] Implement read replicas if needed
- [ ] 18.5 Frontend bundle optimization
  - [ ] Tree-shaking unused code
  - [ ] Minification and compression
  - [ ] Analyze bundle size with webpack-bundle-analyzer

## 19. Accessibility (WCAG 2.1 AA)

- [ ] 19.1 Semantic HTML
  - [ ] Use proper heading hierarchy
  - [ ] Add ARIA labels to interactive elements
  - [ ] Add alt text to all images
- [ ] 19.2 Keyboard navigation
  - [ ] Ensure all interactive elements are keyboard accessible
  - [ ] Add visible focus indicators
  - [ ] Implement skip navigation link
  - [ ] Test tab order (RTL)
- [ ] 19.3 Screen reader support
  - [ ] Add ARIA live regions for dynamic content
  - [ ] Add status announcements for actions
  - [ ] Test with NVDA/JAWS
- [ ] 19.4 Color contrast
  - [ ] Verify 4.5:1 ratio for text
  - [ ] Ensure status not conveyed by color alone
- [ ] 19.5 Accessibility audit
  - [ ] Run axe DevTools audit
  - [ ] Fix all issues
  - [ ] Manual testing with assistive technologies

## 20. Security

- [ ] 20.1 Implement input validation
  - [ ] Validate all user inputs (backend + frontend)
  - [ ] Sanitize HTML to prevent XSS
  - [ ] Use parameterized queries (prevent SQL injection)
- [ ] 20.2 Implement authentication checks
  - [ ] Verify customer owns profile before updates
  - [ ] Add rate limiting to sensitive endpoints
  - [ ] Implement CSRF protection
- [ ] 20.3 Secure avatar uploads
  - [ ] Validate file types (whitelist: JPEG, PNG)
  - [ ] Limit file size (max 5MB)
  - [ ] Scan for malware (ClamAV or similar)
  - [ ] Strip EXIF data
- [ ] 20.4 Payment data security
  - [ ] Never store raw card data (use tokenization)
  - [ ] Ensure PCI DSS compliance
  - [ ] Encrypt sensitive data at rest (AES-256)
- [ ] 20.5 Security audit
  - [ ] Run OWASP ZAP scan
  - [ ] Fix all high/medium vulnerabilities
  - [ ] Conduct penetration testing

## 21. Documentation

- [ ] 21.1 API documentation
  - [ ] Document all endpoints in Swagger/OpenAPI
  - [ ] Add request/response examples
  - [ ] Document authentication requirements
- [ ] 21.2 Code documentation
  - [ ] Add XML comments to C# public APIs
  - [ ] Add JSDoc comments to TypeScript interfaces/functions
- [ ] 21.3 User guide
  - [ ] Create customer profile page user guide (Persian)
  - [ ] Add screenshots for key features
  - [ ] Document common workflows
- [ ] 21.4 Implementation summary
  - [ ] Write IMPLEMENTATION_SUMMARY.md documenting key decisions

## 22. Deployment

- [ ] 22.1 Staging deployment
  - [ ] Deploy backend to staging
  - [ ] Deploy frontend to staging
  - [ ] Run smoke tests
- [ ] 22.2 UAT (User Acceptance Testing)
  - [ ] Create test accounts
  - [ ] Test all workflows with stakeholders
  - [ ] Collect feedback and address issues
- [ ] 22.3 Production deployment
  - [ ] Create deployment plan with rollback procedure
  - [ ] Schedule maintenance window (if needed)
  - [ ] Deploy database migrations
  - [ ] Deploy backend with feature flag (disabled)
  - [ ] Deploy frontend with feature flag (disabled)
  - [ ] Enable feature flag for 10% of users
  - [ ] Monitor error rates, performance metrics
  - [ ] Gradual rollout to 50%, then 100%
- [ ] 22.4 Post-deployment monitoring
  - [ ] Monitor application logs for errors
  - [ ] Track Core Web Vitals (LCP, FID, CLS)
  - [ ] Monitor API response times
  - [ ] Set up alerts for anomalies
- [ ] 22.5 Support preparation
  - [ ] Train support team on new features
  - [ ] Create FAQ for customer profile features
  - [ ] Prepare canned responses for common issues

## 23. Post-Launch

- [ ] 23.1 Gather user feedback
  - [ ] Implement in-app feedback form
  - [ ] Conduct user interviews (5-10 customers)
  - [ ] Analyze usage metrics (page views, feature adoption)
- [ ] 23.2 Performance monitoring
  - [ ] Review Core Web Vitals weekly
  - [ ] Identify and fix performance bottlenecks
  - [ ] Optimize slow database queries
- [ ] 23.3 Iterate based on feedback
  - [ ] Prioritize top requested features
  - [ ] Fix usability issues
  - [ ] Plan Phase 2 enhancements
