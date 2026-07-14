import 'package:flutter/material.dart';
import '../../config/theme/app_tokens.dart';

/// Thin helper for consistent snackbars.
class AppSnackbar {
  AppSnackbar._();

  static void _show(BuildContext context, String message, Color color) {
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(
        SnackBar(content: Text(message), backgroundColor: color),
      );
  }

  static void success(BuildContext context, String message) =>
      _show(context, message, AppColors.primaryDark);

  static void error(BuildContext context, String message) =>
      _show(context, message, AppColors.danger);

  static void info(BuildContext context, String message) =>
      _show(context, message, Colors.black87);
}
