# Design: Persian RTL Support Architecture

## Context
The booksy-admin application currently only supports English with left-to-right (LTR) text direction. The booksy-frontend application already has a complete Persian RTL implementation that includes:
- Vue-i18n for translations
- Pinia locale store for language/direction management
- Vazir and B Nazanin Persian fonts
- RTL composables and CSS utilities
- Persian number formatting and Jalaali calendar support

This design leverages the proven patterns from booksy-frontend while adapting them for the admin panel's unique requirements (Ant Design Vue instead of custom components).

## Goals / Non-Goals

### Goals
- Full Persian language support with professional RTL layout
- Seamless language switching between Persian (RTL) and English (LTR)
- Consistent font rendering using Vazir (primary) and B Nazanin (fallback)
- Ant Design Vue components properly mirrored in RTL mode
- Persistent language preference across sessions
- Zero breaking changes to existing functionality
- Maintain code consistency with booksy-frontend patterns

### Non-Goals
- Jalaali (Persian) calendar implementation (use Gregorian for admin)
- Persian number formatting (۰-۹) - use Western numerals for data consistency
- Arabic or Hebrew language support (only Persian and English)
- Mobile-specific RTL optimizations (responsive design already works)
- Automated translation (translations will be manual)

## Decisions

### Decision 1: Use vue-i18n v10.x
**Why**: Industry-standard i18n library for Vue 3, used successfully in booksy-frontend
**Alternatives considered**:
- Custom translation system - Rejected (reinventing the wheel)
- Ant Design i18n only - Rejected (insufficient for full app translation)

### Decision 2: Pinia Store for Locale Management
**Why**: Centralized state management, reactive updates, localStorage persistence
**Pattern**: Mirror booksy-frontend's locale store architecture
```typescript
{
  currentLocale: 'fa' | 'en',
  direction: 'rtl' | 'ltr',
  dateFormat: 'gregorian',
  numberFormat: 'western'
}
```

### Decision 3: Vazir Font Family
**Why**: Most popular Persian font, excellent readability, comprehensive weight support
**Weights included**: 100 (Thin), 300 (Light), 400 (Regular), 500 (Medium), 700 (Bold)
**Fallback**: B Nazanin (400, 700) for older browser compatibility
**Application**: CSS attribute selector `html[lang="fa"] *` to scope font loading

### Decision 4: CSS Logical Properties for RTL
**Why**: Modern, maintainable approach to bidirectional layouts
**Pattern**: Use `margin-inline-start` instead of `margin-left`, `padding-inline-end` instead of `padding-right`
**Fallback**: Provide utility classes for older browsers if needed

### Decision 5: Ant Design ConfigProvider for RTL
**Why**: Ant Design Vue has built-in RTL support via ConfigProvider
**Implementation**:
```vue
<a-config-provider :direction="direction" :locale="antdLocale">
  <router-view />
</a-config-provider>
```
This automatically mirrors all Ant Design components in RTL mode.

### Decision 6: Default to Persian Locale
**Why**: Target market is Persian-speaking users
**Behavior**: First-time visitors see Persian UI, can switch to English
**Persistence**: Save preference to localStorage key: `booksy_admin_locale`

### Decision 7: Language Switcher in Header
**Why**: Accessible, visible location for language toggle
**Position**: AdminLayout header, near user profile menu
**Design**: Icon + text dropdown showing "فارسی" / "English"

## Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                       App.vue                           │
│  ┌────────────────────────────────────────────────┐    │
│  │  <a-config-provider :direction :locale>        │    │
│  │    <router-view />                             │    │
│  │  </a-config-provider>                          │    │
│  └────────────────────────────────────────────────┘    │
│         ↑ Binds direction from useRTL()                │
└─────────────────────────────────────────────────────────┘
                           │
        ┌──────────────────┼──────────────────┐
        ↓                  ↓                  ↓
  ┌──────────┐      ┌──────────┐      ┌──────────┐
  │ i18n     │      │ Locale   │      │ useRTL   │
  │ Plugin   │◄─────│ Store    │─────►│ Composable│
  │          │      │ (Pinia)  │      │          │
  └──────────┘      └──────────┘      └──────────┘
        ↑                  ↑                  ↑
        │                  │                  │
  ┌──────────┐      ┌──────────┐      ┌──────────┐
  │ fa.json  │      │localStorage│     │ CSS      │
  │ en.json  │      │ Persist  │      │ Utilities│
  └──────────┘      └──────────┘      └──────────┘
```

## Component Changes

### 1. main.ts Initialization
```typescript
import { createI18n } from 'vue-i18n'
import fa from './locales/fa.json'
import en from './locales/en.json'

const i18n = createI18n({
  legacy: false,
  locale: 'fa',
  fallbackLocale: 'en',
  messages: { fa, en },
  globalInjection: true,
})

app.use(i18n)
```

### 2. App.vue Direction Binding
```vue
<template>
  <a-config-provider :direction="direction" :locale="antdLocale">
    <div id="app" :dir="direction" :lang="currentLocale">
      <router-view />
    </div>
  </a-config-provider>
