import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../../config/feature_flags.dart';
import '../../../../config/routes/app_router.dart';
import '../../../../config/theme/app_colors.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/utils/person_name.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/di/injection.dart';
import '../../../../core/network/connectivity_service.dart';
import '../../../../core/utils/jalali_formatter.dart';
import '../../../../core/utils/person_name.dart';
import '../../../../core/widgets/widgets.dart';
import '../../../auth/presentation/bloc/auth_bloc.dart';
import '../../../auth/presentation/bloc/auth_state.dart';
import '../../../profile/presentation/bloc/profile_cubit.dart';
import '../../domain/business_days.dart';
import '../../domain/entities/booking_entities.dart';
import '../bloc/booking_bloc.dart';
import '../widgets/booking_name_sheet.dart';
import '../widgets/service_selection_step.dart';
import '../widgets/slot_picker.dart';
import '../../../../core/utils/price_formatter.dart';

/// Stepped booking flow: service → staff (auto-skipped for single-staff
/// providers) → Jalali date/slot picker → confirmation. The bloc is an
/// app-scoped singleton so selections survive a login round-trip at the
/// confirmation gate.
class BookingFlowPage extends StatefulWidget {
  final String providerId;

  /// A service to start with (tapped on the salon's profile, or a past visit booked again); null starts empty.
  final String? initialServiceId;

  /// The bloc to drive; the app-scoped singleton when null. Tests pass their own.
  final BookingBloc? bloc;

  /// Drives the offline banner. The wizard covers the tab shell, whose banner it would otherwise hide; the router
  /// passes the app's service, and a page built without one shows no banner.
  final ConnectivityService? connectivity;

  /// Saves the name asked for at the confirm step; the page makes its own when null. Tests pass their own.
  final ProfileCubit? profileCubit;

  const BookingFlowPage({
    super.key,
    required this.providerId,
    this.initialServiceId,
    this.bloc,
    this.connectivity,
    this.profileCubit,
  });

  @override
  State<BookingFlowPage> createState() => _BookingFlowPageState();
}

class _BookingFlowPageState extends State<BookingFlowPage> {
  late final BookingBloc _bloc;

  @override
  void initState() {
    super.initState();
    _bloc = widget.bloc ?? getIt<BookingBloc>();
    _bloc.add(_started);
  }

  /// A service chosen on the salon's profile starts the flow with it (the bloc ignores an id the salon lacks).
  BookingStarted get _started =>
      BookingStarted(widget.providerId, serviceId: widget.initialServiceId);

  /// The flow as it stood when a booking was created on this page. The success buttons reset the singleton bloc
  /// before navigating away, so the success screen keeps showing this instead of flashing an empty flow. Set from
  /// the listener, which only hears changes after the page opened: a success left in the app-scoped bloc by an
  /// earlier visit is not latched, and [BookingStarted] clears it.
  BookingState? _submitted;

  /// [child] under the offline banner, when the page has a connectivity service to follow.
  Widget _offlineAware(Widget child) {
    final connectivity = widget.connectivity;
    return connectivity == null ? child : OfflineBanner(connectivity: connectivity, child: child);
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
          if (state.submitStatus == SubmitStatus.success) {
            setState(() => _submitted = state);
          } else if (state.submitStatus == SubmitStatus.slotTaken) {
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
          // Only a booking made on this page: the listener latches it before this rebuild, and a success left in the
          // app-scoped bloc by an earlier visit must not flash as this visit's.
          final submitted = _submitted;
          if (submitted != null) {
            return _SuccessView(bloc: _bloc, booking: submitted);
          }

          final theme = Theme.of(context);
          final onAppBar = theme.appBarTheme.foregroundColor ??
              theme.colorScheme.onPrimary;
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
                    // Explicit colours: the theme's fill is the app bar's own blue (1.00:1) and its track fell back to
                    // the green accent, so the unfilled part read as the progress. The bar's foreground on a faint
                    // track of the same colour reads on the blue at every step.
                    child: LinearProgressIndicator(
                      value: (stepIndex + 1) / steps.length,
                      minHeight: 4,
                      color: onAppBar,
                      backgroundColor: onAppBar.withValues(alpha: 0.3),
                    ),
                  ),
                ),
              ),
              body: _offlineAware(switch (state.providerStatus) {
                BookingProviderStatus.loading => Padding(
                    padding: const EdgeInsets.all(AppSpacing.md),
                    child: SkeletonLoader.list(items: 4, itemHeight: 80),
                  ),
                BookingProviderStatus.error => ErrorState(
                    message: state.providerError,
                    onRetry: () => _bloc.add(_started),
                  ),
                BookingProviderStatus.loaded => switch (state.step) {
                    BookingStep.service =>
                      ServiceSelectionStep(state: state),
                    BookingStep.staff => _StaffStep(state: state),
                    BookingStep.time => _TimeStep(state: state),
                    BookingStep.confirm => _ConfirmStep(
                        state: state,
                        providerId: widget.providerId,
                        profileCubit: widget.profileCubit,
                      ),
                  },
              }),
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
    // The bloc searches for the first day with free times as the step opens and sets the date; until then the strip
    // shows today by the bloc's clock.
    final today = bloc.today;
    final selected = state.date ?? today;
    final notice = state.freeDayNotice;
    return SlotPicker(
      today: today,
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
      emptyReason: state.slotsReason,
      // Today plus the salon's window: the server refuses later days, so offering them is a dead end.
      daysToShow: (state.provider?.maxAdvanceBookingDays ?? 7) + 1,
      closedWeekdays: BusinessDays.closedWeekdays(
        state.provider?.businessHours ?? const [],
      ),
      notice: switch (notice) {
        FreeDayNotice.movedFromToday => AppStrings.bookingMovedFromToday,
        FreeDayNotice.movedFromPickedDay => AppStrings.bookingMovedFromPickedDay,
        FreeDayNotice.noneInWindow => AppStrings.bookingNoFreeDayInWindow,
        null => null,
      },
      // Once a search has found nothing up to the end of the window, the button would only repeat it.
      onFindNextFreeDay: notice == FreeDayNotice.noneInWindow
          ? null
          : () => bloc.add(const BookingNextFreeDayRequested()),
    );
  }
}

