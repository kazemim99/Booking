import 'package:flutter/material.dart';
import '../../config/theme/app_text_styles.dart';
import '../../config/theme/app_tokens.dart';
import '../constants/app_strings.dart';
import 'app_button.dart';

/// Standard modal bottom sheet: themed shape + drag handle, safe-area aware,
/// keyboard-aware for sheets containing inputs.
class AppBottomSheet {
  AppBottomSheet._();

  static Future<T?> show<T>({
    required BuildContext context,
    required Widget child,
    String? title,
    bool isScrollControlled = false,
  }) {
    return showModalBottomSheet<T>(
      context: context,
      isScrollControlled: isScrollControlled,
      useSafeArea: true,
      builder: (context) => Padding(
        padding: EdgeInsets.only(
          left: AppSpacing.md,
          right: AppSpacing.md,
          top: AppSpacing.xs,
          bottom: AppSpacing.md + MediaQuery.of(context).viewInsets.bottom,
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            if (title != null) ...[
              Text(title, style: AppTextStyles.h3, textAlign: TextAlign.center),
              const SizedBox(height: AppSpacing.md),
            ],
            child,
          ],
        ),
      ),
    );
  }
}

/// Confirmation sheet for consequential actions. Returns true when confirmed.
/// Destructive actions get a destructive-styled confirm button.
class ConfirmSheet {
  ConfirmSheet._();

  static Future<bool> show({
    required BuildContext context,
    required String title,
    required String body,
    required String confirmLabel,
    String cancelLabel = AppStrings.back,
    bool destructive = false,
  }) async {
    final result = await AppBottomSheet.show<bool>(
      context: context,
      title: title,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(
            body,
            style: AppTextStyles.body,
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: AppSpacing.lg),
          destructive
              ? AppButton.destructive(
                  label: confirmLabel,
                  onPressed: () => Navigator.of(context).pop(true),
                )
              : AppButton(
                  label: confirmLabel,
                  onPressed: () => Navigator.of(context).pop(true),
                ),
          const SizedBox(height: AppSpacing.xs),
          AppButton.secondary(
            label: cancelLabel,
            onPressed: () => Navigator.of(context).pop(false),
          ),
        ],
      ),
    );
    return result ?? false;
  }
}
