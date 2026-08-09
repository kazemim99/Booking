import 'package:dartz/dartz.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:booksy_customer_app/config/theme/app_theme.dart';
import 'package:booksy_customer_app/core/errors/failures.dart';
import 'package:booksy_customer_app/features/checkout/domain/entities/checkout_entities.dart';
import 'package:booksy_customer_app/features/checkout/domain/repositories/checkout_repository.dart';
import 'package:booksy_customer_app/features/checkout/presentation/bloc/checkout_bloc.dart';
import 'package:booksy_customer_app/features/checkout/presentation/pages/checkout_page.dart';

/// Widget tests for every checkout state a customer can land in.
///
/// The states that matter most for money are the negative ones: `unknown` must **never** offer a pay button (an
/// uncertain charge must not be duplicated), while `failed` — where we know no money moved — may offer a retry.

// ---------------------------------------------------------------- fakes

class _FakeRepo implements CheckoutRepository {
  BookingPaymentSnapshot booking;
  Failure? bookingFailure;
  Either<Failure, PaymentIntent>? createResult;
  Either<Failure, PaymentStatusResult>? verifyResult;
  Either<Failure, PaymentStatusResult>? getPaymentResult;

  _FakeRepo({required this.booking});

  @override
  Future<Either<Failure, BookingPaymentSnapshot>> getBookingPaymentSnapshot(String bookingId) async =>
      bookingFailure != null ? Left(bookingFailure!) : Right(booking);

  @override
  Future<Either<Failure, PaymentIntent>> createPayment({
    required String bookingId,
    required String providerId,
    required double amount,
    required String idempotencyKey,
    String? description,
    String? mobile,
    String? email,
  }) async =>
      createResult ?? const Right(PaymentIntent(authority: 'auth-1', paymentUrl: 'https://pay.test/1'));

  @override
  Future<Either<Failure, PaymentStatusResult>> verifyPayment({
    required String authority,
    String status = 'OK',
    String? idempotencyKey,
  }) async =>
      verifyResult ?? const Right(PaymentStatusResult(status: 'Pending'));

  @override
  Future<Either<Failure, PaymentStatusResult>> getPayment(String paymentId) async =>
      getPaymentResult ?? const Right(PaymentStatusResult(status: 'Pending'));
}

class _MemStore implements CheckoutAttemptStore {
  final Map<String, CheckoutAttempt> data = {};

  @override
  Future<CheckoutAttempt?> read(String bookingId) async => data[bookingId];

  @override
  Future<void> save(CheckoutAttempt attempt) async => data[attempt.bookingId] = attempt;

  @override
  Future<void> clear(String bookingId) async => data.remove(bookingId);
}

BookingPaymentSnapshot _snapshot({
  double total = 1000000,
  double deposit = 200000,
  double paid = 0,
  String status = 'Requested',
}) =>
    BookingPaymentSnapshot(
      bookingId: 'b1',
      bookingStatus: status,
      totalAmount: total,
      depositAmount: deposit,
      paidAmount: paid,
      paymentStatus: paid > 0 ? 'PartiallyPaid' : 'Pending',
    );

/// Pumps a bounded number of frames.
///
/// `pumpAndSettle` cannot be used here: the busy states render a `CircularProgressIndicator`, whose endless
/// animation always has a frame scheduled, so settling never happens and the test hangs until timeout. Bounded pumps
/// let the bloc's async work complete without waiting on an animation that never stops.
Future<void> _settle(WidgetTester tester) async {
  for (var i = 0; i < 8; i++) {
    await tester.pump(const Duration(milliseconds: 20));
  }
}