/// Label/value rows of a booking, values aligned to the row's end (the left edge in RTL).
class _SummaryRows extends StatelessWidget {
  final List<(String, String)> rows;

  const _SummaryRows({required this.rows});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Column(
      children: [
        for (final (label, value) in rows)
          Padding(
            padding: const EdgeInsets.symmetric(vertical: AppSpacing.xxs),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(label, style: theme.textTheme.bodyMedium),
                const SizedBox(width: AppSpacing.md),
                Expanded(
                  child: Text(
                    value,
                    style: theme.textTheme.titleSmall,
                    textAlign: TextAlign.end,
                  ),
                ),
              ],
            ),
          ),
      ],
    );
  }
}

/// Who does the visit, as the confirm step names them: the chosen member, else the one the slot is held by, else
/// "anyone". Never a placeholder or a phone number — production QA 2026-09-23 found the salon's owner named
/// «ارائه‌دهنده 9123135143» here — so a person with no real name is named by the salon, as the server does.
String _staffLabel(BookingState state, TimeSlot slot) {
  final named = state.staff?.name ?? slot.staffName;
  if (named == null) return AppStrings.bookingAnyStaff;
  return personNameOrNull(named) ?? state.provider?.businessName ?? AppStrings.bookingAnyStaff;
}

/// Salon, service(s), date and time of a visit — the part of the summary both the confirm and success screens show.
List<(String, String)> _visitRows(BookingState state, TimeSlot slot) => [
      (AppStrings.bookingProvider, state.provider?.businessName ?? ''),
      (
        // Plural label once the visit bundles more than one service.
        state.services.length > 1
            ? AppStrings.servicesTitle
            : AppStrings.bookingService,
        state.services.map((s) => s.name).join('، '),
      ),
      (AppStrings.bookingDate, JalaliFormatter.formatDate(slot.startTime)),
      (AppStrings.bookingTime, JalaliFormatter.formatTime(slot.startTime)),
    ];

/// A tinted note: what the customer should expect after this screen.
class _InfoNote extends StatelessWidget {
  final String? title;
  final String body;

