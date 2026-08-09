import 'dart:math';

import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../domain/entities/checkout_entities.dart';
import '../../domain/repositories/checkout_repository.dart';

// ==================== Events ====================

abstract class CheckoutEvent extends Equatable {
  const CheckoutEvent();

  @override
  List<Object?> get props => const [];
}

/// Opens checkout for a booking. Reads server state first, and resumes an interrupted attempt rather than charging.
class CheckoutStarted extends CheckoutEvent {
  final String bookingId;
  final String providerId;

  const CheckoutStarted({required this.bookingId, required this.providerId});

  @override
  List<Object?> get props => [bookingId, providerId];
}

/// Customer confirmed they want to pay — create the gateway payment and hand off to the browser.
class CheckoutPaymentRequested extends CheckoutEvent {
  const CheckoutPaymentRequested();
}

/// The app came back to the foreground (or the user tapped "I've paid"): re-read the server-owned outcome.
class CheckoutReturned extends CheckoutEvent {
  const CheckoutReturned();
}

/// The customer abandoned the gateway; settle the attempt as cancelled so it does not linger as pending.
class CheckoutCancelled extends CheckoutEvent {
  const CheckoutCancelled();
}

/// Start a fresh attempt after a definitive failure (a new idempotency key — this is a new charge, intentionally).
class CheckoutRetried extends CheckoutEvent {
  const CheckoutRetried();
}

// ==================== State ====================

enum CheckoutStatus {
  initial,

  /// Reading booking/payment state from the server.
  loading,

  /// Showing the amount due, awaiting the customer's confirmation.
  ready,

  /// Creating the gateway payment.
  creating,

  /// Payment created; the customer must complete it in the external browser.
  awaitingPayment,

  /// Re-reading the authoritative outcome after a return/resume.
  verifying,

  /// Server confirms the money arrived.
  paid,

  /// Server says this attempt did not succeed; a new attempt may be started.
  failed,

  /// Could not determine the outcome (offline/error). Deliberately distinct from `failed`: we must NOT invite a
  /// second charge when the first one's fate is unknown — reconciliation owns that case.
  unknown,

  /// The provider requires no deposit, so there is nothing to collect online. The booking follows its normal
  /// confirmation path and the customer settles with the provider — we never invent an up-front charge.
  nothingDue,
}

class CheckoutState extends Equatable {
  final CheckoutStatus status;
  final String? bookingId;
  final String? providerId;
  final BookingPaymentSnapshot? booking;
  final CheckoutAttempt? attempt;

  /// The gateway URL to open, when a payment has been created.
  final String? paymentUrl;
  final String? refNumber;
  final String? message;

  const CheckoutState({
    this.status = CheckoutStatus.initial,
    this.bookingId,
    this.providerId,
    this.booking,
    this.attempt,
    this.paymentUrl,
    this.refNumber,
    this.message,
  });

  /// The amount this checkout collects (deposit when required, else the full price).
  double get amountDue => booking?.amountDue ?? 0;

  bool get isBusy =>
      status == CheckoutStatus.loading || status == CheckoutStatus.creating || status == CheckoutStatus.verifying;

  /// True when a previously created attempt can be verified instead of re-created.
  bool get canResume => attempt?.isResumable ?? false;

  CheckoutState copyWith({
    CheckoutStatus? status,
    String? bookingId,
    String? providerId,
    BookingPaymentSnapshot? booking,
    CheckoutAttempt? attempt,
    String? paymentUrl,
    String? refNumber,
    String? message,
    bool clearMessage = false,
    bool clearPaymentUrl = false,
  }) {
    return CheckoutState(
      status: status ?? this.status,
      bookingId: bookingId ?? this.bookingId,
      providerId: providerId ?? this.providerId,
      booking: booking ?? this.booking,
      attempt: attempt ?? this.attempt,
      paymentUrl: clearPaymentUrl ? null : (paymentUrl ?? this.paymentUrl),
      refNumber: refNumber ?? this.refNumber,
      message: clearMessage ? null : (message ?? this.message),
    );
  }

