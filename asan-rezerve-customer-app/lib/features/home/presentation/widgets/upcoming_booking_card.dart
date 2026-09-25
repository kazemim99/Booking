import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../config/routes/app_router.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/utils/jalali_formatter.dart';
import '../../../../core/widgets/widgets.dart';
import '../../domain/entities/upcoming_booking.dart';

/// The user's next booking, surfaced at the top of home — the highest-intent
/// content for a returning user. Tapping opens the appointment detail.
class UpcomingBookingCard extends StatelessWidget {
  final UpcomingBooking booking;

  const UpcomingBookingCard({super.key, required this.booking});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final status = StatusBadge.tryParse(booking.status);
    final when = JalaliFormatter.formatDateTime(booking.dateTime);

    return AppCard(
      semanticLabel:
          '${booking.serviceName}، ${booking.providerName}، $when'
          '${status != null ? '، ${StatusBadge(status: status).label}' : ''}',
      onTap: () => context.go(Routes.appointmentDetail(booking.id)),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  booking.serviceName,
                  style: theme.textTheme.titleMedium,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
              ),
              if (status != null) StatusBadge(status: status),
            ],
          ),
          const SizedBox(height: AppSpacing.xxs),
          Text(
            booking.providerName,
            style: theme.textTheme.bodyMedium,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
          ),
          const SizedBox(height: AppSpacing.sm),
          Row(
            children: [
              Icon(
                Icons.event_outlined,
                size: 16,
                color: theme.colorScheme.primary,
              ),
              const SizedBox(width: AppSpacing.xxs),
              Text(
                when,
                style: theme.textTheme.titleSmall?.copyWith(
                  color: theme.colorScheme.primary,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
