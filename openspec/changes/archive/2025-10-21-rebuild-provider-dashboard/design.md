# Design Document: Provider Dashboard Rebuild

## Context

The Provider Dashboard and Layout are the primary interfaces for service providers to monitor their business performance, manage bookings, and access quick actions. The current implementation uses basic styling without a cohesive design system, lacks modern visual patterns, and needs enhanced RTL/LTR support for the multi-language platform (Persian, English, Arabic). The provider layout includes the header, sidebar navigation, and main content area that wraps all provider portal pages.

### Constraints
- Must maintain all existing functionality (no feature loss)
- Must work seamlessly in both RTL (Persian, Arabic) and LTR (English) layouts
- Light mode only (no dark theme)
- Must use existing component architecture
- Must be responsive (mobile, tablet, desktop)
- Must maintain performance (no significant bundle size increase)

### Stakeholders
- Service providers (primary users)
- Frontend development team
- UX/design team

## Goals / Non-Goals

### Goals
- Create a modern, professional dashboard and layout interface that reflects current design trends
- Establish a reusable design system with design tokens for consistency across dashboard and layout
- Enhance RTL/LTR support with proper bidirectional layouts for both dashboard content and navigation
- Improve visual hierarchy and information architecture in dashboard and navigation
- Add data visualization for better insights
- Implement smooth transitions and micro-interactions throughout layout and dashboard
- Modernize provider navigation (sidebar, header, footer) with better UX patterns
- Maintain or improve current performance metrics

### Non-Goals
- Dark theme implementation (explicitly excluded)
- Changing backend API or data structures
- Modifying global navigation or language switcher
- Adding new business features (pure visual/UX rebuild)
- Complete design system for entire application (dashboard-focused)

## Decisions

### 1. Design System Architecture

**Decision**: Implement design tokens using CSS custom properties (variables) with a centralized token file.

**Why**:
- CSS custom properties have excellent browser support and no build overhead
- Easy to update and maintain
- Native support for runtime theming if needed later
- Works perfectly with both RTL and LTR
- No additional dependencies

**Implementation**:
```scss
// design-tokens.scss
:root {
  /* Color Palette - Modern, Professional */
  --color-primary-50: #f0f4ff;
  --color-primary-500: #667eea;
  --color-primary-600: #5a67d8;

  /* Spacing Scale */
  --spacing-xs: 0.5rem;   // 8px
  --spacing-sm: 0.75rem;  // 12px
  --spacing-md: 1rem;     // 16px
  --spacing-lg: 1.5rem;   // 24px
  --spacing-xl: 2rem;     // 32px

  /* Typography */
  --font-size-xs: 0.75rem;
  --font-size-sm: 0.875rem;
  --font-size-base: 1rem;
  --font-size-lg: 1.125rem;
  --font-size-xl: 1.25rem;

  /* Shadows - Modern, Subtle */
  --shadow-sm: 0 1px 2px 0 rgba(0, 0, 0, 0.05);
  --shadow-md: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
  --shadow-lg: 0 10px 15px -3px rgba(0, 0, 0, 0.1);

  /* Border Radius */
  --radius-sm: 0.375rem;
  --radius-md: 0.5rem;
  --radius-lg: 0.75rem;
  --radius-xl: 1rem;
}
```

### 2. Component Modernization Strategy

**Decision**: Enhance existing custom components rather than introduce a third-party framework.

**Why**:
- Team already familiar with component structure
- Full control over RTL implementation
- Smaller bundle size
- Unique brand identity
- Existing components already integrated with vue-i18n and useRTL

**Components to enhance**:
- Card: Add subtle shadows, better spacing, optional gradient borders
- Button: Add loading states, icon support, ripple effects
- Stats Cards: Add trend indicators, animations, better visual hierarchy
- Charts: Add new lightweight chart components

### 3. Data Visualization Library

**Decision**: Use Apache ECharts with RTL configuration.

**Why**:
- Lightweight and performant
- Excellent RTL support (via `rtl: true` option)
- Rich chart types (line, bar, pie for analytics)
- Vue 3 compatible (vue-echarts wrapper)
- Active maintenance and large community
- Better for RTL than Chart.js

**Alternatives considered**:
- Chart.js: Popular but weaker RTL support, requires custom plugins
- D3.js: Too low-level, larger learning curve, heavier bundle
- Recharts: React-focused, not ideal for Vue

### 4. Layout System

**Decision**: Use CSS Grid for main dashboard layout with logical properties for RTL.

**Why**:
- Modern, flexible, and well-supported
- Logical properties (`margin-inline-start` vs `margin-left`) handle RTL automatically
- Better for complex responsive layouts
- No framework dependencies

**Example**:
```scss
.dashboard-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: var(--spacing-lg);

  /* RTL-safe spacing */
  margin-inline: var(--spacing-md);
  padding-inline-start: var(--spacing-lg);
}
```

### 5. Modern Design Patterns

**Decision**: Implement following modern patterns:
- **Glassmorphism cards**: Subtle backdrop blur with translucent backgrounds
- **Gradient accents**: Subtle gradients for primary actions and highlights
- **Micro-interactions**: Hover states, focus rings, loading skeletons
- **Progressive disclosure**: Collapsible sections, expand/collapse animations
- **Skeleton loading**: Better perceived performance than spinners

