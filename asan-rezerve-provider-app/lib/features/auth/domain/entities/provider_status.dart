/// Provider verification/lifecycle status.
///
/// Values match the backend ServiceCatalog `ProviderStatus` enum and the Vue
/// `ProviderStatus` (AUTH_SPECIFICATION.md §4.4).
enum ProviderStatus {
  drafted('Drafted'),
  pendingVerification('PendingVerification'),
  verified('Verified'),
  active('Active'),
  inactive('Inactive'),
  suspended('Suspended'),
  archived('Archived');

  const ProviderStatus(this.wireName);

  /// The exact string the backend emits (JWT `provider_status`, status API).
  final String wireName;

  /// Parses a backend status string; returns null when absent/unrecognized.
  static ProviderStatus? tryParse(String? value) {
    if (value == null || value.isEmpty) return null;
    for (final s in ProviderStatus.values) {
      if (s.wireName == value) return s;
    }
    return null;
  }

  /// Provider must complete business onboarding (no profile yet, or drafted).
  /// Routing relies on this rather than the backend `requiresOnboarding` flag,
  /// which is unreliable (AUTH_SPECIFICATION.md BUG-1).
  bool get needsOnboarding =>
      this == ProviderStatus.drafted;

  /// Onboarding complete and the provider may use the dashboard.
  bool get canUseDashboard =>
      this == ProviderStatus.verified ||
      this == ProviderStatus.active ||
      this == ProviderStatus.pendingVerification;

  /// Access must be blocked with an "account unavailable" screen
  /// (AUTH_SPECIFICATION.md E-14 / D-1 — Flutter improvement over Vue).
  bool get isBlocked =>
      this == ProviderStatus.suspended ||
      this == ProviderStatus.inactive ||
      this == ProviderStatus.archived;
}
