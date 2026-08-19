import 'package:flutter/material.dart';
import '../../config/theme/app_tokens.dart';

/// One destination in the floating nav pill.
class AppBottomBarItem {
  final IconData icon;
  final String? semanticLabel;
  final int badgeCount;

  const AppBottomBarItem({
    required this.icon,
    this.semanticLabel,
    this.badgeCount = 0,
  });
}

/// Floating blue bottom navigation pill (DESIGN_LANGUAGE.md §5.11):
/// 16px side gutters, 20px above the bottom edge, 68dp tall, radius 16,
/// chrome-blue fill, white 24px icons with coral count badges. The active
/// item is full-opacity with a small white dot; inactive items are dimmed.
/// An optional [center] widget (e.g. a create action) sits mid-pill,
/// splitting the destinations around it.
class AppBottomBar extends StatelessWidget {
  final List<AppBottomBarItem> items;
  final int activeIndex;
  final ValueChanged<int>? onTap;
  final Widget? center;

  const AppBottomBar({
    super.key,
    required this.items,
    required this.activeIndex,
    this.onTap,
    this.center,
  });

  @override
  Widget build(BuildContext context) {
    final half = (items.length + 1) ~/ 2;

    return SafeArea(
      top: false,
      child: Container(
        height: 68,
        margin: const EdgeInsets.fromLTRB(
          AppSpacing.md,
          0,
          AppSpacing.md,
          20,
        ),
        decoration: BoxDecoration(
          color: AppColors.appBar,
          borderRadius: BorderRadius.circular(AppRadius.panel),
        ),
        clipBehavior: Clip.antiAlias,
        child: Row(
          children: [
            for (var i = 0; i < half; i++) Expanded(child: _item(context, i)),
            if (center != null) Expanded(child: Center(child: center)),
            for (var i = half; i < items.length; i++)
              Expanded(child: _item(context, i)),
          ],
        ),
      ),
    );
  }

  Widget _item(BuildContext context, int index) {
    final item = items[index];
    final isActive = index == activeIndex;
    final iconColor = isActive ? Colors.white : Colors.white70;

    Widget icon = Icon(item.icon, size: AppIconSize.md, color: iconColor);
    if (item.badgeCount > 0) {
      icon = Badge(
        backgroundColor: AppColors.danger,
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
        onTap: isActive || onTap == null ? null : () => onTap!(index),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            icon,
            const SizedBox(height: 4),
            AnimatedOpacity(
              duration: AppMotion.fast,
              curve: AppMotion.curve,
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