**Why**: These patterns are current industry standards (seen in Stripe, Linear, Notion) and improve perceived quality and user engagement.

### 6. Color Palette

**Decision**: Use a modern, professional color scheme:
- Primary: Indigo/Blue (#667eea) - Trust, professionalism
- Success: Green (#10b981) - Positive metrics, growth
- Warning: Amber (#f59e0b) - Pending items, attention needed
- Danger: Red (#ef4444) - Errors, critical items
- Neutrals: Modern gray scale (#f9fafb to #111827)

**Why**:
- Accessible (meets WCAG AA standards)
- Professional and modern
- Works well in RTL/LTR contexts
- Sufficient contrast for readability

## Risks / Trade-offs

### Risk 1: User Re-familiarization
**Impact**: Medium - Users need to learn new layout
**Mitigation**:
- Maintain same information architecture (stats, bookings, actions)
- Add optional onboarding tooltip tour
- Gradual rollout with feature flag if needed

### Risk 2: Bundle Size Increase
**Impact**: Low - Adding ECharts increases bundle
**Mitigation**:
- Use tree-shaking to import only needed chart types
- Lazy-load charts (dynamic import)
- Target: Keep total increase under 100KB gzipped

### Risk 3: RTL Layout Issues
**Impact**: Medium - Complex layouts can break in RTL
**Mitigation**:
- Use CSS logical properties throughout
- Test extensively in all three languages (fa, en, ar)
- Leverage existing useRTL composable
- Create RTL-specific test cases

### Risk 4: Development Time
**Impact**: Medium - Complete rebuild takes time
**Mitigation**:
- Break into phases (design tokens → components → charts)
- Reuse existing component logic where possible
- Focus on dashboard first, expand design system later

## Migration Plan

### Phase 1: Design Foundation (Week 1)
1. Create design tokens file
2. Update shared component styles (Card, Button, Badge, Skeleton)
3. Set up ECharts integration
4. Create component library documentation

### Phase 2: Provider Layout Rebuild (Week 2)
1. Rebuild ProviderLayout container
2. Rebuild ProviderHeader with modern styling
3. Rebuild ProviderSidebar with modern navigation
4. Rebuild NavItem with enhanced interactivity
5. Rebuild ProviderFooter
6. Test layout RTL/LTR and responsive behavior

### Phase 3: Dashboard Components (Week 3)
1. Rebuild WelcomeCard with modern design
2. Enhance QuickStatsCard with animations and trends
3. Redesign RecentBookingsCard with better data presentation
4. Update QuickActionsCard with better visual hierarchy

### Phase 4: Data Visualization (Week 4)
1. Add revenue trend chart component
2. Add bookings trend chart component
3. Implement chart loading states
4. Add chart interactions (tooltips, zoom)

### Phase 5: Main Dashboard View (Week 5)
1. Rebuild ProviderDashboardView with new layout
2. Integrate all modernized components
3. Add loading skeletons
4. Implement responsive breakpoints

### Phase 6: Testing & Refinement (Week 6)
1. RTL/LTR testing across all languages (layout + dashboard)
2. Responsive testing (mobile, tablet, desktop)
3. Performance testing and optimization
4. Accessibility audit (keyboard navigation, screen readers)
5. User acceptance testing

### Rollback Plan
- Feature flag: `FEATURE_MODERN_PROVIDER_UI` to toggle between old/new layout and dashboard
- Keep old files for 1 sprint:
  - Dashboard: `views/dashboard/legacy/ProviderDashboardView.legacy.vue`
  - Layout: `layouts/legacy/ProviderLayout.legacy.vue`
  - Navigation: `components/navigation/legacy/`
- Database: No changes, pure frontend
- Quick rollback: Toggle feature flag to `false`

## Open Questions

1. **Color palette approval**: Does the proposed color scheme align with Booksy brand guidelines? (Need design team review)
2. **Chart types**: Which specific charts are most valuable for providers? (Line, bar, pie, donut?)
3. **Animation preferences**: How much animation is appropriate? (Subtle vs prominent)
4. **Mobile-first priority**: Should mobile view be prioritized over desktop?
5. **Performance budget**: What's the acceptable maximum bundle size increase?
6. **Navigation structure**: Should the sidebar navigation sections/items change or remain the same?
7. **Layout customization**: Should providers be able to customize their dashboard layout (drag-drop widgets)?
8. **Mobile navigation**: Should mobile use bottom navigation bar instead of sidebar overlay?

## Technical Specifications

### Browser Support
- Chrome/Edge: Last 2 versions
- Firefox: Last 2 versions
- Safari: Last 2 versions
- Mobile Safari/Chrome: Last 2 versions

### Performance Targets
- First Contentful Paint (FCP): < 1.5s
- Largest Contentful Paint (LCP): < 2.5s
- Time to Interactive (TTI): < 3.5s
- Bundle size increase: < 100KB gzipped

### Accessibility Requirements
- WCAG 2.1 Level AA compliance
- Keyboard navigation support
- Screen reader compatibility
- Focus indicators on all interactive elements
- Sufficient color contrast (4.5:1 for text)

### RTL/LTR Requirements
- All layouts must flip correctly in RTL
- Charts must render in correct direction
- Icons must flip when directionally meaningful
- Text alignment must adapt to language direction
- No hardcoded left/right values (use logical properties)
