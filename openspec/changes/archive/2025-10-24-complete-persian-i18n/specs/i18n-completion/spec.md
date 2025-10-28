# Persian i18n Completion for Provider Dashboard

## ADDED Requirements

### REQ-I18N-001: Complete English Translation Keys (en.json)

**Priority**: HIGH

The English translation file must include all keys currently used in the provider dashboard components.

#### Scenario: Developer loads dashboard and all English text displays correctly
- **WHEN** the dashboard is set to English language and I navigate to the provider dashboard
- **THEN** all text elements display proper English translations
- **AND** no "missing translation" warnings appear in console
- **AND** all `dashboard.welcome` keys are present (completeProfile, completeProfileMessage, completeNow)
- **AND** all `dashboard.actions` keys are present (addService, addServiceDescription, viewCalendar, viewCalendarDescription, editHours, editHoursDescription, viewProfile, viewProfileDescription)
- **AND** all `dashboard.bookings` keys are present (noBookings, noBookingsMessage)
- **AND** all `dashboard.charts` keys are present (barChart, lineChart, totalBookings, completed, cancelled, completionRate, total, average, trend)
- **AND** all `footer` keys are present (help, support, terms, privacy)
- **AND** all `common` keys are present (retry, viewAll)

#### Scenario: en.json file is valid JSON
- **WHEN** I parse the en.json file with a JSON validator
- **THEN** the file parses without errors
- **AND** all keys follow camelCase naming conventions
- **AND** no values contain placeholder text like "TODO" or "FIXME"

---

### REQ-I18N-002: Complete Persian Translation Keys (fa.json)

**Priority**: HIGH

The Persian translation file must include all keys from en.json with accurate, culturally appropriate Persian translations.

#### Scenario: Developer loads dashboard and all Persian text displays correctly
- **WHEN** the dashboard is set to Persian language and I navigate to the provider dashboard
- **THEN** all text elements display proper Persian translations
- **AND** all translations use formal Persian appropriate for business context
- **AND** translations avoid transliteration and use proper Persian words
- **AND** text renders correctly in RTL direction
- **AND** no "missing translation" warnings appear in console

#### Scenario: fa.json has complete parity with en.json
- **WHEN** the en.json file has N translation keys and I compare with fa.json
- **THEN** fa.json has exactly N corresponding keys
- **AND** the JSON structure matches en.json
- **AND** the file uses UTF-8 encoding
- **AND** the file parses without errors

#### Scenario: Persian translations follow quality guidelines
- **WHEN** a Persian translation in fa.json is reviewed
- **THEN** it uses formal register (e.g., "تکمیل کنید" not "کن")
- **AND** it prefers Persian roots over Arabic loanwords where natural
- **AND** it is concise (similar length to English)
- **AND** it uses standard Persian numerals in text

---

## MODIFIED Requirements

### REQ-I18N-003: Set Persian as Default Language

**Priority**: HIGH

The i18n configuration must set Persian (fa) as the default locale, with English (en) as fallback.

**Affected Files**:
- `booksy-frontend/src/i18n/index.ts` (or equivalent i18n config file)

#### Scenario: Fresh browser session loads Persian by default
- **WHEN** I have no language preference stored in browser, I clear browser cache and localStorage, and I load the provider dashboard for the first time
- **THEN** all text displays in Persian
- **AND** the locale is set to 'fa'
- **AND** no console warnings about missing locale appear

#### Scenario: Language fallback works correctly
- **WHEN** the dashboard is set to Persian and a translation key is missing from fa.json
- **THEN** the system falls back to the English translation
- **AND** no error is thrown
- **AND** a console warning indicates the missing key

#### Scenario: Language switcher remains functional
- **WHEN** the dashboard is displaying in Persian and I use the language switcher to change to English
- **THEN** all text updates to English
- **AND** when I switch back to Persian, all text returns to Persian
- **AND** my language preference persists across page reloads

**Implementation Example**:
```typescript
const i18n = createI18n({
  legacy: false,
  locale: 'fa', // Changed from 'en'
  fallbackLocale: 'en',
  messages: {
    fa: faMessages,
    en: enMessages,
    ar: arMessages
  }
})
```

