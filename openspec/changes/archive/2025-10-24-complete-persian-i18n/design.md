# Design: Persian Internationalization

## Context
The provider dashboard has been completely rebuilt with modern components and full RTL support. However, the translation infrastructure is incomplete, resulting in English text appearing in a Persian/RTL interface.

## Technical Decisions

### 1. Translation File Structure
**Decision**: Maintain existing nested JSON structure
**Rationale**:
- Existing pattern works well: `dashboard.charts.total`, `footer.help`
- Easy to organize by feature area
- Prevents key conflicts
- Makes maintenance easier

### 2. Primary Language Configuration
**Decision**: Set Persian as default in i18n initialization
**Rationale**:
- Target audience is primarily Persian-speaking users
- Dashboard was designed with RTL-first approach
- Reduces cognitive load for primary user base
- English remains available via language switcher

**Implementation**:
```javascript
// In i18n config or main.ts
const i18n = createI18n({
  legacy: false,
  locale: 'fa', // Changed from 'en' to 'fa'
  fallbackLocale: 'en',
  messages: { fa, en, ar }
})
```

### 3. Missing Key Handling
**Decision**: Use English fallback for missing keys
**Rationale**:
- Prevents blank UI elements
- Makes missing translations obvious during development
- English is secondary language for users

### 4. Translation Categories

#### Dashboard Section (High Priority)
Required keys:
```
dashboard.welcome.completeProfile
dashboard.welcome.completeProfileMessage
dashboard.welcome.completeNow
dashboard.actions.addService
dashboard.actions.addServiceDescription
dashboard.actions.viewCalendar
dashboard.actions.viewCalendarDescription
dashboard.actions.editHours
dashboard.actions.editHoursDescription
dashboard.actions.viewProfile
dashboard.actions.viewProfileDescription
dashboard.bookings.noBookings
dashboard.bookings.noBookingsMessage
dashboard.charts.barChart
dashboard.charts.lineChart
dashboard.charts.totalBookings
dashboard.charts.completed
dashboard.charts.cancelled
dashboard.charts.completionRate
dashboard.charts.total
dashboard.charts.average
dashboard.charts.trend
```

#### Footer Section (Medium Priority)
```
footer.help
footer.support
footer.terms
footer.privacy
```

#### Common Section Additions (Low Priority)
```
common.retry
common.viewAll
```

## Translation Guidelines

### Persian Translation Best Practices
1. **Use formal Persian** - Professional tone for business users
2. **Avoid transliteration** - Use proper Persian equivalents
3. **Keep RTL-aware** - Consider text direction in longer phrases
4. **Be concise** - Match English brevity where culturally appropriate
5. **Use Persian numerals** - ۱، ۲، ۳ for Persian locale (optional enhancement)

### Examples
```json
// Good
"dashboard.actions.addService": "افزودن خدمت"

// Avoid (transliteration)
"dashboard.actions.addService": "اضافه کردن سرویس"

// Good (concise)
"dashboard.charts.total": "مجموع"

// Avoid (too verbose)
"dashboard.charts.total": "مجموع کل درآمدها"
```

## Validation Strategy

### 1. Completeness Check
```bash
# Compare keys between en.json and fa.json
# Identify missing keys
# Generate report
```

### 2. Runtime Validation
- Enable i18n missing key warnings in development
- Use browser console to identify untranslated strings
- Test all dashboard routes with Persian locale

### 3. Visual QA
- [ ] Check all dashboard cards
- [ ] Verify chart labels
- [ ] Test footer links
- [ ] Validate action descriptions
- [ ] Confirm welcome messages

## Rollout Plan

### Phase 1: Translation Files (Week 1)
1. Add all missing keys to en.json
2. Translate all keys to Persian in fa.json
3. Validate JSON syntax
4. Test with language switcher

### Phase 2: Primary Language (Week 1)
1. Update i18n configuration
2. Set Persian as default
3. Ensure fallback works
4. Test fresh browser sessions

### Phase 3: Testing & Refinement (Week 1)
1. Full dashboard walkthrough in Persian
2. Test language switching
3. Verify RTL layout with translated text
4. Get user feedback on translations
5. Refine based on feedback

## Future Enhancements
- Add Arabic translations (ar.json already referenced)
- Implement date/number localization
- Add currency formatting per locale
- Create translation management workflow
- Add translation coverage reports
