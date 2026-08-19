import 'package:flutter/material.dart';

import '../../../../config/theme/app_colors.dart';
import '../../../../config/theme/app_text_styles.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/utils/jalali_formatter.dart';
import '../../../booking/domain/entities/booking_entities.dart';

/// "ساعات کاری": one row per weekday, with a green "باز است" pill next to the
/// section title while the salon is open right now.
///
/// The rows are rendered exactly as the repository parsed them — Persian day
/// names, Saturday-first, `HH:mm` times and an `isClosed` flag — so weekday
/// naming and ordering live in one place only and are never re-derived here.
class WorkingHoursSection extends StatelessWidget {
  final List<BusinessHour> hours;

  /// Injected in tests; defaults to the wall clock.
  final DateTime? now;

  const WorkingHoursSection({super.key, required this.hours, this.now});

  /// Persian weekday names keyed by `DateTime.weekday` (1 = Monday … 7 = Sunday).
  static const Map<int, String> _weekdayNames = {
    1: 'دوشنبه',
    2: 'سه‌شنبه',
    3: 'چهارشنبه',
    4: 'پنجشنبه',
    5: 'جمعه',
    6: 'شنبه',
    7: 'یکشنبه',
  };

  /// Strips spaces and zero-width marks before comparing day names.
  ///
  /// «سه‌شنبه» is spelled with a zero-width non-joiner (U+200C). Comparing raw
  /// strings would let one invisible character silently make Tuesday never
  /// match, so both sides are normalised through here. Code points are matched
  /// numerically so the rule stays visible in ASCII source.
  static String _normalize(String value) {
    final buffer = StringBuffer();
    for (final rune in value.runes) {
      // Space, plus the U+200B–U+200F zero-width / direction marks.
      if (rune == 0x20 || (rune >= 0x200b && rune <= 0x200f)) continue;
      buffer.writeCharCode(rune);
    }
    return buffer.toString();
  }

  /// Whether the provider is open at [when].
  ///
  /// Returns `null` when it cannot be told — today's row is missing, or the row
  /// is open but carries no times. Callers must show no badge in that case
  /// rather than guessing "closed".
  @visibleForTesting
  static bool? isOpenNow(List<BusinessHour> hours, DateTime when) {
    final todayName = _weekdayNames[when.weekday];
    if (todayName == null) return null;

    final target = _normalize(todayName);
    BusinessHour? today;
    for (final hour in hours) {
      if (_normalize(hour.dayOfWeek) == target) {
        today = hour;
        break;
      }
    }
    if (today == null) return null;
    if (today.isClosed) return false;

    final open = _minutes(today.openTime);
    final close = _minutes(today.closeTime);
    if (open == null || close == null) return null;

    final nowMinutes = when.hour * 60 + when.minute;
    // A close time at or before the open time means the day runs past midnight.
    if (close <= open) {
      return nowMinutes >= open || nowMinutes < close;
    }
    return nowMinutes >= open && nowMinutes < close;
  }

  /// `"09:30"` → 570. Null for anything that is not `HH:mm`.
  static int? _minutes(String? time) {
    if (time == null) return null;
    final parts = time.split(':');
    if (parts.length < 2) return null;
    final h = int.tryParse(parts[0]);
    final m = int.tryParse(parts[1]);
    if (h == null || m == null) return null;
    return h * 60 + m;
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final open = isOpenNow(hours, now ?? DateTime.now());

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            Expanded(
              child: Text(
                AppStrings.workingHoursTitle,
                style: theme.textTheme.titleLarge,
              ),
            ),
            if (open == true) const _OpenNowBadge(),
          ],
        ),
        const SizedBox(height: AppSpacing.xs),
        for (final hour in hours)
          Padding(
            padding: const EdgeInsets.symmetric(vertical: AppSpacing.xxs),
            child: Row(
              children: [
                Expanded(
                  child: Text(
                    hour.dayOfWeek,
                    style: theme.textTheme.bodyMedium,
                  ),
                ),
                Text(
                  _rangeLabel(hour),
                  style: theme.textTheme.bodyMedium?.copyWith(
                    color: hour.isClosed
                        ? theme.colorScheme.onSurfaceVariant
                        : theme.colorScheme.onSurface,
                  ),
                ),
              ],
            ),
          ),
      ],
    );
  }

  static String _rangeLabel(BusinessHour hour) {
    if (hour.isClosed) return AppStrings.closedDay;
    final open = hour.openTime;
    final close = hour.closeTime;
    // An open day with no times is still not something to invent hours for.
    if (open == null || close == null) return AppStrings.closedDay;
    return JalaliFormatter.toPersianDigits('$open – $close');
  }
}

/// Green "open now" pill. Colour is never the only signal — the words say it.
class _OpenNowBadge extends StatelessWidget {
  const _OpenNowBadge();

  @override
  Widget build(BuildContext context) {
    return Semantics(
      label: AppStrings.openNow,
      child: Container(
        key: const Key('provider-open-now-badge'),
        padding: const EdgeInsets.symmetric(
          horizontal: AppSpacing.xs,
          vertical: AppSpacing.xxs,
        ),
        decoration: BoxDecoration(
          color: AppColors.successTint,
          borderRadius: BorderRadius.circular(AppRadius.full),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(
              Icons.schedule,
              size: AppIconSize.sm,
              color: AppColors.successText,
            ),
            const SizedBox(width: AppSpacing.xxs),
            Text(
              AppStrings.openNow,
              style: AppTextStyles.small.copyWith(
                color: AppColors.successText,
                fontWeight: AppTextStyles.semibold,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
