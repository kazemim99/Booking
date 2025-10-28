# Design Document: Complete Business Profile Management

## Context

The Booksy platform currently has an incomplete provider business profile management system. While basic business information and hours can be edited, critical features for managing services, staff, media, and settings are either placeholders or missing entirely. This creates a poor provider experience and prevents businesses from effectively operating on the platform.

This change completes the business profile management by implementing comprehensive CRUD interfaces for all provider-managed resources with a focus on:
- **UX Excellence**: Intuitive, consistent interfaces that follow modern SaaS patterns
- **Mobile-First**: Responsive design enabling management from any device
- **Performance**: Fast, optimistic updates with proper loading states
- **Maintainability**: Reusable components and clear separation of concerns

### Stakeholders
- **Providers**: Primary users who need comprehensive business management tools
- **Customers**: Benefit from complete, accurate provider profiles
- **Development Team**: Responsible for implementing and maintaining the features

### Constraints
- Must integrate with existing backend domain models (Provider, Service, Staff aggregates)
- Must maintain consistency with existing UI patterns (Card components, Button variants, etc.)
- Must support Persian (RTL) and English languages
- Image upload requires cloud storage infrastructure (decision needed on provider)
- Must work on mobile devices and tablets, not just desktop

## Goals / Non-Goals

### Goals
1. Complete all placeholder provider management views with full functionality
2. Provide intuitive UX for service catalog management (create, edit, organize, publish)
3. Enable comprehensive staff management (profiles, schedules, assignments)
4. Implement provider settings for booking preferences, notifications, and policies
5. Add media gallery management for business photos and portfolios
6. Ensure consistent UX patterns across all management interfaces
7. Support real-time validation, inline feedback, and optimistic updates
8. Enable bulk operations for efficient management at scale

### Non-Goals
1. **NOT** creating customer-facing booking interface (separate concern)
2. **NOT** implementing advanced analytics or reporting (future enhancement)
3. **NOT** building payment processing infrastructure (integration only)
4. **NOT** implementing calendar sync logic (configuration UI only)
5. **NOT** creating mobile native apps (responsive web only)
6. **NOT** building automated marketing features (future consideration)

## Decisions

### Decision 1: Unified Profile Hub with Tabbed Navigation

**What**: Implement a centralized business profile hub (`BusinessProfileHub.vue`) that serves as the main navigation for all profile management sections using a tabbed interface.

**Why**:
- Providers need a single entry point to access all business management features
- Tabs provide clear visual hierarchy and context for current location
- Enables profile completion tracking across all sections
- Familiar pattern from popular SaaS tools (Stripe, Shopify, etc.)

**Alternatives Considered**:
- Separate pages with sidebar navigation: More traditional but adds navigation complexity
- Dashboard with widgets: Good for overview but poor for management workflows
- Wizard-style flow: Too rigid for ongoing management (better for onboarding)

**Trade-offs**:
- Pro: Excellent discoverability and user orientation
- Pro: Easy to show profile completion progress
- Con: Potentially large initial bundle if not code-split properly
- Mitigation: Use Vue Router's lazy loading for tab components

### Decision 2: Card-Based Layouts for All List Views

**What**: Use a consistent card-based grid layout for displaying services, staff, and gallery items with hover states, actions, and status indicators.

**Why**:
- Visually scannable and works well on all screen sizes
- Accommodates images, metadata, and actions naturally
- Already established pattern in existing codebase (dashboard cards)
- Allows for rich information density without clutter

**Alternatives Considered**:
- Table view: Better for dense data but less visual, poor mobile UX
- List view: Good for simple items but doesn't showcase images well
- Hybrid table/card toggle: Adds complexity without clear benefit

**Trade-offs**:
- Pro: Excellent visual appeal and information hierarchy
- Pro: Natural responsive behavior
- Con: Takes more vertical space than tables
- Mitigation: Provide filtering and search to reduce visible items

### Decision 3: Optimistic Updates with Rollback

**What**: Implement optimistic UI updates in Pinia stores that immediately reflect changes in the UI before server confirmation, with automatic rollback on failure.

**Why**:
- Creates perception of instant responsiveness
- Reduces perceived latency for common operations
- Standard pattern in modern web applications
- Improves provider satisfaction with management tools

**Implementation Pattern**:
```typescript
async updateService(serviceId: string, updates: Partial<Service>) {
  const original = this.services.find(s => s.id === serviceId)
  const optimistic = { ...original, ...updates }

  // Optimistically update UI
  this.services = this.services.map(s =>
    s.id === serviceId ? optimistic : s
  )

  try {
    const result = await serviceApi.update(serviceId, updates)
    this.services = this.services.map(s =>
      s.id === serviceId ? result : s
    )
  } catch (error) {
    // Rollback on failure
    this.services = this.services.map(s =>
      s.id === serviceId ? original : s
    )
    throw error
  }
}
```

