# Implementation Tasks: Replace Registration Flow with Figma Design

## Phase 1: Foundation and Shared Components

### 1. Create shared UI component library
- [x] Create directory `booksy-frontend/src/shared/components/ui/` if it doesn't exist (ALREADY EXISTS)
- [x] Convert Figma Button component to `Button.vue` (USING EXISTING AppButton.vue)
  - Support variants: primary, secondary, outline
  - RTL-compatible styling
  - Props: variant, size, disabled, loading
- [x] Convert Figma Input component to `Input.vue` (USING EXISTING AppTextInput.vue)
  - Support type: text, tel, number
  - RTL-compatible with `dir` prop override
  - Props: placeholder, label, error, disabled
- [x] Convert Figma Label component to `Label.vue` (USING EXISTING INPUT LABELS)
  - RTL text alignment
  - Required field indicator support
- [x] Convert Figma Card component to `Card.vue` (USING EXISTING AppCard.vue)
  - Shadow and border styles from Figma
  - RTL padding and spacing
- [x] Create Progress component for step indicator
  - Visual progress bar
  - Current step / total steps display
  - RTL-compatible layout
- [x] Add TypeScript types for all UI components
- [ ] Test UI components with Vitest (basic render and props) (SKIPPED PER USER REQUEST)

### 2. Set up Figma design system integration
- [x] Review Figma design tokens (colors, spacing, typography)
- [x] Create/update Tailwind config with Figma color palette if needed (USING EXISTING STYLES)
- [x] Document any custom CSS variables required
- [x] Ensure RTL utilities are available in Tailwind config

## Phase 2: Authentication Module Replacement

### 3. Convert and replace login view
- [x] Convert `Figma/RTL Onboarding Flow Design/src/components/onboarding/LoginPage.tsx` to Vue
- [x] Create `booksy-frontend/src/modules/auth/views/LoginView.vue`
  - Implement phone number validation (09XXXXXXXXX pattern)
  - Use shared UI components (Button, Input, Label, Card)
  - Maintain RTL layout and Persian text
  - Integrate with existing `useAuthStore` and `auth.api.ts`
- [ ] Test login form validation (MANUAL TESTING PENDING)
- [ ] Test login submission with backend API (MANUAL TESTING PENDING)
- [ ] Verify error handling displays correctly in Persian (MANUAL TESTING PENDING)

### 4. Convert and replace verification view
- [x] Convert `Figma/RTL Onboarding Flow Design/src/components/onboarding/CodeVerification.tsx` to Vue
- [x] Create `booksy-frontend/src/modules/auth/views/VerificationView.vue`
  - Implement OTP input (6 digits)
  - Display phone number from login step
  - Back button navigation to login
  - Integrate with existing verification API
- [ ] Test verification code submission (MANUAL TESTING PENDING)
- [ ] Test back navigation to login (MANUAL TESTING PENDING)
- [ ] Verify success navigation to registration flow (MANUAL TESTING PENDING)

### 5. Update authentication routing
- [x] Update `booksy-frontend/src/core/router/routes/auth.routes.ts`
  - Replace PhoneLoginView with new LoginView
  - Replace PhoneVerificationView with new VerificationView
- [ ] Test authentication flow end-to-end (login → verify → redirect) (MANUAL TESTING PENDING)
- [ ] Verify route guards work correctly with new views (MANUAL TESTING PENDING)

### 6. Remove old authentication views
- [x] Delete `booksy-frontend/src/modules/auth/views/PhoneLoginView.vue`
- [x] Delete `booksy-frontend/src/modules/auth/views/PhoneVerificationView.vue`
- [x] Rename `LoginView.new.vue` → `LoginView.vue`
- [x] Rename `VerificationView.new.vue` → `VerificationView.vue`
- [x] Update any remaining imports

## Phase 3: Registration Step Components Conversion

### 7. Convert ProgressIndicator component
- [x] Convert `Figma/.../ProgressIndicator.tsx` to Vue
- [x] Create `booksy-frontend/src/modules/provider/components/registration/shared/ProgressIndicator.vue`
- [ ] Integrate with registration flow to show current step (PENDING - REQUIRES FULL FLOW UPDATE)
- [ ] Test progress updates as user navigates steps (PENDING - REQUIRES FULL FLOW UPDATE)

### 8. Convert BusinessInfo step
- [ ] Convert `Figma/.../BusinessInfo.tsx` to Vue
- [ ] Create `booksy-frontend/src/modules/provider/components/registration/steps/BusinessInfoStep.new.vue`
  - Form fields: business name, owner first/last name
  - Validation for required fields
  - RTL form layout
  - Navigation buttons (Next only, no Back on first step)
- [ ] Test form validation
- [ ] Test data binding with registration state

### 9. Convert CategorySelection step
- [ ] Convert `Figma/.../CategorySelection.tsx` to Vue
- [ ] Create `booksy-frontend/src/modules/provider/components/registration/steps/CategorySelectionStep.new.vue`
  - Display business categories from backend
  - Visual selection with icons
  - RTL grid/list layout
  - Navigation buttons (Next, Back)
