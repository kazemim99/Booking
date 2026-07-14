import 'package:equatable/equatable.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../../config/routes/app_router.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/di/injection.dart';
import '../../../../core/utils/jalali_formatter.dart';
import '../../../../core/widgets/widgets.dart';
import '../../domain/entities/booking_summary.dart';
import '../../domain/repositories/bookings_repository.dart';

enum _DetailStatus { loading, loaded, error }

class _DetailState extends Equatable {
  final _DetailStatus status;
  final BookingSummary? booking;
  final String? errorMessage;

  const _DetailState({
    this.status = _DetailStatus.loading,
    this.booking,
    this.errorMessage,
  });

  @override
  List<Object?> get props => [status, booking, errorMessage];
}

class _DetailCubit extends Cubit<_DetailState> {
  final BookingsRepository repository;

  _DetailCubit(this.repository) : super(const _DetailState());

  /// The list endpoint's CustomerBookingDto is the only booking shape the
  /// app consumes; the detail is found in the user's own lists.
  Future<void> load(String bookingId) async {
    emit(const _DetailState());
    final results = await Future.wait([
      repository.getMyBookings(upcoming: true),
      repository.getMyBookings(upcoming: false),
    ]);

    for (final result in results) {
      final match = result.fold<BookingSummary?>(
        (_) => null,
        (bookings) =>
            bookings.where((b) => b.id == bookingId).firstOrNull,
      );
      if (match != null) {
        emit(_DetailState(status: _DetailStatus.loaded, booking: match));
        return;
      }
    }

    emit(const _DetailState(
      status: _DetailStatus.error,
      errorMessage: AppStrings.genericError,
    ));
  }
}

/// Appointment detail (deep-linkable at /appointments/:id, auth-gated by
/// the router).
class AppointmentDetailPage extends StatelessWidget {
  final String bookingId;

  const AppointmentDetailPage({super.key, required this.bookingId});

  @override
  Widget build(BuildContext context) {
    return BlocProvider(
      create: (_) => _DetailCubit(getIt())..load(bookingId),
      child: Scaffold(
        appBar: AppBar(title: const Text(AppStrings.appointmentsTitle)),
        body: BlocBuilder<_DetailCubit, _DetailState>(
          builder: (context, state) {
            return StateSwitcher(
              status: switch (state.status) {
                _DetailStatus.loading => ViewStatus.loading,
                _DetailStatus.loaded => ViewStatus.content,
                _DetailStatus.error => ViewStatus.error,
              },
              errorMessage: state.errorMessage,
              onRetry: () => context.read<_DetailCubit>().load(bookingId),
              skeleton: Padding(
                padding: const EdgeInsets.all(AppSpacing.md),
                child: SkeletonLoader.list(items: 1, itemHeight: 220),
              ),
              contentBuilder: (context) =>
                  _DetailContent(booking: state.booking!),
            );
          },
        ),
      ),
    );
  }
}

class _DetailContent extends StatelessWidget {
  final BookingSummary booking;

  const _DetailContent({required this.booking});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final status = StatusBadge.tryParse(booking.status);

    final rows = <(String, String)>[
      (AppStrings.bookingProvider, booking.providerName),
      (AppStrings.bookingService, booking.serviceName),
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
            '${booking.price.toStringAsFixed(0)} ${booking.currency}'.trim(),
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
                const SizedBox(height: AppSpacing.sm),
              ],
              for (final (label, value) in rows)
                Padding(
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
                          textAlign: TextAlign.left,
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
        AppButton.secondary(
          label: AppStrings.bookingProvider,
          icon: Icons.storefront_outlined,
          onPressed: () =>
              context.push(Routes.providerDetail(booking.providerId)),
        ),
      ],
    );
  }
}
