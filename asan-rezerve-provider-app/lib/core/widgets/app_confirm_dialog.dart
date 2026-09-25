import 'package:flutter/material.dart';
import '../../config/theme/app_tokens.dart';
import '../constants/app_strings.dart';
import 'app_button.dart';
import 'app_dialog_header.dart';

/// Generic confirm/cancel dialog (spec: shared-ui-components — the dialog
/// overlay, first wired for logout confirmation). Flat white r16 panel and
/// barrier dim come from the app's `DialogThemeData`; this just composes
/// [AppDialogHeader] with a message and a `ButtonSize.dialog` action row.
///
/// Returns `true` if the destructive/confirm action was tapped, `false` or
/// `null` for cancel/dismiss.
Future<bool?> showAppConfirmDialog(
  BuildContext context, {
  required String title,
  required String message,
  String? confirmLabel,
  String? cancelLabel,
  bool destructive = true,
}) {
  return showDialog<bool>(
    context: context,
    barrierColor: AppColors.dialogBarrier,
    builder: (dialogContext) => AlertDialog(
      contentPadding: const EdgeInsets.fromLTRB(20, 20, 20, 20),
      content: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          AppDialogHeader(
            title: title,
            onClose: () => Navigator.of(dialogContext).pop(false),
          ),
          Text(
            message,
            textAlign: TextAlign.center,
            style: const TextStyle(color: AppColors.ink),
          ),
          const SizedBox(height: AppSpacing.lg),
          Row(
            children: [
              Expanded(
                child: AppButton.secondary(
                  size: AppButtonSize.dialog,
                  label: cancelLabel ?? AppStrings.cancel,
                  onPressed: () => Navigator.of(dialogContext).pop(false),
                ),
              ),
              const SizedBox(width: AppSpacing.sm),
              Expanded(
                child: destructive
                    ? AppButton.destructive(
                        key: const Key('confirm-dialog-confirm'),
                        size: AppButtonSize.dialog,
                        label: confirmLabel ?? AppStrings.confirm,
                        onPressed: () => Navigator.of(dialogContext).pop(true),
                      )
                    : AppButton(
                        key: const Key('confirm-dialog-confirm'),
                        size: AppButtonSize.dialog,
                        label: confirmLabel ?? AppStrings.confirm,
                        onPressed: () => Navigator.of(dialogContext).pop(true),
                      ),
              ),
            ],
          ),
        ],
      ),
    ),
  );
}