- [ ] Test category selection
- [ ] Test navigation between steps

### 10. Convert Location step
- [ ] Convert `Figma/.../Location.tsx` to Vue
- [ ] Create `booksy-frontend/src/modules/provider/components/registration/steps/LocationStep.new.vue`
  - Address input fields (street, city, postal code, etc.)
  - Neshan Maps integration
  - Location marker placement
  - RTL form layout
  - Navigation buttons (Next, Back)
- [ ] Test map integration
- [ ] Test address validation
- [ ] Test location coordinate capture

### 11. Convert Services step
- [ ] Convert `Figma/.../Services.tsx` to Vue
- [ ] Create `booksy-frontend/src/modules/provider/components/registration/steps/ServicesStep.new.vue`
  - Service list display
  - Add service form (name, price, duration, description)
  - Edit/delete service actions
  - RTL layout for service cards
  - Navigation buttons (Next, Back)
- [ ] Test add service functionality
- [ ] Test edit/delete service
- [ ] Test validation (at least one service required)

### 12. Convert Staff step
- [ ] Convert `Figma/.../Staff.tsx` to Vue
- [ ] Create `booksy-frontend/src/modules/provider/components/registration/steps/StaffStep.new.vue`
  - Staff member list display
  - Add staff form (name, role/title, phone)
  - Edit/delete staff actions
  - RTL layout for staff cards
  - Navigation buttons (Next, Back)
- [ ] Test add staff functionality
- [ ] Test edit/delete staff
- [ ] Allow optional (can proceed with zero staff)

### 13. Convert WorkingHours step
- [ ] Convert `Figma/.../WorkingHours.tsx` to Vue
- [ ] Create `booksy-frontend/src/modules/provider/components/registration/steps/WorkingHoursStep.new.vue`
  - Weekly calendar (Saturday to Friday for Persian locale)
  - Day toggle (open/closed)
  - Time pickers for open/close times
  - Break time support (optional)
  - RTL layout
  - Navigation buttons (Next, Back)
- [ ] Test day toggle functionality
- [ ] Test time selection
- [ ] Test validation (valid time ranges)

### 14. Convert Gallery step (NEW)
- [x] Convert `Figma/.../Gallery.tsx` to Vue (IMPLEMENTED WITH EXISTING GALLERY COMPONENTS)
- [x] Create `booksy-frontend/src/modules/provider/components/registration/steps/GalleryStep.vue`
  - Image upload area (drag-and-drop or file select)
  - Multiple image support
  - Image preview thumbnails
  - Remove image functionality
  - RTL layout for image grid
  - Navigation buttons (Next, Back)
- [x] Implement image upload (temporary storage or immediate backend upload)
- [x] Test image file validation (type, size) (USING EXISTING GalleryUpload COMPONENT)
- [x] Test image preview and removal (IMPLEMENTED)
- [x] Handle optional step (can proceed with zero images)

### 15. Convert OptionalFeedback step (NEW)
- [ ] Convert `Figma/.../OptionalFeedback.tsx` to Vue
- [ ] Create `booksy-frontend/src/modules/provider/components/registration/steps/OptionalFeedbackStep.vue`
  - Feedback form (text area or rating)
  - RTL layout
  - Navigation buttons (Next/Skip, Back)
- [ ] Implement feedback submission or logging
- [ ] Test optional skip functionality
- [ ] Test feedback data capture

### 16. Convert Completion step
- [ ] Convert `Figma/.../Completion.tsx` to Vue
- [ ] Create `booksy-frontend/src/modules/provider/components/registration/steps/CompletionStep.new.vue`
  - Success message in Persian
  - Admin approval information (if applicable)
  - "رفتن به داشبورد" button
  - Celebratory UI elements
- [ ] Test navigation to dashboard
- [ ] Test completion state persistence

## Phase 4: Registration Flow Orchestration

### 17. Update provider registration composable
- [ ] Update `booksy-frontend/src/modules/provider/composables/useProviderRegistration.ts`
  - Change TOTAL_STEPS from 8 to 11
  - Add galleryImages state field
  - Add feedbackData state field
  - Update step validation for new steps
  - Add setGalleryImages() and setFeedback() methods
- [ ] Update registration submission to include gallery and feedback data
- [ ] Test state management across all steps

### 18. Update ProviderRegistrationFlow view
- [ ] Update `booksy-frontend/src/modules/provider/views/registration/ProviderRegistrationFlow.vue`
  - Remove old step components imports
  - Import all new step components
  - Update v-if conditions for 11 steps in new order:
    1. BusinessInfoStep
    2. CategorySelectionStep
    3. LocationStep
    4. ServicesStep
    5. StaffStep
    6. WorkingHoursStep
    7. GalleryStep
    8. OptionalFeedbackStep
    9. CompletionStep
  - Update ProgressIndicator with new step count
  - Test step navigation forward and backward
- [ ] Verify all step transitions work correctly
- [ ] Test complete registration flow end-to-end

