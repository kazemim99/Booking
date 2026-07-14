import 'package:flutter/material.dart';

/// Primary filled button with a loading state. Text variant via [AppButton.text].
class AppButton extends StatelessWidget {
  final String label;
  final VoidCallback? onPressed;
  final bool loading;
  final bool _isText;

  const AppButton({
    super.key,
    required this.label,
    this.onPressed,
    this.loading = false,
  }) : _isText = false;

  const AppButton.text({
    super.key,
    required this.label,
    this.onPressed,
    this.loading = false,
  }) : _isText = true;

  @override
  Widget build(BuildContext context) {
    final child = loading
        ? const SizedBox(
            height: 20,
            width: 20,
            child: CircularProgressIndicator(strokeWidth: 2),
          )
        : Text(label);
    final effectiveOnPressed = loading ? null : onPressed;

    if (_isText) {
      return TextButton(onPressed: effectiveOnPressed, child: child);
    }
    return FilledButton(onPressed: effectiveOnPressed, child: child);
  }
}
