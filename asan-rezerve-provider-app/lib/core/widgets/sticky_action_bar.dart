import 'package:flutter/material.dart';
import '../../config/theme/app_tokens.dart';

/// Sticky footer for form/detail screens (DESIGN_LANGUAGE.md §5.6): white,
/// 1px hairline top border, 12px padding plus the bottom safe area. Place it
/// below the scroll view (e.g. in `bottomNavigationBar` or a Column), never
/// inside it.
///
/// Two sanctioned layouts: paired (default — primary and secondary side by
/// side, equal widths, 10px gap) and [stacked] (primary above secondary,
/// both full-width). The primary action always sits on the leading side.
class StickyActionBar extends StatelessWidget {
  final Widget primary;
  final Widget? secondary;
  final bool stacked;

  const StickyActionBar({
    super.key,
    required this.primary,
    this.secondary,
    this.stacked = false,
  });

  static const double _gap = 10;

  @override
  Widget build(BuildContext context) {
    final Widget actions;
    if (secondary == null) {
      actions = primary;
    } else if (stacked) {
      actions = Column(
        mainAxisSize: MainAxisSize.min,
        children: [primary, const SizedBox(height: _gap), secondary!],
      );
    } else {
      actions = Row(
        children: [
          Expanded(child: primary),
          const SizedBox(width: _gap),
          Expanded(child: secondary!),
        ],
      );
    }

    return Container(
      decoration: const BoxDecoration(
        color: Colors.white,
        border: Border(
          top: BorderSide(color: AppColors.menuBorder),
        ),
      ),
      padding: const EdgeInsets.all(AppSpacing.card),
      child: SafeArea(top: false, child: actions),
    );
  }
}
