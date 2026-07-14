import 'package:flutter/material.dart';
import 'package:pinput/pinput.dart';
import '../utils/persian_digits.dart';

/// 6-digit OTP input (Pinput): numeric, SMS autofill, paste, auto-submit on
/// completion, inline error + shake feedback via [errorText].
class OtpInput extends StatelessWidget {
  final TextEditingController controller;
  final FocusNode? focusNode;
  final String? errorText;
  final ValueChanged<String> onCompleted;
  final ValueChanged<String>? onChanged;

  const OtpInput({
    super.key,
    required this.controller,
    this.focusNode,
    this.errorText,
    required this.onCompleted,
    this.onChanged,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final base = PinTheme(
      width: 48,
      height: 56,
      textStyle: theme.textTheme.headlineSmall,
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: theme.colorScheme.outline),
      ),
    );

    return Column(
      children: [
        Directionality(
          textDirection: TextDirection.ltr,
          child: Pinput(
            length: 6,
            controller: controller,
            focusNode: focusNode,
            autofocus: true,
            keyboardType: TextInputType.number,
            inputFormatters: const [DigitsOnlyInputFormatter()],
            defaultPinTheme: base,
            focusedPinTheme: base.copyWith(
              decoration: base.decoration!.copyWith(
                border: Border.all(color: theme.colorScheme.primary, width: 2),
              ),
            ),
            errorPinTheme: base.copyWith(
              decoration: base.decoration!.copyWith(
                border: Border.all(color: theme.colorScheme.error, width: 2),
              ),
            ),
            forceErrorState: errorText != null,
            onCompleted: onCompleted,
            onChanged: onChanged,
          ),
        ),
        if (errorText != null) ...[
          const SizedBox(height: 8),
          Text(
            errorText!,
            style: theme.textTheme.bodySmall?.copyWith(
              color: theme.colorScheme.error,
            ),
            textAlign: TextAlign.center,
          ),
        ],
      ],
    );
  }
}
