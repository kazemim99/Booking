import 'package:flutter/material.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/widgets/app_card.dart';
import '../../../../core/widgets/app_empty_state.dart';
import '../../domain/entities/home_booking.dart';
import 'booking_card.dart';

/// Zone: today's agenda — the day timeline. Renders the positive empty state
/// when the day has no bookings (never a bare "nothing here").
class TodayAgenda extends StatelessWidget {
  final List<HomeBooking> bookings;
  final int tomorrowApptCount;
  final VoidCallback onAddAppointment;
  final void Function(String id) onComplete;
  final void Function(String id) onNoShow;

  const TodayAgenda({
    super.key,
    required this.bookings,
    required this.tomorrowApptCount,
    required this.onAddAppointment,
    required this.onComplete,
    required this.onNoShow,
  });

  @override
  Widget build(BuildContext context) {
    if (bookings.isEmpty) {
      return AppCard(
        child: Padding(
          padding: const EdgeInsets.symmetric(vertical: AppSpacing.md),
          child: AppEmptyState.add(
            icon: Icons.event_available_outlined,
            message: AppStrings.homeAgendaEmptyTitle,
            description: tomorrowApptCount > 0
                ? AppStrings.homeNextAppt(
                    AppStrings.homeTomorrowCount(tomorrowApptCount))
                : AppStrings.homeAgendaEmptyBodySetup,
            actionLabel: '+ ${AppStrings.homeAddAppointment}',
            onAction: onAddAppointment,
          ),
        ),
      );
    }

    // The first not-done booking is the "current" focus row.
    final currentIndex = bookings.indexWhere((b) => !b.isDone);

    return AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Expanded(
                child: Text(
                  AppStrings.homeAgendaTitle,
                  style: TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.w700,
                    color: AppColors.ink,
                  ),
                ),
              ),
              Text(
                AppStrings.homeAgendaCount(bookings.length),
                style: const TextStyle(fontSize: 13, color: AppColors.muted),
              ),
            ],
          ),
          const SizedBox(height: AppSpacing.sm),
          for (var i = 0; i < bookings.length; i++)
            _AgendaRow(
              booking: bookings[i],
              isCurrent: i == currentIndex,
              onComplete: onComplete,
              onNoShow: onNoShow,
            ),
        ],
      ),
    );
  }
}

class _AgendaRow extends StatelessWidget {
  final HomeBooking booking;
  final bool isCurrent;
  final void Function(String id) onComplete;
  final void Function(String id) onNoShow;

  const _AgendaRow({
    required this.booking,
    required this.isCurrent,
    required this.onComplete,
    required this.onNoShow,
  });

  @override
  Widget build(BuildContext context) {
    final done = booking.isDone;

    return Padding(
      key: Key('agenda-row-${booking.id}'),
      padding: const EdgeInsets.only(bottom: AppSpacing.sm),
      child: BookingCard(
        booking: booking,
        highlighted: isCurrent,
        trailing: done
            ? null
            : PopupMenuButton<String>(
                key: Key('agenda-menu-${booking.id}'),
                iconSize: AppIconSize.action,
                iconColor: AppColors.muted,
                onSelected: (v) => v == 'complete'
                    ? onComplete(booking.id)
                    : onNoShow(booking.id),
                itemBuilder: (_) => const [
                  PopupMenuItem(
                    value: 'complete',
                    child: Text(AppStrings.homeActionComplete),
                  ),
                  PopupMenuItem(
                    value: 'noshow',
                    child: Text(AppStrings.homeActionNoShow),
                  ),
                ],
              ),
      ),
    );
  }
}

