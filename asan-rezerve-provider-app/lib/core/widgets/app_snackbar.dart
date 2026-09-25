import 'package:flutter/material.dart';
import '../../config/theme/app_tokens.dart';

/// Thin helper for consistent snackbars.
class AppSnackbar {
  AppSnackbar._();

  /// Clears a bottom action row (a full-width button with AppSpacing.lg padding
  /// and its safe area). A docked snackbar covered the onboarding wizard's
  /// "بعدی" button, which sits in the body rather than in
  /// Scaffold.bottomNavigationBar — the only case Flutter lifts it by itself.
  static const double _actionRowClearance = 104;

  static void _show(BuildContext context, String message, Color color) {
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(
        SnackBar(
          content: Text(message),
          backgroundColor: color,
          behavior: SnackBarBehavior.floating,
          margin: const EdgeInsets.fromLTRB(
            AppSpacing.md,
            0,
            AppSpacing.md,
            _actionRowClearance,
          ),
        ),
      );
  }

  static void success(BuildContext context, String message) =>
      _show(context, message, AppColors.success);

  static void error(BuildContext context, String message) =>
      _show(context, message, AppColors.danger);

  static void info(BuildContext context, String message) =>
      _show(context, message, AppColors.ink);
}
