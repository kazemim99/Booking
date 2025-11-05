# Replace Registration Flow with Figma-Generated Design

## Why
The current registration and onboarding flow uses an older UI design that doesn't align with the modern, RTL Persian user experience requirements. A new Figma-generated design has been created that implements a comprehensive Booksy-style onboarding flow with:
- Modern RTL Persian interface matching target market expectations
- Improved user experience with visual step indicators and progress tracking
- Additional steps (Gallery, Optional Feedback) for better provider profiles
- Consistent design system using shadcn/ui patterns
- Better mobile responsiveness and accessibility

The existing Vue registration flow has accumulated technical debt with duplicate files (`.IMPROVED`, `.FIXED` suffixes) and inconsistent authentication flows. This change provides an opportunity to consolidate, modernize, and improve the entire onboarding experience.

## What Changes
- **Replace authentication module**: Convert and replace existing auth views ([PhoneLoginView.vue](booksy-frontend/src/modules/auth/views/PhoneLoginView.vue), [PhoneVerificationView.vue](booksy-frontend/src/modules/auth/views/PhoneVerificationView.vue)) with new Figma-based design
- **Reorder registration steps**: Align with Figma flow sequence (Login → Verification → BusinessInfo → Category → Location → Services → Staff → WorkingHours → Gallery → Feedback → Completion)
- **Add new steps**:
  - Gallery upload step for provider portfolio images
  - Optional feedback step for user experience improvement
- **Convert React to Vue 3**: Translate all Figma-exported React components to Vue 3 Composition API while maintaining RTL and Persian layout
- **Clean up legacy files**: Remove duplicate and legacy registration components (`.IMPROVED`, `.FIXED`, `.legacy` files)
- **Update routing**: Modify router configuration to support new step sequence
- **Preserve backend integration**: Maintain all existing API endpoints and data structures

## Impact
- **Affected specs**:
  - `provider-registration` (MODIFIED - reorder steps, add Gallery and Feedback steps)
  - `authentication` (MODIFIED - replace login/verification UI)

- **Affected code**:
  - **Frontend**:
    - **Auth Module** (`booksy-frontend/src/modules/auth/`):
      - Replace `views/PhoneLoginView.vue` with converted LoginPage
      - Replace `views/PhoneVerificationView.vue` with converted CodeVerification
      - Update `composables/usePhoneVerification.ts` if needed for new UI patterns
    - **Provider Registration** (`booksy-frontend/src/modules/provider/`):
      - Convert all Figma onboarding components from React to Vue 3
      - Replace `components/registration/steps/` with new converted components
      - Update `views/registration/ProviderRegistrationFlow.vue` to use new step order
      - Update `composables/useProviderRegistration.ts` for new step sequence
      - Add new Gallery and OptionalFeedback step components
    - **Router** (`booksy-frontend/src/core/router/`):
      - Update routes to support new step order
    - **Files to delete**:
      - `components/registration/steps/BusinessLocationStep.FIXED.vue`
      - `components/registration/steps/ServicesStep.IMPROVED.vue`
      - All files in legacy directories (dashboard/legacy/, navigation/legacy/, layouts/legacy/)

- **Backend**: No changes required - existing API endpoints remain unchanged
- **Database**: No schema changes
- **Breaking changes**: None for backend; frontend registration flow will be replaced entirely
- **User Impact**: Existing providers won't be affected; new registrations will see improved UX