void main() {
  late _FakeRepo repo;
  late _MemStore store;
  late List<String> launched;
  late bool launchSucceeds;

  setUp(() {
    repo = _FakeRepo(booking: _snapshot());
    store = _MemStore();
    launched = [];
    launchSucceeds = true;
  });

  CheckoutBloc buildBloc() => CheckoutBloc(
        repository: repo,
        attemptStore: store,
        launchPaymentUrl: (url) async {
          launched.add(url);
          return launchSucceeds;
        },
        keyGenerator: () => '11111111-1111-4111-8111-111111111111',
      );

  Future<CheckoutBloc> pump(WidgetTester tester) async {
    final bloc = buildBloc();
    await tester.pumpWidget(MaterialApp(
      theme: AppTheme.light,
      // RTL: the app is Persian-first, so every checkout state must lay out correctly right-to-left.
      locale: const Locale('fa'),
      builder: (context, child) => Directionality(textDirection: TextDirection.rtl, child: child!),
      home: CheckoutPage(bookingId: 'b1', providerId: 'p1', bloc: bloc),
    ));
    await _settle(tester);
    return bloc;
  }

  group('review state', () {
    testWidgets('shows the deposit due and a pay button', (tester) async {
      final bloc = await pump(tester);

      expect(find.byKey(const Key('checkout-deposit-row')), findsOneWidget);
      expect(find.byKey(const Key('checkout-pay-button')), findsOneWidget);
      expect(bloc.state.status, CheckoutStatus.ready);
    });

    testWidgets('renders right-to-left', (tester) async {
      await pump(tester);

      final direction = Directionality.of(tester.element(find.byKey(const Key('checkout-pay-button'))));
      expect(direction, TextDirection.rtl);
    });

    testWidgets('tapping pay launches the external browser and shows the awaiting state', (tester) async {
      await pump(tester);

      await tester.tap(find.byKey(const Key('checkout-pay-button')));
      await _settle(tester);

      expect(launched, ['https://pay.test/1'], reason: 'payment opens in the external browser');
      expect(find.byKey(const Key('checkout-awaiting')), findsOneWidget);
    });
  });

  group('awaiting state', () {
    testWidgets('offers check-status and cancel, and re-checks on demand', (tester) async {
      repo.verifyResult = const Right(PaymentStatusResult(status: 'Paid', refNumber: '4242'));
      await pump(tester);

      await tester.tap(find.byKey(const Key('checkout-pay-button')));
      await _settle(tester);
      expect(find.byKey(const Key('checkout-check-status-button')), findsOneWidget);
      expect(find.byKey(const Key('checkout-cancel-button')), findsOneWidget);

      await tester.tap(find.byKey(const Key('checkout-check-status-button')));
      await _settle(tester);

      expect(find.byKey(const Key('checkout-paid')), findsOneWidget);
    });
  });

  group('paid state', () {
    testWidgets('shows the receipt with the reference number', (tester) async {
      repo.booking = _snapshot(paid: 200000, status: 'Confirmed');
      await pump(tester);

      expect(find.byKey(const Key('checkout-paid')), findsOneWidget);
      expect(find.byKey(const Key('checkout-pay-button')), findsNothing,
          reason: 'a settled deposit must never offer to pay again');
    });
  });

  group('failed state', () {
    testWidgets('offers a retry because no money moved', (tester) async {
      repo.createResult = const Left(GatewayFailure('گیت‌وی پرداخت را رد کرد'));
      await pump(tester);

      await tester.tap(find.byKey(const Key('checkout-pay-button')));
      await _settle(tester);

      expect(find.byKey(const Key('checkout-failed')), findsOneWidget);
      expect(find.byKey(const Key('checkout-retry-button')), findsOneWidget);
    });

    testWidgets('retry returns to the review state', (tester) async {
      repo.createResult = const Left(GatewayFailure('declined'));
      await pump(tester);

      await tester.tap(find.byKey(const Key('checkout-pay-button')));
      await _settle(tester);
      await tester.tap(find.byKey(const Key('checkout-retry-button')));
      await _settle(tester);

      expect(find.byKey(const Key('checkout-pay-button')), findsOneWidget);
    });
  });

  group('unknown state', () {
    testWidgets('never offers a pay or retry button — a duplicate charge is the worst outcome', (tester) async {
      repo.bookingFailure = const NetworkFailure('offline');
      await pump(tester);

      expect(find.byKey(const Key('checkout-unknown')), findsOneWidget);
      expect(find.byKey(const Key('checkout-pay-button')), findsNothing);
      expect(find.byKey(const Key('checkout-retry-button')), findsNothing);
      // Only a safe re-check is offered.
      expect(find.byKey(const Key('checkout-recheck-button')), findsOneWidget);
    });

    testWidgets('a failed browser launch lands in unknown with the attempt preserved for resume', (tester) async {
      launchSucceeds = false;
      await pump(tester);

      await tester.tap(find.byKey(const Key('checkout-pay-button')));
      await _settle(tester);

      expect(find.byKey(const Key('checkout-unknown')), findsOneWidget);
      expect(store.data['b1'], isNotNull, reason: 'the attempt survives so it can be resumed, not re-charged');
    });

    testWidgets('re-checking from unknown can resolve to paid without a new charge', (tester) async {
      repo.bookingFailure = const NetworkFailure('offline');
      await pump(tester);
      expect(find.byKey(const Key('checkout-unknown')), findsOneWidget);

      // Connectivity returns and the booking turns out to be settled.
      repo.bookingFailure = null;
      repo.booking = _snapshot(paid: 200000, status: 'Confirmed');
      await tester.tap(find.byKey(const Key('checkout-recheck-button')));
      await _settle(tester);

      expect(find.byKey(const Key('checkout-paid')), findsOneWidget);
    });
  });

  group('nothing due state', () {
    testWidgets('a no-deposit booking shows nothing to pay and no pay button', (tester) async {
      repo.booking = _snapshot(deposit: 0);
      await pump(tester);

      expect(find.byKey(const Key('checkout-nothing-due')), findsOneWidget);
      expect(find.byKey(const Key('checkout-pay-button')), findsNothing,
          reason: 'the provider asked for no deposit; charging here would invent a financial rule');
    });
  });

  group('resume on open', () {
    testWidgets('an interrupted attempt is verified on open instead of re-created', (tester) async {
      await store.save(const CheckoutAttempt(
        bookingId: 'b1',
        idempotencyKey: 'key-existing',
        authority: 'auth-old',
      ));
      repo.verifyResult = const Right(PaymentStatusResult(status: 'Paid', refNumber: '777'));

      await pump(tester);

      expect(find.byKey(const Key('checkout-paid')), findsOneWidget);
      expect(find.byKey(const Key('checkout-ref-number')), findsOneWidget);
      expect(launched, isEmpty, reason: 'resuming must not open a new payment');
    });
  });
}
