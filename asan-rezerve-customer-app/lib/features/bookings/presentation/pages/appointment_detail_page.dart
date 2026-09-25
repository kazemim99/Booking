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
import '../../../reviews/domain/entities/review.dart';
import '../../../reviews/presentation/write_review_flow.dart';
import '../../domain/entities/booking_summary.dart';
import '../bloc/appointment_detail_cubit.dart';
import '../bloc/reschedule_cubit.dart';
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
    final outcome = await Navigator.of(context, rootNavigator: true).push<RescheduleOutcome>(
      MaterialPageRoute(builder: (_) => ReschedulePage(booking: booking)),
    );
    // The server moved the visit to a new booking; the screen follows it.
    if (outcome != null) {
      await cubit.rescheduled(outcome.newStartTime, newBookingId: outcome.newBookingId);
    }
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
        // After a visit the review is the first thing asked for; before the
        // salon has marked it done, the page says so instead of just lacking it.
        _ReviewSection(state: state),
        // The same rules as the list cards: an active booking still ahead.
        if (booking.canReschedule) ...[
          AppButton(
            key: const Key('appointment-reschedule'),
            label: AppStrings.rescheduleBooking,
            icon: Icons.edit_calendar_outlined,
            // Inside the salon's window it is disabled, and says why right here (QA 2026-09-24).
            onPressed: state.cancelling || booking.rescheduleBlockedReason != null
                ? null
                : () => _reschedule(context),
          ),
          if (booking.rescheduleBlockedReason != null)
            Padding(
              padding: const EdgeInsets.only(top: AppSpacing.xs),
              child: Text(
                booking.rescheduleBlockedReason!,
                style: Theme.of(context).textTheme.bodySmall?.copyWith(
                      color: Theme.of(context).colorScheme.onSurfaceVariant,
                    ),
              ),
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

/// Where this visit's review stands, as one block: asked for (with the stars
/// right there), written and waiting or published, or not possible yet and why.
/// Nothing at all for a visit that is still ahead, cancelled or missed.
class _ReviewSection extends StatelessWidget {
  final AppointmentDetailState state;

  const _ReviewSection({required this.state});

  BookingSummary get booking => state.booking!;

  Future<void> _write(BuildContext context, {double? rating}) async {
    final cubit = context.read<AppointmentDetailCubit>();
    final saved = await writeReviewForBooking(
      context,
      bookingId: booking.id,
      subject: reviewSubject(booking.providerName, booking.serviceName),
      rating: rating,
    );
    if (saved) cubit.reviewed();
  }

  /// The salon's one review, written from this visit or another, changed from here.
  Future<void> _edit(BuildContext context) async {
    final cubit = context.read<AppointmentDetailCubit>();
    final saved = await editReviewForBooking(
      context,
      reviewId: booking.reviewId!,
      subject: reviewSubject(booking.providerName, booking.serviceName),
    );
    // An edit goes back to approval, like a new review.
    if (saved) cubit.reviewed();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final muted = theme.textTheme.bodySmall
        ?.copyWith(color: theme.colorScheme.onSurfaceVariant);

    if (state.canWriteReview) {
      return Padding(
        padding: const EdgeInsets.only(bottom: AppSpacing.sm),
        child: AppCard(
          key: const Key('appointment-review-prompt'),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(AppStrings.reviewPromptTitle,
                  style: theme.textTheme.titleSmall),
              const SizedBox(height: AppSpacing.xxs),
              Text(AppStrings.reviewPromptSubtitle, style: muted),
              const SizedBox(height: AppSpacing.xs),
              // Tapping a star is the review started: the dialog opens with it chosen.
              Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  for (var star = 1; star <= 5; star++)
                    IconButton(
                      key: Key('appointment-quick-star-$star'),
                      tooltip: AppStrings.reviewStarLabel(
                          JalaliFormatter.toPersianDigits('$star')),
                      iconSize: 32,
                      constraints:
                          const BoxConstraints(minWidth: 48, minHeight: 48),
                      icon: Icon(Icons.star_outline_rounded,
                          color: theme.colorScheme.primary),
                      onPressed: () => _write(context, rating: star.toDouble()),
                    ),
                ],
              ),
              const SizedBox(height: AppSpacing.xs),
              AppButton(
                key: const Key('appointment-write-review'),
                label: AppStrings.reviewWriteAction,
                icon: Icons.rate_review_outlined,
                onPressed: () => _write(context),
              ),
            ],
          ),
        ),
      );
    }

    final status = booking.reviewStatus ??
        (state.reviewed ? ReviewModerationStatus.pending : null);
    if (status != null) {
      final (icon, color, line) = switch (status) {
        ReviewModerationStatus.pending => (
            Icons.hourglass_top_rounded,
            theme.colorScheme.tertiary,
            AppStrings.reviewSubmittedPending
          ),
        ReviewModerationStatus.published => (
            Icons.check_circle_outline_rounded,
            theme.colorScheme.primary,
            AppStrings.reviewSubmittedPublished
          ),
        ReviewModerationStatus.rejected => (
            Icons.info_outline_rounded,
            theme.colorScheme.error,
            AppStrings.reviewSubmittedRejected
          ),
        ReviewModerationStatus.hidden => (
            Icons.visibility_off_outlined,
            theme.colorScheme.onSurfaceVariant,
            AppStrings.reviewSubmittedHidden
          ),
      };
      return Padding(
        padding: const EdgeInsets.only(bottom: AppSpacing.sm),
        child: AppCard(
          key: const Key('appointment-review-status'),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Icon(icon, color: color),
              const SizedBox(width: AppSpacing.sm),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                        booking.reviewFromOtherVisit
                            ? AppStrings.reviewAlreadyForSalon
                            : AppStrings.reviewSubmittedTitle,
                        key: const Key('appointment-review-status-title'),
                        style: theme.textTheme.titleSmall),
                    const SizedBox(height: AppSpacing.xxs),
                    Text(line, style: muted),
                    Wrap(
                      children: [
                        // One review per salon: while it may still be
                        // changed, this visit offers the edit, never a second
                        // review (reviews-and-reschedule-round2 item 8).
                        if (booking.canEditReview)
                          AppButton.text(
                            key: const Key('appointment-edit-review'),
                            label: AppStrings.reviewEditExisting,
                            icon: Icons.edit_outlined,
                            onPressed: () => _edit(context),
                          ),
                        AppButton.text(
                          key: const Key('appointment-my-reviews'),
                          label: AppStrings.myReviewsTitle,
                          onPressed: () => context.push(Routes.myReviews),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      );
    }

    // The visit is over and the salon has not marked it done: the action is
    // there, disabled, with what will unlock it — the same way «تغییر زمان»
    // says why it cannot be used.
    if (booking.reviewBlockedReason case final reason?) {
      return Padding(
        padding: const EdgeInsets.only(bottom: AppSpacing.sm),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const AppButton.secondary(
              key: Key('appointment-write-review'),
              label: AppStrings.reviewWriteAction,
              icon: Icons.rate_review_outlined,
              onPressed: null,
            ),
            Padding(
              padding: const EdgeInsets.only(top: AppSpacing.xs),
              child: Text(reason,
                  key: const Key('appointment-review-blocked'), style: muted),
            ),
          ],
        ),
      );
    }

    return const SizedBox.shrink();
  }
}
