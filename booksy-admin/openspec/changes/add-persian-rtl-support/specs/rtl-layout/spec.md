# RTL Layout Management Capability

## ADDED Requirements

### Requirement: Pinia Locale Store
The system SHALL provide a Pinia store for managing locale, direction, and related preferences.

#### Scenario: Store initialization
- **WHEN** the application initializes
- **THEN** the locale store SHALL be created with default state
- **AND** default locale SHALL be 'fa'
- **AND** default direction SHALL be 'rtl'
- **AND** default dateFormat SHALL be 'gregorian'
- **AND** default numberFormat SHALL be 'western'

#### Scenario: Locale change updates direction
- **WHEN** locale is changed to 'fa'
- **THEN** direction SHALL automatically update to 'rtl'
- **WHEN** locale is changed to 'en'
- **THEN** direction SHALL automatically update to 'ltr'

#### Scenario: Store persistence
- **WHEN** any locale preference changes
- **THEN** the state SHALL be persisted to localStorage
- **AND** SHALL use key `booksy_admin_locale_settings`
- **AND** SHALL restore settings on next app load

### Requirement: RTL Composable
The system SHALL provide a `useRTL()` composable for directional helpers and utilities.

#### Scenario: Direction reactive state
- **WHEN** `useRTL()` is called
- **THEN** it SHALL return reactive `direction` computed property
- **AND** direction SHALL be 'rtl' or 'ltr' based on current locale

#### Scenario: Text alignment helper
- **WHEN** calling `getTextAlign()` from useRTL
- **THEN** it SHALL return 'right' in RTL mode
- **AND** SHALL return 'left' in LTR mode

#### Scenario: Float helper
- **WHEN** calling `getFloat('start')` from useRTL
- **THEN** it SHALL return 'right' in RTL mode
- **AND** SHALL return 'left' in LTR mode
- **WHEN** calling `getFloat('end')` from useRTL
- **THEN** it SHALL return 'left' in RTL mode
- **AND** SHALL return 'right' in LTR mode

#### Scenario: Spacing helper
- **WHEN** calling `getMarginStart('16px')` from useRTL
- **THEN** it SHALL return margin-right:16px in RTL mode
- **AND** SHALL return margin-left:16px in LTR mode

### Requirement: Document Direction Binding
The system SHALL dynamically bind text direction to document root elements.

#### Scenario: HTML dir attribute
- **WHEN** locale changes
- **THEN** `document.documentElement.dir` SHALL be set to 'rtl' or 'ltr'
- **AND** SHALL trigger CSS direction inheritance

#### Scenario: HTML lang attribute
- **WHEN** locale changes
- **THEN** `document.documentElement.lang` SHALL be set to locale code
- **AND** SHALL enable proper font loading via CSS attribute selector

#### Scenario: Body CSS classes
- **WHEN** direction is RTL
- **THEN** `<body>` SHALL have class 'rtl'
- **AND** SHALL remove class 'ltr'
- **WHEN** direction is LTR
- **THEN** `<body>` SHALL have class 'ltr'
- **AND** SHALL remove class 'rtl'

### Requirement: Ant Design RTL Configuration
The system SHALL configure Ant Design Vue components for RTL support via ConfigProvider.

#### Scenario: ConfigProvider direction binding
- **WHEN** App.vue renders
- **THEN** `<a-config-provider>` SHALL have `:direction` prop bound to current direction
- **AND** all child Ant Design components SHALL render in that direction

#### Scenario: ConfigProvider locale binding
- **WHEN** locale is 'fa'
- **THEN** `<a-config-provider>` SHALL use `faIR` locale from ant-design-vue
- **WHEN** locale is 'en'
- **THEN** `<a-config-provider>` SHALL use `enUS` locale from ant-design-vue

#### Scenario: Component mirroring
- **WHEN** direction is 'rtl'
- **THEN** Ant Design components SHALL mirror their layout
- **AND** dropdowns SHALL align to right
- **AND** modals SHALL have close button on left
- **AND** tables SHALL have actions column on left

### Requirement: CSS Logical Properties
The system SHALL use CSS logical properties for directional styling.

#### Scenario: Margin inline utilities
- **WHEN** using `margin-inline-start` CSS property
- **THEN** it SHALL apply to right edge in RTL
- **AND** SHALL apply to left edge in LTR

#### Scenario: Padding inline utilities
- **WHEN** using `padding-inline-end` CSS property
- **THEN** it SHALL apply to left edge in RTL
- **AND** SHALL apply to right edge in LTR

#### Scenario: Border inline utilities
- **WHEN** using `border-inline-start` CSS property
- **THEN** it SHALL apply to right border in RTL
- **AND** SHALL apply to left border in LTR

### Requirement: RTL Utility Classes
The system SHALL provide utility CSS classes for RTL-safe styling.

#### Scenario: Text alignment classes
- **WHEN** using class `text-start`
- **THEN** text SHALL align right in RTL
- **AND** text SHALL align left in LTR

#### Scenario: Float classes
- **WHEN** using class `float-start`
- **THEN** element SHALL float right in RTL
- **AND** element SHALL float left in LTR

#### Scenario: RTL-specific transforms
- **WHEN** using class `rtl-flip`
- **THEN** element SHALL be horizontally flipped in RTL via scaleX(-1)
- **AND** SHALL remain normal in LTR

### Requirement: Language Switcher Component
The system SHALL provide a language switcher in AdminLayout header for toggling between locales.

#### Scenario: Language switcher display
- **WHEN** viewing the admin panel header
- **THEN** language switcher SHALL be visible
- **AND** SHALL display current language name ('فارسی' or 'English')
- **AND** SHALL have a globe icon

#### Scenario: Language selection
- **WHEN** clicking the language switcher
- **THEN** a dropdown menu SHALL appear
- **AND** SHALL list available languages: Persian (فارسی) and English
- **WHEN** selecting a language
- **THEN** the locale SHALL change immediately
- **AND** all UI text SHALL update to selected language
- **AND** direction SHALL switch to corresponding mode (RTL/LTR)

#### Scenario: Active language indication
- **WHEN** viewing the language dropdown
- **THEN** current language SHALL have a checkmark or highlight
- **AND** SHALL be visually distinct from other options
