import 'package:flutter/material.dart';

import '../../config/theme/app_tokens.dart';

/// A circular icon button that reads as a distinct affordance on top of a
/// coloured bar (used for the home app bar's map-search entry point).
///
/// The visible circle is [diameter], but the tap area is always at least
/// [AppTouchTarget.min] so it stays accessible.
class AppCircleIconButton extends StatelessWidget {
  final IconData icon;
  final VoidCallback? onPressed;
  final String semanticLabel;

  /// Circle fill. Defaults to a translucent white wash for blue chrome.
  final Color? backgroundColor;
  final Color? foregroundColor;
  final double diameter;

  const AppCircleIconButton({
    super.key,
    required this.icon,
    required this.onPressed,
    required this.semanticLabel,
    this.backgroundColor,
    this.foregroundColor,
    this.diameter = 40,
  });

  @override
  Widget build(BuildContext context) {
    final onBar = foregroundColor ??
        Theme.of(context).appBarTheme.foregroundColor ??
        Theme.of(context).colorScheme.onPrimary;

    return Semantics(
      button: true,
      label: semanticLabel,
      child: Tooltip(
        message: semanticLabel,
        child: SizedBox(
          width: AppTouchTarget.min,
          height: AppTouchTarget.min,
          child: Center(
            child: Material(
              color: backgroundColor ?? onBar.withValues(alpha: 0.18),
              shape: const CircleBorder(),
              clipBehavior: Clip.antiAlias,
              child: InkWell(
                onTap: onPressed,
                child: SizedBox(
                  width: diameter,
                  height: diameter,
                  child: Icon(icon, size: AppIconSize.action, color: onBar),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
