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
  final ValueChanged<String>? onChanged;
  final ValueChanged<String>? onSubmitted;

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
    this.onChanged,
    this.onSubmitted,
  });

  @override
  Widget build(BuildContext context) {
    return TextField(
      controller: controller,
      focusNode: focusNode,
      keyboardType: keyboardType,
      textDirection: contentDirection,
      inputFormatters: inputFormatters,
      maxLength: maxLength,
      autofillHints: autofillHints,
      onChanged: onChanged,
      onSubmitted: onSubmitted,
      decoration: InputDecoration(
        labelText: label,
        hintText: hint,
        errorText: errorText,
        prefixIcon: prefixIcon != null ? Icon(prefixIcon) : null,
        counterText: '',
      ),
    );
  }
}
