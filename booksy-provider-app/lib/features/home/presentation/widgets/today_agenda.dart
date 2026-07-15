import 'package:flutter/material.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/widgets/app_card.dart';
import '../../../../core/widgets/app_empty_state.dart';
import '../../domain/entities/home_booking.dart';

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
          child: AppEmptyState(
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

  String get _time {
    final s = booking.start;
    if (s == null) return '—';
    return '${s.hour.toString().padLeft(2, '0')}:${s.minute.toString().padLeft(2, '0')}';
  }

  @override
  Widget build(BuildContext context) {
    final done = booking.isDone;
    final trailing = switch (booking.status) {
      HomeBookingStatus.completed => const _StatusLabel(
          AppStrings.homeStatusDone, AppColors.success, Icons.check_circle),
      HomeBookingStatus.noShow => const _StatusLabel(
          AppStrings.homeStatusNoShow, AppColors.muted, Icons.person_off),
      HomeBookingStatus.pending => const _StatusLabel(
          AppStrings.homeStatusPending, AppColors.primary, Icons.schedule),
      _ => null,
    };

    return Container(
      key: Key('agenda-row-${booking.id}'),
      constraints: const BoxConstraints(minHeight: 48),
      margin: const EdgeInsets.only(bottom: AppSpacing.xs),
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.sm,
        vertical: AppSpacing.xs,
      ),
      decoration: BoxDecoration(
        color: isCurrent ? AppColors.primarySoft : null,
        borderRadius: BorderRadius.circular(AppRadius.sm),
      ),
      child: Row(
        children: [
          Text(
            _time,
            style: TextStyle(
              fontSize: 15,
              fontWeight: FontWeight.w600,
              color: done ? AppColors.muted : AppColors.ink,
            ),
          ),
          const SizedBox(width: AppSpacing.sm),
          Expanded(
            child: Text(
              [booking.clientName, booking.serviceName]
                  .where((s) => s.isNotEmpty)
                  .join(' · '),
              overflow: TextOverflow.ellipsis,
              style: TextStyle(
                fontSize: 14,
                color: done ? AppColors.muted : AppColors.ink,
              ),
            ),
          ),
          ?trailing,
          if (!done)
            PopupMenuButton<String>(
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
        ],
      ),
    );
  }
}

class _StatusLabel extends StatelessWidget {
  final String label;
  final Color color;
  final IconData icon;

  const _StatusLabel(this.label, this.color, this.icon);

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: AppIconSize.sm, color: color),
        const SizedBox(width: AppSpacing.xs),
        Text(label, style: TextStyle(fontSize: 12, color: color)),
      ],
    );
  }
}