  const _InfoNote({this.title, required this.body});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Container(
      padding: const EdgeInsets.all(AppSpacing.sm),
      decoration: BoxDecoration(
        color: AppColors.infoTint,
        borderRadius: BorderRadius.circular(AppRadius.md),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Icon(
            Icons.info_outline,
            size: AppIconSize.action,
            color: AppColors.infoText,
          ),
          const SizedBox(width: AppSpacing.xs),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                if (title != null) ...[
                  Text(
                    title!,
                    style: theme.textTheme.titleSmall?.copyWith(
                      color: AppColors.infoText,
                    ),
                  ),
                  const SizedBox(height: AppSpacing.xxs),
                ],
                Text(
                  body,
                  style: theme.textTheme.bodySmall?.copyWith(
                    color: AppColors.infoText,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _ConfirmStep extends StatelessWidget {
  final BookingState state;
  final String providerId;
  final ProfileCubit? profileCubit;

  const _ConfirmStep({
    required this.state,
    required this.providerId,
    this.profileCubit,
  });

  /// A guest signs in first; a customer still named by the OTP placeholder gives a first and last name — the salon
  /// saw «مشتری 9384444636» on the booking (QA recording 2026-09-23 #9). Here there is no skip: closing the sheet
  /// books nothing. The name page after sign-up stays skippable; only a booking needs the name.
  Future<void> _confirm(BuildContext context) async {
    final authState = context.read<AuthBloc>().state;
    if (authState is! Authenticated) {
      // Point-of-need login: selections live in the singleton
      // bloc, so the round-trip lands back here intact.
      final target = Uri.encodeComponent(Routes.bookingFlow(providerId));
      context.push('${Routes.login}?redirect=$target');
      return;
    }
    final bookings = context.read<BookingBloc>();
    final user = authState.session.user;
    // First AND last name before a salon receives the request (QA 2026-09-23).
    if (!hasFullName(user.firstName, user.lastName)) {
      final named = await BookingNameSheet.show(context, cubit: profileCubit);
      if (!named) return;
    }
    bookings.add(const BookingSubmitted());
  }

  @override
  Widget build(BuildContext context) {
    final services = state.services;
    final slot = state.slot;
    if (services.isEmpty || slot == null) {
      return const SizedBox.shrink();
    }

    final rows = <(String, String)>[
      ..._visitRows(state, slot),
      (AppStrings.bookingStaff, _staffLabel(state, slot)),
      (
        AppStrings.bookingDuration,
        JalaliFormatter.toPersianDigits(
          '${state.totalDurationMinutes} دقیقه',
        ),
      ),
      (
        AppStrings.bookingPrice,
        JalaliFormatter.toPersianDigits(
          PriceFormatter.format(state.totalPrice.round()),
        ),
      ),
    ];

    // A compact card that scrolls with the note under it, and the button pinned below — the card used to stretch to
    // fill the screen, mostly empty.
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Expanded(
          child: ListView(
            padding: const EdgeInsets.all(AppSpacing.md),
            children: [
              AppCard(child: _SummaryRows(rows: rows)),
              const SizedBox(height: AppSpacing.md),
              const _InfoNote(
                title: AppStrings.bookingWhatNextTitle,
                body: AppStrings.bookingWhatNextBody,
              ),
            ],
          ),
        ),
        SafeArea(
          top: false,
          child: Padding(
            padding: const EdgeInsetsDirectional.fromSTEB(
              AppSpacing.md,
              0,
              AppSpacing.md,
              AppSpacing.md,
            ),
            child: AppButton(
              label: AppStrings.bookingConfirmCta,
              loading: state.submitStatus == SubmitStatus.submitting,
              onPressed: () => _confirm(context),
            ),
          ),
        ),
      ],
    );
  }
}

class _SuccessView extends StatelessWidget {
  final BookingBloc bloc;

  /// The flow as it stood when the booking was created. Every button resets the bloc, so the recap and the checkout
  /// target are read from here, never from the bloc's live state.
  final BookingState booking;

  const _SuccessView({required this.bloc, required this.booking});

  /// The checkout location for the just-created booking, or null when checkout must not be offered.
  ///
  /// Gated by [FeatureFlags.checkoutEnabled] so the journey stays dark until it clears its release gates. The flag
  /// only controls whether payment is *offered*: with it off the booking is still created and the backend still
  /// enforces its deposit gate, so this can never bypass payment — it just doesn't collect it in-app yet.
  String? get _checkoutTarget {
    if (!FeatureFlags.checkoutEnabled) return null;
    final bookingId = booking.bookingId;
    final providerId = booking.providerId;
    if (bookingId == null || bookingId.isEmpty) return null;
    if (providerId == null || providerId.isEmpty) return null;
    return Routes.checkoutFor(bookingId, providerId);
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final slot = booking.slot;
    final checkoutTarget = _checkoutTarget;
    return Scaffold(
      body: SafeArea(
        child: LayoutBuilder(
          builder: (context, constraints) => SingleChildScrollView(
            padding: const EdgeInsets.all(AppSpacing.lg),
            child: ConstrainedBox(
              constraints: BoxConstraints(
                minHeight: (constraints.maxHeight - 2 * AppSpacing.lg)
                    .clamp(0.0, double.infinity),
              ),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  const Icon(
                    Icons.check_circle_outline,
                    size: AppIconSize.hero,
                    // The AA green: the accent (colorScheme.secondary) is 2.38:1 on white.
                    color: AppColors.accentStrong,
                  ),
                  const SizedBox(height: AppSpacing.md),
                  Text(
                    AppStrings.bookingSuccessRequestedTitle,
                    style: theme.textTheme.headlineSmall,
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: AppSpacing.md),
                  if (slot != null) ...[
                    AppCard(child: _SummaryRows(rows: _visitRows(booking, slot))),
                    const SizedBox(height: AppSpacing.md),
                  ],
                  // New bookings are created as Requested: nothing is final until the salon accepts.
                  const _InfoNote(body: AppStrings.bookingSuccessAwaiting),
                  const SizedBox(height: AppSpacing.xl),
                  // Deposit coupling (create-then-pay). The booking already exists and holds the slot; the server
                  // decides whether a deposit is owed, so we simply offer to continue into checkout and let it ask.
                  // Nothing here can confirm a booking — the backend gate does that only on a verified deposit.
                  if (checkoutTarget != null)
                    Padding(
                      padding: const EdgeInsets.only(bottom: AppSpacing.xs),
                      child: AppButton(
                        key: const Key('booking-pay-deposit-button'),
                        label: AppStrings.checkoutPayCta,
                        onPressed: () {
                          bloc.add(const BookingReset());
                          context.push(checkoutTarget);
                        },
                      ),
                    ),
                  AppButton(
                    key: const Key('booking-view-appointments-button'),
                    label: AppStrings.bookingViewAppointments,
                    variant: checkoutTarget == null
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
        ),
      ),
    );
  }
}
