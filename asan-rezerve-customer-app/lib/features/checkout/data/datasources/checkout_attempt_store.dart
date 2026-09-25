import 'dart:convert';

import 'package:shared_preferences/shared_preferences.dart';

import '../../domain/entities/checkout_entities.dart';
import '../../domain/repositories/checkout_repository.dart';

/// Persists the in-flight checkout attempt (idempotency key + gateway authority) across app restarts.
///
/// This is what makes an interrupted checkout safe: if the app is killed while the customer is paying in the
/// browser, the next launch finds the attempt and *verifies* it instead of creating a second payment. Reusing the
/// stored idempotency key also means that even a re-issued create is de-duplicated by the server rather than
/// charging again.
///
/// `SharedPreferences` (not secure storage) is deliberate: an authority and a random key are not secrets — they are
/// useless without the user's authenticated session, and the server is the sole authority on payment state.
class SharedPrefsCheckoutAttemptStore implements CheckoutAttemptStore {
  static const String _keyPrefix = 'checkout_attempt_';

  final SharedPreferences prefs;

  SharedPrefsCheckoutAttemptStore({required this.prefs});

  String _storageKey(String bookingId) => '$_keyPrefix$bookingId';

  @override
  Future<CheckoutAttempt?> read(String bookingId) async {
    final raw = prefs.getString(_storageKey(bookingId));
    if (raw == null || raw.isEmpty) return null;
    try {
      final decoded = jsonDecode(raw);
      if (decoded is Map<String, dynamic>) return CheckoutAttempt.fromJson(decoded);
      return null;
    } catch (_) {
      // Corrupt entry must never block checkout; treat as "no attempt".
      return null;
    }
  }

  @override
  Future<void> save(CheckoutAttempt attempt) async {
    await prefs.setString(_storageKey(attempt.bookingId), jsonEncode(attempt.toJson()));
  }

  @override
  Future<void> clear(String bookingId) async {
    await prefs.remove(_storageKey(bookingId));
  }
}