---

### REQ-I18N-004: Validation and Testing

**Priority**: MEDIUM

All translations must be validated and tested across the provider dashboard.

#### Scenario: All dashboard pages display correctly in Persian
- **WHEN** the dashboard is set to Persian and I navigate through all dashboard sections
- **THEN** dashboard home page displays Persian text
- **AND** charts display Persian labels
- **AND** recent bookings list shows Persian text
- **AND** quick actions have Persian descriptions
- **AND** footer shows Persian link text
- **AND** header and navigation display Persian
- **AND** user menu dropdown shows Persian items

#### Scenario: No missing translation warnings in console
- **WHEN** the dashboard is set to Persian, I navigate through all dashboard pages, and I check the browser console
- **THEN** I see zero "missing translation" warnings
- **AND** I see zero i18n-related errors

#### Scenario: RTL layout works with Persian text
- **WHEN** the dashboard is displaying Persian text and I review the layout
- **THEN** text alignment is right-to-left
- **AND** UI elements flow from right to left
- **AND** no text overflow occurs
- **AND** no text truncation occurs
- **AND** spacing and margins are correct

#### Scenario: Responsive layouts work with Persian
- **WHEN** the dashboard is displaying Persian text and I test on mobile viewport (375px)
- **THEN** layout is correct and readable
- **AND** when I test on tablet viewport (768px), layout is correct and readable
- **AND** when I test on desktop viewport (1440px), layout is correct and readable

---

### REQ-I18N-005: Documentation

**Priority**: LOW

Translation conventions and maintenance procedures must be documented.

#### Scenario: Developer can add new translation key following guide
- **WHEN** I am a developer who needs to add a new translation and I follow the translation guide documentation
- **THEN** I successfully add the key to en.json
- **AND** I successfully add the Persian translation to fa.json
- **AND** I understand the naming conventions
- **AND** I understand the Persian style guidelines
- **AND** the new translation displays correctly in the dashboard

#### Scenario: Documentation covers all necessary topics
- **WHEN** the translation documentation exists and I review the documentation
- **THEN** it includes translation key naming conventions
- **AND** it includes a guide for adding new translations
- **AND** it includes Persian translation guidelines
- **AND** it includes RTL considerations
- **AND** it includes testing procedures

---

## Dependencies

- Existing i18n infrastructure (Vue I18n)
- Current en.json structure
- Current fa.json structure
- Provider dashboard components using `$t()` helper

## Risks and Mitigations

**Risk**: Inaccurate or unclear Persian translations
**Mitigation**: Use formal Persian conventions; request native speaker review if needed

**Risk**: Breaking language switcher functionality
**Mitigation**: Test bidirectional language switching thoroughly

**Risk**: RTL layout issues with new Persian text
**Mitigation**: Test with various text lengths; use CSS logical properties

**Risk**: Missing translation keys discovered after deployment
**Mitigation**: Comprehensive grep of all `$t()` usage before implementation

## Success Metrics

- 0 missing translation warnings in console
- 100% of dashboard components display Persian by default
- Language switcher success rate: 100%
- No RTL layout regressions
- User feedback on Persian quality: positive

## Implementation Notes

### Phase 1: Preparation
- Grep all `$t()` usage to identify required keys
- Compare en.json and fa.json to find gaps
- Create master list of missing keys

### Phase 2: English Completion
- Add all missing keys to en.json
- Use clear, concise English
- Follow existing naming patterns

### Phase 3: Persian Translation
- Translate all keys to Persian
- Apply formal tone consistently
- Ensure RTL compatibility

### Phase 4: Configuration
- Update i18n config to set Persian default
- Verify fallback behavior
- Test fresh loads

### Phase 5: Validation
- Manual testing on all dashboard pages
- Console log review
- Responsive testing
- Language switch testing

### Phase 6: Documentation
- Write translation guide
- Document conventions
- Update README

## Related Specs

None (initial spec for this change)

## References

- Vue I18n documentation: https://vue-i18n.intlify.dev/
- Persian language style guides
- RTL best practices for web
