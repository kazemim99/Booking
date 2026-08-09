/// Checkout domain entities.
///
/// Alpha checkout is a **server-owned redirect flow**: we ask the backend to create a gateway payment, the customer
/// pays in an external browser, ZarinPal redirects to the backend callback which verifies the payment server-side,
/// and we then re-read the authoritative state. Nothing here treats a browser return as proof of payment.
library;

/// The outcome of asking the server to create a gateway payment.
class PaymentIntent {
  /// Server-side payment id, when the backend returned one.
  final String? paymentId;

  /// Gateway authority — the handle used to verify this attempt afterwards.
  final String authority;

  /// The gateway URL the customer must open to pay.
  final String paymentUrl;

  const PaymentIntent({
    required this.authority,
    required this.paymentUrl,
    this.paymentId,
  });

  bool get isUsable => authority.isNotEmpty && paymentUrl.isNotEmpty;
}

/// Server-owned payment state. `isPaid` is the only thing the UI may treat as "money received".
class PaymentStatusResult {
  final String? paymentId;
  final String? bookingId;
  final String status;
  final String? refNumber;
  final String? failureReason;

  const PaymentStatusResult({
    required this.status,
    this.paymentId,
    this.bookingId,
    this.refNumber,
    this.failureReason,
  });

  static const _paidStatuses = {'paid', 'partiallypaid', 'completed'};
  static const _failedStatuses = {'failed', 'cancelled', 'canceled', 'expired'};

  bool get isPaid => _paidStatuses.contains(status.toLowerCase());
  bool get isFailed => _failedStatuses.contains(status.toLowerCase());

  /// Neither settled nor failed — the customer may still be paying, or the callback has not landed yet.
  bool get isPending => !isPaid && !isFailed;
}

/// What a booking says about its own money, straight from the server. Authoritative for deposit gating.
class BookingPaymentSnapshot {
  final String bookingId;
  final String bookingStatus;
  final double totalAmount;
  final double depositAmount;
  final double paidAmount;
  final String paymentStatus;

  const BookingPaymentSnapshot({
    required this.bookingId,
    required this.bookingStatus,
    required this.totalAmount,
    required this.depositAmount,
    required this.paidAmount,
    required this.paymentStatus,
  });

  bool get requiresDeposit => depositAmount > 0;

  /// True when the deposit obligation is already covered — the signal that no further charge is needed.
  bool get isDepositPaid => paidAmount >= depositAmount && depositAmount > 0;

  bool get isConfirmed => bookingStatus.toLowerCase() == 'confirmed';

  /// The amount this checkout collects: **only ever the configured deposit**.
  ///
  /// Deliberately not the full price. The backend's gate is `Policy.RequireDeposit` + a deposit percentage; a
  /// provider who requires no deposit is not asking for online pre-payment, so charging the full amount up front
  /// would invent a financial rule the server does not have. When no deposit is configured, nothing is due here and
  /// the customer settles with the provider as before.
  double get amountDue => depositAmount;

  /// True when this checkout has money to collect right now.
  bool get hasAmountDue => requiresDeposit && !isDepositPaid;
}

/// A checkout attempt persisted locally so an interrupted flow can be resumed instead of re-charging.
///
/// The `idempotencyKey` is the crux of duplicate protection: replaying a create with the same key makes the server
/// return the original result rather than charging again (backend C2 §2 atomic idempotency reservation).
class CheckoutAttempt {
  final String bookingId;
  final String idempotencyKey;
  final String? authority;
  final String? paymentId;

  const CheckoutAttempt({
    required this.bookingId,
    required this.idempotencyKey,
    this.authority,
    this.paymentId,
  });

  /// True when the attempt reached the gateway and can be verified rather than restarted.
  bool get isResumable => (authority != null && authority!.isNotEmpty) || (paymentId != null && paymentId!.isNotEmpty);

  CheckoutAttempt copyWith({String? authority, String? paymentId}) => CheckoutAttempt(
        bookingId: bookingId,
        idempotencyKey: idempotencyKey,
        authority: authority ?? this.authority,
        paymentId: paymentId ?? this.paymentId,
      );

  Map<String, dynamic> toJson() => {
        'bookingId': bookingId,
        'idempotencyKey': idempotencyKey,
        if (authority != null) 'authority': authority,
        if (paymentId != null) 'paymentId': paymentId,
      };

  static CheckoutAttempt? fromJson(Map<String, dynamic> json) {
    final bookingId = json['bookingId'] as String?;
    final key = json['idempotencyKey'] as String?;
    if (bookingId == null || bookingId.isEmpty || key == null || key.isEmpty) return null;
    return CheckoutAttempt(
      bookingId: bookingId,
      idempotencyKey: key,
      authority: json['authority'] as String?,
      paymentId: json['paymentId'] as String?,
    );
  }
}
