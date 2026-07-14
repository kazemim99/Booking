import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../../config/routes/app_router.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/di/injection.dart';
import '../../../../core/utils/jalali_formatter.dart';
import '../../../../core/widgets/widgets.dart';
import '../../../auth/presentation/bloc/auth_bloc.dart';
import '../../../auth/presentation/bloc/auth_state.dart';
import '../../domain/entities/booking_summary.dart';
import '../bloc/appointments_bloc.dart';
import 'reschedule_page.dart';

/// Appointments tab: upcoming/past segmentation with status-driven cards,
/// cancel (confirm sheet, optimistic + rollback) and reschedule actions.
/// Guests see a login prompt in place.
class AppointmentsPage extends StatelessWidget {
  const AppointmentsPage({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<AuthBloc, AuthState>(
      buildWhen: (prev, next) =>
          next is Authenticated || next is Unauthenticated || next is LoggedOut,
      builder: (context, authState) {
        if (authState is! Authenticated) {
          return const _GuestPrompt();
        }
        return BlocProvider(
          create: (_) =>
              getIt<AppointmentsBloc>()..add(const AppointmentsRequested()),
          child: const _AppointmentsView(),
        );
      },
    );
  }
}

class _GuestPrompt extends StatelessWidget {
  const _GuestPrompt();

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: Padding(
            padding: const EdgeInsets.all(AppSpacing.lg),
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Container(
                  width: 96,
                  height: 96,
                  alignment: Alignment.center,
                  decoration: BoxDecoration(
                    color: theme.colorScheme.surface,
                    borderRadius: BorderRadius.circular(AppRadius.lg),
                  ),
                  child: Icon(
                    Icons.calendar_month_outlined,
                    size: 48,
                    color: theme.colorScheme.onSurfaceVariant,
                  ),
                ),
                const SizedBox(height: AppSpacing.lg),
                Text(
                  AppStrings.appointmentsGuestTitle,
                  style: theme.textTheme.headlineSmall,
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: AppSpacing.xs),
                Text(
                  AppStrings.appointmentsGuestSubtitle,
                  style: theme.textTheme.bodyMedium,
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: AppSpacing.xl),
                AppButton(
                  label: AppStrings.findProviders,
                  onPressed: () => context.go(Routes.explore),
                ),
                const SizedBox(height: AppSpacing.md),
                Text(
                  AppStrings.appointmentsGuestQuestion,
                  style: theme.textTheme.bodyMedium,
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: AppSpacing.xs),
                AppButton.secondary(
                  label: AppStrings.login,
                  onPressed: () => context.push(
                    '${Routes.login}?redirect='
                    '${Uri.encodeComponent(Routes.appointments)}',
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _AppointmentsView extends StatefulWidget {
  const _AppointmentsView();

  @override
  State<_AppointmentsView> createState() => _AppointmentsViewState();
}

class _AppointmentsViewState extends State<_AppointmentsView> {
  bool _showUpcoming = true;

  Future<void> _onRefresh() {
    final bloc = context.read<AppointmentsBloc>();
    bloc.add(const AppointmentsRequested());
    return bloc.stream
        .firstWhere((s) => s.status != AppointmentsStatus.loading)
        .then((_) {});
  }

  Future<void> _confirmCancel(
    BuildContext context,
    BookingSummary booking,
  ) async {
    final bloc = context.read<AppointmentsBloc>();
    final confirmed = await ConfirmSheet.show(
      context: context,
      title: AppStrings.cancelBookingConfirmTitle,
      body: AppStrings.cancelBookingConfirmBody,
      confirmLabel: AppStrings.cancelBooking,
      destructive: true,
    );
    if (confirmed) {
      bloc.add(AppointmentCancelled(booking));
    }
  }

  Future<void> _reschedule(
    BuildContext context,
    BookingSummary booking,
  ) async {
    final bloc = context.read<AppointmentsBloc>();
    final newStartTime = await Navigator.of(context).push<DateTime>(
      MaterialPageRoute(
        builder: (_) => ReschedulePage(booking: booking),
      ),
    );
    if (newStartTime != null) {
      bloc.add(AppointmentRescheduled(booking.id, newStartTime));
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text(AppStrings.appointmentsTitle)),
      body: BlocConsumer<AppointmentsBloc, AppointmentsState>(
        listener: (context, state) {
          switch (state.notice) {
            case AppointmentsNotice.cancelSuccess:
              AppSnackbar.success(context, AppStrings.cancelBookingSuccess);
            case AppointmentsNotice.cancelFailure:
              AppSnackbar.error(
                context,
                state.errorMessage ?? AppStrings.genericError,
              );
            case AppointmentsNotice.none:
              break;
          }
        },
        builder: (context, state) {
          return Column(
            children: [
              Padding(
                padding: const EdgeInsets.all(AppSpacing.md),
                child: SegmentedButton<bool>(
                  segments: const [
                    ButtonSegment(
                      value: true,
                      label: Text(AppStrings.appointmentsUpcoming),
                    ),
                    ButtonSegment(
                      value: false,
                      label: Text(AppStrings.appointmentsPast),
                    ),
                  ],
                  selected: {_showUpcoming},
                  onSelectionChanged: (selection) =>
                      setState(() => _showUpcoming = selection.first),
                ),
              ),
              Expanded(
                child: StateSwitcher(
                  status: switch (state.status) {
                    AppointmentsStatus.loading => ViewStatus.loading,
                    AppointmentsStatus.error => ViewStatus.error,
                    AppointmentsStatus.empty => ViewStatus.empty,
                    AppointmentsStatus.loaded => ViewStatus.content,
                  },
                  errorMessage: state.errorMessage,
                  onRetry: () => context
                      .read<AppointmentsBloc>()
                      .add(const AppointmentsRequested()),
                  skeleton: Padding(
                    padding:
                        const EdgeInsets.symmetric(horizontal: AppSpacing.md),
                    child: SkeletonLoader.list(items: 3, itemHeight: 128),
                  ),
                  empty: EmptyState(
                    icon: Icons.event_available_outlined,
                    title: AppStrings.appointmentsEmptyTitle,
                    subtitle: AppStrings.appointmentsEmptySubtitle,
                    ctaLabel: AppStrings.findProvider,
                    onCta: () => context.go(Routes.explore),
                  ),
                  contentBuilder: (context) {
                    final bookings =
                        _showUpcoming ? state.upcoming : state.past;
                    if (bookings.isEmpty) {
                      return EmptyState(
                        icon: Icons.event_available_outlined,
                        title: AppStrings.appointmentsEmptyTitle,
                        subtitle: AppStrings.appointmentsEmptySubtitle,
                        ctaLabel: AppStrings.findProvider,
                        onCta: () => context.go(Routes.explore),
                      );
                    }
                    return RefreshIndicator(
                      onRefresh: _onRefresh,
                      child: ListView.separated(
                        physics: const AlwaysScrollableScrollPhysics(),
                        padding: const EdgeInsets.fromLTRB(
                          AppSpacing.md,
                          0,
                          AppSpacing.md,
                          AppSpacing.md,
                        ),
                        itemCount: bookings.length,
                        separatorBuilder: (_, __) =>
                            const SizedBox(height: AppSpacing.sm),
                        itemBuilder: (context, index) => _BookingCard(
                          booking: bookings[index],
                          onCancel: () =>
                              _confirmCancel(context, bookings[index]),
                          onReschedule: () =>
                              _reschedule(context, bookings[index]),
                        ),
                      ),
                    );
                  },
                ),
              ),
            ],
          );
        },
      ),
    );
  }
}

class _BookingCard extends StatelessWidget {
  final BookingSummary booking;
  final VoidCallback onCancel;
  final VoidCallback onReschedule;

  const _BookingCard({
    required this.booking,
    required this.onCancel,
    required this.onReschedule,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final status = StatusBadge.tryParse(booking.status);
    final when = JalaliFormatter.formatDateTime(booking.startTime);

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
                  style: theme.textTheme.titleSmall,
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
          const SizedBox(height: AppSpacing.xs),
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
          if (booking.canCancel || booking.canReschedule) ...[
            const SizedBox(height: AppSpacing.xs),
            Row(
              children: [
                if (booking.canReschedule)
                  AppButton.text(
                    label: AppStrings.rescheduleBooking,
                    onPressed: onReschedule,
                  ),
                if (booking.canCancel)
                  TextButton(
                    onPressed: onCancel,
                    style: TextButton.styleFrom(
                      foregroundColor: theme.colorScheme.error,
                    ),
                    child: const Text(AppStrings.cancelBooking),
                  ),
              ],
            ),
          ],
        ],
      ),
    );
  }
}
