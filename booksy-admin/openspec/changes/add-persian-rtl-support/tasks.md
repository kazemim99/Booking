# Implementation Tasks: Persian RTL Support

## 1. Infrastructure Setup

- [ ] 1.1 Install vue-i18n dependency (`npm install vue-i18n@^10.0.8`)
- [ ] 1.2 Install @persian-tools/persian-tools for utilities (`npm install @persian-tools/persian-tools@^4.0.4`)
- [ ] 1.3 Create directory structure:
  - `src/locales/` for translation files
  - `src/assets/fonts/vazir/` for Vazir font files
  - `src/assets/fonts/bnazanin/` for B Nazanin font files
  - `src/composables/` for RTL composable
  - `src/stores/` (already exists) for locale store
  - `src/types/` (already exists) for TypeScript types

## 2. Font Assets

- [ ] 2.1 Download and add Vazir font files to `src/assets/fonts/vazir/`:
  - Vazir-Thin.woff2 (weight 100)
  - Vazir-Light.woff2 (weight 300)
  - Vazir.woff2 (weight 400, Regular)
  - Vazir-Medium.woff2 (weight 500)
  - Vazir-Bold.woff2 (weight 700)
- [ ] 2.2 Download and add B Nazanin font files to `src/assets/fonts/bnazanin/`:
  - BNazanin.woff2 (weight 400, Regular)
  - BNazanin-Bold.woff2 (weight 700)
- [ ] 2.3 Create `src/assets/styles/fonts.css` with @font-face declarations for all fonts
- [ ] 2.4 Import fonts.css in `src/assets/styles/main.css`
- [ ] 2.5 Add CSS rule `html[lang="fa"] * { font-family: 'Vazir', 'B Nazanin', Tahoma, 'Iranian Sans', sans-serif !important; }`

## 3. TypeScript Types

- [ ] 3.1 Create `src/types/locale.types.ts` with enums and interfaces:
  - Language enum ('fa' | 'en')
  - Direction enum ('rtl' | 'ltr')
  - DateFormat enum ('gregorian' | 'jalaali')
  - NumberFormat enum ('western' | 'persian')
  - LocaleState interface
  - LocaleConfig interface

## 4. Pinia Locale Store

- [ ] 4.1 Create `src/stores/locale.store.ts` with defineStore
- [ ] 4.2 Implement state:
  - currentLocale: 'fa' | 'en'
  - direction: 'rtl' | 'ltr'
  - dateFormat: 'gregorian'
  - numberFormat: 'western'
- [ ] 4.3 Implement getters:
  - isRTL: computed boolean
  - isPersian: computed boolean
- [ ] 4.4 Implement actions:
  - setLocale(locale: string)
  - toggleLocale()
  - initializeFromStorage()
  - persistToStorage()
- [ ] 4.5 Add localStorage persistence logic with key `booksy_admin_locale_settings`
- [ ] 4.6 Add automatic direction update when locale changes

## 5. RTL Composable

- [ ] 5.1 Create `src/composables/useRTL.ts`
- [ ] 5.2 Implement direction computed property from locale store
- [ ] 5.3 Implement helper functions:
  - getTextAlign(): returns 'right' | 'left'
  - getFloat(position): returns directional float value
  - getMarginStart(value): returns margin with direction
  - getMarginEnd(value): returns margin with direction
  - getPaddingStart(value): returns padding with direction
  - getPaddingEnd(value): returns padding with direction
- [ ] 5.4 Implement document direction updater (sets document.documentElement.dir)
- [ ] 5.5 Implement body class updater (adds 'rtl' or 'ltr' class)

## 6. RTL CSS Utilities

- [ ] 6.1 Create `src/assets/styles/rtl.css` with utility classes:
  - .text-start, .text-end
  - .float-start, .float-end
  - .rtl-flip (scaleX(-1) in RTL)
  - .rtl-rotate-180
- [ ] 6.2 Add CSS logical property utilities:
  - .m-inline-start-*, .m-inline-end-*
  - .p-inline-start-*, .p-inline-end-*
  - .border-inline-start-*, .border-inline-end-*
- [ ] 6.3 Import rtl.css in `src/assets/styles/main.css`
- [ ] 6.4 Add base RTL rules: `html[dir='rtl'] { direction: rtl; }` and `html[dir='ltr'] { direction: ltr; }`

