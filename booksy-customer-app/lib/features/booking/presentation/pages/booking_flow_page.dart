import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../../config/feature_flags.dart';
import '../../../../config/routes/app_router.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/di/injection.dart';
import '../../../../core/utils/jalali_formatter.dart';
import '../../../../core/widgets/widgets.dart';
import '../../../auth/presentation/bloc/auth_bloc.dart';
import '../../../auth/presentation/bloc/auth_state.dart';
import '../bloc/booking_bloc.dart';
import '../widgets/service_selection_step.dart';
import '../widgets/slot_picker.dart';

/// Stepped booking flow: service → staff (auto-skipped for single-staff
/// providers) → Jalali date/slot picker → confirmation. The bloc is an
/// app-scoped singleton so selections survive a login round-trip at the
/// confirmation gate.
class BookingFlowPage extends StatefulWidget {
  final String providerId;

  const BookingFlowPage({super.key, required this.providerId});

  @override
  State<BookingFlowPage> createState() => _BookingFlowPageState();
}

class _BookingFlowPageState extends State<BookingFlowPage> {
  late final BookingBloc _bloc;

  @override
  void initState() {
    super.initState();
    _bloc = getIt<BookingBloc>();
    _bloc.add(BookingStarted(widget.providerId));
  }

  String get _stepTitle {
    switch (_bloc.state.step) {
      case BookingStep.service:
        return AppStrings.bookingSelectServices;
      case BookingStep.staff:
        return AppStrings.bookingSelectStaff;
      case BookingStep.time:
        return AppStrings.bookingSelectTime;
      case BookingStep.confirm:
        return AppStrings.bookingConfirmTitle;
    }
  }

  @override
  Widget build(BuildContext context) {
    return BlocProvider.value(
      value: _bloc,
      child: BlocConsumer<BookingBloc, BookingState>(
        listener: (context, state) {
          if (state.submitStatus == SubmitStatus.slotTaken) {
            AppSnackbar.error(
              context,
              state.submitError ?? AppStrings.bookingSlotTaken,
            );
          } else if (state.submitStatus == SubmitStatus.error &&
              state.submitError != null) {
            AppSnackbar.error(context, state.submitError!);
          }
        },
        builder: (context, state) {
          if (state.submitStatus == SubmitStatus.success) {
            return _SuccessView(bloc: _bloc);
          }

          final steps = state.visibleSteps;
          final stepIndex = steps.indexOf(state.step).clamp(0, steps.length - 1);

          return PopScope(
            canPop: stepIndex == 0,
            onPopInvokedWithResult: (didPop, _) {
              if (!didPop) _bloc.add(const BookingStepBack());
            },
            child: Scaffold(
              appBar: AppBar(
                title: Text(_stepTitle),
                bottom: PreferredSize(
                  preferredSize: const Size.fromHeight(4),
                  child: Semantics(
                    label:
                        'مرحله ${JalaliFormatter.toPersianDigits('${stepIndex + 1}')} از ${JalaliFormatter.toPersianDigits('${steps.length}')}',
                    child: LinearProgressIndicator(
                      value: (stepIndex + 1) / steps.length,
                    ),
                  ),
                ),
              ),
              body: switch (state.providerStatus) {
                BookingProviderStatus.loading => Padding(
                    padding: const EdgeInsets.all(AppSpacing.md),
                    child: SkeletonLoader.list(items: 4, itemHeight: 80),
                  ),
                BookingProviderStatus.error => ErrorState(
                    message: state.providerError,
                    onRetry: () =>
                        _bloc.add(BookingStarted(widget.providerId)),
                  ),
                BookingProviderStatus.loaded => switch (state.step) {
                    BookingStep.service =>
                      ServiceSelectionStep(state: state),
                    BookingStep.staff => _StaffStep(state: state),
                    BookingStep.time => _TimeStep(state: state),
                    BookingStep.confirm => _ConfirmStep(
                        state: state,
                        providerId: widget.providerId,
                      ),
                  },
              },
            ),
          );
        },
      ),
    );
  }
}

