import 'package:flutter/material.dart';
import '../../config/theme/app_tokens.dart';

/// Standard surface card. Wraps Material's themed Card and adds an optional
/// tap handler with proper ink feedback and semantics.
class AppCard extends StatelessWidget {
  final Widget child;
  final VoidCallback? onTap;
  final EdgeInsetsGeometry padding;
  final String? semanticLabel;

  const AppCard({
    super.key,
    required this.child,
    this.onTap,
    this.padding = const EdgeInsets.all(AppSpacing.md),
    this.semanticLabel,
  });

  @override
  Widget build(BuildContext context) {
    final content = Padding(padding: padding, child: child);

    final card = Card(
      clipBehavior: Clip.antiAlias,
      child: onTap != null
          ? InkWell(onTap: onTap, child: content)
          : content,
    );

    if (semanticLabel != null) {
      return Semantics(
        label: semanticLabel,
        button: onTap != null,
        container: true,
        child: card,
      );
    }
    return card;
  }
}
