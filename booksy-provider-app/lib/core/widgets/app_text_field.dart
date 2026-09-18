import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

/// Labeled text field with inline error support and optional LTR content
/// direction (for phone numbers in an RTL layout).
class AppTextField extends StatelessWidget {
  final TextEditingController? controller;
  final FocusNode? focusNode;
  final String? label;
  final String? hint;
  final String? errorText;
  final IconData? prefixIcon;
  final TextInputType? keyboardType;
  final TextDirection? contentDirection;
  final List<TextInputFormatter>? inputFormatters;
  final int? maxLength;
  final List<String>? autofillHints;
  final int? maxLines;
  final int? minLines;
  final ValueChanged<String>? onChanged;
  final ValueChanged<String>? onSubmitted;

  /// Marks the field as required with a red asterisk after the label, so the user
  /// knows BEFORE pressing Next which fields must be filled.
  final bool isRequired;

  /// Shows the value but does not let the user change it (e.g. the mobile number
  /// they already verified at sign-in). Rendered visibly disabled.
  final bool readOnly;

  /// Fires when focus leaves this field — the hook for "validate on blur", so a
  /// required-field error appears under the field as soon as the user tabs away.
  final VoidCallback? onBlur;

  const AppTextField({
    super.key,
    this.controller,
    this.focusNode,
    this.label,
    this.hint,
    this.errorText,
    this.prefixIcon,
    this.keyboardType,
    this.contentDirection,
    this.inputFormatters,
    this.maxLength,
    this.autofillHints,
    this.maxLines = 1,
    this.minLines,
    this.onChanged,
    this.onSubmitted,
    this.isRequired = false,
    this.readOnly = false,
    this.onBlur,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final field = TextField(
      controller: controller,
      focusNode: focusNode,
      keyboardType: keyboardType,
      textDirection: contentDirection,
      inputFormatters: inputFormatters,
      maxLength: maxLength,
      autofillHints: autofillHints,
      maxLines: maxLines,
      minLines: minLines,
      onChanged: onChanged,
      onSubmitted: onSubmitted,
      readOnly: readOnly,
      enabled: !readOnly,
      decoration: InputDecoration(
        // `label` (a widget) instead of `labelText` so the required asterisk can be
        // coloured independently of the label text.
        label: label == null
            ? null
            : Text.rich(
                TextSpan(
                  text: label,
                  children: [
                    if (isRequired)
                      TextSpan(
                        text: ' *',
                        style: TextStyle(color: theme.colorScheme.error),
                      ),
                  ],
                ),
              ),
        hintText: hint,
        errorText: errorText,
        // Error lines can be long in Persian; let them wrap instead of clipping.
        errorMaxLines: 2,
        prefixIcon: prefixIcon != null ? Icon(prefixIcon) : null,
        counterText: '',
      ),
    );

    if (onBlur == null) return field;

    // A non-focusable, non-traversable wrapper: it takes no focus itself, it only
    // reports when focus leaves the TextField inside it.
    return Focus(
      canRequestFocus: false,
      skipTraversal: true,
      onFocusChange: (hasFocus) {
        if (!hasFocus) onBlur!();
      },
      child: field,
    );
  }
}