class _StaffStep extends StatelessWidget {
  final BookingState state;

  const _StaffStep({required this.state});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final staff = state.provider?.activeStaff ?? [];
    return ListView(
      padding: const EdgeInsets.all(AppSpacing.md),
      children: [
        AppCard(
          onTap: () =>
              context.read<BookingBloc>().add(const BookingStaffSelected(null)),
          semanticLabel: AppStrings.bookingAnyStaff,
          child: Row(
            children: [
              Icon(Icons.groups_outlined, color: theme.colorScheme.primary),
              const SizedBox(width: AppSpacing.sm),
              Expanded(
                child: Text(
                  AppStrings.bookingAnyStaff,
                  style: theme.textTheme.titleSmall,
                ),
              ),
              if (state.anyStaff)
                Icon(
                  Icons.check_circle,
                  size: 20,
                  color: theme.colorScheme.primary,
                ),
            ],
          ),
        ),
        const SizedBox(height: AppSpacing.sm),
        ...staff.map(
          (member) => Padding(
            padding: const EdgeInsets.only(bottom: AppSpacing.sm),
            child: AppCard(
              onTap: () => context
                  .read<BookingBloc>()
                  .add(BookingStaffSelected(member)),
              semanticLabel: member.name,
              child: Row(
                children: [
                  CircleAvatar(
                    radius: 20,
                    backgroundColor:
                        theme.colorScheme.primary.withValues(alpha: 0.1),
                    child: Icon(
                      Icons.person_outline,
                      color: theme.colorScheme.primary,
                    ),
                  ),
                  const SizedBox(width: AppSpacing.sm),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(member.name, style: theme.textTheme.titleSmall),
                        if (member.role?.isNotEmpty == true)
                          Text(member.role!,
                              style: theme.textTheme.bodySmall),
                      ],
                    ),
                  ),
                  if (state.staff?.id == member.id)
                    Icon(
                      Icons.check_circle,
                      size: 20,
                      color: theme.colorScheme.primary,
                    ),
                ],
              ),
            ),
          ),
        ),
      ],
    );
  }
}

class _TimeStep extends StatelessWidget {
  final BookingState state;

  const _TimeStep({required this.state});

  @override
  Widget build(BuildContext context) {
    final bloc = context.read<BookingBloc>();
    final selected = state.date ?? DateTime.now();
    return SlotPicker(
      selectedDate: selected,
      onDateSelected: (day) => bloc.add(BookingDateSelected(day)),
      status: switch (state.slotsStatus) {
        SlotsStatus.initial || SlotsStatus.loading => SlotPickerStatus.loading,
        SlotsStatus.error => SlotPickerStatus.error,
        SlotsStatus.loaded => SlotPickerStatus.loaded,
      },
      slots: state.slots,
      selectedSlot: state.slot,
      onSlotSelected: (slot) => bloc.add(BookingSlotSelected(slot)),
      onRetry: () => bloc.add(BookingDateSelected(selected)),
    );
  }
}

class _ConfirmStep extends StatelessWidget {
  final BookingState state;
  final String providerId;

  const _ConfirmStep({required this.state, required this.providerId});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final services = state.services;
    final slot = state.slot;
    if (services.isEmpty || slot == null) {
      return const SizedBox.shrink();
    }

    final rows = <(String, String)>[
      (AppStrings.bookingProvider, state.provider?.businessName ?? ''),
      (
        // Plural label once the visit bundles more than one service.
        services.length > 1
            ? AppStrings.servicesTitle
            : AppStrings.bookingService,
        services.map((s) => s.name).join('، '),
      ),
      (
        AppStrings.bookingStaff,
        state.staff?.name ?? slot.staffName ?? AppStrings.bookingAnyStaff,
      ),
      (AppStrings.bookingDate, JalaliFormatter.formatDate(slot.startTime)),
      (AppStrings.bookingTime, JalaliFormatter.formatTime(slot.startTime)),
      (
        AppStrings.bookingDuration,
        JalaliFormatter.toPersianDigits(
          '${state.totalDurationMinutes} دقیقه',
        ),
      ),
      (
        AppStrings.bookingPrice,
        JalaliFormatter.toPersianDigits(
          '${state.totalPrice.toStringAsFixed(0)} ${state.currency}'.trim(),
        ),
      ),
    ];