**Trade-offs**:
- Pro: Significantly improved perceived performance
- Pro: Better user experience for common operations
- Con: More complex error handling logic
- Con: Risk of state inconsistency if not implemented carefully
- Mitigation: Comprehensive error handling and retry logic

### Decision 4: Drag-and-Drop for Ordering

**What**: Implement native HTML5 drag-and-drop for reordering services, gallery images, and other orderable lists using a custom `DragDropList.vue` component.

**Why**:
- Intuitive interface for ordering that users understand
- More efficient than up/down buttons for large lists
- Creates sense of direct manipulation and control
- Standard pattern in modern admin interfaces

**Library Decision**: Use VueDraggable (Vue 3 compatible) for robust drag-and-drop:
- Well-maintained, widely used library
- Handles edge cases (mobile touch, accessibility, nested dragging)
- Integrates cleanly with Vue 3 Composition API

**Alternatives Considered**:
- Custom implementation: More control but high complexity and accessibility challenges
- Up/Down buttons: Simple but tedious for reordering many items
- Manual sort field entry: Precise but poor UX

**Trade-offs**:
- Pro: Excellent UX for reordering
- Pro: Proven library reduces implementation risk
- Con: Adds ~20KB to bundle size
- Con: Requires careful mobile touch handling
- Mitigation: Code-split components using drag-drop, test thoroughly on mobile

### Decision 5: Image Upload with Cloud Storage

**What**: Implement client-side image upload with preview, cropping, and optimization before uploading to cloud storage (AWS S3, Cloudflare R2, or similar).

**Why**:
- Providers need to showcase their business with photos
- Client-side optimization reduces storage and bandwidth costs
- Modern expectations for image upload UX (cropping, preview)
- Centralized media management improves consistency

**Architecture**:
1. Client uploads image to signed upload URL from backend
2. Backend generates signed URLs with appropriate permissions
3. Client optimizes image (resize, compress) before upload
4. Backend stores metadata and URL in database
5. Images served via CDN for performance

**Library Decisions**:
- **vue-advanced-cropper** for image cropping: Feature-rich, good Vue 3 support
- **browser-image-compression** for client-side compression: Reduces upload size significantly

**Storage Provider Decision**: **Defer to infrastructure team** - Design supports any S3-compatible storage:
- AWS S3: Industry standard, most features
- Cloudflare R2: Cost-effective, no egress fees
- DigitalOcean Spaces: Good balance of cost and features

**Alternatives Considered**:
- Direct server upload: Simpler but slower, server resource intensive
- Base64 in database: Terrible performance and scalability
- No image support: Incomplete provider profiles

**Trade-offs**:
- Pro: Scalable, performant image hosting
- Pro: Excellent UX with preview and cropping
- Con: Requires cloud storage infrastructure setup
- Con: Adds complexity to deployment
- Mitigation: Abstract storage behind interface for provider flexibility

### Decision 6: Real-Time Validation with Inline Feedback

**What**: Implement field-level validation that runs on blur/change with inline error messages, using Zod schemas for type-safe validation.

**Why**:
- Immediate feedback prevents frustration of failed form submissions
- Zod provides runtime validation that matches TypeScript types
- Reduces server load by catching errors client-side
- Modern UX expectation for form interactions

**Validation Strategy**:
```typescript
// Define schema with Zod
const serviceSchema = z.object({
  name: z.string().min(3).max(100),
  basePrice: z.number().positive(),
  duration: z.number().int().min(15).max(480),
})

// Use in component with real-time validation
const { errors, validate } = useValidation(serviceSchema)
const handleFieldBlur = (field: string) => validate(field)
```

**Alternatives Considered**:
- VeeValidate: More features but heavier, older patterns
- Manual validation: Too much boilerplate, error-prone
- Server-only validation: Poor UX, wasted round-trips

**Trade-offs**:
- Pro: Type-safe validation matching TypeScript
- Pro: Excellent developer experience
- Pro: Reduced bundle size vs VeeValidate
- Con: Still need server-side validation (never trust client)
- Mitigation: Share Zod schemas between client and server if possible

### Decision 7: Bulk Operations with Multi-Select

**What**: Enable bulk selection via checkboxes on card items, showing a toolbar with bulk actions when items are selected.

**Why**:
- Providers with many services/staff need efficient management
- Industry-standard pattern (Gmail, file managers, etc.)
- Significantly faster than individual operations
- Creates sense of power and efficiency

**UX Pattern**:
1. Click checkbox on any card to enter selection mode
2. Toolbar appears at top with action buttons and selection count
3. Select more items or use "Select All" option
4. Choose bulk action (Activate, Deactivate, Delete, etc.)
5. Confirmation dialog shows affected items
6. Progress indicator for multi-item operations
7. Summary of success/failure results

