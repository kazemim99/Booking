import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

/// Standard app text field: themed decoration, label above, inline error.
/// Direction-sensitive content (phone numbers, codes) can force LTR while
/// the field itself stays RTL-positioned.
class AppTextField extends StatelessWidget {
  final TextEditingController? controller;
  final String? label;
  final String? hint;
  final String? errorText;
  final IconData? prefixIcon;
  final TextInputType? keyboardType;
  final TextDirection? contentDirection;
  final List<TextInputFormatter>? inputFormatters;
  final ValueChanged<String>? onChanged;
  final ValueChanged<String>? onSubmitted;
  final FormFieldValidator<String>? validator;
  final Iterable<String>? autofillHints;
  final bool enabled;
  final bool autofocus;
  final int? maxLength;
  final FocusNode? focusNode;

  /// Lines the field shows: one unless a caller wants room to write.
  final int? maxLines;

  /// Shows [maxLength]'s «used/max» counter under the field. Off by default:
  /// a name or a phone number is capped silently, a long comment is not.
  final bool showCounter;

  const AppTextField({
    super.key,
    this.controller,
    this.label,
    this.hint,
    this.errorText,
    this.prefixIcon,
    this.keyboardType,
    this.contentDirection,
    this.inputFormatters,
    this.onChanged,
    this.onSubmitted,
    this.validator,
    this.autofillHints,
    this.enabled = true,
    this.autofocus = false,
    this.maxLength,
    this.focusNode,
    this.maxLines = 1,
    this.showCounter = false,
  });

  @override
  Widget build(BuildContext context) {
    return TextFormField(
      controller: controller,
      focusNode: focusNode,
      enabled: enabled,
      autofocus: autofocus,
      keyboardType: keyboardType,
      textDirection: contentDirection,
      inputFormatters: inputFormatters,
      onChanged: onChanged,
      onFieldSubmitted: onSubmitted,
      validator: validator,
      autofillHints: autofillHints,
      maxLength: maxLength,
      maxLines: maxLines,
      decoration: InputDecoration(
        labelText: label,
        hintText: hint,
        errorText: errorText,
        // null lets the field draw its own counter (with its spoken
        // "characters remaining"); '' hides it.
        counterText: showCounter ? null : '',
        prefixIcon: prefixIcon != null ? Icon(prefixIcon) : null,
      ),
    );
  }
}
