import 'package:flutter/material.dart';
import '../../config/theme/app_colors.dart';
import '../constants/app_strings.dart';

/// Standard feedback snackbars. Every user-initiated mutation must end in
/// one of these (or a success screen) — silent success/failure is prohibited.
class AppSnackbar {
  AppSnackbar._();

  static void success(
    BuildContext context,
    String message, {
    VoidCallback? onUndo,
  }) {
    _show(
      context,
      message,
      icon: Icons.check_circle_outline,
      background: AppColors.successText,
      action: onUndo != null
          ? SnackBarAction(label: AppStrings.undo, onPressed: onUndo)
          : null,
    );
  }

  static void error(BuildContext context, String message) {
    _show(
      context,
      message,
      icon: Icons.error_outline,
      background: AppColors.errorText,
    );
  }

  static void info(BuildContext context, String message) {
    _show(context, message, icon: Icons.info_outline, background: null);
  }

  static void _show(
    BuildContext context,
    String message, {
    required IconData icon,
    Color? background,
    SnackBarAction? action,
  }) {
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(
        SnackBar(
          backgroundColor: background,
          action: action,
          content: Row(
            children: [
              Icon(icon, color: Colors.white, size: 20),
              const SizedBox(width: 8),
              Expanded(child: Text(message)),
            ],
          ),
        ),
      );
  }
}
