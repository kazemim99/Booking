import 'package:equatable/equatable.dart';
import 'provider_status.dart';

/// Authenticated provider user (domain entity).
class ProviderUser extends Equatable {
  final String id;
  final String phoneNumber;
  final String? email;
  final String? firstName;
  final String? lastName;
  final String fullName;

  const ProviderUser({
    required this.id,
    required this.phoneNumber,
    this.email,
    this.firstName,
    this.lastName,
    required this.fullName,
  });

  String get displayName => fullName.trim().isNotEmpty ? fullName : phoneNumber;

  @override
  List<Object?> get props => [id, phoneNumber, email, firstName, lastName, fullName];
}

/// A completed provider authentication session.
class ProviderSession extends Equatable {
  final String accessToken;
  final String refreshToken;
  final int expiresIn;
  final ProviderUser user;

  /// Provider aggregate id from ServiceCatalog; null when no profile yet.
  final String? providerId;

  /// Provider status derived from the response / JWT; null when no profile.
  final ProviderStatus? providerStatus;

  /// True on first-time registration (new user created by the backend).
  final bool isNewProvider;

  /// Backend hint that onboarding is required. Treated as a hint only —
  /// routing derives the truth from [providerStatus]/[providerId]
  /// (AUTH_SPECIFICATION.md BUG-1).
  final bool requiresOnboarding;

  const ProviderSession({
    required this.accessToken,
    required this.refreshToken,
    required this.expiresIn,
    required this.user,
    this.providerId,
    this.providerStatus,
    required this.isNewProvider,
    required this.requiresOnboarding,
  });

  /// True when the provider must be routed to the onboarding wizard:
  /// brand-new provider, no ServiceCatalog profile, or a Drafted profile.
  bool get needsOnboarding =>
      isNewProvider ||
      providerId == null ||
      (providerStatus?.needsOnboarding ?? true);

  /// True when the provider's account is blocked (suspended/inactive/archived)
  /// and must see the "account unavailable" screen (E-14 / D-1).
  bool get isBlocked => providerStatus?.isBlocked ?? false;

  ProviderSession copyWith({
    String? accessToken,
    String? refreshToken,
    int? expiresIn,
    ProviderStatus? providerStatus,
    String? providerId,
  }) {
    return ProviderSession(
      accessToken: accessToken ?? this.accessToken,
      refreshToken: refreshToken ?? this.refreshToken,
      expiresIn: expiresIn ?? this.expiresIn,
      user: user,
      providerId: providerId ?? this.providerId,
      providerStatus: providerStatus ?? this.providerStatus,
      isNewProvider: isNewProvider,
      requiresOnboarding: requiresOnboarding,
    );
  }

  @override
  List<Object?> get props => [
        accessToken,
        refreshToken,
        expiresIn,
        user,
        providerId,
        providerStatus,
        isNewProvider,
        requiresOnboarding,
      ];
}
