import 'package:flutter/material.dart';

import '../../../../config/theme/app_text_styles.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/utils/jalali_formatter.dart';
import 'service_categories.dart';

/// Size of a map pin's tap target. Also the clustering grid pitch, so two pins
/// only merge when they really would overlap.
///
/// Must be tall enough for [MapProviderPin]'s badge (34) plus its pointer
/// glyph (`AppIconSize.sm` = 16) stacked in a column — 48 clips that by 2px,
/// which only ever showed up once the pin was laid out with real constraints
/// (a widget test), never in the plain `flutter analyze`/unit-test baseline.
const double kMapPinSize = 56;

/// A single provider's pin: a rounded badge holding the category glyph, with a
/// small pointer underneath so it reads as anchored to a place rather than
/// floating over it.
class MapProviderPin extends StatelessWidget {
  /// The provider's `ServiceCategory` enum name, used only to pick a glyph.
  final String? category;
  final bool selected;
  final String semanticLabel;
  final VoidCallback onTap;

  const MapProviderPin({
    super.key,
    required this.category,
    required this.selected,
    required this.semanticLabel,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final background =
        selected ? theme.colorScheme.primary : theme.colorScheme.surface;
    final foreground =
        selected ? theme.colorScheme.onPrimary : theme.colorScheme.primary;

    return _PinTapTarget(
      semanticLabel: semanticLabel,
      selected: selected,
      onTap: onTap,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            width: 34,
            height: 34,
            decoration: BoxDecoration(
              color: background,
              shape: BoxShape.circle,
              border: Border.all(color: theme.colorScheme.primary, width: 2),
            ),
            child: Icon(
              _glyphFor(category),
              size: AppIconSize.action,
              color: foreground,
            ),
          ),
          // The anchor point. Deliberately not an image asset: one widget
          // renders identically on every platform, including Flutter web.
          Transform.translate(
            offset: const Offset(0, -2),
            child: Icon(
              Icons.arrow_drop_down,
              size: AppIconSize.sm,
              color: theme.colorScheme.primary,
            ),
          ),
        ],
      ),
    );
  }

  static IconData _glyphFor(String? category) {
    for (final choice in kServiceCategories) {
      if (choice.apiValue == category) return choice.icon;
    }
    return Icons.storefront_outlined;
  }
}

/// A pin standing in for several providers that overlap at this zoom. Tapping
/// it zooms the map in so the group splits apart.
class MapClusterPin extends StatelessWidget {
  final int count;
  final bool selected;
  final VoidCallback onTap;

  const MapClusterPin({
    super.key,
    required this.count,
    required this.selected,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final persianCount = JalaliFormatter.toPersianDigits('$count');

    return _PinTapTarget(
      semanticLabel: AppStrings.mapClusterLabel(persianCount),
      selected: selected,
      onTap: onTap,
      child: Container(
        width: 38,
        height: 38,
        alignment: Alignment.center,
        decoration: BoxDecoration(
          color: theme.colorScheme.primary,
          shape: BoxShape.circle,
          border: Border.all(
            color: selected
                ? theme.colorScheme.onPrimary
                : theme.colorScheme.primary,
            width: 2,
          ),
        ),
        child: Text(
          persianCount,
          style: theme.textTheme.labelLarge?.copyWith(
            color: theme.colorScheme.onPrimary,
            fontWeight: AppTextStyles.bold,
          ),
          maxLines: 1,
        ),
      ),
    );
  }
}

/// Shared chrome for both pin kinds: a full [kMapPinSize] tap target (the
/// visible badge is smaller than the accessible one) and a semantics button.
class _PinTapTarget extends StatelessWidget {
  final String semanticLabel;
  final bool selected;
  final VoidCallback onTap;
  final Widget child;

  const _PinTapTarget({
    required this.semanticLabel,
    required this.selected,
    required this.onTap,
    required this.child,
  });

  @override
  Widget build(BuildContext context) {
    return Semantics(
      button: true,
      selected: selected,
      label: semanticLabel,
      child: GestureDetector(
        behavior: HitTestBehavior.opaque,
        onTap: onTap,
        child: SizedBox(
          width: kMapPinSize,
          height: kMapPinSize,
          // Bottom-aligned: the pointer sits on the coordinate, not the badge's
          // middle. The marker's own alignment puts that bottom edge on the point.
          child: Align(alignment: Alignment.bottomCenter, child: child),
        ),
      ),
    );
  }
}
