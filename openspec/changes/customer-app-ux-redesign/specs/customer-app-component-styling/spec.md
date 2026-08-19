# customer-app-component-styling

## ADDED Requirements

### Requirement: Shared component library is the only styling surface
The app SHALL provide a component library in `core/widgets` covering at minimum `AppButton` (primary, secondary, text, destructive variants with a loading state), `AppTextField`, `OtpInput`, `AppCard`, `StatusBadge`, `SkeletonLoader`, `EmptyState`, `ErrorState` with retry, `OfflineBanner`, `AppBottomSheet`, `ConfirmSheet`, and `AppSnackbar` (success/error variants, optional undo). Feature screens MUST compose these components rather than styling Material widgets locally.

#### Scenario: Feature screen composes rather than styles
- **WHEN** a feature screen needs a button, card, input, badge, sheet, or feedback state
- **THEN** it composes the corresponding `core/widgets` component instead of styling a Material widget inline

#### Scenario: Button exposes loading state
- **WHEN** an `AppButton` is placed in loading state during an async action
- **THEN** it shows a progress indicator, disables further taps, and retains its layout size

#### Scenario: Destructive action uses confirmation
- **WHEN** a screen offers a destructive action (e.g. cancel booking, logout)
- **THEN** it presents `ConfirmSheet` stating the consequence before executing, and the confirming button uses the destructive variant

### Requirement: Component accessibility baseline
Every shared component SHALL provide: a touch target of at least 48×48dp, text contrast meeting WCAG AA against its background, a Persian `Semantics` label for non-text controls, correct rendering at OS text scale up to 1.3 without clipping or overflow, and animations that respect `MediaQuery.disableAnimations`.

#### Scenario: Font scaling does not break layout
- **WHEN** the OS font scale is set to 1.3
- **THEN** all shared components render without text clipping, overflow errors, or overlapping controls

#### Scenario: Reduced motion honored
- **WHEN** the OS reduce-motion/disable-animations setting is on
- **THEN** shared components skip or minimize their transition animations