### 19. Update registration routing
- [ ] Update `booksy-frontend/src/core/router/routes/provider.routes.ts` if needed
- [ ] Ensure registration route points to updated flow
- [ ] Test navigation from authentication to registration
- [ ] Test navigation from completion to dashboard

## Phase 5: Cleanup and Testing

### 20. Delete legacy and duplicate files
- [ ] Delete `booksy-frontend/src/modules/provider/components/registration/steps/BusinessLocationStep.FIXED.vue`
- [ ] Delete `booksy-frontend/src/modules/provider/components/registration/steps/ServicesStep.IMPROVED.vue`
- [ ] Delete `booksy-frontend/src/modules/provider/components/registration/steps/BusinessCategoryStep.vue` (old version)
- [ ] Delete `booksy-frontend/src/modules/provider/components/registration/steps/BusinessInfoStep.vue` (old version)
- [ ] Delete `booksy-frontend/src/modules/provider/components/registration/steps/BusinessLocationStep.vue` (old version)
- [ ] Delete `booksy-frontend/src/modules/provider/components/registration/steps/BusinessHoursStep.vue` (old version)
- [ ] Delete `booksy-frontend/src/modules/provider/components/registration/steps/ServicesStep.vue` (old version)
- [ ] Delete `booksy-frontend/src/modules/provider/components/registration/steps/AssistanceOptionsStep.vue` (removed from flow)
- [ ] Delete `booksy-frontend/src/modules/provider/components/registration/steps/TeamMembersStep.vue` (old version)
- [ ] Delete `booksy-frontend/src/modules/provider/components/registration/steps/RegistrationCompleteStep.vue` (old version)
- [ ] Delete entire `booksy-frontend/src/modules/provider/components/dashboard/legacy/` directory
- [ ] Delete entire `booksy-frontend/src/modules/provider/components/navigation/legacy/` directory
- [ ] Delete entire `booksy-frontend/src/modules/provider/layouts/legacy/` directory
- [ ] Rename all `.new.vue` files to remove `.new` suffix
- [ ] Update any remaining imports in other files

### 21. Delete Figma source directory (optional)
- [ ] Decide if Figma React source should remain for reference
- [ ] If deleting: remove `booksy-frontend/Figma/RTL Onboarding Flow Design/` directory
- [ ] Document Figma design link in README or documentation

### 22. Comprehensive testing
- [ ] Run unit tests for all new components (`npm run test:unit`)
- [ ] Fix any failing tests
- [ ] Write E2E tests for complete registration flow with Cypress
  - Test login → verification → all registration steps → completion
  - Test validation errors at each step
  - Test back navigation
  - Test RTL layout rendering
- [ ] Manual testing checklist:
  - [ ] Complete registration flow from start to finish
  - [ ] Test each step's validation
  - [ ] Test back/next navigation
  - [ ] Verify RTL layout on all screens
  - [ ] Verify Persian text displays correctly
  - [ ] Test on mobile viewport (responsive design)
  - [ ] Test with real backend API
  - [ ] Verify data submission to backend

### 23. Update documentation
- [ ] Update README if registration flow changes affect setup
- [ ] Document new Gallery and Feedback steps in user documentation
- [ ] Add comments to complex component logic
- [ ] Update any developer onboarding docs with new flow

### 24. Final validation and review
- [ ] Run `npm run build` and fix any build errors
- [ ] Run `npm run lint` and fix any linting issues
- [ ] Review all converted components for code quality
- [ ] Verify no TypeScript errors (`npm run type-check`)
- [ ] Verify all TODO comments are resolved or documented
- [ ] Test production build locally
- [ ] Get peer code review on changes

## Dependencies and Parallel Work

**Can be done in parallel:**
- Task 1 (UI components) and Task 2 (design system) can run concurrently
- Tasks 8-16 (individual step conversions) can be parallelized after Task 7 is complete

**Sequential dependencies:**
- Tasks 1-2 must complete before Tasks 3-6 (auth needs UI components)
- Tasks 3-6 must complete before starting Tasks 7-16 (auth must work first)
- Tasks 7-16 must complete before Task 17-18 (all steps must exist before orchestration)
- Task 18 must complete before Task 19 (flow must work before routing)
- Tasks 17-19 must complete before Task 20-24 (don't delete old files until new flow works)

## Estimated Complexity

**High complexity:** Tasks 10 (Location with map), 11 (Services), 14 (Gallery), 17-18 (orchestration)
**Medium complexity:** Tasks 3-4 (auth views), 8-9, 12-13, 15-16 (registration steps)
**Low complexity:** Tasks 1-2 (UI components), 7 (progress indicator), 20-24 (cleanup and testing)

## Success Criteria

- [ ] All Figma components successfully converted to Vue 3
- [ ] Complete registration flow works end-to-end
- [ ] All existing backend APIs remain compatible
- [ ] RTL layout works correctly on all screens
- [ ] Persian text displays correctly throughout
- [ ] All legacy and duplicate files removed
- [ ] All unit tests pass
- [ ] E2E tests pass for complete flow
- [ ] No TypeScript or linting errors
- [ ] Production build succeeds
- [ ] Code review approved
