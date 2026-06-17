# Internationalization (i18n) Capability

## ADDED Requirements

### Requirement: Vue-i18n Integration
The system SHALL integrate vue-i18n v10.x for internationalization support with Persian (fa) and English (en) locales.

#### Scenario: Default Persian locale
- **WHEN** the application loads for the first time
- **THEN** the default locale SHALL be Persian (fa)
- **AND** the fallback locale SHALL be English (en)

#### Scenario: Translation key resolution
- **WHEN** a translation key is requested via $t() function
- **THEN** the system SHALL return the translation for the current locale
- **AND** if the key is missing, SHALL fallback to the English translation
- **AND** if English is also missing, SHALL return the key itself

### Requirement: Translation File Structure
The system SHALL organize translations in JSON files under `src/locales/` directory with hierarchical key structure.

#### Scenario: Persian translations loaded
- **WHEN** the locale is set to 'fa'
- **THEN** the system SHALL load translations from `src/locales/fa.json`
- **AND** all UI text SHALL display in Persian

#### Scenario: English translations loaded
- **WHEN** the locale is set to 'en'
- **THEN** the system SHALL load translations from `src/locales/en.json`
- **AND** all UI text SHALL display in English

#### Scenario: Translation key hierarchy
- **GIVEN** a translation key like `auth.login.title`
- **THEN** the JSON structure SHALL be `{ "auth": { "login": { "title": "..." } } }`
- **AND** nested keys SHALL be accessible via dot notation

### Requirement: Component Translation Usage
The system SHALL provide translation functions in all Vue components via vue-i18n composition API.

#### Scenario: Template translation
- **WHEN** using `{{ $t('key') }}` in component template
- **THEN** the translated text SHALL render correctly
- **AND** SHALL reactively update when locale changes

#### Scenario: Script translation
- **WHEN** using `t('key')` from `useI18n()` in script setup
- **THEN** the translated text SHALL return correctly
- **AND** SHALL be available in computed properties and methods

#### Scenario: Pluralization support
- **WHEN** translation key has plural forms (e.g., `item | items`)
- **THEN** the system SHALL select correct form based on count parameter
- **AND** SHALL support Persian plural rules

### Requirement: Translation Coverage
The system SHALL translate all user-facing text including menus, forms, messages, and errors.

#### Scenario: Menu items translated
- **WHEN** viewing the navigation menu
- **THEN** all menu items SHALL display translated text
- **AND** SHALL include: Dashboard, Users, Providers, Services, Analytics, Payments, Orders, Logs, Settings

#### Scenario: Form labels translated
- **WHEN** viewing any form
- **THEN** all labels, placeholders, and buttons SHALL be translated
- **AND** SHALL include validation error messages

#### Scenario: System messages translated
- **WHEN** API requests succeed or fail
- **THEN** all success/error messages SHALL be translated
- **AND** SHALL provide meaningful feedback in current locale

### Requirement: Locale Persistence
The system SHALL persist the user's language preference across browser sessions.

#### Scenario: Save locale preference
- **WHEN** user changes language
- **THEN** the preference SHALL be saved to localStorage with key `booksy_admin_locale`
- **AND** SHALL persist the locale code ('fa' or 'en')

#### Scenario: Restore locale on load
- **WHEN** the application initializes
- **THEN** the system SHALL check localStorage for saved locale
- **AND** if found, SHALL set that locale as current
- **AND** if not found, SHALL use default Persian locale

### Requirement: Global Injection
The system SHALL make translation functions globally available without explicit imports.

#### Scenario: Global $t function available
- **WHEN** globalInjection is enabled in i18n config
- **THEN** `$t()` function SHALL be available in all component templates
- **AND** SHALL not require importing useI18n() in every component

#### Scenario: Options API compatibility
- **WHEN** using Options API components
- **THEN** `this.$t()` SHALL be available via component instance
- **AND** SHALL work identically to Composition API
