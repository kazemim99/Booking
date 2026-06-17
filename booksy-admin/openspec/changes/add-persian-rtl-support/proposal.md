# Change: Add Persian RTL Support to Admin Panel

## Why
The Booksy admin panel currently only supports English (LTR) and lacks Persian/Farsi language support. Since the target market is Persian-speaking users in Iran, the admin panel must provide full Persian language support with proper right-to-left (RTL) text direction and Persian fonts. The booksy-frontend already has comprehensive Persian RTL implementation that serves as a reference model for this change.

## What Changes
- **Add vue-i18n dependency** for internationalization support (Persian & English)
- **Create Pinia locale store** for managing language selection, text direction, and locale preferences
- **Add Persian font files** (Vazir as primary, B Nazanin as fallback) with proper @font-face declarations
- **Create translation files** (fa.json, en.json) for all UI text, menus, forms, and messages
- **Implement RTL composable** for directional helpers (text-align, float, spacing utilities)
- **Update App.vue** to bind text direction dynamically based on selected locale
- **Configure Ant Design Vue** for RTL mode using ConfigProvider component
- **Create RTL CSS utilities** using CSS logical properties (margin-inline, padding-inline, etc.)
- **Add language switcher component** in AdminLayout header for toggling between Persian/English
- **Translate all hardcoded text** to use i18n translation keys

## Impact

### Affected Specifications
- **New Capability**: `i18n` - Internationalization infrastructure
- **New Capability**: `rtl-layout` - Right-to-left layout management
- **New Capability**: `persian-fonts` - Persian typography system
- **New Capability**: `ui-components` - RTL-aware component styling

### Affected Code
- `package.json` - Add vue-i18n, @persian-tools/persian-tools dependencies
- `src/main.ts` - Initialize i18n plugin with default Persian locale
- `src/App.vue` - Add direction binding and RTL class application
- `src/stores/` - New locale store for language/direction management
- `src/composables/` - New useRTL composable for directional utilities
- `src/assets/fonts/` - New directory with Vazir and B Nazanin font files
- `src/assets/styles/` - New fonts.css and rtl.css for styling
- `src/locales/` - New directory with fa.json and en.json translation files
- `src/layouts/AdminLayout.vue` - Add language switcher component
- All `src/views/**/*.vue` - Replace hardcoded text with translation keys
- All `src/components/**/*.vue` - Replace hardcoded text with translation keys

### Dependencies
- `vue-i18n@^10.0.8` - Vue 3 internationalization library
- `@persian-tools/persian-tools@^4.0.4` - Persian utilities (optional, for number/date formatting)
- Font files: Vazir (100, 300, 400, 500, 700 weights) and B Nazanin (400, 700 weights)

### Breaking Changes
None - This is an additive change that enhances existing functionality without breaking current behavior.

### Migration Notes
- Default locale will be Persian (fa) with RTL direction
- English users can switch language via header switcher
- Existing API calls and data structures remain unchanged
- Browser localStorage will persist language preference
