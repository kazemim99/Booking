import 'package:flutter/material.dart';
import '../../config/theme/app_tokens.dart';

/// Size ramp from the Coliride button system (DESIGN_LANGUAGE.md §5.1).
enum AppButtonSize {
  /// 30dp / 14 bold — section-header Edit, inline actions.
  small,

  /// 40dp / 15.5 bold — dialog CTAs.
  dialog,

  /// 46dp / 16 bold — in-card actions.
  medium,

  /// 46dp / 17 bold — screen-level CTAs (default).
  big,
}

enum _AppButtonRole { primary, secondary, destructive, text }

/// Buttons in the four Coliride roles: primary (filled blue), secondary
/// (white with 2px blue outline), destructive (filled coral), and text.
/// All sizes are full-width by default via the theme's `Size.fromHeight` —
/// wrap in [Expanded] when placing inside a Row.
class AppButton extends StatelessWidget {
  final String label;
  final VoidCallback? onPressed;
  final bool loading;
  final AppButtonSize size;
  final _AppButtonRole _role;

  const AppButton({
    super.key,
    required this.label,
    this.onPressed,
    this.loading = false,
    this.size = AppButtonSize.big,
  }) : _role = _AppButtonRole.primary;

  const AppButton.secondary({
    super.key,
    required this.label,
    this.onPressed,
    this.loading = false,
    this.size = AppButtonSize.big,
  }) : _role = _AppButtonRole.secondary;

  const AppButton.destructive({
    super.key,
    required this.label,
    this.onPressed,
    this.loading = false,
    this.size = AppButtonSize.big,
  }) : _role = _AppButtonRole.destructive;

  const AppButton.text({
    super.key,
    required this.label,
    this.onPressed,
    this.loading = false,
    this.size = AppButtonSize.big,
  }) : _role = _AppButtonRole.text;

  double get _height => switch (size) {
        AppButtonSize.small => AppDimens.buttonSmallHeight,
        AppButtonSize.dialog => AppDimens.buttonDialogHeight,
        AppButtonSize.medium || AppButtonSize.big => AppDimens.buttonHeight,
      };

  double get _fontSize => switch (size) {
        AppButtonSize.small => AppDimens.buttonSmallFontSize,
        AppButtonSize.dialog => AppDimens.buttonDialogFontSize,
        AppButtonSize.medium => AppDimens.buttonMediumFontSize,
        AppButtonSize.big => AppDimens.buttonFontSize,
      };

  EdgeInsets get _padding => switch (size) {
        AppButtonSize.small => const EdgeInsets.symmetric(horizontal: 14),
        AppButtonSize.dialog => const EdgeInsets.symmetric(horizontal: 18),
        AppButtonSize.medium ||
        AppButtonSize.big =>
          const EdgeInsets.symmetric(horizontal: 16),
      };

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    // Spinner must contrast with the button surface: white on filled
    // (primary/destructive) backgrounds, brand blue otherwise.
    final spinnerColor = switch (_role) {
      _AppButtonRole.primary || _AppButtonRole.destructive => scheme.onPrimary,
      _AppButtonRole.secondary || _AppButtonRole.text => scheme.primary,
    };
    final child = loading
        ? SizedBox(
            height: 20,
            width: 20,
            child: CircularProgressIndicator(
              strokeWidth: 2,
              color: spinnerColor,
            ),
          )
        : Text(label);
    final effectiveOnPressed = loading ? null : onPressed;
    // Per-size overrides on top of the themed role styles.
    final sizeStyle = ButtonStyle(
      minimumSize: WidgetStatePropertyAll(Size.fromHeight(_height)),
      padding: WidgetStatePropertyAll(_padding),
      textStyle: WidgetStatePropertyAll(
        TextStyle(
          fontSize: _fontSize,
          fontWeight: FontWeight.bold,
          fontFamily: 'Vazir',
        ),
      ),
    );

    return switch (_role) {
      _AppButtonRole.primary => FilledButton(
          onPressed: effectiveOnPressed,
          style: sizeStyle,
          child: child,
        ),
      _AppButtonRole.destructive => FilledButton(
          onPressed: effectiveOnPressed,
          style: sizeStyle.copyWith(
            backgroundColor: WidgetStateProperty.resolveWith(
              (states) => states.contains(WidgetState.disabled)
                  ? AppColors.disabled
                  : AppColors.danger,
            ),
          ),
          child: child,
        ),
      _AppButtonRole.secondary => OutlinedButton(
          onPressed: effectiveOnPressed,
          style: sizeStyle,
          child: child,
        ),
      _AppButtonRole.text => TextButton(
          onPressed: effectiveOnPressed,
          style: sizeStyle,
          child: child,
        ),
    };
  }
}
