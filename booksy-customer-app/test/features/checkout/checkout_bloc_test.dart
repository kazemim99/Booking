import 'package:dartz/dartz.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:booksy_customer_app/core/errors/failures.dart';
import 'package:booksy_customer_app/features/checkout/domain/entities/checkout_entities.dart';
import 'package:booksy_customer_app/features/checkout/domain/repositories/checkout_repository.dart';
import 'package:booksy_customer_app/features/checkout/presentation/bloc/checkout_bloc.dart';

/// C4 Alpha checkout bloc.
///
/// The behaviours under test are the money-safety ones: a customer must never be charged twice, an interrupted
/// checkout must resume rather than re-charge, and an unknown outcome must never be presented as a failure that
/// invites a second payment.

// ==================== fakes ====================

BookingPaymentSnapshot _booking({
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

class FakeCheckoutRepository implements CheckoutRepository {
  BookingPaymentSnapshot booking = _booking();
  Failure? bookingFailure;

  /// Queued create outcomes; each call consumes one (so a retry can differ from the first attempt).
  final List<Either<Failure, PaymentIntent>> createResults = [];
  final List<Either<Failure, PaymentStatusResult>> verifyResults = [];
  final List<Either<Failure, PaymentStatusResult>> getPaymentResults = [];

  int createCalls = 0;
  int verifyCalls = 0;
  int getPaymentCalls = 0;
  final List<String> createKeys = [];
  final List<String> verifyStatuses = [];

  @override
  Future<Either<Failure, BookingPaymentSnapshot>> getBookingPaymentSnapshot(String bookingId) async {
    if (bookingFailure != null) return Left(bookingFailure!);
    return Right(booking);
  }

  @override
  Future<Either<Failure, PaymentIntent>> createPayment({
    required String bookingId,
    required String providerId,
    required double amount,
    required String idempotencyKey,
    String? description,
    String? mobile,
    String? email,
  }) async {
    createCalls++;
    createKeys.add(idempotencyKey);
    if (createResults.isEmpty) {
      return const Right(PaymentIntent(authority: 'auth-1', paymentUrl: 'https://pay.test/1'));
    }
    return createResults.removeAt(0);
  }

  @override
  Future<Either<Failure, PaymentStatusResult>> verifyPayment({
    required String authority,
    String status = 'OK',
    String? idempotencyKey,
  }) async {
    verifyCalls++;
    verifyStatuses.add(status);
    if (verifyResults.isEmpty) return const Right(PaymentStatusResult(status: 'Pending'));
    return verifyResults.removeAt(0);
  }

  @override
  Future<Either<Failure, PaymentStatusResult>> getPayment(String paymentId) async {
    getPaymentCalls++;
    if (getPaymentResults.isEmpty) return const Right(PaymentStatusResult(status: 'Pending'));
    return getPaymentResults.removeAt(0);
  }
}

class InMemoryAttemptStore implements CheckoutAttemptStore {
  final Map<String, CheckoutAttempt> _store = {};
  int clears = 0;

  @override
  Future<CheckoutAttempt?> read(String bookingId) async => _store[bookingId];

  @override
  Future<void> save(CheckoutAttempt attempt) async => _store[attempt.bookingId] = attempt;

  @override
  Future<void> clear(String bookingId) async {
    clears++;
    _store.remove(bookingId);
  }
}

void main() {
  late FakeCheckoutRepository repo;
  late InMemoryAttemptStore store;
  late List<String> launched;
  late bool launchSucceeds;
  late int keyCounter;

  CheckoutBloc buildBloc() => CheckoutBloc(
        repository: repo,
        attemptStore: store,
        launchPaymentUrl: (url) async {
          launched.add(url);
          return launchSucceeds;
        },
        keyGenerator: () => 'key-${++keyCounter}',
      );

  setUp(() {
    repo = FakeCheckoutRepository();
    store = InMemoryAttemptStore();
    launched = [];
    launchSucceeds = true;
    keyCounter = 0;
  });

  const start = CheckoutStarted(bookingId: 'b1', providerId: 'p1');

  group('start', () {
    test('shows the deposit amount due when a deposit is required', () async {
      final bloc = buildBloc();
      bloc.add(start);
      await bloc.stream.firstWhere((s) => s.status == CheckoutStatus.ready);

      expect(bloc.state.amountDue, 200000);
      expect(repo.createCalls, 0, reason: 'nothing may be charged before the customer confirms');
      await bloc.close();
    });

    test('collects nothing when the provider requires no deposit', () async {
      // The backend gate is Policy.RequireDeposit + a deposit percentage. A provider who requires no deposit is not
      // asking for online pre-payment, so charging the full price here would invent a financial rule.
      repo.booking = _booking(deposit: 0);
      final bloc = buildBloc();
      bloc.add(start);
      await bloc.stream.firstWhere((s) => s.status == CheckoutStatus.nothingDue);

      expect(bloc.state.amountDue, 0);
      expect(repo.createCalls, 0);
      await bloc.close();
    });

    test('a pay request is refused outright when nothing is due', () async {
      repo.booking = _booking(deposit: 0);
      final bloc = buildBloc();
      bloc.add(start);
      await bloc.stream.firstWhere((s) => s.status == CheckoutStatus.nothingDue);

      bloc.add(const CheckoutPaymentRequested());
      await Future<void>.delayed(Duration.zero);

      expect(repo.createCalls, 0, reason: 'no charge may be created when the provider asked for no deposit');
      expect(bloc.state.status, CheckoutStatus.nothingDue);
      await bloc.close();
    });

    test('short-circuits to paid when the deposit is already settled — never offers to pay again', () async {
      repo.booking = _booking(paid: 200000, status: 'Confirmed');
      final bloc = buildBloc();
      bloc.add(start);
      await bloc.stream.firstWhere((s) => s.status == CheckoutStatus.paid);

      expect(repo.createCalls, 0);
      await bloc.close();
    });

    test('an unreachable server yields unknown, not a failure that invites paying again', () async {
      repo.bookingFailure = const NetworkFailure('offline');
      final bloc = buildBloc();
      bloc.add(start);
      await bloc.stream.firstWhere((s) => s.status == CheckoutStatus.unknown);

      expect(repo.createCalls, 0);
      await bloc.close();
    });

    test('resumes and verifies a stored attempt instead of creating a second payment', () async {
      await store.save(const CheckoutAttempt(bookingId: 'b1', idempotencyKey: 'key-existing', authority: 'auth-old'));
      repo.verifyResults.add(const Right(PaymentStatusResult(status: 'Paid', refNumber: '999')));

      final bloc = buildBloc();
      bloc.add(start);
      await bloc.stream.firstWhere((s) => s.status == CheckoutStatus.paid);

      expect(repo.verifyCalls, 1, reason: 'the interrupted attempt is verified');
      expect(repo.createCalls, 0, reason: 'a resumable attempt must never be re-created');
      expect(bloc.state.refNumber, '999');
      await bloc.close();
    });
  });

  group('pay', () {
    test('creates the payment, persists the attempt, and opens the gateway URL', () async {
      repo.createResults.add(const Right(
          PaymentIntent(authority: 'auth-9', paymentUrl: 'https://pay.test/9', paymentId: 'pay-9')));

      final bloc = buildBloc();
      bloc.add(start);
      await bloc.stream.firstWhere((s) => s.status == CheckoutStatus.ready);
      bloc.add(const CheckoutPaymentRequested());
      await bloc.stream.firstWhere((s) => s.status == CheckoutStatus.awaitingPayment);

      expect(launched, ['https://pay.test/9']);
      final stored = await store.read('b1');
      expect(stored?.authority, 'auth-9', reason: 'the attempt must survive an app kill mid-redirect');
      expect(stored?.paymentId, 'pay-9');
      await bloc.close();
    });

    test('reuses the persisted idempotency key so a repeated create cannot charge twice', () async {
      final bloc = buildBloc();
      bloc.add(start);
      await bloc.stream.firstWhere((s) => s.status == CheckoutStatus.ready);

      bloc.add(const CheckoutPaymentRequested());
      await bloc.stream.firstWhere((s) => s.status == CheckoutStatus.awaitingPayment);
      bloc.add(const CheckoutPaymentRequested());
      await bloc.stream.firstWhere((s) => s.status == CheckoutStatus.awaitingPayment);

      expect(repo.createKeys.length, 2);
      expect(repo.createKeys[0], repo.createKeys[1],
          reason: 'the same attempt must present the same key so the server de-duplicates it');
      await bloc.close();
    });

    test('a 409 on a re-press verifies the existing attempt instead of charging again', () async {
      // First press succeeds and stores an authority; the second press is rejected as a duplicate by the server.
      repo.createResults.add(const Right(PaymentIntent(authority: 'auth-1', paymentUrl: 'https://pay.test/1')));
      repo.createResults.add(const Left(DuplicateRequestFailure('already in progress')));
      repo.verifyResults.add(const Right(PaymentStatusResult(status: 'Paid', refNumber: '777')));

      final bloc = buildBloc();
      bloc.add(start);
      await bloc.stream.firstWhere((s) => s.status == CheckoutStatus.ready);
      bloc.add(const CheckoutPaymentRequested());
      await bloc.stream.firstWhere((s) => s.status == CheckoutStatus.awaitingPayment);

      bloc.add(const CheckoutPaymentRequested());
      final paid = await bloc.stream.firstWhere((s) => s.status == CheckoutStatus.paid);

      expect(repo.verifyCalls, 1, reason: 'the duplicate is resolved by verifying, not by a new charge');
      expect(paid.refNumber, '777');
      await bloc.close();
    });

    test('a 409 with no gateway handle resolves from the booking and never re-creates', () async {
      // The in-flight request belongs to an earlier/other session, so we hold no authority to verify against.
      repo.createResults.add(const Left(DuplicateRequestFailure('already in progress')));

      final bloc = buildBloc();
      bloc.add(start);
      await bloc.stream.firstWhere((s) => s.status == CheckoutStatus.ready);
      bloc.add(const CheckoutPaymentRequested());
      await bloc.stream.firstWhere((s) => s.status == CheckoutStatus.unknown);

      expect(repo.createCalls, 1, reason: 'never re-create after a duplicate rejection');
      expect(repo.verifyCalls, 0, reason: 'nothing to verify without an authority');
      await bloc.close();
    });

    test('a 409 whose sibling request already paid the booking reports paid', () async {
      repo.createResults.add(const Left(DuplicateRequestFailure('already in progress')));

      final bloc = buildBloc();
      bloc.add(start);
      await bloc.stream.firstWhere((s) => s.status == CheckoutStatus.ready);

      // The concurrent request settled the deposit in the meantime.
      repo.booking = _booking(paid: 200000, status: 'Confirmed');
      bloc.add(const CheckoutPaymentRequested());
      await bloc.stream.firstWhere((s) => s.status == CheckoutStatus.paid);

      expect(repo.createCalls, 1);
      await bloc.close();
    });

    test('a gateway refusal is a definitive failure (no charge happened)', () async {
      repo.createResults.add(const Left(GatewayFailure('gateway said no')));

      final bloc = buildBloc();
      bloc.add(start);
      await bloc.stream.firstWhere((s) => s.status == CheckoutStatus.ready);
      bloc.add(const CheckoutPaymentRequested());
      final failed = await bloc.stream.firstWhere((s) => s.status == CheckoutStatus.failed);

      expect(failed.message, 'gateway said no');
      await bloc.close();
    });

    test('a transport error while creating yields unknown, not failed', () async {
      repo.createResults.add(const Left(NetworkFailure('timeout')));

      final bloc = buildBloc();
      bloc.add(start);
      await bloc.stream.firstWhere((s) => s.status == CheckoutStatus.ready);
      bloc.add(const CheckoutPaymentRequested());
      await bloc.stream.firstWhere((s) => s.status == CheckoutStatus.unknown);

      // The create may or may not have reached the gateway — the attempt is kept for a later resume.
      expect(await store.read('b1'), isNotNull);
      await bloc.close();
    });

    test('a browser that fails to open is unknown, and the attempt is preserved', () async {
      launchSucceeds = false;

      final bloc = buildBloc();
      bloc.add(start);
      await bloc.stream.firstWhere((s) => s.status == CheckoutStatus.ready);
      bloc.add(const CheckoutPaymentRequested());
      await bloc.stream.firstWhere((s) => s.status == CheckoutStatus.unknown);

      expect(await store.read('b1'), isNotNull);
      await bloc.close();
    });
  });

  group('return / resume', () {
    test('a paid result clears the attempt and reports the reference number', () async {
      repo.getPaymentResults.add(const Right(PaymentStatusResult(status: 'Paid', refNumber: '4242')));
      repo.createResults.add(const Right(
          PaymentIntent(authority: 'auth-1', paymentUrl: 'https://pay.test/1', paymentId: 'pay-1')));

      final bloc = buildBloc();
      bloc.add(start);
      await bloc.stream.firstWhere((s) => s.status == CheckoutStatus.ready);
      bloc.add(const CheckoutPaymentRequested());
      await bloc.stream.firstWhere((s) => s.status == CheckoutStatus.awaitingPayment);

      bloc.add(const CheckoutReturned());
      final paid = await bloc.stream.firstWhere((s) => s.status == CheckoutStatus.paid);

      expect(paid.refNumber, '4242');
      expect(await store.read('b1'), isNull, reason: 'a settled attempt is cleared');
      await bloc.close();
    });

    test('a still-pending gateway keeps awaiting payment and never re-charges', () async {
      repo.getPaymentResults.add(const Right(PaymentStatusResult(status: 'Pending')));
      repo.createResults.add(const Right(
          PaymentIntent(authority: 'auth-1', paymentUrl: 'https://pay.test/1', paymentId: 'pay-1')));

      final bloc = buildBloc();
      bloc.add(start);
      await bloc.stream.firstWhere((s) => s.status == CheckoutStatus.ready);
      bloc.add(const CheckoutPaymentRequested());
      await bloc.stream.firstWhere((s) => s.status == CheckoutStatus.awaitingPayment);

      bloc.add(const CheckoutReturned());
      await bloc.stream.firstWhere((s) => s.status == CheckoutStatus.awaitingPayment);

      expect(repo.createCalls, 1);
      expect(await store.read('b1'), isNotNull, reason: 'still resumable');
      await bloc.close();
    });

    test('returning with no stored attempt falls back to the booking\'s authoritative state', () async {
      repo.booking = _booking(paid: 200000, status: 'Confirmed');

      final bloc = buildBloc();
      bloc.add(start);
      await bloc.stream.firstWhere((s) => s.status == CheckoutStatus.paid);
      await bloc.close();
    });

    test('cancelling reports NOK so the server settles the attempt', () async {
      repo.createResults.add(const Right(PaymentIntent(authority: 'auth-1', paymentUrl: 'https://pay.test/1')));
      repo.verifyResults.add(const Right(PaymentStatusResult(status: 'Failed', failureReason: 'User cancelled')));

      final bloc = buildBloc();
      bloc.add(start);
      await bloc.stream.firstWhere((s) => s.status == CheckoutStatus.ready);
      bloc.add(const CheckoutPaymentRequested());
      await bloc.stream.firstWhere((s) => s.status == CheckoutStatus.awaitingPayment);

      bloc.add(const CheckoutCancelled());
      final failed = await bloc.stream.firstWhere((s) => s.status == CheckoutStatus.failed);

      expect(repo.verifyStatuses, ['NOK']);
      expect(failed.message, 'User cancelled');
      await bloc.close();
    });
  });

  group('retry', () {
    test('a retry after a definitive failure uses a NEW key (a deliberate new charge)', () async {
      repo.createResults.add(const Left(GatewayFailure('declined')));

      final bloc = buildBloc();
      bloc.add(start);
      await bloc.stream.firstWhere((s) => s.status == CheckoutStatus.ready);
      bloc.add(const CheckoutPaymentRequested());
      await bloc.stream.firstWhere((s) => s.status == CheckoutStatus.failed);

      bloc.add(const CheckoutRetried());
      await bloc.stream.firstWhere((s) => s.status == CheckoutStatus.ready);
      bloc.add(const CheckoutPaymentRequested());
      await bloc.stream.firstWhere((s) => s.status == CheckoutStatus.awaitingPayment);

      expect(repo.createKeys.length, 2);
      expect(repo.createKeys[0], isNot(repo.createKeys[1]),
          reason: 'a genuinely new attempt after a failed one must not replay the old result');
      await bloc.close();
    });

    test('retry is refused while the outcome is unknown — no second charge on uncertainty', () async {
      repo.createResults.add(const Left(NetworkFailure('timeout')));

      final bloc = buildBloc();
      bloc.add(start);
      await bloc.stream.firstWhere((s) => s.status == CheckoutStatus.ready);
      bloc.add(const CheckoutPaymentRequested());
      await bloc.stream.firstWhere((s) => s.status == CheckoutStatus.unknown);

      bloc.add(const CheckoutRetried());
      await Future<void>.delayed(Duration.zero);

      expect(bloc.state.status, CheckoutStatus.unknown, reason: 'retry must not clear an unknown attempt');
      await bloc.close();
    });
  });
}
