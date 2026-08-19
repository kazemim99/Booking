import 'package:flutter/material.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/widgets/app_status_badge.dart';
import '../../domain/entities/home_booking.dart';

/// Rich booking card (ColiRide ride-card anatomy, Figma node 13345-39995):
/// avatar · bold client name · price — status pill on the trailing edge;
/// beneath, a muted row with the service and the start→end time range.
///
/// Shared by the Home agenda and the Calendar timeline so the same booking
/// never renders two different ways.
class BookingCard extends StatelessWidget {
  final HomeBooking booking;

  /// Soft-highlights the card (the agenda's "current" focus row).
  final bool highlighted;

  /// Optional trailing affordance under the pill (e.g. the agenda's ⋮ menu).
  final Widget? trailing;
  final VoidCallback? onTap;

  const BookingCard({
    super.key,
    required this.booking,
    this.highlighted = false,
    this.trailing,
    this.onTap,
  });

  static String _hm(DateTime t) =>
      '${t.hour.toString().padLeft(2, '0')}:${t.minute.toString().padLeft(2, '0')}';

  /// Grouped thousands without intl: 250000 → «۲۵۰,۰۰۰» handled upstream by
  /// font digits; keep Western digits consistent with the rest of the app.
  static String _money(double amount) {
    final whole = amount.round().toString();
    final grouped = StringBuffer();
    for (var i = 0; i < whole.length; i++) {
      if (i > 0 && (whole.length - i) % 3 == 0) grouped.write(',');
      grouped.write(whole[i]);
    }
    return grouped.toString();
  }

  (String, AppBadgeStatus) get _badge => switch (booking.status) {
        HomeBookingStatus.pending =>
          (AppStrings.homeStatusPending, AppBadgeStatus.warning),
        HomeBookingStatus.confirmed =>
          (AppStrings.homeStatusConfirmed, AppBadgeStatus.success),
        HomeBookingStatus.completed =>
          (AppStrings.homeStatusDone, AppBadgeStatus.success),
        HomeBookingStatus.noShow =>
          (AppStrings.homeStatusNoShow, AppBadgeStatus.danger),
        HomeBookingStatus.cancelled =>
          (AppStrings.homeStatusCancelled, AppBadgeStatus.neutral),
      };

  @override
  Widget build(BuildContext context) {
    final done = booking.isDone || booking.status == HomeBookingStatus.cancelled;
    final ink = done ? AppColors.muted : AppColors.ink;
    final (badgeLabel, badgeStatus) = _badge;
    final name =
        booking.clientName.isNotEmpty ? booking.clientName : booking.serviceName;

    return Material(
      color: highlighted ? AppColors.primarySoft : Colors.white,
      borderRadius: BorderRadius.circular(AppRadius.md),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(AppRadius.md),
        child: Container(
          padding: const EdgeInsets.all(AppSpacing.card),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(AppRadius.md),
            border: Border.all(
              color: highlighted ? AppColors.primary : AppColors.menuBorder,
            ),
          ),
          child: Row(
            children: [
              CircleAvatar(
                radius: 20,
                backgroundColor:
                    done ? AppColors.surface : AppColors.primarySoft,
                child: Text(
                  name.isNotEmpty ? name.characters.first : '؟',
                  style: TextStyle(
                    fontSize: 15,
                    fontWeight: FontWeight.w700,
                    color: done ? AppColors.muted : AppColors.primary,
                  ),
                ),
              ),
              const SizedBox(width: AppSpacing.card),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Expanded(
                          child: Text(
                            name,
                            overflow: TextOverflow.ellipsis,
                            style: TextStyle(
                              fontSize: 15,
                              fontWeight: FontWeight.w700,
                              color: ink,
                            ),
                          ),
                        ),
                        if (booking.price != null) ...[
                          const SizedBox(width: AppSpacing.sm),
                          Text(
                            _money(booking.price!),
                            style: TextStyle(
                              fontSize: 14,
                              fontWeight: FontWeight.w700,
                              color: ink,
                            ),
                          ),
                        ],
                      ],
                    ),
                    const SizedBox(height: AppSpacing.xs),
                    Row(
                      children: [
                        if (booking.start != null) ...[
                          const Icon(Icons.schedule,
                              size: 14, color: AppColors.icon),
                          const SizedBox(width: 4),
                          Text(
                            booking.end == null
                                ? _hm(booking.start!)
                                : '${_hm(booking.start!)} تا ${_hm(booking.end!)}',
                            // Times render LTR inside the RTL row.
                            textDirection: TextDirection.ltr,
                            style: const TextStyle(
                                fontSize: 13, color: AppColors.subtitle),
                          ),
                        ],
                        if (booking.start != null &&
                            booking.serviceName.isNotEmpty &&
                            booking.clientName.isNotEmpty)
                          const Text(
                            '  ·  ',
                            style: TextStyle(
                                fontSize: 13, color: AppColors.subtitle),
                          ),
                        if (booking.serviceName.isNotEmpty &&
                            booking.clientName.isNotEmpty)
                          Flexible(
                            child: Text(
                              booking.serviceName,
                              overflow: TextOverflow.ellipsis,
                              style: const TextStyle(
                                  fontSize: 13, color: AppColors.subtitle),
                            ),
                          ),
                      ],
                    ),
                  ],
                ),
              ),
              const SizedBox(width: AppSpacing.sm),
              Column(
                crossAxisAlignment: CrossAxisAlignment.end,
                children: [
                  AppStatusBadge(label: badgeLabel, status: badgeStatus),
                ],
              ),
              ?trailing,
            ],
          ),
        ),
      ),
    );
  }
}
