import 'package:json_annotation/json_annotation.dart';

part 'auth_models.g.dart';

// ==================== Request Models ====================

/// Send verification code request
@JsonSerializable()
class SendVerificationCodeRequest {
  final String phoneNumber;
  final String countryCode;

  SendVerificationCodeRequest({
    required this.phoneNumber,
    this.countryCode = '+98', // Default to Iran
  });

  factory SendVerificationCodeRequest.fromJson(Map<String, dynamic> json) =>
      _$SendVerificationCodeRequestFromJson(json);

  Map<String, dynamic> toJson() => _$SendVerificationCodeRequestToJson(this);
}

/// Complete customer authentication request
@JsonSerializable()
class CompleteCustomerAuthRequest {
  final String phoneNumber;
  final String code;
  final String? firstName;
  final String? lastName;
  final String? email;

  CompleteCustomerAuthRequest({
    required this.phoneNumber,
    required this.code,
    this.firstName,
    this.lastName,
    this.email,
  });

  factory CompleteCustomerAuthRequest.fromJson(Map<String, dynamic> json) =>
      _$CompleteCustomerAuthRequestFromJson(json);

  Map<String, dynamic> toJson() => _$CompleteCustomerAuthRequestToJson(this);
}

/// Resend OTP request
@JsonSerializable()
class ResendOtpRequest {
  final String phoneNumber;

  ResendOtpRequest({required this.phoneNumber});

  factory ResendOtpRequest.fromJson(Map<String, dynamic> json) =>
      _$ResendOtpRequestFromJson(json);

  Map<String, dynamic> toJson() => _$ResendOtpRequestToJson(this);
}

/// Refresh token request
@JsonSerializable()
class RefreshTokenRequest {
  final String refreshToken;

  RefreshTokenRequest({required this.refreshToken});

  factory RefreshTokenRequest.fromJson(Map<String, dynamic> json) =>
      _$RefreshTokenRequestFromJson(json);

  Map<String, dynamic> toJson() => _$RefreshTokenRequestToJson(this);
}

// ==================== Response Models ====================

/// Send verification code response
@JsonSerializable()
class SendVerificationCodeResponse {
  final String verificationId;
  final String maskedPhoneNumber;
  final String expiresAt;
  final int maxAttempts;
  final String message;

  SendVerificationCodeResponse({
    required this.verificationId,
    required this.maskedPhoneNumber,
    required this.expiresAt,
    required this.maxAttempts,
    required this.message,
  });

  factory SendVerificationCodeResponse.fromJson(Map<String, dynamic> json) =>
      _$SendVerificationCodeResponseFromJson(json);

  Map<String, dynamic> toJson() => _$SendVerificationCodeResponseToJson(this);
}

/// Complete customer authentication response
@JsonSerializable()
class CompleteCustomerAuthResponse {
  final String accessToken;
  final String refreshToken;
  final String userId;
  final String customerId;
  final UserDto user;
  final CustomerDto? customer;
  final int expiresIn;

  CompleteCustomerAuthResponse({
    required this.accessToken,
    required this.refreshToken,
    required this.userId,
    required this.customerId,
    required this.user,
    this.customer,
    required this.expiresIn,
  });

  factory CompleteCustomerAuthResponse.fromJson(Map<String, dynamic> json) =>
      _$CompleteCustomerAuthResponseFromJson(json);

  Map<String, dynamic> toJson() => _$CompleteCustomerAuthResponseToJson(this);
}

/// User DTO
@JsonSerializable()
class UserDto {
  final String id;
  final String phoneNumber;
  final String? email;
  final String? firstName;
  final String? lastName;
  final String? profilePictureUrl;
  final bool emailVerified;
  final bool phoneVerified;
  final DateTime createdAt;
  final DateTime? updatedAt;

  UserDto({
    required this.id,
    required this.phoneNumber,
    this.email,
    this.firstName,
    this.lastName,
    this.profilePictureUrl,
    this.emailVerified = false,
    this.phoneVerified = false,
    required this.createdAt,
    this.updatedAt,
  });

  factory UserDto.fromJson(Map<String, dynamic> json) =>
      _$UserDtoFromJson(json);

  Map<String, dynamic> toJson() => _$UserDtoToJson(this);

  String get fullName {
    if (firstName != null && lastName != null) {
      return '$firstName $lastName';
    }
    return firstName ?? lastName ?? phoneNumber;
  }
}

/// Customer DTO
@JsonSerializable()
class CustomerDto {
  final String id;
  final String userId;
  final String? preferredLanguage;
  final List<String>? favoriteProviders;
  final int bookingCount;
  final DateTime createdAt;
  final DateTime? updatedAt;

  CustomerDto({
    required this.id,
    required this.userId,
    this.preferredLanguage,
    this.favoriteProviders,
    this.bookingCount = 0,
    required this.createdAt,
    this.updatedAt,
  });

  factory CustomerDto.fromJson(Map<String, dynamic> json) =>
      _$CustomerDtoFromJson(json);

  Map<String, dynamic> toJson() => _$CustomerDtoToJson(this);
}

/// Refresh token response
@JsonSerializable()
class RefreshTokenResponse {
  final String accessToken;
  final String refreshToken;
  final int expiresIn;

  RefreshTokenResponse({
    required this.accessToken,
    required this.refreshToken,
    required this.expiresIn,
  });

  factory RefreshTokenResponse.fromJson(Map<String, dynamic> json) =>
      _$RefreshTokenResponseFromJson(json);

  Map<String, dynamic> toJson() => _$RefreshTokenResponseToJson(this);
}
