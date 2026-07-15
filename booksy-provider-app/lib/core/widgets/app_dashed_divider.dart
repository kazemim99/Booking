import 'package:flutter/material.dart';
import '../../config/theme/app_tokens.dart';

/// Dashed divider (spec: design-system-foundations): 5px dash segments.
/// Reserved for receipt-like summary contexts (e.g., the preview step's
/// price summary) — the standard divider everywhere else is the solid
/// themed [Divider].
class AppDashedDivider extends StatelessWidget {
  final double height;
  final Color color;

  const AppDashedDivider({
    super.key,
    this.height = 1,
    this.color = AppColors.divider,
  });

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        const dashWidth = 5.0;
        final dashCount = (constraints.constrainWidth() / (2 * dashWidth)).floor();
        return Flex(
          direction: Axis.horizontal,
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: List.generate(dashCount, (_) {
            return SizedBox(
              width: dashWidth,
              height: height,
              child: DecoratedBox(decoration: BoxDecoration(color: color)),
            );
          }),
        );
      },
    );
  }
}
