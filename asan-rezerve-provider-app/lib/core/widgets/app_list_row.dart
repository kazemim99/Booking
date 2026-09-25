import 'package:flutter/material.dart';
import '../../config/theme/app_tokens.dart';

/// Compact list row (spec: shared-ui-components): soft fill, radius 10,
/// navy title, muted subtitle, decorative leading icon, chevron when
/// tappable. Minimum height 48dp keeps the accessibility floor even in
/// compact density.
class AppListRow extends StatelessWidget {
  final IconData? leadingIcon;
  final Widget? leading;
  final String title;
  final String? subtitle;
  final Widget? trailing;
  final VoidCallback? onTap;

  const AppListRow({
    super.key,
    this.leadingIcon,
    this.leading,
    required this.title,
    this.subtitle,
    this.trailing,
    this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final resolvedTrailing = trailing ??
        (onTap != null
            ? const Icon(
                Icons.arrow_forward_ios,
                size: AppIconSize.sm,
                color: AppColors.icon,
              )
            : null);

    return Material(
      color: AppColors.surfaceSoft,
      borderRadius: BorderRadius.circular(AppRadius.sm + 2),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(AppRadius.sm + 2),
        child: Container(
          constraints: const BoxConstraints(minHeight: 48),
          padding: const EdgeInsets.symmetric(
            horizontal: AppSpacing.card,
            vertical: AppSpacing.sm,
          ),
          child: Row(
            children: [
              if (leading != null) ...[
                leading!,
                const SizedBox(width: AppSpacing.card),
              ] else if (leadingIcon != null) ...[
                Icon(leadingIcon, size: AppIconSize.md, color: AppColors.icon),
                const SizedBox(width: AppSpacing.card),
              ],
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Text(
                      title,
                      style: const TextStyle(
                        color: AppColors.ink,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    if (subtitle != null) ...[
                      const SizedBox(height: 2),
                      Text(
                        subtitle!,
                        style: const TextStyle(
                          color: AppColors.muted,
                          fontSize: 12,
                        ),
                      ),
                    ],
                  ],
                ),
              ),
              if (resolvedTrailing != null) ...[
                const SizedBox(width: AppSpacing.sm),
                resolvedTrailing,
              ],
            ],
          ),
        ),
      ),
    );
  }
}