  @override
  List<Object?> get props =>
      [status, bookingId, providerId, booking, attempt, paymentUrl, refNumber, message];
}

// ==================== Bloc ====================

/// Opens the gateway URL. Injected so the flow is testable without a browser, and so the launch mechanism (external
/// browser today) can change without touching the bloc.
typedef PaymentUrlLauncher = Future<bool> Function(String url);

/// Generates idempotency keys; injectable for deterministic tests.
typedef IdempotencyKeyGenerator = String Function();

/// Alpha checkout: external-browser redirect with server-owned verification.
///
/// Invariants this bloc protects:
/// * **Never charge twice.** Before creating anything it reads the server: an already-paid deposit short-circuits to
///   `paid`, and a stored resumable attempt is *verified* rather than re-created. A re-created attempt reuses the
///   stored idempotency key so the server's reservation de-duplicates it.
/// * **Never trust the client.** `paid` is only ever set from a server response.
/// * **Never invite a duplicate charge on uncertainty.** Transport failures land in `unknown`, not `failed`.
class CheckoutBloc extends Bloc<CheckoutEvent, CheckoutState> {
  final CheckoutRepository repository;
  final CheckoutAttemptStore attemptStore;
  final PaymentUrlLauncher launchPaymentUrl;
  final IdempotencyKeyGenerator generateIdempotencyKey;

  CheckoutBloc({
    required this.repository,
    required this.attemptStore,
    required this.launchPaymentUrl,
    IdempotencyKeyGenerator? keyGenerator,
  })  : generateIdempotencyKey = keyGenerator ?? _defaultKeyGenerator,
        super(const CheckoutState()) {
    on<CheckoutStarted>(_onStarted);
    on<CheckoutPaymentRequested>(_onPaymentRequested);
    on<CheckoutReturned>(_onReturned);
    on<CheckoutCancelled>(_onCancelled);
    on<CheckoutRetried>(_onRetried);
  }

  static final Random _random = Random.secure();

  /// RFC-4122 v4 GUID (the backend parses the Idempotency-Key header as a GUID).
  static String _defaultKeyGenerator() {
    final bytes = List<int>.generate(16, (_) => _random.nextInt(256));
    bytes[6] = (bytes[6] & 0x0f) | 0x40; // version 4
    bytes[8] = (bytes[8] & 0x3f) | 0x80; // variant
    String hex(int start, int end) =>
        bytes.sublist(start, end).map((b) => b.toRadixString(16).padLeft(2, '0')).join();
    return '${hex(0, 4)}-${hex(4, 6)}-${hex(6, 8)}-${hex(8, 10)}-${hex(10, 16)}';
  }

  Future<void> _onStarted(CheckoutStarted event, Emitter<CheckoutState> emit) async {
    emit(CheckoutState(
      status: CheckoutStatus.loading,
      bookingId: event.bookingId,
      providerId: event.providerId,
    ));

    final snapshot = await repository.getBookingPaymentSnapshot(event.bookingId);

    await snapshot.fold(
      (failure) async => emit(state.copyWith(status: CheckoutStatus.unknown, message: failure.message)),
      (booking) async {
        // Already settled server-side — never offer to pay again.
        if (booking.isDepositPaid) {
          await attemptStore.clear(event.bookingId);
          emit(state.copyWith(status: CheckoutStatus.paid, booking: booking));
          return;
        }

        // No deposit configured ⇒ nothing to collect online. Never charge the full price here.
        if (!booking.requiresDeposit) {
          await attemptStore.clear(event.bookingId);
          emit(state.copyWith(status: CheckoutStatus.nothingDue, booking: booking));
          return;
        }

        // An interrupted attempt exists: verify it rather than starting a second charge.
        final stored = await attemptStore.read(event.bookingId);
        if (stored != null && stored.isResumable) {
          emit(state.copyWith(status: CheckoutStatus.verifying, booking: booking, attempt: stored));
          await _verifyAttempt(stored, emit, reportedStatus: 'OK');
          return;
        }

        emit(state.copyWith(
          status: CheckoutStatus.ready,
          booking: booking,
          attempt: stored,
        ));
      },
    );
  }

