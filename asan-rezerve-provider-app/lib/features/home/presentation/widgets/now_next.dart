import 'package:flutter/material.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/widgets/app_card.dart';
import '../../domain/entities/home_booking.dart';

/// Zone: now/next — the current or upcoming appointment as an actionable hero
/// (two-tap promises: complete / no-show / call).
class NowNext extends StatelessWidget {
  final HomeBooking booking;
  final bool inProgress;
  final void Function(String id) onComplete;
  final void Function(String id) onNoShow;
  final void Function(HomeBooking booking) onCall;

  /// The salon's clock, which decides what the server will take. Defaults to now.
  final DateTime? now;

  const NowNext({
    super.key,
    required this.booking,
    required this.inProgress,
    required this.onComplete,
    required this.onNoShow,
    required this.onCall,
    this.now,
  });

  String get _time {
    final s = booking.start;
    if (s == null) return '—';
    return '${s.hour.toString().padLeft(2, '0')}:${s.minute.toString().padLeft(2, '0')}';
  }

  @override
  Widget build(BuildContext context) {
    final at = now ?? DateTime.now();
    // Only what the server takes: a request is confirmed first, «تکمیل» opens 15 minutes before the start, and
    // «عدم حضور» once the time is over. The card used to offer both on anything, and they failed.
    final canComplete = booking.canCompleteAt(at);
    final canNoShow = booking.canMarkNoShowAt(at);
    return AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  inProgress
                      ? AppStrings.homeNowLabel
                      : AppStrings.homeNextLabel,
                  style:
                      const TextStyle(fontSize: 12, color: AppColors.muted),
                ),
              ),
              Text(
                _time,
                style: const TextStyle(
                  fontSize: 17,
                  fontWeight: FontWeight.w700,
                  color: AppColors.ink,
                ),
              ),
            ],
          ),
          const SizedBox(height: AppSpacing.xs),
          Text(
            [booking.clientName, booking.serviceName]
                .where((s) => s.isNotEmpty)
                .join(' · '),
            style: const TextStyle(fontSize: 15, color: AppColors.ink),
          ),
          const SizedBox(height: AppSpacing.md),
          // Width-constrained buttons: the themed button is infinite-width
          // (Size.fromHeight) and must never sit bare inside a Row.
          Row(
            children: [
              if (canComplete)
                Expanded(
                  child: FilledButton.icon(
                    key: const Key('nownext-complete'),
                    onPressed: () => onComplete(booking.id),
                    icon: const Icon(Icons.check, size: AppIconSize.action),
                    label: const Text(AppStrings.homeActionComplete),
                  ),
                ),
              if (canNoShow) ...[
                const SizedBox(width: AppSpacing.sm),
                Expanded(
                  child: OutlinedButton.icon(
                    key: const Key('nownext-noshow'),
                    onPressed: () => onNoShow(booking.id),
                    icon:
                        const Icon(Icons.person_off, size: AppIconSize.action),
                    label: const Text(AppStrings.homeActionNoShow),
                  ),
                ),
              ],
              if (!canComplete)
                Expanded(
                  child: Text(
                    booking.status == HomeBookingStatus.pending
                        ? AppStrings.homeNextAwaitsConfirmation
                        : AppStrings.homeCompleteLaterHint,
                    key: const Key('nownext-later'),
                    style: const TextStyle(fontSize: 13, color: AppColors.muted),
                  ),
                ),
              if (booking.clientPhone.isNotEmpty) ...[
                const SizedBox(width: AppSpacing.sm),
                IconButton(
                  key: const Key('nownext-call'),
                  tooltip: AppStrings.homeActionCall,
                  onPressed: () => onCall(booking),
                  icon: const Icon(Icons.phone_outlined,
                      color: AppColors.primary),
                ),
              ],
            ],
          ),
        ],
      ),
    );
  }
}