**Alternatives Considered**:
- No bulk operations: Forces tedious one-by-one management
- Action on each card: Clutters UI, requires many confirmations
- Command palette: Too advanced for most users

**Trade-offs**:
- Pro: Huge efficiency gain for power users
- Pro: Familiar pattern users understand
- Con: Adds UI complexity and state management
- Con: Error handling is more complex (partial failures)
- Mitigation: Clear visual feedback, granular success/failure reporting

### Decision 8: Progressive Disclosure for Complex Forms

**What**: Break complex forms (service editor, staff editor) into logical sections with expandable/collapsible panels or stepped progression.

**Why**:
- Reduces cognitive load by showing only relevant fields
- Prevents overwhelming providers with long forms
- Allows focusing on one concern at a time
- Enables skipping optional sections efficiently

**Pattern for Service Editor**:
1. **Basic Info** (always visible): Name, description, category
2. **Pricing** (expandable): Base price, tiers, deposits
3. **Timing** (expandable): Duration, prep, buffer
4. **Options** (expandable): Add-ons, required selections
5. **Staff** (expandable): Qualified staff assignments
6. **Images** (expandable): Service photos
7. **Availability** (expandable): Booking rules, locations

**Alternatives Considered**:
- Multi-step wizard: Too rigid for editing
- Single long form: Overwhelming and hard to navigate
- Separate pages per section: Too much navigation

**Trade-offs**:
- Pro: Manageable complexity even for feature-rich forms
- Pro: Clear information architecture
- Con: Requires more clicks to access all fields
- Mitigation: Sensible defaults for collapsed sections, expand on validation error

## Risks / Trade-offs

### Risk 1: Image Upload Infrastructure Dependency

**Risk**: Implementation is blocked if cloud storage infrastructure is not available or decision is delayed.

**Impact**: High - Multiple features (logo, cover, service images, gallery) depend on this.

**Mitigation**:
1. Design abstraction layer for storage backend (interface-based)
2. Implement file upload UI and client-side logic first
3. Use temporary local storage or mock service for development
4. Coordinate with infrastructure team early for storage decision
5. Consider staged rollout: Basic features first, images in follow-up

**Fallback**: Allow URL input as temporary solution until upload is ready.

### Risk 2: Scope Creep and Timeline

**Risk**: Feature-rich specification may lead to extended development time and scope expansion.

**Impact**: Medium - Delays completion of provider management capabilities.

**Mitigation**:
1. Prioritize MVP features: Basic CRUD for services and staff first
2. Defer "nice-to-have" features: Advanced analytics, integrations
3. Implement iteratively: Ship core functionality, enhance based on feedback
4. Use feature flags to enable gradual rollout
5. Time-box advanced features (bulk operations, drag-drop) for Phase 2

**Phasing Recommendation**:
- **Phase 1 (MVP)**: Service CRUD, Staff CRUD, Basic settings
- **Phase 2**: Image upload, Gallery, Advanced settings
- **Phase 3**: Bulk operations, Analytics, Integrations

### Risk 3: Mobile UX Complexity

**Risk**: Complex management interfaces may not translate well to mobile devices.

**Impact**: Medium - Many providers may manage business from mobile devices.

**Mitigation**:
1. Mobile-first design approach from start
2. Simplified mobile layouts where needed (stack instead of grid)
3. Touch-friendly controls (larger tap targets, swipe actions)
4. Test on actual devices throughout development
5. Consider progressive enhancement (desktop gets richer features)

**Key Mobile Considerations**:
- Drag-drop may need alternative on mobile (up/down buttons)
- Bulk select may use long-press instead of checkboxes
- Complex forms may stack vertically instead of multi-column
- Image cropping needs touch-optimized controls

### Risk 4: Backend API Insufficiency

**Risk**: Existing backend endpoints may not support all required operations (bulk actions, complex filtering, etc.).

**Impact**: Medium - May require backend changes, increasing scope.

**Mitigation**:
1. Audit existing API endpoints early in planning
2. Design frontend to work with current capabilities first
3. Identify gaps and prioritize based on user impact
4. Implement client-side workarounds where feasible (multiple requests for bulk ops)
5. Coordinate with backend team for needed enhancements

**Known Gaps** (to verify):
- Bulk service operations endpoint
- Staff performance metrics endpoint
- Media management endpoints
- Provider settings persistence

### Risk 5: State Management Complexity

**Risk**: Managing state across multiple stores (services, staff, settings, gallery) with relationships and consistency requirements.

**Impact**: Medium - Bugs from inconsistent state, poor performance.

**Mitigation**:
1. Clear ownership: Each store owns its data, no duplication
2. Computed properties for derived state
3. Events/composition for cross-store communication
4. Thorough testing of state transitions
5. DevTools integration for debugging state issues