    return Padding(
      padding: const EdgeInsets.all(AppSpacing.md),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Expanded(
            child: AppCard(
              child: Column(
                children: [
                  for (final (label, value) in rows)
                    Padding(
                      padding: const EdgeInsets.symmetric(
                        vertical: AppSpacing.xs,
                      ),
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
                ],
              ),
            ),
          ),
          const SizedBox(height: AppSpacing.md),
          AppButton(
            label: AppStrings.bookingConfirmCta,
            loading: state.submitStatus == SubmitStatus.submitting,
            onPressed: () {
              final authState = context.read<AuthBloc>().state;
              if (authState is! Authenticated) {
                // Point-of-need login: selections live in the singleton
                // bloc, so the round-trip lands back here intact.
                final target =
                    Uri.encodeComponent(Routes.bookingFlow(providerId));
                context.push('${Routes.login}?redirect=$target');
                return;
              }
              context.read<BookingBloc>().add(const BookingSubmitted());
            },
          ),
        ],
      ),
    );
  }
}

class _SuccessView extends StatelessWidget {
  final BookingBloc bloc;

  const _SuccessView({required this.bloc});

  /// The checkout location for the just-created booking, or null when checkout must not be offered.
  ///
  /// Gated by [FeatureFlags.checkoutEnabled] so the journey stays dark until it clears its release gates. The flag
  /// only controls whether payment is *offered*: with it off the booking is still created and the backend still
  /// enforces its deposit gate, so this can never bypass payment — it just doesn't collect it in-app yet.
  String? get _checkoutTarget {
    if (!FeatureFlags.checkoutEnabled) return null;
    final bookingId = bloc.state.bookingId;
    final providerId = bloc.state.providerId;
    if (bookingId == null || bookingId.isEmpty) return null;
    if (providerId == null || providerId.isEmpty) return null;
    return Routes.checkoutFor(bookingId, providerId);
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(AppSpacing.lg),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Icon(
                Icons.check_circle_outline,
                size: 80,
                color: theme.colorScheme.secondary,
              ),
              const SizedBox(height: AppSpacing.lg),
              Text(
                AppStrings.bookingSuccessTitle,
                style: theme.textTheme.headlineSmall,
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: AppSpacing.xs),
              Text(
                AppStrings.bookingSuccessSubtitle,
                style: theme.textTheme.bodyMedium,
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: AppSpacing.xl),
              // Deposit coupling (create-then-pay). The booking already exists and holds the slot; the server
              // decides whether a deposit is owed, so we simply offer to continue into checkout and let it ask.
              // Nothing here can confirm a booking — the backend gate does that only on a verified deposit.
              if (_checkoutTarget != null)
                Padding(
                  padding: const EdgeInsets.only(bottom: AppSpacing.xs),
                  child: AppButton(
                    key: const Key('booking-pay-deposit-button'),
                    label: AppStrings.checkoutPayCta,
                    onPressed: () {
                      bloc.add(const BookingReset());
                      context.push(_checkoutTarget!);
                    },
                  ),
                ),
              AppButton(
                key: const Key('booking-view-appointments-button'),
                label: AppStrings.bookingViewAppointments,
                variant: _checkoutTarget == null
                    ? AppButtonVariant.primary
                    : AppButtonVariant.secondary,
                onPressed: () {
                  bloc.add(const BookingReset());
                  context.go(Routes.appointments);
                },
              ),
              const SizedBox(height: AppSpacing.xs),
              AppButton.secondary(
                label: AppStrings.back,
                onPressed: () {
                  bloc.add(const BookingReset());
                  context.go(Routes.home);
                },
              ),
            ],
          ),
        ),
      ),
    );
  }
}