## 7. i18n Configuration

- [ ] 7.1 Create `src/locales/fa.json` with Persian translation structure:
  - app: { name }
  - common: { save, cancel, delete, edit, create, search, ... }
  - auth: { login, logout, email, password, ... }
  - navigation: { dashboard, users, providers, services, analytics, payments, orders, logs, settings }
  - user: { title, createUser, editUser, deleteUser, ... }
  - provider: { title, approveProvider, ... }
  - validation: { required, invalid, ... }
  - messages: { success, error, ... }
- [ ] 7.2 Create `src/locales/en.json` with English translations (mirror structure)
- [ ] 7.3 Update `src/main.ts` to import and configure vue-i18n:
  - Import createI18n, fa.json, en.json
  - Create i18n instance with legacy: false, locale: 'fa', fallbackLocale: 'en', globalInjection: true
  - app.use(i18n)

## 8. App.vue Updates

- [ ] 8.1 Import and configure a-config-provider from ant-design-vue
- [ ] 8.2 Import Ant Design locales (faIR, enUS)
- [ ] 8.3 Import useLocaleStore and useRTL
- [ ] 8.4 Create computed properties for currentLocale, direction, antdLocale
- [ ] 8.5 Wrap router-view with a-config-provider: `<a-config-provider :direction="direction" :locale="antdLocale">`
- [ ] 8.6 Add :dir and :lang bindings to root div: `<div id="app" :dir="direction" :lang="currentLocale">`
- [ ] 8.7 Test basic direction switching works

## 9. Language Switcher Component

- [ ] 9.1 Create `src/components/common/LanguageSwitcher.vue`
- [ ] 9.2 Import GlobalOutlined icon from @ant-design/icons-vue
- [ ] 9.3 Implement dropdown with Persian (فارسی) and English options
- [ ] 9.4 Implement locale change handler using locale store
- [ ] 9.5 Show current language with checkmark in dropdown
- [ ] 9.6 Add component to AdminLayout header (near user profile menu)
- [ ] 9.7 Test language switching updates entire UI

## 10. View Components Translation - Authentication

- [ ] 10.1 Update `src/views/Login.vue`:
  - Replace "Login" with {{ $t('auth.login') }}
  - Replace "Email" with {{ $t('auth.email') }}
  - Replace "Password" with {{ $t('auth.password') }}
  - Replace "Remember Me" with {{ $t('auth.rememberMe') }}
  - Replace button text with translations
- [ ] 10.2 Test login page in both languages

## 11. View Components Translation - Dashboard

- [ ] 11.1 Update `src/views/dashboard/Dashboard.vue`:
  - Replace "Dashboard" with {{ $t('navigation.dashboard') }}
  - Replace all chart titles with translations
  - Replace all stat card labels with translations
- [ ] 11.2 Test dashboard page in both languages

## 12. View Components Translation - Users

- [ ] 12.1 Update `src/views/users/UserList.vue`:
  - Replace "Users" with {{ $t('user.title') }}
  - Replace "Create User" with {{ $t('user.createUser') }}
  - Replace table column headers with translations
  - Replace action button labels with translations
- [ ] 12.2 Update `src/views/users/UserForm.vue`:
  - Replace all form labels with translations
  - Replace validation messages with translations
- [ ] 12.3 Test users section in both languages

## 13. View Components Translation - Providers

- [ ] 13.1 Update `src/views/providers/ProviderList.vue`:
  - Replace "Providers" with {{ $t('provider.title') }}
  - Replace table headers and actions with translations
- [ ] 13.2 Update `src/views/providers/ProviderDetails.vue`:
  - Replace all labels and buttons with translations
- [ ] 13.3 Test providers section in both languages

## 14. View Components Translation - Services

- [ ] 14.1 Update `src/views/services/ServiceList.vue`:
  - Replace all text with translation keys
- [ ] 14.2 Update `src/views/services/ServiceForm.vue`:
  - Replace form labels with translations
- [ ] 14.3 Test services section in both languages

## 15. View Components Translation - Analytics

- [ ] 15.1 Update `src/views/analytics/Analytics.vue`:
  - Replace chart titles with translations
  - Replace stat labels with translations
