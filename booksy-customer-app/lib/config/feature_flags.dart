/// Compile-time feature flags, supplied with `--dart-define`.
///
/// Example: `flutter run -d chrome --dart-define=CHECKOUT_ENABLED=true`
///
/// A `const` flag lets the tree-shaker drop disabled features entirely, and keeps the default explicit in code
/// rather than in a build script.
class FeatureFlags {
  const FeatureFlags._();

  /// Customer checkout (deposit payment via the external-browser gateway flow).
  ///
  /// **OFF by default, deliberately.** The journey stays dark until it has passed web E2E and the remaining release
  /// gates are reviewed. When off, the booking flow behaves exactly as before — the booking is still created and the
  /// server still enforces its deposit gate, so this flag only controls whether the app *offers* to pay. It is not a
  /// bypass: it can never confirm a booking that the backend considers unpaid.
  static const bool checkoutEnabled =
      bool.fromEnvironment('CHECKOUT_ENABLED', defaultValue: false);
}
