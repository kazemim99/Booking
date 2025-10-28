# Complete Persian Internationalization (i18n)

## Why

The Booksy platform serves a Persian-speaking user base as its primary market. While the provider dashboard has been rebuilt with full RTL support, it currently defaults to English and is missing critical Persian translations. This creates a poor user experience for our primary audience and undermines the value of the RTL work already completed.

**Business Impact**:
- Persian-speaking providers are the majority user base
- English-only interface creates friction and confusion
- Incomplete translations appear unprofessional
- RTL investment is wasted if content remains in English

**User Impact**:
- Providers must navigate in a language they may not understand well
- Mixed Persian/English text is confusing and inconsistent
- Key actions and chart labels are unclear
- Professional credibility is diminished

**Technical Readiness**:
- Dashboard infrastructure already supports RTL
- i18n system is in place and working
- Only missing translation content and default language setting
- Low effort, high impact change

## Overview
Complete the Persian (Farsi) translation file and make Persian the primary language for the Booksy provider dashboard. Currently, the dashboard is RTL-enabled but displays English text because the Persian translation file (fa.json) is missing many required keys.

## Problem Statement
The provider dashboard has been rebuilt with full RTL support, but:
1. **fa.json is incomplete** - Missing ~70% of translation keys that exist in en.json
2. **en.json is incomplete** - Missing dashboard-specific keys (charts, actions, footer, welcome messages)
3. **Persian is not the primary language** - System defaults to English despite RTL layout
4. **Inconsistent user experience** - Persian users see a mix of Persian and English text

## Scope
1. Add all missing translation keys to fa.json (dashboard, charts, footer, actions, bookings)
2. Add missing dashboard keys to en.json for consistency
3. Set Persian (fa) as the default/primary language
4. Ensure all newly built dashboard components have proper translations

## Out of Scope
- Translating admin panel or other modules beyond provider dashboard
- Adding new languages beyond Persian and English
- Modifying existing translation structure or i18n configuration
- Business Profile page implementation (separate proposal needed)

## Success Criteria
- [ ] All dashboard components display in Persian by default
- [ ] No missing translation warnings in browser console
- [ ] Language can be switched between Persian and English seamlessly
- [ ] All chart labels, footer links, and dashboard actions are translated
- [ ] RTL layout works correctly with Persian text

## Dependencies
- Completed provider dashboard rebuild (already done)
- Vue i18n configuration (already in place)

## What Changes

This change completes the Persian i18n implementation for the provider dashboard:

1. **Translation Files**:
   - Add ~50 missing translation keys to `booksy-frontend/src/locales/fa.json`
   - Add ~30 missing dashboard keys to `booksy-frontend/src/locales/en.json`
   - Include dashboard.welcome, dashboard.actions, dashboard.bookings, dashboard.charts, footer, and common sections

2. **i18n Configuration**:
   - Update default locale from 'en' to 'fa' in i18n config file
   - Maintain English as fallback locale
   - Ensure language switcher continues to work

3. **Specification Changes**:
   - Add REQ-I18N-001: Complete English Translation Keys
   - Add REQ-I18N-002: Complete Persian Translation Keys
   - Modify REQ-I18N-003: Set Persian as Default Language
   - Add REQ-I18N-004: Validation and Testing requirements
   - Add REQ-I18N-005: Documentation requirements

## Technical Notes
- Translation keys identified from component usage via `$t()` function
- Approximately 50+ new keys needed in fa.json
- Approximately 30+ dashboard keys needed in en.json
- Primary language setting likely in main.ts or i18n config file