  Future<void> _onPaymentRequested(CheckoutPaymentRequested event, Emitter<CheckoutState> emit) async {
    final bookingId = state.bookingId;
    final providerId = state.providerId;
    final booking = state.booking;
    if (bookingId == null || providerId == null || booking == null) return;

    // Hard guard: never initiate a charge when the server says nothing is due. Protects against a UI path (or a
    // stale state) trying to collect money the provider never asked for.
    if (!booking.hasAmountDue) {
      emit(state.copyWith(
        status: booking.isDepositPaid ? CheckoutStatus.paid : CheckoutStatus.nothingDue,
      ));
      return;
    }

    // Reuse the stored key when one exists so a repeat create is de-duplicated by the server instead of charging.
    final existing = state.attempt ?? await attemptStore.read(bookingId);
    final attempt = existing ??
        CheckoutAttempt(bookingId: bookingId, idempotencyKey: generateIdempotencyKey());

    // Persist BEFORE calling the gateway: if we crash mid-create, the next launch can still resume/verify.
    await attemptStore.save(attempt);
    emit(state.copyWith(status: CheckoutStatus.creating, attempt: attempt, clearMessage: true));

    final result = await repository.createPayment(
      bookingId: bookingId,
      providerId: providerId,
      amount: booking.amountDue,
      idempotencyKey: attempt.idempotencyKey,
    );

    await result.fold(
      (failure) async {
        // A duplicate/in-flight rejection means an earlier request may already be charging: resolve state, never
        // re-create. We may not hold that request's authority (it could predate this attempt, or come from another
        // device), so fall back to the booking — authoritative for whether the deposit is now covered.
        if (failure is DuplicateRequestFailure) {
          emit(state.copyWith(status: CheckoutStatus.verifying));
          if (attempt.isResumable) {
            await _verifyAttempt(attempt, emit, reportedStatus: 'OK');
          } else {
            await _resolveFromBooking(bookingId, emit,
                unpaidStatus: CheckoutStatus.unknown, unpaidMessage: failure.message);
          }
          return;
        }
        if (failure is GatewayFailure) {
          emit(state.copyWith(status: CheckoutStatus.failed, message: failure.message));
          return;
        }
        emit(state.copyWith(status: CheckoutStatus.unknown, message: failure.message));
      },
      (intent) async {
        final updated = attempt.copyWith(authority: intent.authority, paymentId: intent.paymentId);
        await attemptStore.save(updated);

        final opened = await launchPaymentUrl(intent.paymentUrl);
        emit(state.copyWith(
          status: opened ? CheckoutStatus.awaitingPayment : CheckoutStatus.unknown,
          attempt: updated,
          paymentUrl: intent.paymentUrl,
          message: opened ? null : 'Could not open the payment page.',
          clearMessage: opened,
        ));
      },
    );
  }

  Future<void> _onReturned(CheckoutReturned event, Emitter<CheckoutState> emit) async {
    final bookingId = state.bookingId;
    if (bookingId == null) return;

    final attempt = state.attempt ?? await attemptStore.read(bookingId);
    emit(state.copyWith(status: CheckoutStatus.verifying, clearMessage: true));

    if (attempt != null && attempt.isResumable) {
      await _verifyAttempt(attempt, emit, reportedStatus: 'OK');
      return;
    }

    // No attempt handle — fall back to the booking's own authoritative money state. Nothing was created for this
    // session, so "not paid" simply means the customer can still start a payment.
    await _resolveFromBooking(bookingId, emit, unpaidStatus: CheckoutStatus.ready);
  }

