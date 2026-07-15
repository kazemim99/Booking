import 'package:flutter/material.dart';

import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/widgets/app_card.dart';
import '../../domain/entities/home_booking.dart';

/// Zone: action queue — pending booking requests needing the provider's
/// approval (Request mode's loudest widget). Inline confirm/decline keeps the
/// two-tap promise.
class ActionQueue extends StatelessWidget {
  final List<HomeBooking> requests;
  final void Function(String id) onConfirm;
  final void Function(String id) onDecline;

  const ActionQueue({
    super.key,
    required this.requests,
    required this.onConfirm,
    required this.onDecline,
  });

  @override
  Widget build(BuildContext context) {
    if (requests.isEmpty) return const SizedBox.shrink();

    return AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Icon(Icons.notifications_active_outlined,
                  size: AppIconSize.md, color: AppColors.primary),
              const SizedBox(width: AppSpacing.sm),
              const Expanded(
                child: Text(
                  AppStrings.homeRequestsTitle,
                  style: TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.w700,
                    color: AppColors.ink,
                  ),
                ),
              ),
              Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: AppSpacing.sm,
                  vertical: 2,
                ),
                decoration: BoxDecoration(
                  color: AppColors.primary,
                  borderRadius: BorderRadius.circular(AppRadius.lg),
                ),
                child: Text(
                  '${requests.length}',
                  style: const TextStyle(fontSize: 12, color: Colors.white),
                ),
              ),
            ],
          ),
          const Divider(color: AppColors.divider, height: AppSpacing.lg),
          for (final request in requests) _RequestRow(
            booking: request,
            onConfirm: onConfirm,
            onDecline: onDecline,
          ),
        ],
      ),
    );
  }
}

class _RequestRow extends StatelessWidget {
  final HomeBooking booking;
  final void Function(String id) onConfirm;
  final void Function(String id) onDecline;

  const _RequestRow({
    required this.booking,
    required this.onConfirm,
    required this.onDecline,
  });

  String get _when {
    final s = booking.start;
    if (s == null) return '';
    return '${s.hour.toString().padLeft(2, '0')}:${s.minute.toString().padLeft(2, '0')}';
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      key: Key('request-row-${booking.id}'),
      padding: const EdgeInsets.only(bottom: AppSpacing.sm),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            [booking.clientName, booking.serviceName, _when]
                .where((s) => s.isNotEmpty)
                .join(' · '),
            style: const TextStyle(fontSize: 14, color: AppColors.ink),
          ),
          const SizedBox(height: AppSpacing.sm),
          // Constrained via Expanded — never a bare themed button in a Row.
          Row(
            children: [
              Expanded(
                child: OutlinedButton(
                  key: Key('decline-${booking.id}'),
                  onPressed: () => onDecline(booking.id),
                  style: OutlinedButton.styleFrom(
                    foregroundColor: AppColors.danger,
                    side: const BorderSide(color: AppColors.danger),
                  ),
                  child: const Text(AppStrings.homeDecline),
                ),
              ),
              const SizedBox(width: AppSpacing.sm),
              Expanded(
                child: FilledButton(
                  key: Key('confirm-${booking.id}'),
                  onPressed: () => onConfirm(booking.id),
                  child: const Text(AppStrings.homeConfirm),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
