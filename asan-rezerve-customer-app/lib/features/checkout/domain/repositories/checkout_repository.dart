import 'package:dartz/dartz.dart';
import '../../../../core/errors/failures.dart';
import '../entities/checkout_entities.dart';

/// A checkout failure raised when the gateway itself refused to create a payment (no money moved).
class GatewayFailure extends Failure {
  const GatewayFailure(super.message);
}

/// Raised when a duplicate/in-flight request is rejected by the server's idempotency reservation (HTTP 409).
/// The caller should re-read state rather than charging again.
class DuplicateRequestFailure extends Failure {
  const DuplicateRequestFailure(super.message);
}

abstract class CheckoutRepository {
  /// Reads the booking's server-owned money state (deposit requirement, amount already paid, booking status).
  Future<Either<Failure, BookingPaymentSnapshot>> getBookingPaymentSnapshot(String bookingId);

  /// Asks the server to create a gateway payment for this attempt.
  ///
  /// [idempotencyKey] MUST be stable for a given attempt: replaying a create with the same key makes the server
  /// return the original result instead of charging again.
  Future<Either<Failure, PaymentIntent>> createPayment({
    required String bookingId,
    required String providerId,
    required double amount,
    required String idempotencyKey,
    String? description,
    String? mobile,
    String? email,
  });

  /// Verifies an attempt by authority. Idempotent server-side: an already-verified payment returns its stored
  /// result. [status] reports what the gateway told the client ("OK"/"NOK") and is never treated as proof.
  Future<Either<Failure, PaymentStatusResult>> verifyPayment({
    required String authority,
    String status = 'OK',
    String? idempotencyKey,
  });

  /// Reads a payment record — a pure read used to confirm state on return/resume.
  Future<Either<Failure, PaymentStatusResult>> getPayment(String paymentId);
}

/// Persists the in-flight checkout attempt so an interrupted flow (app killed mid-redirect, browser never returned)
/// resumes instead of starting a second charge.
abstract class CheckoutAttemptStore {
  Future<CheckoutAttempt?> read(String bookingId);
  Future<void> save(CheckoutAttempt attempt);
  Future<void> clear(String bookingId);
}