**Pattern for Related Data**:
```typescript
// Service store references staff by ID only
service.qualifiedStaffIds: string[]

// Component composes data when needed
const qualifiedStaff = computed(() =>
  service.qualifiedStaffIds.map(id =>
    staffStore.getStaffById(id)
  )
)
```

## Migration Plan

### Phase 1: Foundation (Week 1-2)
1. Create shared components (ImageUpload, DragDropList, Calendar, etc.)
2. Set up Pinia stores with API integration
3. Define TypeScript types and Zod schemas
4. Implement base layouts and navigation structure
5. **Validation**: All shared components work in isolation

### Phase 2: Service Management (Week 3-4)
1. Implement ServiceListView with filtering and search
2. Implement ServiceEditorView with all sections
3. Add drag-and-drop reordering
4. Integrate with backend APIs
5. **Validation**: Can create, edit, and manage services end-to-end

### Phase 3: Staff Management (Week 5-6)
1. Implement StaffListView with filtering
2. Implement StaffEditorView with schedule management
3. Add staff availability calendar views
4. Integrate with backend APIs
5. **Validation**: Can create, edit, and manage staff with schedules

### Phase 4: Settings & Gallery (Week 7-8)
1. Implement ProviderSettingsView with all setting sections
2. Implement GalleryView with image upload
3. Set up cloud storage integration
4. Add profile preview functionality
5. **Validation**: All settings persist, images upload successfully

### Phase 5: Integration & Polish (Week 9-10)
1. Integrate all sections into BusinessProfileHub
2. Add bulk operations across management interfaces
3. Implement comprehensive error handling
4. Add loading states and skeletons
5. Perform accessibility and mobile testing
6. **Validation**: Complete provider workflow works smoothly

### Rollback Plan
Each phase is independently valuable:
- Can ship Phase 2 (Services) without Phase 3 (Staff)
- Can defer image upload to later release
- Feature flags allow gradual rollout and quick disable if issues arise

### Database Migration
No schema changes required - existing domain models support all features.
Only potential addition: Provider settings table for preferences (low risk).

## Open Questions

### Q1: Cloud Storage Provider
**Question**: Which cloud storage provider should be used for image hosting (AWS S3, Cloudflare R2, DigitalOcean Spaces)?

**Options**:
- AWS S3: Industry standard, most features, higher cost
- Cloudflare R2: Cost-effective (no egress fees), good performance
- DigitalOcean Spaces: Good balance, simpler API

**Recommendation**: Cloudflare R2 for cost efficiency, fallback to AWS S3 if R2 limitations found.

**Decision Needed By**: Before Phase 4 (Week 7)

### Q2: Image Optimization Strategy
**Question**: Should image optimization happen client-side, server-side, or both?

**Options**:
- Client-side only: Reduces server load, slower uploads on slow connections
- Server-side only: Simpler client, more server resources needed
- Both: Best UX and efficiency, more complex

**Recommendation**: Hybrid - client-side resize to max dimensions, server-side generate thumbnails/optimized versions.

**Decision Needed By**: Before Phase 4 (Week 7)

### Q3: Internationalization Approach
**Question**: How should new UI text be internationalized, especially for user-generated content (policies, descriptions)?

**Options**:
- Static UI only: Simple but limits customization
- Template-based with variables: Good balance
- Full rich-text internationalization: Complex but most flexible

**Recommendation**: Template-based for policies/notifications, static for UI chrome, RTL testing for all layouts.

**Decision Needed By**: Before Phase 2 (Week 3)

### Q4: Mobile App Consideration
**Question**: Should design accommodate future native mobile app, or optimize purely for responsive web?

**Recommendation**: Design for responsive web, ensure clean API boundaries. Native app can reuse APIs if built later.

**Decision Needed By**: Now (affects component architecture)

### Q5: Real-time Updates
**Question**: Should changes made by one user (e.g., owner) be reflected in real-time for other users (e.g., staff) viewing same data?

**Options**:
- No real-time: Simpler, requires refresh to see changes
- Polling: Simple to implement, moderate resource usage
- WebSockets: Real-time, more complex infrastructure

**Recommendation**: Start without real-time, add polling for critical views (booking calendar) if needed, defer WebSockets to future.

**Decision Needed By**: Before Phase 5 (Week 9)

## Success Criteria

1. **Functional**: Providers can fully manage services, staff, settings, and media through intuitive UIs
2. **Complete**: All placeholder views replaced with functional implementations
3. **Performant**: Page loads <2s on 3G, optimistic updates feel instant
4. **Accessible**: WCAG 2.1 AA compliant, keyboard navigable, screen-reader friendly
5. **Mobile**: All features functional on mobile devices with touch-optimized UX
6. **Tested**: >80% test coverage, E2E tests for critical flows passing
7. **Adopted**: >70% of providers complete profile within first week after launch
