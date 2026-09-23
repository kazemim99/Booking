import 'package:flutter/material.dart';
import '../../config/theme/app_colors.dart';
import '../../config/theme/app_text_styles.dart';
import '../../config/theme/app_tokens.dart';
import '../utils/persian_formatter.dart';

/// One destination in the floating nav pill.
class AppBottomBarItem {
  final IconData icon;
  final IconData selectedIcon;

  /// The tab's name: drawn under the icon and announced as the item's label.
  final String semanticLabel;
  final int badgeCount;

  const AppBottomBarItem({
    required this.icon,
    required this.selectedIcon,
    required this.semanticLabel,
    this.badgeCount = 0,
  });
}

/// Floating blue bottom-navigation pill, aligned to the Provider app:
/// 16px side gutters, 20px above the bottom edge, at least 68dp tall, radius 16,
/// chrome-blue fill, 24px icons with coral count badges.
///
/// Every item shows its name under the icon (decision 2, 2026-09-23 — it replaced
/// the icon-only O1). Labels are full white in both states (4.6:1 on the bar); the
/// active item is told apart by a bold label, the filled icon and a pill behind
/// the icon, never by dimming text. Inactive icons are pale blue at ≥ 3:1. The
/// pill grows with the text scale instead of clipping the labels. Each item is a
/// single semantics node (name + button + selected), so the visible label is not
/// announced twice.
///
/// It sits in the navigation shell's `bottomNavigationBar` slot and drives the
/// same branch-switching callback; routes, tabs and back-stack are unchanged.
class AppBottomBar extends StatelessWidget {
  final List<AppBottomBarItem> items;
  final int activeIndex;
  final ValueChanged<int>? onTap;

  const AppBottomBar({
    super.key,
    required this.items,
    required this.activeIndex,
    this.onTap,
  });

  static const double _minHeight = 68;

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      top: false,
      child: Container(
        key: const Key('app-bottom-bar'),
        constraints: const BoxConstraints(minHeight: _minHeight),
        margin: const EdgeInsetsDirectional.fromSTEB(
            AppSpacing.md, 0, AppSpacing.md, 20),
        decoration: BoxDecoration(
          color: AppColors.appBar,
          borderRadius: BorderRadius.circular(AppRadius.panel),
        ),
        clipBehavior: Clip.antiAlias,
        // IntrinsicHeight lets every item stretch to the tallest one, so each
        // whole column is tappable even when the text scale grows the bar.
        child: IntrinsicHeight(
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              for (var i = 0; i < items.length; i++)
                Expanded(child: _item(context, i)),
            ],
          ),
        ),
      ),
    );
  }

  Widget _item(BuildContext context, int index) {
    final item = items[index];
    final isActive = index == activeIndex;

    Widget icon = Icon(
      isActive ? item.selectedIcon : item.icon,
      size: AppIconSize.md,
      color: isActive ? AppColors.navIconActive : AppColors.navIconInactive,
    );
    if (item.badgeCount > 0) {
      icon = Badge(
        backgroundColor: AppColors.error,
        textColor: Colors.white,
        label: Text(
          item.badgeCount > 99 ? '۹۹+' : '${item.badgeCount}'.toPersianDigits(),
          style: const TextStyle(fontSize: 12.5, fontWeight: FontWeight.w600),
        ),
        offset: const Offset(4.5, -4.5),
        child: icon,
      );
    }

    // The indicator pill behind the active icon (M3-style); inactive items keep
    // the same box so the icons do not shift when the selection moves.
    icon = AnimatedContainer(
      key: isActive ? Key('app-bottom-bar-indicator-$index') : null,
      duration: MediaQuery.of(context).disableAnimations
          ? Duration.zero
          : AppMotion.fast,
      curve: AppMotion.emphasized,
      width: 56,
      height: 30,
      alignment: Alignment.center,
      decoration: BoxDecoration(
        color: isActive ? AppColors.navIndicator : Colors.transparent,
        borderRadius: BorderRadius.circular(AppRadius.full),
      ),
      child: icon,
    );

    return Semantics(
      container: true,
      button: true,
      selected: isActive,
      label: item.semanticLabel,
      value: item.badgeCount > 0
          ? (item.badgeCount > 99 ? '۹۹+' : '${item.badgeCount}'.toPersianDigits())
          : null,
      child: InkWell(
        onTap: onTap == null ? null : () => onTap!(index),
        child: ExcludeSemantics(
          child: Padding(
            padding: const EdgeInsets.symmetric(
              horizontal: AppSpacing.xxs,
              vertical: AppSpacing.xs,
            ),
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              mainAxisSize: MainAxisSize.min,
              children: [
                icon,
                const SizedBox(height: AppSpacing.xxs),
                Text(
                  item.semanticLabel,
                  maxLines: 1,
                  softWrap: false,
                  overflow: TextOverflow.ellipsis,
                  textAlign: TextAlign.center,
                  style: AppTextStyles.small.copyWith(
                    color: AppColors.navLabel,
                    fontWeight:
                        isActive ? AppTextStyles.bold : AppTextStyles.medium,
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
