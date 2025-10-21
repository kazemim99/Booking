# Rebuild Provider Dashboard

## Why

The current Provider Dashboard layout no longer meets modern UI/UX standards and lacks a cohesive design system. The dashboard needs a complete rebuild to provide a professional, modern interface with enhanced RTL/LTR support, better visual hierarchy, and improved user experience while maintaining light mode only.

## What Changes

- **BREAKING**: Complete removal and rebuild of existing Provider Dashboard view ([ProviderDashboardView.vue](../../booksy-frontend/src/modules/provider/views/dashboard/ProviderDashboardView.vue))
- **BREAKING**: Complete rebuild of ProviderLayout and all navigation components (header, sidebar, footer)
- Implement modern design system with design tokens (colors, spacing, typography)
- Redesign and reorganize all dashboard components with modern aesthetic
- Redesign provider navigation with modern sidebar, header, and footer components
- Enhance RTL/LTR support across all dashboard and layout elements
- Remove dark theme support completely (light mode only)
- Keep language switcher in main header/navigation (global header, not dashboard-specific)
- Add data visualization for analytics (charts for bookings/revenue trends)
- Implement modern interaction patterns (micro-animations, loading skeletons, smooth transitions)
- Maintain all existing functionality (stats cards, recent bookings, quick actions, welcome card, status banners, navigation)

## Impact

- **Affected specs**: `provider-dashboard` (new capability), `provider-layout` (new capability)
- **Affected code**:
  - `booksy-frontend/src/modules/provider/views/dashboard/ProviderDashboardView.vue` (complete rewrite)
  - `booksy-frontend/src/modules/provider/layouts/ProviderLayout.vue` (complete rewrite)
  - `booksy-frontend/src/modules/provider/components/dashboard/` (all dashboard components)
  - `booksy-frontend/src/modules/provider/components/navigation/` (header, sidebar, footer, nav items)
  - `booksy-frontend/src/shared/styles/` (new design tokens and variables)
  - Potentially shared UI components for modernization
- **Breaking changes**: Complete visual redesign of layout and dashboard may require user re-familiarization
- **Dependencies**: May add lightweight charting library (Apache ECharts)
- **RTL/LTR**: Enhanced bidirectional layout support using existing `useRTL` composable
