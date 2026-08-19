import 'package:flutter/material.dart';

/// Text tab bar (DESIGN_LANGUAGE.md §5.7): 16 w700 labels, active green with
/// a 2px green bottom indicator, inactive navy ink, hairline divider under
/// the whole bar — all supplied by the app theme's [TabBarThemeData].
/// Scrollable by default when more than three tabs.
class AppTextTabs extends StatelessWidget {
  final TabController? controller;
  final List<String> tabs;
  final bool? isScrollable;

  const AppTextTabs({
    super.key,
    this.controller,
    required this.tabs,
    this.isScrollable,
  });

  @override
  Widget build(BuildContext context) {
    return TabBar(
      controller: controller,
      isScrollable: isScrollable ?? tabs.length > 3,
      tabAlignment:
          (isScrollable ?? tabs.length > 3) ? TabAlignment.start : null,
      tabs: [for (final label in tabs) Tab(text: label)],
    );
  }
}
