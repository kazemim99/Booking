import 'package:flutter/material.dart';
import '../../config/theme/app_colors.dart';
import '../../config/theme/app_tokens.dart';

enum AppButtonVariant { primary, secondary, text, destructive }

/// Standard app button. Always ≥48dp tall, shows a spinner and blocks taps
/// while [loading] is true without changing its size.
class AppButton extends StatelessWidget {
  final String label;
  final VoidCallback? onPressed;
  final AppButtonVariant variant;
  final bool loading;
  final bool expanded;
  final IconData? icon;

  const AppButton({
    super.key,
    required this.label,
    required this.onPressed,
    this.variant = AppButtonVariant.primary,
    this.loading = false,
    this.expanded = true,
    this.icon,
  });

  const AppButton.secondary({
    super.key,
    required this.label,
    required this.onPressed,
    this.loading = false,
    this.expanded = true,
    this.icon,
  }) : variant = AppButtonVariant.secondary;

  const AppButton.text({
    super.key,
    required this.label,
    required this.onPressed,
    this.loading = false,
    this.expanded = false,
    this.icon,
  }) : variant = AppButtonVariant.text;

  const AppButton.destructive({
    super.key,
    required this.label,
    required this.onPressed,
    this.loading = false,
    this.expanded = true,
    this.icon,
  }) : variant = AppButtonVariant.destructive;

  @override
  Widget build(BuildContext context) {
    final effectiveOnPressed = loading ? null : onPressed;

    final child = loading
        ? SizedBox(
            height: 20,
            width: 20,
            child: CircularProgressIndicator(
              strokeWidth: 2,
              color: variant == AppButtonVariant.secondary ||
                      variant == AppButtonVariant.text
                  ? AppColors.primary
                  : Colors.white,
            ),
          )
        : icon != null
            ? Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Icon(icon, size: 20),
                  const SizedBox(width: AppSpacing.xs),
                  Text(label),
                ],
              )
            : Text(label);

    Widget button;
    switch (variant) {
      case AppButtonVariant.primary:
        button = ElevatedButton(onPressed: effectiveOnPressed, child: child);
      case AppButtonVariant.secondary:
        button = OutlinedButton(onPressed: effectiveOnPressed, child: child);
      case AppButtonVariant.text:
        button = TextButton(onPressed: effectiveOnPressed, child: child);
      case AppButtonVariant.destructive:
        button = ElevatedButton(
          onPressed: effectiveOnPressed,
          // The AA red from the theme (white on it is 6.47:1); the coral AppColors.error
          // (2.92:1) is for badges and fills, never a button.
          style: ElevatedButton.styleFrom(
            backgroundColor: Theme.of(context).colorScheme.error,
            foregroundColor: Theme.of(context).colorScheme.onError,
          ),
          child: child,
        );
    }

    button = Semantics(
      button: true,
      enabled: effectiveOnPressed != null,
      label: label,
      child: button,
    );

    if (expanded) {
      return SizedBox(width: double.infinity, child: button);
    }
    return button;
  }
}