- [ ] 15.2 Test analytics page in both languages

## 16. View Components Translation - Other Sections

- [ ] 16.1 Update Payments views with translations
- [ ] 16.2 Update Orders views with translations
- [ ] 16.3 Update Logs views with translations
- [ ] 16.4 Update Settings views with translations

## 17. AdminLayout Translation

- [ ] 17.1 Update `src/layouts/AdminLayout.vue`:
  - Replace menu item labels with translations
  - Replace header elements with translations
  - Test menu in both languages

## 18. API Error Messages Translation

- [ ] 18.1 Update `src/utils/axios.ts`:
  - Add i18n import
  - Translate error messages using t() function
- [ ] 18.2 Test error messages appear in current locale

## 19. RTL Layout Testing

- [ ] 19.1 Test all forms in RTL mode:
  - Labels align right
  - Input fields flow RTL
  - Buttons in correct order
- [ ] 19.2 Test all tables in RTL mode:
  - Columns reversed
  - Actions column on left
  - Pagination works correctly
- [ ] 19.3 Test all modals in RTL mode:
  - Close button on left
  - Buttons in correct order
- [ ] 19.4 Test sidebar and header in RTL mode:
  - Sidebar on right
  - Logo on right
  - Menus aligned correctly

## 20. Ant Design Component RTL Verification

- [ ] 20.1 Verify dropdowns align correctly in RTL
- [ ] 20.2 Verify select components work in RTL
- [ ] 20.3 Verify date pickers work in RTL
- [ ] 20.4 Verify notifications appear from correct side
- [ ] 20.5 Verify breadcrumbs flow correctly in RTL

## 21. Cross-Browser Testing

- [ ] 21.1 Test in Chrome (RTL + fonts)
- [ ] 21.2 Test in Firefox (RTL + fonts)
- [ ] 21.3 Test in Edge (RTL + fonts)
- [ ] 21.4 Test in Safari (RTL + fonts)

## 22. Responsive Testing

- [ ] 22.1 Test RTL layout on desktop (1920x1080)
- [ ] 22.2 Test RTL layout on tablet (768px width)
- [ ] 22.3 Test RTL layout on mobile (375px width)

## 23. Performance Optimization

- [ ] 23.1 Verify font files are properly compressed (WOFF2)
- [ ] 23.2 Check bundle size increase is acceptable (<500KB)
- [ ] 23.3 Verify font-display: swap is working (no FOUT)
- [ ] 23.4 Test initial page load time with fonts

## 24. Documentation

- [ ] 24.1 Update README.md with RTL support information
- [ ] 24.2 Document how to add new translations
- [ ] 24.3 Document RTL CSS utility classes
- [ ] 24.4 Add screenshots of Persian UI to documentation

## 25. Final Validation

- [ ] 25.1 Run `openspec validate add-persian-rtl-support --strict`
- [ ] 25.2 Verify all translation keys are used (no unused keys)
- [ ] 25.3 Verify no hardcoded English text remains
- [ ] 25.4 Test complete user workflow in Persian (login → dashboard → create user → logout)
- [ ] 25.5 Test complete user workflow in English
- [ ] 25.6 Verify language preference persists across browser refresh

## Dependencies and Parallelization

**Sequential Dependencies:**
- Tasks 1-3 (Infrastructure, Fonts, Types) must complete before other tasks
- Task 7 (i18n config) requires Task 1 complete
- Task 8 (App.vue) requires Tasks 4, 5, 7 complete
- Tasks 10-17 (Translation) require Tasks 7, 8, 9 complete

**Parallelizable Tasks:**
- Tasks 2 (Fonts) and 3 (Types) can be done in parallel
- Tasks 4 (Store) and 5 (Composable) can be done in parallel
- Tasks 6 (CSS) can be done in parallel with 4-5
- Tasks 10-17 (All view translations) can be done in parallel once Task 8 is complete
- Tasks 19-22 (Testing) can be done in parallel

**Estimated Timeline:**
- Phase 1 (Tasks 1-9): 2-3 days
- Phase 2 (Tasks 10-18): 2-3 days
- Phase 3 (Tasks 19-25): 2 days
- **Total: 6-8 days**
