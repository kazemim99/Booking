import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../config/routes/app_router.dart';
import '../../../../config/theme/app_text_styles.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/widgets/forward_chevron.dart';
import '../../../../core/widgets/widgets.dart';
import '../../../home/domain/entities/provider_summary.dart';

/// A provider card in the map's bottom carousel: cover image, business name,
/// the shared [ProviderMetaLine] (rating or «هنوز نظری ندارد», starting price,
/// free times, distance), the address, and a forward chevron.
///
/// The card is the salon: tapping it opens the profile, like every other
/// provider card. Selecting the matching pin is the carousel's job — a swipe
/// lands on a page and `PageView.onPageChanged` selects it — so the card needs
/// no "مشاهده پروفایل" button of its own (the QA pass removed the same button
/// from the nearby card).
class MapProviderCard extends StatelessWidget {
  final ProviderSummary provider;

  /// Whether this card is the one the map pin is highlighting.
  final bool selected;

  /// Today, for reading the free-slot day as «امروز»/«فردا». Injected by tests.
  final DateTime? now;

  const MapProviderCard({
    super.key,
    required this.provider,
    this.selected = false,
    this.now,
  });

  static const double _coverSize = 84;

  /// How tall the carousel band must be to hold a card at this text scale:
  /// the padding and border, the name, a meta line wrapped over up to three
  /// lines, the address, and room for the raised card's shadow. Never shorter
  /// than the cover image. The carousel is a [PageView], which needs a fixed
  /// height, so the band grows with the text instead of clipping the card.
  ///
  /// Line heights come from the text tokens the card is drawn with (the
  /// theme's `titleMedium` is [AppTextStyles.bodySemibold], its `bodySmall`
  /// is [AppTextStyles.small]), so a token change moves the band with it
  /// instead of silently clipping the card.
  static double bandHeight(TextScaler textScaler) {
    double line(TextStyle style) =>
        textScaler.scale(style.fontSize!) * style.height!;
    const chrome = 2 * AppSpacing.sm + 2 * 2; // padding + selected border
    const shadow = AppSpacing.xs;
    final name = line(AppTextStyles.bodySemibold);
    final meta = 3 * line(AppTextStyles.small) + 2 * AppSpacing.xxs;
    final address = line(AppTextStyles.small);
    final text = name + AppSpacing.xxs + meta + AppSpacing.xxs + address;
    return (chrome + math.max(_coverSize, text) + shadow).ceilToDouble();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final meta = ProviderMetaLine(
      rating: provider.rating,
      reviewCount: provider.reviewCount,
      startingPrice: provider.startingPrice,
      distanceKm: provider.distance,
      nextFreeDate: provider.nextFreeDate,
      freeSlotCount: provider.freeSlotCount,
      now: now,
    );

    return Semantics(
      label: provider.name,
      button: true,
      selected: selected,
      container: true,
      child: Material(
        color: theme.colorScheme.surface,
        elevation: selected ? AppElevation.high : AppElevation.medium,
        borderRadius: BorderRadius.circular(AppRadius.card),
        clipBehavior: Clip.antiAlias,
        child: InkWell(
          onTap: () => context.push(Routes.providerDetail(provider.id)),
          child: Container(
            decoration: BoxDecoration(
              borderRadius: BorderRadius.circular(AppRadius.card),
              border: Border.all(
                color: selected
                    ? theme.colorScheme.primary
                    : theme.dividerColor,
                width: selected ? 2 : 1,
              ),
            ),
            padding: const EdgeInsets.all(AppSpacing.sm),
            child: Row(
              children: [
                ClipRRect(
                  borderRadius: BorderRadius.circular(AppRadius.md),
                  child: ProviderImage(
                    imageUrl: provider.imageUrl,
                    width: _coverSize,
                    height: _coverSize,
                  ),
                ),
                const SizedBox(width: AppSpacing.sm),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      // The card's own label already says the name.
                      ExcludeSemantics(
                        child: Text(
                          provider.name,
                          style: theme.textTheme.titleMedium,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                      if (meta.hasContent) ...[
                        const SizedBox(height: AppSpacing.xxs),
                        meta,
                      ],
                      if (provider.addressLine != null &&
                          provider.addressLine!.isNotEmpty) ...[
                        const SizedBox(height: AppSpacing.xxs),
                        Text(
                          provider.addressLine!,
                          style: theme.textTheme.bodySmall,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ],
                    ],
                  ),
                ),
                const SizedBox(width: AppSpacing.xxs),
                ForwardChevron(color: theme.colorScheme.onSurfaceVariant),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
