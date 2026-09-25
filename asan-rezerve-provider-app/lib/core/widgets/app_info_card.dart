import 'package:flutter/material.dart';
import '../../config/theme/app_tokens.dart';
import 'app_card.dart';

/// Card with an attached top tag strip and a tinted icon container
/// (spec: shared-ui-components; adapted from Coliride's tagged info card).
/// The standard presentation for wizard preview-step section summaries.
class AppInfoCard extends StatelessWidget {
  /// Label inside the top tag strip (e.g., the section name).
  final String tagText;

  /// Icon shown both in the tag strip (16dp) and the 40×40 tinted container.
  final IconData icon;

  /// Card body content.
  final Widget child;

  /// Optional trailing widget beside the body (e.g., an edit action).
  final Widget? trailing;

  final Color? iconBackgroundColor;
  final Color? iconColor;

  const AppInfoCard({
    super.key,
    required this.tagText,
    required this.icon,
    required this.child,
    this.trailing,
    this.iconBackgroundColor,
    this.iconColor,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        // Attached top tag.
        Container(
          padding: const EdgeInsets.symmetric(
            horizontal: AppSpacing.card,
            vertical: 6,
          ),
          decoration: const BoxDecoration(
            color: AppColors.border,
            borderRadius: BorderRadius.vertical(
              top: Radius.circular(AppRadius.md),
            ),
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(icon, size: AppIconSize.sm, color: AppColors.ink),
              const SizedBox(width: 6),
              Text(
                tagText,
                style: const TextStyle(
                  fontSize: 12,
                  color: AppColors.ink,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ],
          ),
        ),
        AppCard(
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                width: 40,
                height: 40,
                decoration: BoxDecoration(
                  color: iconBackgroundColor ?? AppColors.primarySoft,
                  borderRadius: BorderRadius.circular(AppRadius.sm),
                ),
                child: Icon(
                  icon,
                  size: AppIconSize.md,
                  color: iconColor ?? AppColors.appBar,
                ),
              ),
              const SizedBox(width: AppSpacing.card),
              Expanded(child: child),
              ?trailing,
            ],
          ),
        ),
      ],
    );
  }
}
