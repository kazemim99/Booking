import 'package:flutter/material.dart';
import 'package:pinput/pinput.dart';
import '../../config/theme/app_colors.dart';
import '../../config/theme/app_text_styles.dart';
import '../../config/theme/app_tokens.dart';
import '../constants/app_strings.dart';

/// Segmented 6-digit OTP input with paste support and platform autofill
/// (iOS oneTimeCode keyboard hint, Android SMS autofill via the platform).
class OtpInput extends StatelessWidget {
  final TextEditingController? controller;
  final FocusNode? focusNode;
  final int length;
  final bool enabled;
  final String? errorText;
  final ValueChanged<String>? onCompleted;
  final ValueChanged<String>? onChanged;

  const OtpInput({
    super.key,
    this.controller,
    this.focusNode,
    this.length = 6,
    this.enabled = true,
    this.errorText,
    this.onCompleted,
    this.onChanged,
  });

  @override
  Widget build(BuildContext context) {
    final defaultPin = PinTheme(
      width: AppTouchTarget.min,
      height: 56,
      textStyle: AppTextStyles.h2,
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(AppRadius.md),
        border: Border.all(color: AppColors.border),
      ),
    );

    return Semantics(
      textField: true,
      label: AppStrings.otpTitle,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          Directionality(
            // OTP digits always read left-to-right.
            textDirection: TextDirection.ltr,
            child: Pinput(
              controller: controller,
              focusNode: focusNode,
              length: length,
              enabled: enabled,
              autofocus: true,
              keyboardType: TextInputType.number,
              autofillHints: const [AutofillHints.oneTimeCode],
              defaultPinTheme: defaultPin,
              focusedPinTheme: defaultPin.copyWith(
                decoration: defaultPin.decoration!.copyWith(
                  border: Border.all(color: AppColors.primary, width: 2),
                ),
              ),
              errorPinTheme: defaultPin.copyWith(
                decoration: defaultPin.decoration!.copyWith(
                  border: Border.all(color: AppColors.error),
                ),
              ),
              forceErrorState: errorText != null,
              onCompleted: onCompleted,
              onChanged: onChanged,
            ),
          ),
          if (errorText != null) ...[
            const SizedBox(height: AppSpacing.xs),
            Text(
              errorText!,
              style: AppTextStyles.small.copyWith(color: AppColors.errorText),
              textAlign: TextAlign.center,
            ),
          ],
        ],
      ),
    );
  }
}