  Future<void> _onCancelled(CheckoutCancelled event, Emitter<CheckoutState> emit) async {
    final attempt = state.attempt;
    if (attempt == null || !attempt.isResumable) {
      emit(state.copyWith(status: CheckoutStatus.failed, message: 'Payment cancelled.'));
      return;
    }

    // Report the cancellation so the server settles it now (it still verifies with the gateway; an already-paid
    // payment is never flipped to failed).
    emit(state.copyWith(status: CheckoutStatus.verifying));
    await _verifyAttempt(attempt, emit, reportedStatus: 'NOK');
  }

  Future<void> _onRetried(CheckoutRetried event, Emitter<CheckoutState> emit) async {
    final bookingId = state.bookingId;
    final booking = state.booking;
    if (bookingId == null || booking == null) return;

    // Only a definitively failed attempt may be retried; a new key means a deliberate new charge.
    if (state.status != CheckoutStatus.failed) return;

    await attemptStore.clear(bookingId);
    emit(state.copyWith(
      status: CheckoutStatus.ready,
      attempt: CheckoutAttempt(bookingId: bookingId, idempotencyKey: generateIdempotencyKey()),
      clearMessage: true,
      clearPaymentUrl: true,
    ));
  }

  /// Verifies an attempt against the server and maps the authoritative answer onto the state.
  Future<void> _verifyAttempt(
    CheckoutAttempt attempt,
    Emitter<CheckoutState> emit, {
    required String reportedStatus,
  }) async {
    // Defensive: without a gateway handle there is nothing to verify against, so consult the booking instead.
    // (Reached when a create was rejected or interrupted before an authority was ever stored.)
    if (!attempt.isResumable) {
      await _resolveFromBooking(attempt.bookingId, emit, unpaidStatus: CheckoutStatus.unknown);
      return;
    }

    // Prefer a pure read when we know the payment id; fall back to the idempotent verify by authority.
    final result = attempt.paymentId != null && attempt.paymentId!.isNotEmpty
        ? await repository.getPayment(attempt.paymentId!)
        : await repository.verifyPayment(
            authority: attempt.authority!,
            status: reportedStatus,
            idempotencyKey: attempt.idempotencyKey,
          );

    await result.fold(
      (failure) async {
        // Unknown outcome: keep the attempt so a later resume can settle it, and do not offer a second charge.
        emit(state.copyWith(status: CheckoutStatus.unknown, message: failure.message));
      },
      (payment) async {
        if (payment.isPaid) {
          await attemptStore.clear(attempt.bookingId);
          emit(state.copyWith(
            status: CheckoutStatus.paid,
            refNumber: payment.refNumber,
            clearMessage: true,
          ));
          return;
        }
        if (payment.isFailed) {
          await attemptStore.clear(attempt.bookingId);
          emit(state.copyWith(status: CheckoutStatus.failed, message: payment.failureReason));
          return;
        }
        // Still pending at the gateway: the customer may not have finished. Keep awaiting; never re-charge.
        emit(state.copyWith(status: CheckoutStatus.awaitingPayment, clearMessage: true));
      },
    );
  }

  /// Resolves checkout state from the booking itself — the server's authority on whether the money obligation is
  /// already covered. [unpaidStatus] lets the caller choose what "not yet paid" means in its context: `ready` when
  /// nothing has been attempted, `unknown` when a charge may be in flight and a second one must not be invited.
  Future<void> _resolveFromBooking(
    String bookingId,
    Emitter<CheckoutState> emit, {
    required CheckoutStatus unpaidStatus,
    String? unpaidMessage,
  }) async {
    final snapshot = await repository.getBookingPaymentSnapshot(bookingId);
    await snapshot.fold(
      (failure) async => emit(state.copyWith(status: CheckoutStatus.unknown, message: failure.message)),
      (booking) async {
        if (booking.isDepositPaid) {
          await attemptStore.clear(bookingId);
          emit(state.copyWith(status: CheckoutStatus.paid, booking: booking, clearMessage: true));
          return;
        }
        emit(state.copyWith(
          status: unpaidStatus,
          booking: booking,
          message: unpaidMessage,
          clearMessage: unpaidMessage == null,
        ));
      },
    );
  }
}
