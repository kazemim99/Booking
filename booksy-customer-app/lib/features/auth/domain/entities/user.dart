import 'package:equatable/equatable.dart';

/// User Entity (Domain Layer)
/// Clean architecture: Domain entities should not depend on data layer
class User extends Equatable {
  final String id;
  final String phoneNumber;
  final String? email;
  final String? firstName;
  final String? lastName;
  final String? profilePictureUrl;
  final bool emailVerified;
  final bool phoneVerified;
  final DateTime createdAt;

  const User({
    required this.id,
    required this.phoneNumber,
    this.email,
    this.firstName,
    this.lastName,
    this.profilePictureUrl,
    this.emailVerified = false,
    this.phoneVerified = false,
    required this.createdAt,
  });

  String get fullName {
    if (firstName != null && lastName != null) {
      return '$firstName $lastName';
    }
    return firstName ?? lastName ?? phoneNumber;
  }

  String get displayName => fullName;

  @override
  List<Object?> get props => [
        id,
        phoneNumber,
        email,
        firstName,
        lastName,
        profilePictureUrl,
        emailVerified,
        phoneVerified,
        createdAt,
      ];
}

/// Customer Entity
class Customer extends Equatable {
  final String id;
  final String userId;
  final String? preferredLanguage;
  final List<String> favoriteProviders;
  final int bookingCount;
  final DateTime createdAt;

  const Customer({
    required this.id,
    required this.userId,
    this.preferredLanguage,
    this.favoriteProviders = const [],
    this.bookingCount = 0,
    required this.createdAt,
  });

  @override
  List<Object?> get props => [
        id,
        userId,
        preferredLanguage,
        favoriteProviders,
        bookingCount,
        createdAt,
      ];
}

/// Auth Session
class AuthSession extends Equatable {
  final String accessToken;
  final String refreshToken;
  final User user;
  final Customer? customer;
  final int expiresIn;

  const AuthSession({
    required this.accessToken,
    required this.refreshToken,
    required this.user,
    this.customer,
    required this.expiresIn,
  });

  @override
  List<Object?> get props => [
        accessToken,
        refreshToken,
        user,
        customer,
        expiresIn,
      ];
}
