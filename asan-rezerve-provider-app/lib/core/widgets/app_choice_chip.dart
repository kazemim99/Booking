import 'package:flutter/material.dart';
import '../../config/theme/app_tokens.dart';

/// Fill + accent pair for one pastel chip (DESIGN_LANGUAGE.md §1.4/§5.10).
/// Accent colors the leading icon; fill is the resting tile background.
class AppChipStyle {
  final Color fill;
  final Color accent;

  const AppChipStyle({required this.fill, required this.accent});

  static const pink = AppChipStyle(
    fill: AppColors.chipPink,
    accent: Color(0xFFE87FC7), // estimated saturated sibling (§9.5)
  );
  static const periwinkle = AppChipStyle(
    fill: AppColors.chipPeriwinkle,
    accent: AppColors.primary,
  );
  static const mint = AppChipStyle(
    fill: AppColors.chipMint,
    accent: Color(0xFF41CC9D),
  );
  static const rose = AppChipStyle(
    fill: AppColors.chipRose,
    accent: Color(0xFFF78E90),
  );
  static const butter = AppChipStyle(
    fill: AppColors.chipButter,
    accent: Color(0xFFDEC36C),
  );

  /// Cycle used when callers lay chips out in a grid without picking hues.
  static const family = [pink, periwinkle, mint, rose, butter];
}

/// Multi-select pastel tile chip: pastel fill + ink text + accent icon when
/// unselected; solid green fill + white icon/text when selected (production
/// Coliride selected state). Whole tile is the tap target.
class AppChoiceChip extends StatelessWidget {
  final String label;
  final IconData? icon;
  final AppChipStyle style;
  final bool selected;
  final VoidCallback? onTap;

  const AppChoiceChip({
    super.key,
    required this.label,
    this.icon,
    this.style = AppChipStyle.periwinkle,
    this.selected = false,
    this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final fill = selected ? AppColors.success : style.fill;
    final iconColor = selected ? Colors.white : style.accent;
    final textColor = selected ? Colors.white : AppColors.ink;

    return Material(
      color: fill,
      borderRadius: BorderRadius.circular(AppRadius.md),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(AppRadius.md),
        child: Semantics(
          selected: selected,
          button: true,
          child: Container(
            constraints: const BoxConstraints(minHeight: 48),
            padding: const EdgeInsets.symmetric(
              horizontal: AppSpacing.card,
              vertical: AppSpacing.sm,
            ),
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                if (icon != null) ...[
                  Icon(icon, size: AppIconSize.md, color: iconColor),
                  const SizedBox(width: AppSpacing.sm + 2),
                ],
                Flexible(
                  child: Text(
                    label,
                    style: TextStyle(
                      color: textColor,
                      fontSize: 13,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
