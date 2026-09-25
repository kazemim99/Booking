# Persian Fonts Typography Capability

## ADDED Requirements

### Requirement: Vazir Font Family
The system SHALL include Vazir font as the primary Persian font with multiple weights.

#### Scenario: Vazir font weights available
- **WHEN** Persian locale is active
- **THEN** the following Vazir font weights SHALL be available:
  - Vazir Thin (weight: 100)
  - Vazir Light (weight: 300)
  - Vazir Regular (weight: 400)
  - Vazir Medium (weight: 500)
  - Vazir Bold (weight: 700)

#### Scenario: Font file formats
- **WHEN** loading Vazir fonts
- **THEN** fonts SHALL be provided in WOFF2 format for optimal compression
- **AND** SHALL support all modern browsers (Chrome, Firefox, Edge, Safari)

#### Scenario: Font files location
- **WHEN** building the application
- **THEN** Vazir font files SHALL be located in `src/assets/fonts/vazir/` directory
- **AND** SHALL be named: `Vazir-Thin.woff2`, `Vazir-Light.woff2`, `Vazir.woff2`, `Vazir-Medium.woff2`, `Vazir-Bold.woff2`

### Requirement: B Nazanin Fallback Font
The system SHALL include B Nazanin font as a fallback Persian font for broader compatibility.

#### Scenario: B Nazanin font weights
- **WHEN** Vazir font fails to load or is unavailable
- **THEN** B Nazanin font SHALL be used as fallback
- **AND** SHALL provide Regular (400) and Bold (700) weights

#### Scenario: B Nazanin font files
- **WHEN** loading B Nazanin fonts
- **THEN** font files SHALL be located in `src/assets/fonts/bnazanin/` directory
- **AND** SHALL be named: `BNazanin.woff2`, `BNazanin-Bold.woff2`

### Requirement: Font Face Declarations
The system SHALL declare all Persian fonts using CSS @font-face rules in `src/assets/styles/fonts.css`.

#### Scenario: Vazir font face declarations
- **WHEN** fonts.css is loaded
- **THEN** it SHALL contain @font-face rules for all Vazir weights
- **AND** each SHALL specify font-family: 'Vazir'
- **AND** SHALL use appropriate font-weight values (100, 300, 400, 500, 700)
- **AND** SHALL use font-display: swap for optimal loading

#### Scenario: B Nazanin font face declarations
- **WHEN** fonts.css is loaded
- **THEN** it SHALL contain @font-face rules for B Nazanin Regular and Bold
- **AND** each SHALL specify font-family: 'B Nazanin'
- **AND** SHALL use font-weight values (400, 700)

### Requirement: Persian Font Application
The system SHALL apply Persian fonts only when Persian locale is active using CSS attribute selectors.

#### Scenario: Font application via lang attribute
- **WHEN** `html[lang="fa"]` is present
- **THEN** all elements SHALL use font-family: 'Vazir', 'B Nazanin', Tahoma, 'Iranian Sans', sans-serif
- **AND** SHALL apply with !important to override component styles

#### Scenario: Font not applied in English
- **WHEN** `html[lang="en"]` is present
- **THEN** Persian fonts SHALL NOT be applied
- **AND** system SHALL use default sans-serif fonts

#### Scenario: Font fallback chain
- **WHEN** Vazir font is unavailable
- **THEN** system SHALL fallback to B Nazanin
- **AND** if B Nazanin is unavailable, SHALL fallback to Tahoma
- **AND** if Tahoma is unavailable, SHALL fallback to 'Iranian Sans'
- **AND** finally SHALL fallback to system sans-serif

### Requirement: Font Performance Optimization
The system SHALL optimize font loading for performance and user experience.

#### Scenario: Font display strategy
- **WHEN** loading fonts
- **THEN** SHALL use `font-display: swap` strategy
- **AND** SHALL show system fallback font immediately
- **AND** SHALL swap to Persian font when loaded

#### Scenario: Font subsetting consideration
- **WHEN** building for production
- **THEN** fonts SHOULD include only Persian and English glyphs
- **AND** SHOULD exclude unnecessary Unicode ranges for smaller file size

#### Scenario: Font preloading
- **WHEN** index.html loads
- **THEN** critical font files (Vazir Regular 400) SHOULD be preloaded
- **AND** SHALL use `<link rel="preload" as="font">` for faster rendering

### Requirement: Typography Consistency
The system SHALL maintain consistent typography across Persian and English text.

#### Scenario: Line height adjustment
- **WHEN** Persian text is rendered
- **THEN** line-height SHALL be at least 1.8 for readability
- **AND** SHALL accommodate Persian diacritics and tall characters

#### Scenario: Font size consistency
- **WHEN** switching between locales
- **THEN** font sizes SHALL remain the same across Persian and English
- **AND** Persian text SHALL be equally readable at same size

#### Scenario: Font weight mapping
- **WHEN** applying bold text in Persian
- **THEN** SHALL use Vazir Bold (700) or B Nazanin Bold (700)
- **AND** SHALL maintain visual hierarchy consistency with English

### Requirement: Fonts CSS Import
The system SHALL import fonts.css in main application styles.

#### Scenario: Import in main.css
- **WHEN** application styles load
- **THEN** `src/assets/styles/main.css` SHALL import fonts.css
- **AND** import SHALL occur before other style declarations

#### Scenario: Font availability in components
- **WHEN** any component renders
- **THEN** Persian fonts SHALL be available globally
- **AND** SHALL not require component-level font imports
