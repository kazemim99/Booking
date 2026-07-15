import 'package:flutter/material.dart';
import '../../config/theme/app_tokens.dart';

enum AppBadgeStatus { success, warning, danger, neutral }

/// Semantic status pill (spec: shared-ui-components): radius 6, 12×6 padding,
/// 12sp semi-bold label, color-coded background per status.
class AppStatusBadge extends StatelessWidget {
  final String label;
  final AppBadgeStatus status;

  const AppStatusBadge({
    super.key,
    required this.label,
    this.status = AppBadgeStatus.neutral,
  });

  @override
  Widget build(BuildContext context) {
    final (background, foreground) = switch (status) {
      AppBadgeStatus.success => (AppColors.success, Colors.white),
      // Navy on amber: white text fails contrast on the light warning hue.
      AppBadgeStatus.warning => (AppColors.warning, AppColors.ink),
      AppBadgeStatus.danger => (AppColors.danger, Colors.white),
      AppBadgeStatus.neutral => (AppColors.border, AppColors.ink),
    };

    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.card,
        vertical: 6,
      ),
      decoration: BoxDecoration(
        color: background,
        borderRadius: BorderRadius.circular(6),
      ),
      child: Text(
        label,
        style: TextStyle(
          color: foreground,
          fontSize: 12,
          fontWeight: FontWeight.w600,
        ),
      ),
    );
  }
}
