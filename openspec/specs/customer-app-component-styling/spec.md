# customer-app-component-styling Specification

## Purpose
TBD - created by archiving change unify-customer-app-with-provider-design. Update Purpose after archive.
## Requirements
### Requirement: Buttons match Provider metrics with names and APIs unchanged
The Customer app's existing button components SHALL render with the Provider's visual metrics — primary: blue fill, height 46, radius 10, bold 17 `Vazir`, elevation 0; secondary: white fill with a 2px primary outline and primary label; text button: ink label — while keeping an effective touch target of ≥48dp. The component file names, class names, and public constructors/APIs MUST NOT change.

#### Scenario: Primary button renders the aligned metrics
- **WHEN** a primary button renders
- **THEN** it is blue-filled, flat (elevation 0), with radius 10 and bold 17 label text

#### Scenario: Touch target floor is preserved
- **WHEN** a button renders at the 46px visual height
- **THEN** its effective tap target is still at least 48dp

#### Scenario: Button API is unchanged
- **WHEN** the styling is applied
- **THEN** the button's file, class, and constructor signature are identical to before

### Requirement: Text inputs are non-filled and bordered
Text-input components SHALL render non-filled with a resting `#EBEEF3` border (~1.9px), a focused border (~2.1px), and the `#E74A3B` error border, at field radius 12.

#### Scenario: Resting and focused input borders
- **WHEN** a text field is at rest, then focused
- **THEN** it shows a non-filled ~1.9px `#EBEEF3` border, becoming a ~2.1px focused border, with no fill color

#### Scenario: Error state uses the input-error red
- **WHEN** a field is in error
- **THEN** its border uses `#E74A3B`

### Requirement: Cards are flat and bordered
Card components SHALL render at elevation 0 with a `#EBEEF3` border and radius 15, with no drop shadow.

#### Scenario: Card renders flat
- **WHEN** a card renders
- **THEN** it has elevation 0, a `#EBEEF3` border, and radius 15

### Requirement: Overlays styled to the Provider look
Bottom sheets SHALL use a 14px top radius and dialogs a flat 16px radius; snackbars SHALL be floating with radius 12 and continue to support success/error variants and an optional undo action.

#### Scenario: Bottom sheet and dialog radii
- **WHEN** a bottom sheet or dialog opens
- **THEN** the sheet has a 14px top radius and the dialog a flat 16px radius

#### Scenario: Snackbar variants preserved
- **WHEN** a success or error snackbar with an undo action is shown
- **THEN** it renders floating at radius 12 with its variant color and a working undo action

### Requirement: Status badges convey status by label plus color
Status-badge components SHALL present each status with both a text label and a color (never color alone), using the aligned palette.

#### Scenario: Badge is not color-only
- **WHEN** a booking-status badge renders
- **THEN** it shows a text label alongside its color

### Requirement: Feedback-state views styled consistently
The empty, error, loading/skeleton, and state-switcher components SHALL render in the aligned visual language — hero-sized iconography, aligned typography, borders over shadows — and the error state MUST keep its retry affordance.

#### Scenario: Error state offers retry in the aligned style
- **WHEN** an error state renders
- **THEN** it shows an aligned-style message with a working retry action

#### Scenario: Loading skeleton matches the aligned surfaces
- **WHEN** a skeleton loader renders
- **THEN** its shapes use the aligned radii and flat surfaces