</template>

<script setup lang="ts">
import { useLocaleStore } from '@/stores/locale.store'
import { useRTL } from '@/composables/useRTL'
import { computed } from 'vue'
import faIR from 'ant-design-vue/es/locale/fa_IR'
import enUS from 'ant-design-vue/es/locale/en_US'

const localeStore = useLocaleStore()
const { direction } = useRTL()

const currentLocale = computed(() => localeStore.currentLocale)
const antdLocale = computed(() => currentLocale.value === 'fa' ? faIR : enUS)
</script>
```

### 3. AdminLayout Language Switcher
```vue
<a-dropdown>
  <a-button>
    <GlobalOutlined />
    {{ currentLocale === 'fa' ? 'فارسی' : 'English' }}
  </a-button>
  <template #overlay>
    <a-menu @click="handleLocaleChange">
      <a-menu-item key="fa">فارسی</a-menu-item>
      <a-menu-item key="en">English</a-menu-item>
    </a-menu>
  </template>
</a-dropdown>
```

## File Structure

```
src/
├── assets/
│   ├── fonts/
│   │   ├── vazir/
│   │   │   ├── Vazir-Thin.woff2
│   │   │   ├── Vazir-Light.woff2
│   │   │   ├── Vazir.woff2            (Regular 400)
│   │   │   ├── Vazir-Medium.woff2
│   │   │   └── Vazir-Bold.woff2
│   │   └── bnazanin/
│   │       ├── BNazanin.woff2         (Regular 400)
│   │       └── BNazanin-Bold.woff2
│   └── styles/
│       ├── fonts.css                   (Font @font-face declarations)
│       ├── rtl.css                     (RTL utility classes)
│       └── main.css                    (Updated with RTL imports)
├── composables/
│   └── useRTL.ts                       (Direction helpers)
├── stores/
│   └── locale.store.ts                 (Language/direction state)
├── locales/
│   ├── fa.json                         (Persian translations)
│   └── en.json                         (English translations)
└── types/
    └── locale.types.ts                 (TypeScript enums/interfaces)
```

## Risks / Trade-offs

### Risk 1: Bundle Size Increase
**Impact**: Adding fonts and i18n increases bundle size by ~500KB
**Mitigation**:
- Use WOFF2 format (best compression)
- Lazy-load translation files if needed
- Font subsetting to include only Persian + English glyphs

### Risk 2: Ant Design RTL Edge Cases
**Impact**: Some Ant Design components may have RTL rendering bugs
**Mitigation**:
- Test all components thoroughly
- Create custom CSS overrides if needed
- Report bugs to Ant Design Vue team

### Risk 3: Translation Maintenance Overhead
**Impact**: All new features require dual translations (Persian + English)
**Mitigation**:
- Use translation key conventions (`auth.login`, `user.create`)
- Maintain translation checklist in PR templates
- Consider future integration with translation management platform

### Risk 4: Developer Onboarding
**Impact**: Developers must learn i18n patterns and RTL considerations
**Mitigation**:
- Document RTL patterns in README
- Provide code examples and templates
- Enforce via code review checklist

## Migration Plan

### Phase 1: Infrastructure Setup (Day 1)
1. Install dependencies (vue-i18n, Persian fonts)
2. Create locale store and RTL composable
3. Add font files and CSS declarations
4. Update App.vue with ConfigProvider
5. Test basic direction switching

### Phase 2: Translation Files (Day 2-3)
1. Create translation file structure
2. Extract all hardcoded English text
3. Generate translation keys
4. Translate to Persian
5. Validate translation coverage

### Phase 3: Component Updates (Day 4-5)
1. Update all views to use $t() translation function
2. Update AdminLayout with language switcher
3. Test all pages in both languages
4. Fix RTL layout issues

### Phase 4: Polish & Testing (Day 6-7)
1. Test all forms and tables in RTL
2. Verify charts and visualizations
3. Test language persistence
4. Browser compatibility testing
5. Documentation updates

### Rollback Plan
If critical issues arise:
1. Set default locale to 'en' in main.ts
2. Hide language switcher in AdminLayout
3. Persian fonts will be dormant but harmless
4. No data migration needed (purely frontend)

## Open Questions

1. **Date Formatting**: Should admin panel support Jalaali calendar or stick with Gregorian?
   - **Recommendation**: Use Gregorian for consistency with backend APIs

2. **Number Formatting**: Should we display Persian numerals (۰-۹) or Western (0-9)?
   - **Recommendation**: Use Western numerals for data clarity and API compatibility

3. **Translation Workflow**: Manual translation or use translation service?
   - **Recommendation**: Manual for v1, evaluate Lokalise/Crowdin later

4. **Font Loading Strategy**: Synchronous or async font loading?
   - **Recommendation**: Synchronous to prevent FOUT (Flash of Unstyled Text)

5. **RTL for Charts**: Should ECharts be mirrored in RTL mode?
   - **Recommendation**: No - charts use universal visual language, mirroring may confuse
