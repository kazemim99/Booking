import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../../config/routes/app_router.dart';
import '../../../../config/theme/app_tokens.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/utils/price_formatter.dart';
import '../../../../core/widgets/widgets.dart';
import '../bloc/checkout_bloc.dart';

/// Deposit checkout. Drives the Alpha external-browser flow and reflects **server-owned** payment state.
///
/// The page never decides whether money is owed or received — it renders what `CheckoutBloc` reports after asking
/// the backend. Notably `unknown` is presented as "we can't tell yet, don't pay again", never as a failure with a
/// tempting pay button, because a duplicate charge is the worst outcome available here.
///
/// Lifecycle-aware: coming back to the foreground (the customer returning from the browser) re-reads the
/// authoritative outcome instead of trusting anything the browser handed back.
class CheckoutPage extends StatefulWidget {
  final String bookingId;
  final String providerId;

  /// The bloc driving this checkout. Passed in (the router resolves it from the service locator) rather than looked
  /// up here, so the page stays free of any DI import — that keeps it cheap to widget-test and keeps the dependency
  /// direction one-way.
  final CheckoutBloc bloc;

  const CheckoutPage({
    super.key,
    required this.bookingId,
    required this.providerId,
    required this.bloc,
  });

  @override
  State<CheckoutPage> createState() => _CheckoutPageState();
}

class _CheckoutPageState extends State<CheckoutPage> with WidgetsBindingObserver {
  late final CheckoutBloc _bloc;

  @override
  void initState() {
    super.initState();
    _bloc = widget.bloc;
    _bloc.add(CheckoutStarted(bookingId: widget.bookingId, providerId: widget.providerId));
    WidgetsBinding.instance.addObserver(this);
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    // The bloc is created per navigation (a get_it factory), so this page owns its lifetime.
    _bloc.close();
    super.dispose();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    // The customer may have just finished (or abandoned) payment in the browser. Re-read the server's answer.
    if (state == AppLifecycleState.resumed && _bloc.state.status == CheckoutStatus.awaitingPayment) {
      _bloc.add(const CheckoutReturned());
    }
  }

  @override
  Widget build(BuildContext context) {
    return BlocProvider<CheckoutBloc>.value(
      value: _bloc,
      child: Scaffold(
        appBar: AppBar(title: const Text(AppStrings.checkoutTitle)),
        // No OfflineBanner wrapper here on purpose: losing connectivity mid-checkout must not be reported as a
        // cosmetic banner. The bloc maps any transport failure to `unknown`, which tells the customer plainly not to
        // pay again and offers only a re-check — the safe outcome for money.
        body: SafeArea(
          child: BlocBuilder<CheckoutBloc, CheckoutState>(
            builder: (context, state) => _CheckoutBody(state: state, bloc: _bloc),
          ),
        ),
      ),
    );
  }
}

class _CheckoutBody extends StatelessWidget {
  final CheckoutState state;
  final CheckoutBloc bloc;

  const _CheckoutBody({required this.state, required this.bloc});

  @override
  Widget build(BuildContext context) {
    switch (state.status) {
      case CheckoutStatus.initial:
      case CheckoutStatus.loading:
      case CheckoutStatus.creating:
      case CheckoutStatus.verifying:
        return const _CheckoutBusy(key: Key('checkout-busy'));

      case CheckoutStatus.ready:
        return _CheckoutReview(state: state, bloc: bloc);

      case CheckoutStatus.awaitingPayment:
        return _CheckoutAwaiting(bloc: bloc);

      case CheckoutStatus.paid:
        return _CheckoutPaid(state: state);

      case CheckoutStatus.failed:
        return _CheckoutFailed(state: state, bloc: bloc);

      case CheckoutStatus.unknown:
        return _CheckoutUnknown(state: state, bloc: bloc);

      case CheckoutStatus.nothingDue:
        return const _CheckoutNothingDue();
    }
  }
}

class _CheckoutBusy extends StatelessWidget {
  const _CheckoutBusy({super.key});

  @override
  Widget build(BuildContext context) =>
      const Center(child: CircularProgressIndicator(key: Key('checkout-progress')));
}

/// Review: shows exactly what will be charged now and what is paid at the venue.
class _CheckoutReview extends StatelessWidget {
  final CheckoutState state;
  final CheckoutBloc bloc;

