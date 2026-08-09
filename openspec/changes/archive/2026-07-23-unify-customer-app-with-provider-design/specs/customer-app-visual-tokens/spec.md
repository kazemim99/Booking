# Spec: customer-app-visual-tokens

## ADDED Requirements

### Requirement: Brand palette aligned to the Provider app
The Customer app's color tokens SHALL resolve to the Provider app's Coliride palette: primary `#3777BF`, app-bar chrome `#3777C0`, secondary/accent success-green `#0AC075`, success `#0AC075`, warning `#FFCB33`, error `#FF6171` (for toasts, badges, and buttons), the darker `#E74A3B` for input-error borders only, resting border `#EBEEF3`, divider `#E5E9F2`, and a white scaffold background. The token class and field names (`AppColors.*`) MUST remain unchanged.

#### Scenario: Primary surfaces use the aligned blue
- **WHEN** any screen renders a primary action or primary-colored surface via the theme
- **THEN** the resolved color is `#3777BF`, not the previous `#1A365D`

#### Scenario: Semantic colors match the Provider values
- **WHEN** a success, warning, or error affordance renders through the theme
- **THEN** it resolves to `#0AC075`, `#FFCB33`, and `#FF6171` respectively (with `#E74A3B` used only for input-error borders)

#### Scenario: Token names are preserved
- **WHEN** the palette is re-valued
- **THEN** no `AppColors` field or class is renamed, moved, or removed — only its value changes

### Requirement: Corner-radius scale aligned to component-specific radii
Themed components SHALL use the Provider's component radii: button 10, field 12, card 15, bottom sheet 14 (top), dialog/panel 16, snackbar 12.

#### Scenario: Each component honors its target radius
- **WHEN** a button, input, card, bottom sheet, dialog, or snackbar renders through the theme
- **THEN** its corner radius equals the aligned target for that component type

### Requirement: Flat borders-over-shadows elevation policy
The theme SHALL render surfaces flat (`elevation: 0`) and express separation with borders and dividers rather than Material shadows. The `AppElevation` class MUST be retained (not renamed or removed), but the theme MUST NOT apply drop shadows to cards, dialogs, app bars, the bottom navigation, or bottom sheets.

#### Scenario: Cards and dialogs render without shadow
- **WHEN** a card or dialog renders through the theme
- **THEN** its elevation is 0 and it is separated by a border, not a shadow

#### Scenario: Elevation class is preserved
- **WHEN** the flat policy is applied
- **THEN** the `AppElevation` class still exists; only its use in the theme changes

### Requirement: Motion tokens aligned to the Provider timing
Animated affordances SHALL use a fast duration of 180ms and the `easeOutCubic` emphasis curve, and MUST continue to honor `MediaQuery.disableAnimations`.

#### Scenario: Fast animations use the aligned timing
- **WHEN** a component runs its fast transition
- **THEN** the duration is 180ms with an `easeOutCubic` curve

#### Scenario: Reduced motion is respected
- **WHEN** the OS reduce-motion setting is enabled
- **THEN** animated components skip or shorten their motion accordingly

### Requirement: Icon-size ramp
The Customer app SHALL adopt a defined icon-size ramp equivalent to the Provider's (small 16, medium 24, action 20, hero 72) and use it for inline, functional, action, and empty/feedback iconography.

#### Scenario: Feedback state uses the hero icon size
- **WHEN** an empty or error state renders its illustration/icon
- **THEN** the icon is sized at the hero ramp value

### Requirement: Typography aligned with contrast guarantee
Typography SHALL keep the shared `Vazir` family and align sizes/weights to the Provider scale, adopting the navy-ink text color `#4D5E80` for headings and body **only where the pairing meets WCAG AA (≥4.5:1 on its background)**. Where a pairing fails AA, the accessible tone MUST be kept instead of the exact Provider hex.

#### Scenario: Body text adopts navy ink when it passes AA
- **WHEN** body text renders on white and `#4D5E80` yields ≥4.5:1
- **THEN** the text uses `#4D5E80`

#### Scenario: Accessibility wins on a failing pairing
- **WHEN** a text/background pairing with the navy ink falls below 4.5:1
- **THEN** the accessible tone is retained and the exception is recorded
