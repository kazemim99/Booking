import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../../config/routes/app_router.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/di/injection.dart';
import '../../../../core/utils/jalali_formatter.dart';
import '../../../../core/utils/person_name.dart';
import '../../../../core/utils/price_formatter.dart';
import '../../../../core/widgets/widgets.dart';
import '../../../reviews/domain/repositories/review_repository.dart';
import '../../../reviews/presentation/widgets/write_review_dialog.dart';
import '../../domain/entities/booking_summary.dart';
import '../bloc/appointment_detail_cubit.dart';
import 'reschedule_page.dart';

/// Appointment detail (deep-linkable at /appointments/:id, auth-gated by
/// the router). Opened from the list, the home "next booking" card and
/// notifications, so it offers the same actions as the list cards.
class AppointmentDetailPage extends StatelessWidget {
  final String bookingId;

  const AppointmentDetailPage({super.key, required this.bookingId});

  @override
  Widget build(BuildContext context) {
    return BlocProvider(
      create: (_) => AppointmentDetailCubit(getIt(), bookingId)..load(),
      child: Scaffold(
        appBar: AppBar(title: const Text(AppStrings.appointmentDetailTitle)),
        body: BlocConsumer<AppointmentDetailCubit, AppointmentDetailState>(
          listenWhen: (prev, next) =>
              next.notice != AppointmentDetailNotice.none &&
              prev.notice != next.notice,
          listener: (context, state) {
            switch (state.notice) {
              case AppointmentDetailNotice.cancelSuccess:
                AppSnackbar.success(context, AppStrings.cancelBookingSuccess);
              case AppointmentDetailNotice.cancelFailure:
                AppSnackbar.error(
                  context,
                  state.errorMessage ?? AppStrings.genericError,
                );
              case AppointmentDetailNotice.none:
                break;
            }
          },
          builder: (context, state) {
            return StateSwitcher(
              status: switch (state.status) {
                AppointmentDetailStatus.loading => ViewStatus.loading,
                AppointmentDetailStatus.loaded => ViewStatus.content,
                AppointmentDetailStatus.error => ViewStatus.error,
              },
              errorMessage: state.errorMessage,
              onRetry: () => context.read<AppointmentDetailCubit>().load(),
              skeleton: Padding(
                padding: const EdgeInsets.all(AppSpacing.md),
                child: SkeletonLoader.list(items: 1, itemHeight: 220),
              ),
              contentBuilder: (context) => _DetailContent(state: state),
            );
          },
        ),
      ),
    );
  }
}

class _DetailContent extends StatelessWidget {
  final AppointmentDetailState state;

  const _DetailContent({required this.state});

  BookingSummary get booking => state.booking!;

  Future<void> _writeReview(BuildContext context) async {
    final cubit = context.read<AppointmentDetailCubit>();
    final draft = await showWriteReviewDialog(context);
    if (draft == null || !context.mounted) return;

    final repository = getIt<ReviewRepository>();
    final result = await repository.createReview(
      bookingId: booking.id,
      rating: draft.rating,
      comment: draft.comment,
      dimensions: draft.dimensions,
    );
    if (!context.mounted) return;
    result.fold(
      (failure) => AppSnackbar.error(context, failure.message),
      (_) {
        cubit.reviewed();
        AppSnackbar.success(context, AppStrings.reviewSaved);
      },
    );
  }

  Future<void> _confirmCancel(BuildContext context) async {
    final cubit = context.read<AppointmentDetailCubit>();
    final confirmed = await ConfirmSheet.show(
      context: context,
      title: AppStrings.cancelBookingConfirmTitle,
      body: AppStrings.cancelBookingConfirmBody,
      confirmLabel: AppStrings.cancelBooking,
      destructive: true,
    );
    if (confirmed) await cubit.cancel();
  }