  const _CheckoutReview({required this.state, required this.bloc});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final booking = state.booking;
    final deposit = booking?.depositAmount ?? 0;
    final total = booking?.totalAmount ?? 0;
    final remaining = (total - deposit).clamp(0, double.infinity);

    return SingleChildScrollView(
      padding: const EdgeInsets.all(AppSpacing.md),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          AppCard(
            child: Column(
              children: [
                _AmountRow(
                  key: const Key('checkout-deposit-row'),
                  label: AppStrings.checkoutDepositLabel,
                  amount: deposit,
                  emphasized: true,
                ),
                const Divider(height: AppSpacing.lg),
                _AmountRow(label: AppStrings.checkoutTotalLabel, amount: total),
                const SizedBox(height: AppSpacing.xs),
                _AmountRow(label: AppStrings.checkoutRemainingLabel, amount: remaining.toDouble()),
              ],
            ),
          ),
          const SizedBox(height: AppSpacing.md),
          Text(
            AppStrings.checkoutGatewayNotice,
            style: theme.textTheme.bodySmall,
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: AppSpacing.lg),
          AppButton(
            key: const Key('checkout-pay-button'),
            label: AppStrings.checkoutPayCta,
            onPressed: () => bloc.add(const CheckoutPaymentRequested()),
          ),
          const SizedBox(height: AppSpacing.xs),
          AppButton.secondary(
            key: const Key('checkout-pay-later-button'),
            label: AppStrings.checkoutPayLaterCta,
            onPressed: () => context.go(Routes.appointments),
          ),
        ],
      ),
    );
  }
}

/// Awaiting: payment is happening in the external browser; we poll the server on demand or on resume.
class _CheckoutAwaiting extends StatelessWidget {
  final CheckoutBloc bloc;

  const _CheckoutAwaiting({required this.bloc});

  @override
  Widget build(BuildContext context) {
    return _CheckoutMessage(
      key: const Key('checkout-awaiting'),
      icon: Icons.hourglass_top_outlined,
      title: AppStrings.checkoutAwaitingTitle,
      body: AppStrings.checkoutAwaitingBody,
      actions: [
        AppButton(
          key: const Key('checkout-check-status-button'),
          label: AppStrings.checkoutCheckStatusCta,
          onPressed: () => bloc.add(const CheckoutReturned()),
        ),
        AppButton.secondary(
          key: const Key('checkout-cancel-button'),
          label: AppStrings.checkoutCancelPaymentCta,
          onPressed: () => bloc.add(const CheckoutCancelled()),
        ),
      ],
    );
  }
}

/// Paid: the receipt. Amount + reference number, both from server-confirmed state.
class _CheckoutPaid extends StatelessWidget {
  final CheckoutState state;

  const _CheckoutPaid({required this.state});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return _CheckoutMessage(
      key: const Key('checkout-paid'),
      icon: Icons.check_circle_outline,
      iconColor: theme.colorScheme.secondary,
      title: AppStrings.checkoutPaidTitle,
      body: AppStrings.checkoutPaidBody,
      extra: state.refNumber == null
          ? null
          : Padding(
              padding: const EdgeInsets.only(top: AppSpacing.sm),
              child: Text(
                '${AppStrings.checkoutRefNumberLabel}: ${state.refNumber}',
                key: const Key('checkout-ref-number'),
                style: theme.textTheme.bodyMedium,
                textAlign: TextAlign.center,
              ),
            ),
      actions: [
        AppButton(
          key: const Key('checkout-view-appointments-button'),
          label: AppStrings.bookingViewAppointments,
          onPressed: () => context.go(Routes.appointments),
        ),
      ],
    );
  }
}

/// Failed: definitively not charged, so offering a retry is safe.
class _CheckoutFailed extends StatelessWidget {
  final CheckoutState state;
  final CheckoutBloc bloc;

