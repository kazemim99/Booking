import 'package:flutter/material.dart';

import '../../config/theme/app_tokens.dart';
import '../constants/app_strings.dart';
import '../utils/jalali_formatter.dart';
import '../utils/price_formatter.dart';
import 'price_band.dart';
import 'provider_rating.dart';

/// The one-line provider summary used on cards and on the profile header:
/// category · rating (review count) · «از … تومان» · free times · distance.
///
/// Every part is optional because the backend does not yet publish all of them
/// (the search payload's starting price is 0 and it carries no distance).
/// The rating is the exception: a known zero review count is shown as "no
/// reviews yet" rather than dropped, because the spec asks for it. A part that has no
/// value is dropped along with its separator, and when nothing at all can be
/// shown the widget collapses to zero height rather than leaving a row of
/// placeholders or fabricated numbers.
class ProviderMetaLine extends StatelessWidget {
  /// Category (or, on the profile header, the city standing in for it).
  final String? category;
  final double? rating;
  final int? reviewCount;

  /// The old `$`/`$$`/`$$$` band. No screen passes it any more: in a Toman app
  /// the glyphs read as dollars, so cards show [startingPrice] instead.
  final PriceBand? priceBand;

  /// The salon's cheapest price, in Toman. Shown as «از ۱۲۰٬۰۰۰ تومان»;
  /// null or zero (the search payload sends 0) hides it.
  final int? startingPrice;

  /// Distance in kilometres, when the response provided one.
  final double? distanceKm;

  /// How soon the salon can be booked: the day of its first free times and how
  /// many there are. Both come from the availability summary; without it the
  /// part is dropped like every other unknown.
  final DateTime? nextFreeDate;
  final int freeSlotCount;

  /// Today, for reading [nextFreeDate] as "امروز"/"فردا". Injected by tests.
  final DateTime? now;

  const ProviderMetaLine({
    super.key,
    this.category,
    this.rating,
    this.reviewCount,
    this.priceBand,
    this.startingPrice,
    this.distanceKm,
    this.nextFreeDate,
    this.freeSlotCount = 0,
    this.now,
  });

  /// «امروز ۵ وقت خالی» — or the weekday, when it is further out.
  static String? freeSlotsLabel(DateTime? date, int count, DateTime today) {
    if (date == null || count <= 0) return null;
    final days = DateTime(date.year, date.month, date.day)
        .difference(DateTime(today.year, today.month, today.day))
        .inDays;
    final when = switch (days) {
      <= 0 => AppStrings.today,
      1 => AppStrings.tomorrow,
      _ => JalaliFormatter.weekday(date),
    };
    return AppStrings.freeSlotsOn(when, count);
  }

  /// Whether this configuration would render anything at all.
  bool get hasContent =>
      (category != null && category!.isNotEmpty) ||
      ProviderRating.hasRating(rating ?? 0, reviewCount) ||
      ProviderRating.isUnrated(reviewCount) ||
      priceBand != null ||
      _hasStartingPrice ||
      distanceKm != null ||
      freeSlotsLabel(nextFreeDate, freeSlotCount, now ?? DateTime.now()) != null;

  bool get _hasStartingPrice => (startingPrice ?? 0) > 0;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final parts = <Widget>[];

    if (category != null && category!.isNotEmpty) {
      parts.add(Text(category!, style: theme.textTheme.bodySmall));
    }
    if (ProviderRating.hasRating(rating ?? 0, reviewCount)) {
      parts.add(ProviderRating(rating: rating ?? 0, reviewCount: reviewCount));
    } else if (ProviderRating.isUnrated(reviewCount)) {
      parts.add(const NoReviewsYetLabel());
    }
    if (priceBand != null) {
      parts.add(PriceBandLabel(band: priceBand!));
    }
    if (_hasStartingPrice) {
      parts.add(Text(
        PriceFormatter.formatFrom(startingPrice!),
        key: const Key('provider-starting-price'),
        style: theme.textTheme.bodySmall,
        maxLines: 1,
        overflow: TextOverflow.ellipsis,
      ));
    }
    final freeSlots =
        freeSlotsLabel(nextFreeDate, freeSlotCount, now ?? DateTime.now());
    if (freeSlots != null) {
      parts.add(
        Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              Icons.event_available_outlined,
              size: AppIconSize.sm,
              color: theme.colorScheme.primary,
            ),
            const SizedBox(width: AppSpacing.xxs),
            Flexible(
              child: Text(
                freeSlots,
                key: const Key('provider-free-slots'),
                style: theme.textTheme.bodySmall
                    ?.copyWith(color: theme.colorScheme.primary),
                overflow: TextOverflow.ellipsis,
              ),
            ),
          ],
        ),
      );
    }
    if (distanceKm != null) {
      parts.add(
        Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              Icons.location_on_outlined,
              size: AppIconSize.sm,
              color: theme.colorScheme.onSurfaceVariant,
            ),
            const SizedBox(width: AppSpacing.xxs),
            // Flexible: a Wrap constrains each part to the line width, and at
            // large font scales the label alone can exceed a narrow card.
            Flexible(
              child: Text(
                AppStrings.distanceKmLabel(
                  JalaliFormatter.toPersianDigits(
                    distanceKm!.toStringAsFixed(1),
                  ),
                ),
                style: theme.textTheme.bodySmall,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
              ),
            ),
          ],
        ),
      );
    }

    if (parts.isEmpty) return const SizedBox.shrink();

    // Each separator travels with the part it precedes, so when the Wrap
    // breaks a line the «·» starts the next line with its part instead of
    // hanging alone at the end of the previous one.
    return Wrap(
      spacing: AppSpacing.xs,
      runSpacing: AppSpacing.xxs,
      crossAxisAlignment: WrapCrossAlignment.center,
      children: [
        for (var i = 0; i < parts.length; i++)
          if (i == 0)
            parts[i]
          else
            Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                // A visual separator only; a screen reader should not say it.
                ExcludeSemantics(
                  child: Text(
                    '·',
                    style: theme.textTheme.bodySmall?.copyWith(
                      color: theme.colorScheme.onSurfaceVariant,
                    ),
                  ),
                ),
                const SizedBox(width: AppSpacing.xs),
                Flexible(child: parts[i]),
              ],
            ),
      ],
    );
  }
}
