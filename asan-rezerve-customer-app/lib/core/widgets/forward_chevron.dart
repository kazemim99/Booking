import 'package:flutter/material.dart';

/// The "opens something" chevron at the end of a row.
///
/// Always [Icons.chevron_right]: Flutter declares it `matchTextDirection: true`, so it already points LEFT in an
/// RTL layout. Picking `chevron_left` by hand in RTL mirrors it a second time and it ends up pointing the wrong
/// way — which is what the profile rows did (QA walkthrough 2026-09-22).
class ForwardChevron extends StatelessWidget {
  final double? size;
  final Color? color;

  const ForwardChevron({super.key, this.size, this.color});

  @override
  Widget build(BuildContext context) =>
      Icon(Icons.chevron_right, size: size, color: color);
}