  const _CheckoutFailed({required this.state, required this.bloc});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return _CheckoutMessage(
      key: const Key('checkout-failed'),
      icon: Icons.error_outline,
      iconColor: theme.colorScheme.error,
      title: AppStrings.checkoutFailedTitle,
      body: state.message ?? AppStrings.checkoutFailedBody,
      actions: [
        AppButton(
          key: const Key('checkout-retry-button'),
          label: AppStrings.retry,
          onPressed: () => bloc.add(const CheckoutRetried()),
        ),
        AppButton.secondary(
          label: AppStrings.bookingViewAppointments,
          onPressed: () => context.go(Routes.appointments),
        ),
      ],
      extra: state.message == null
          ? null
          : Padding(
              padding: const EdgeInsets.only(top: AppSpacing.xs),
              child: Text(
                AppStrings.checkoutFailedBody,
                style: theme.textTheme.bodySmall,
                textAlign: TextAlign.center,
              ),
            ),
    );
  }
}

/// Unknown: the outcome could not be determined. Deliberately offers **no pay button** — only "check again" — so an
/// uncertain charge is never duplicated. Reconciliation settles anything outstanding server-side.
class _CheckoutUnknown extends StatelessWidget {
  final CheckoutState state;
  final CheckoutBloc bloc;

  const _CheckoutUnknown({required this.state, required this.bloc});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return _CheckoutMessage(
      key: const Key('checkout-unknown'),
      icon: Icons.help_outline,
      iconColor: theme.colorScheme.tertiary,
      title: AppStrings.checkoutUnknownTitle,
      body: AppStrings.checkoutUnknownBody,
      extra: state.message == null
          ? null
          : Padding(
              padding: const EdgeInsets.only(top: AppSpacing.xs),
              child: Text(
                state.message!,
                key: const Key('checkout-unknown-detail'),
                style: theme.textTheme.bodySmall,
                textAlign: TextAlign.center,
              ),
            ),
      actions: [
        AppButton(
          key: const Key('checkout-recheck-button'),
          label: AppStrings.checkoutCheckStatusCta,
          onPressed: () => bloc.add(const CheckoutReturned()),
        ),
        AppButton.secondary(
          label: AppStrings.bookingViewAppointments,
          onPressed: () => context.go(Routes.appointments),
        ),
      ],
    );
  }
}

/// Nothing due: the provider requires no deposit, so no online payment is collected.
class _CheckoutNothingDue extends StatelessWidget {
  const _CheckoutNothingDue();

  @override
  Widget build(BuildContext context) {
    return _CheckoutMessage(
      key: const Key('checkout-nothing-due'),
      icon: Icons.info_outline,
      title: AppStrings.checkoutNothingDueTitle,
      body: AppStrings.checkoutNothingDueBody,
      actions: [
        AppButton(
          label: AppStrings.bookingViewAppointments,
          onPressed: () => context.go(Routes.appointments),
        ),
      ],
    );
  }
}

// ---------------------------------------------------------------- shared bits

class _AmountRow extends StatelessWidget {
  final String label;
  final double amount;
  final bool emphasized;

  const _AmountRow({
    super.key,
    required this.label,
    required this.amount,
    this.emphasized = false,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final style = emphasized ? theme.textTheme.titleMedium : theme.textTheme.bodyMedium;
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(label, style: style),
        Text(PriceFormatter.format(amount.round()), style: style),
      ],
    );
  }
}

class _CheckoutMessage extends StatelessWidget {
  final IconData icon;
  final Color? iconColor;
  final String title;
  final String body;
  final Widget? extra;
  final List<Widget> actions;

  const _CheckoutMessage({
    super.key,
    required this.icon,
    required this.title,
    required this.body,
    required this.actions,
    this.iconColor,
    this.extra,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return SingleChildScrollView(
      padding: const EdgeInsets.all(AppSpacing.lg),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const SizedBox(height: AppSpacing.xl),
          Icon(icon, size: 72, color: iconColor ?? theme.colorScheme.primary),
          const SizedBox(height: AppSpacing.lg),
          Text(title, style: theme.textTheme.headlineSmall, textAlign: TextAlign.center),
          const SizedBox(height: AppSpacing.xs),
          Text(body, style: theme.textTheme.bodyMedium, textAlign: TextAlign.center),
          if (extra != null) extra!,
          const SizedBox(height: AppSpacing.xl),
          for (final action in actions) ...[
            action,
            const SizedBox(height: AppSpacing.xs),
          ],
        ],
      ),
    );
  }
}
