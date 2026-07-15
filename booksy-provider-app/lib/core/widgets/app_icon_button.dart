import 'package:flutter/material.dart';
import '../../config/theme/app_tokens.dart';

/// Flat 44×44 icon action button (spec: shared-ui-components): radius 12,
/// 20dp glyph, primary-tinted press feedback, optional count badge, and a
/// padded ≥48dp gesture target.
class AppIconButton extends StatelessWidget {
  final IconData icon;
  final VoidCallback? onTap;
  final String? tooltip;
  final Color? iconColor;
  final String? badgeLabel;

  const AppIconButton({
    super.key,
    required this.icon,
    this.onTap,
    this.tooltip,
    this.iconColor,
    this.badgeLabel,
  });

  @override
  Widget build(BuildContext context) {
    final button = RawMaterialButton(
      onPressed: onTap,
      constraints: const BoxConstraints.tightFor(width: 44, height: 44),
      materialTapTargetSize: MaterialTapTargetSize.padded,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(AppRadius.md),
      ),
      fillColor: Colors.transparent,
      hoverColor: AppColors.primary.withValues(alpha: 0.04),
      highlightColor: AppColors.primary.withValues(alpha: 0.05),
      splashColor: AppColors.primary.withValues(alpha: 0.05),
      elevation: 0,
      focusElevation: 0,
      hoverElevation: 0,
      highlightElevation: 0,
      disabledElevation: 0,
      child: Badge(
        isLabelVisible: badgeLabel != null,
        label: badgeLabel != null ? Text(badgeLabel!) : null,
        child: Icon(
          icon,
          size: AppIconSize.action,
          // Inherits the ambient IconTheme (white inside app bars).
          color: iconColor ?? IconTheme.of(context).color,
        ),
      ),
    );

    if (tooltip == null) return button;
    return Tooltip(message: tooltip!, child: button);
  }
}
