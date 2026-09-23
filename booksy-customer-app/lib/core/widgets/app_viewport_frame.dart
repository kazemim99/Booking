import 'package:flutter/material.dart';

import '../../config/theme/app_colors.dart';

/// Keeps the app phone-shaped on wide screens.
///
/// Every screen is designed for a phone. In a desktop browser (1440 px) the bottom-nav pill, full-width buttons and
/// bottom sheets stretched across the window. Wider than [maxContentWidth], the app is drawn in a centred column of
/// that width on a quiet backdrop (tablets included); at or below it (every phone in portrait) the child is returned
/// as is.
///
/// Inside the column [MediaQuery] reports the column's size, so code that sizes itself from `MediaQuery.sizeOf`
/// (the nav pill, dialogs, bottom sheets) and code that uses a [LayoutBuilder] agree on the width. Apply it once,
/// above the navigator (MaterialApp.builder), so routes and their overlays all live inside the column.
class AppViewportFrame extends StatelessWidget {
  const AppViewportFrame({super.key, required this.child});

  /// The column width on wide screens: a large phone, with room for the booking wizard's day strip.
  static const double maxContentWidth = 560;

  /// Identifies the backdrop drawn outside the column (present only on wide screens).
  static const Key backdropKey = ValueKey('app-viewport-backdrop');

  final Widget child;

  @override
  Widget build(BuildContext context) {
    final media = MediaQuery.of(context);
    if (media.size.width <= maxContentWidth) return child;

    final columnSize = Size(maxContentWidth, media.size.height);
    return ColoredBox(
      key: backdropKey,
      color: AppColors.border,
      child: Align(
        alignment: AlignmentDirectional.topCenter,
        child: SizedBox.fromSize(
          size: columnSize,
          child: MediaQuery(
            // The column is far from the window's side edges, so side insets (notches, rounded corners) no longer
            // apply to it; top/bottom insets and the keyboard still do.
            data: media
                .removePadding(removeLeft: true, removeRight: true)
                .removeViewPadding(removeLeft: true, removeRight: true)
                .copyWith(size: columnSize),
            child: ClipRect(child: child),
          ),
        ),
      ),
    );
  }
}