  Future<void> _reschedule(BuildContext context) async {
    final cubit = context.read<AppointmentDetailCubit>();
    // On the root navigator: rescheduling is a single-purpose task and covers the tab bar, as booking does.
    final newStartTime = await Navigator.of(context, rootNavigator: true).push<DateTime>(
      MaterialPageRoute(builder: (_) => ReschedulePage(booking: booking)),
    );
    if (newStartTime != null) await cubit.rescheduled(newStartTime);
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final status = StatusBadge.tryParse(booking.status);
    // Who does the work, when the booking names someone. A placeholder or a phone number is no name to show
    // (QA recording 2026-09-23 #8: the confirm step read «ارائه‌دهنده 9123135143»).
    final staffName = realFullNameOrNull(booking.staffName);

    final rows = <(String, String)>[
      (AppStrings.bookingProvider, booking.providerName),
      (AppStrings.bookingService, booking.serviceName),
      if (staffName != null) (AppStrings.bookingStaff, staffName),
      (
        AppStrings.bookingDate,
        JalaliFormatter.formatDate(booking.startTime),
      ),
      (
        AppStrings.bookingTime,
        JalaliFormatter.formatTime(booking.startTime),
      ),
      if (booking.durationMinutes > 0)
        (
          AppStrings.bookingDuration,
          JalaliFormatter.toPersianDigits('${booking.durationMinutes} دقیقه'),
        ),
      if (booking.price > 0)
        (
          AppStrings.bookingPrice,
          JalaliFormatter.toPersianDigits(
            PriceFormatter.format(booking.price.round()),
          ),
        ),
    ];

    return ListView(
      padding: const EdgeInsets.all(AppSpacing.md),
      children: [
        AppCard(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              if (status != null) ...[
                StatusBadge(status: status),
                // The badge alone did not say what a request is waiting for (QA recording 2026-09-23 #8).
                if (status == BookingStatus.pending) ...[
                  const SizedBox(height: AppSpacing.xs),
                  Text(
                    AppStrings.appointmentPendingExplanation,
                    key: const Key('appointment-pending-note'),
                    style: theme.textTheme.bodySmall?.copyWith(
                      color: theme.colorScheme.onSurfaceVariant,
                    ),
                  ),
                ],
                const SizedBox(height: AppSpacing.sm),
              ],
              for (final (label, value) in rows)
                Padding(
                  key: label == AppStrings.bookingStaff
                      ? const Key('appointment-staff')
                      : null,
                  padding:
                      const EdgeInsets.symmetric(vertical: AppSpacing.xs),
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(label, style: theme.textTheme.bodyMedium),
                      const Spacer(),
                      Expanded(
                        flex: 2,
                        child: Text(
                          value,
                          style: theme.textTheme.titleSmall,
                          textAlign: TextAlign.end,
                        ),
                      ),
                    ],
                  ),
                ),
              if (booking.cancellationReason?.isNotEmpty == true) ...[
                const SizedBox(height: AppSpacing.xs),
                Text(
                  booking.cancellationReason!,
                  style: theme.textTheme.bodySmall,
                ),
              ],
            ],
          ),
        ),
        const SizedBox(height: AppSpacing.md),
        // The same rules as the list cards: an active booking still ahead.
        if (booking.canReschedule) ...[
          AppButton(
            key: const Key('appointment-reschedule'),
            label: AppStrings.rescheduleBooking,
            icon: Icons.edit_calendar_outlined,
            onPressed: state.cancelling ? null : () => _reschedule(context),
          ),
          const SizedBox(height: AppSpacing.sm),
        ],
        if (booking.canRebook) ...[
          AppButton(
            key: const Key('appointment-book-again'),
            label: AppStrings.bookAgain,
            icon: Icons.replay,
            onPressed: () => context.push(Routes.bookingFlow(
              booking.providerId,
              serviceId: booking.serviceId.isEmpty ? null : booking.serviceId,
            )),
          ),
          const SizedBox(height: AppSpacing.sm),
        ],
        // Only a visit that happened can be reviewed, and only once — the
        // server checks the same, so offering it again would only earn a
        // rejection.
        if (state.canWriteReview) ...[
          AppButton.secondary(
            key: const Key('appointment-write-review'),
            label: AppStrings.reviewWriteAction,
            icon: Icons.star_outline,
            onPressed: () => _writeReview(context),
          ),
          const SizedBox(height: AppSpacing.sm),
        ],
        AppButton.secondary(
          key: const Key('appointment-view-salon'),
          label: AppStrings.appointmentViewSalon,
          icon: Icons.storefront_outlined,
          onPressed: () =>
              context.push(Routes.providerDetail(booking.providerId)),
        ),
        if (booking.canCancel) ...[
          const SizedBox(height: AppSpacing.sm),
          TextButton(
            key: const Key('appointment-cancel'),
            onPressed: state.cancelling ? null : () => _confirmCancel(context),
            style: TextButton.styleFrom(
              foregroundColor: theme.colorScheme.error,
              minimumSize: const Size.fromHeight(48),
            ),
            child: const Text(AppStrings.cancelBooking),
          ),
        ],
      ],
    );
  }
}
