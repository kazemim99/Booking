# Design: Replace Registration Flow with Figma-Generated Design

## Overview
This design document outlines the technical approach for replacing the existing Vue-based registration and authentication flow with a new design exported from Figma. The Figma design is React-based and must be converted to Vue 3 Composition API while maintaining all RTL Persian functionality and backend API compatibility.

## Architecture Decisions

### 1. Component Conversion Strategy

**Decision**: Convert React components to Vue 3 Composition API with `<script setup>` syntax

**Rationale**:
- Maintains consistency with existing codebase architecture (project.md specifies Vue 3 Composition API)
- `<script setup>` provides better TypeScript inference and less boilerplate
- Easier migration path from React hooks to Vue composables

**Pattern**:
```typescript
// React (Figma export)
const [state, setState] = useState(initial)
useEffect(() => { ... }, [deps])

// Vue 3 equivalent
const state = ref(initial)
watch(() => deps, () => { ... })
```

### 2. UI Component Library Mapping

**Decision**: Extract and adapt shadcn/ui patterns to custom Vue components without introducing a new dependency

**Rationale**:
- Figma design uses shadcn/ui (React) components (Button, Input, Label, etc.)
- Project doesn't currently use a Vue UI library for registration flow
- Creating custom Vue components matching the Figma design gives full control over styling and behavior
- Avoids adding large UI library dependency for a focused feature

**Implementation**:
- Create reusable Vue components in `booksy-frontend/src/shared/components/ui/` (if they don't exist)
- Components: Button, Input, Label, Card, Progress
- Use Tailwind CSS classes matching Figma design
- Ensure RTL compatibility in all components

### 3. RTL and Persian Layout Preservation

**Decision**: Maintain all RTL directives and Persian text from Figma components

**Rationale**:
- Target market is Persian-speaking users
- Figma design already includes proper RTL layout
- Vue's `dir="rtl"` attribute and CSS logical properties support RTL

**Implementation**:
- Preserve `dir="ltr"` for phone number inputs (as in Figma LoginPage)
- Use Tailwind RTL utilities (`ps-`, `pe-`, `start-`, `end-`)
- Keep all Persian text strings from Figma components
- Consider extracting to i18n later for multi-language support

### 4. Step Flow and State Management

**Decision**: Update existing `useProviderRegistration` composable to support new step sequence

**Rationale**:
- Existing composable already manages registration state and navigation
- Reusing existing pattern maintains consistency
- Minimal changes to state management logic

**Changes Required**:
```typescript
// Old flow: 8 steps
// 1. Category → 2. BusinessInfo → 3. Location → 4. Hours → 5. Services → 6. Assistance → 7. TeamMembers → 8. Complete

// New flow: 11 steps
// 1. Login → 2. Verification → 3. BusinessInfo → 4. Category → 5. Location → 6. Services → 7. Staff → 8. WorkingHours → 9. Gallery → 10. Feedback → 11. Completion

// Update TOTAL_STEPS constant
const TOTAL_STEPS = 11

// Add new state fields
galleryImages: []
feedbackData: {}
```

### 5. Authentication Integration

**Decision**: Replace auth views but keep existing auth store and API services

**Rationale**:
- Backend authentication endpoints remain unchanged
- Existing `useAuthStore` and API services work correctly
- Only UI layer needs replacement

**Implementation**:
- Convert Figma LoginPage.tsx → LoginView.vue (replace PhoneLoginView.vue)
- Convert Figma CodeVerification.tsx → VerificationView.vue (replace PhoneVerificationView.vue)
- Keep existing `auth.api.ts` and `auth.store.ts`
- Update router paths if needed

### 6. File Cleanup Strategy

**Decision**: Delete all legacy and duplicate files in a single cleanup task

**Rationale**:
- Prevents confusion between old and new implementations
- Reduces codebase size and maintenance burden
- Legacy files have clear suffixes (`.FIXED`, `.IMPROVED`, `.legacy`)

**Files to Delete**:
```
booksy-frontend/src/modules/provider/components/registration/steps/BusinessLocationStep.FIXED.vue
booksy-frontend/src/modules/provider/components/registration/steps/ServicesStep.IMPROVED.vue
booksy-frontend/src/modules/provider/components/dashboard/legacy/
booksy-frontend/src/modules/provider/components/navigation/legacy/
booksy-frontend/src/modules/provider/layouts/legacy/
```

### 7. Progressive Component Conversion

**Decision**: Convert and integrate components incrementally by step

**Rationale**:
- Allows testing each step independently
- Reduces risk of breaking existing functionality
- Easier to identify issues in specific steps

**Order**:
1. Convert shared UI components (Button, Input, etc.)
2. Convert authentication views (Login, Verification)
3. Convert registration steps in sequence
4. Update ProviderRegistrationFlow.vue to orchestrate new steps
5. Test complete flow end-to-end

### 8. Data Mapping and Backend Compatibility

**Decision**: Maintain existing data structures and API contracts

**Rationale**:
- Backend changes are out of scope
- Existing registration API already handles all required data
- Gallery and Feedback data can be submitted as part of existing payload or as optional fields

**Implementation**:
- Gallery images → existing provider registration or separate gallery endpoint (check with provider-gallery-management spec)
- Feedback data → can be logged separately or included in registration metadata
- All other step data maps directly to existing API contracts

## Technical Considerations

### TypeScript Types
- Reuse existing types from `booksy-frontend/src/modules/provider/types/registration.types.ts`
- Add new types for Gallery and Feedback steps
- Ensure all converted components are strongly typed

### Styling Approach
- Use Tailwind CSS (already in project)
- Extract Figma component styles to Tailwind classes
- Create custom CSS variables for Figma design tokens if needed
- Ensure responsive design (mobile-first)

### Accessibility
- Maintain ARIA labels from Figma components
- Ensure keyboard navigation works for all steps
- Test with screen readers for RTL compatibility

### Testing Strategy
- Unit tests for new Vue components (Vitest)
- Integration tests for registration flow (Cypress)
- Manual testing of RTL layout and Persian text rendering
- Test all validation and error handling

## Migration Path

### Phase 1: Foundation (Tasks 1-4)
- Create UI component library
- Set up new directory structure
- Convert shared components

### Phase 2: Authentication (Tasks 5-7)
- Convert Login and Verification views
- Update auth routing
- Test authentication flow

### Phase 3: Registration Steps (Tasks 8-19)
- Convert each registration step component
- Update registration flow orchestrator
- Test step-by-step navigation

### Phase 4: Cleanup and Testing (Tasks 20-24)
- Delete legacy files
- Update routing completely
- End-to-end testing
- Documentation updates

## Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| React patterns don't translate cleanly to Vue | Reference Vue 3 documentation and existing Vue patterns in codebase |
| RTL layout breaks during conversion | Test each component individually with RTL enabled |
| Backend API incompatibility with new data | Validate data structures match existing API contracts before implementation |
| Breaking existing user flows | Keep old files until new flow is fully tested and validated |
| Missing Figma design details | Use Figma design as reference; fallback to existing Vue patterns for gaps |

## Success Criteria
- All Figma components converted to Vue 3
- Registration flow matches Figma design exactly
- All existing backend APIs work without changes
- RTL and Persian text display correctly
- No legacy or duplicate files remain
- All tests pass (unit, integration, E2E)
- User can complete registration end-to-end
