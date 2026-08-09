import 'package:flutter/material.dart';
import '../../config/theme/app_colors.dart';
import '../../config/theme/app_tokens.dart';

/// One destination in the floating nav pill.
class AppBottomBarItem {
  final IconData icon;
  final IconData selectedIcon;
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
/// 16px side gutters, 20px above the bottom edge, 68dp tall, radius 16,
/// chrome-blue fill, white 24px icons with coral count badges. The active
/// item is full-opacity with a small white dot; inactive items are dimmed.
///
/// This is a visual restyle of the bottom bar only — it is dropped into the
/// existing navigation shell's `bottomNavigationBar` slot and drives the same
/// branch-switching callback; routes, tabs, and back-stack behavior are
/// unchanged.
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

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      top: false,
      child: Container(
        height: 68,
        margin: const EdgeInsets.fromLTRB(AppSpacing.md, 0, AppSpacing.md, 20),
        decoration: BoxDecoration(
          color: AppColors.appBar,
          borderRadius: BorderRadius.circular(AppRadius.panel),
        ),
        clipBehavior: Clip.antiAlias,
        child: Row(
          children: [
            for (var i = 0; i < items.length; i++)
              Expanded(child: _item(context, i)),
          ],
        ),
      ),
    );
  }

  Widget _item(BuildContext context, int index) {
    final item = items[index];
    final isActive = index == activeIndex;
    final color = isActive ? Colors.white : Colors.white70;

    Widget icon = Icon(
      isActive ? item.selectedIcon : item.icon,
      size: AppIconSize.md,
      color: color,
    );
    if (item.badgeCount > 0) {
      icon = Badge(
        backgroundColor: AppColors.error,
        textColor: Colors.white,
        label: Text(
          item.badgeCount > 99 ? '۹۹+' : '${item.badgeCount}',
          style: const TextStyle(fontSize: 12.5, fontWeight: FontWeight.w600),
        ),
        offset: const Offset(4.5, -4.5),
        child: icon,
      );
    }

    return Semantics(
      selected: isActive,
      button: true,
      label: item.semanticLabel,
      child: InkWell(
        onTap: onTap == null ? null : () => onTap!(index),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            icon,
            const SizedBox(height: 4),
            AnimatedOpacity(
              duration: AppMotion.fast,
              curve: AppMotion.emphasized,
              opacity: isActive ? 1 : 0,
              child: Container(
                width: 4,
                height: 4,
                decoration: const BoxDecoration(
                  color: Colors.white,
                  shape: BoxShape.circle,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
