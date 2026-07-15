import 'package:flutter/material.dart';
import '../../config/theme/app_tokens.dart';

/// Section header (spec: shared-ui-components): bold navy title on the
/// leading edge with an optional trailing action slot (typically an
/// [AppInlineAddButton]).
class AppSectionHeader extends StatelessWidget {
  final String title;
  final Widget? action;

  const AppSectionHeader({super.key, required this.title, this.action});

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(
          title,
          style: const TextStyle(
            color: AppColors.ink,
            fontSize: 16,
            fontWeight: FontWeight.bold,
          ),
        ),
        ?action,
      ],
    );
  }
}

/// Inline green "add another X" affordance (spec: shared-ui-components):
/// icon + label text button with a shrink-wrapped tap target.
class AppInlineAddButton extends StatelessWidget {
  final String label;
  final VoidCallback? onPressed;
  final IconData icon;

  const AppInlineAddButton({
    super.key,
    required this.label,
    this.onPressed,
    this.icon = Icons.add,
  });

  @override
  Widget build(BuildContext context) {
    final color = onPressed == null
        ? AppColors.success.withValues(alpha: 0.4)
        : AppColors.success;
    return TextButton(
      onPressed: onPressed,
      style: TextButton.styleFrom(
        padding: const EdgeInsets.symmetric(
          horizontal: AppSpacing.card,
          vertical: AppSpacing.sm,
        ),
        minimumSize: Size.zero,
        tapTargetSize: MaterialTapTargetSize.shrinkWrap,
        foregroundColor: AppColors.success,
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: AppIconSize.action, color: color),
          const SizedBox(width: AppSpacing.xs),
          Text(
            label,
            style: TextStyle(
              fontSize: 14,
              fontWeight: FontWeight.w400,
              color: color,
            ),
          ),
        ],
      ),
    );
  }
}
