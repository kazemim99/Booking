// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'auth_models.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

SendVerificationCodeRequest _$SendVerificationCodeRequestFromJson(
        Map<String, dynamic> json) =>
    SendVerificationCodeRequest(
      phoneNumber: json['phoneNumber'] as String,
      countryCode: json['countryCode'] as String? ?? '+98',
    );

Map<String, dynamic> _$SendVerificationCodeRequestToJson(
        SendVerificationCodeRequest instance) =>
    <String, dynamic>{
      'phoneNumber': instance.phoneNumber,
      'countryCode': instance.countryCode,
    };

CompleteCustomerAuthRequest _$CompleteCustomerAuthRequestFromJson(
        Map<String, dynamic> json) =>
    CompleteCustomerAuthRequest(
      phoneNumber: json['phoneNumber'] as String,
      code: json['code'] as String,
      firstName: json['firstName'] as String?,
      lastName: json['lastName'] as String?,
      email: json['email'] as String?,
    );

Map<String, dynamic> _$CompleteCustomerAuthRequestToJson(
        CompleteCustomerAuthRequest instance) =>
    <String, dynamic>{
      'phoneNumber': instance.phoneNumber,
      'code': instance.code,
      'firstName': instance.firstName,
      'lastName': instance.lastName,
      'email': instance.email,
    };

ResendOtpRequest _$ResendOtpRequestFromJson(Map<String, dynamic> json) =>
    ResendOtpRequest(
      phoneNumber: json['phoneNumber'] as String,
    );

Map<String, dynamic> _$ResendOtpRequestToJson(ResendOtpRequest instance) =>
    <String, dynamic>{
      'phoneNumber': instance.phoneNumber,
    };

RefreshTokenRequest _$RefreshTokenRequestFromJson(Map<String, dynamic> json) =>
    RefreshTokenRequest(
      refreshToken: json['refreshToken'] as String,
    );

Map<String, dynamic> _$RefreshTokenRequestToJson(
        RefreshTokenRequest instance) =>
    <String, dynamic>{
      'refreshToken': instance.refreshToken,
    };

SendVerificationCodeResponse _$SendVerificationCodeResponseFromJson(
        Map<String, dynamic> json) =>
    SendVerificationCodeResponse(
      verificationId: json['verificationId'] as String,
      maskedPhoneNumber: json['maskedPhoneNumber'] as String,
      expiresAt: json['expiresAt'] as String,
      maxAttempts: (json['maxAttempts'] as num).toInt(),
      message: json['message'] as String,
    );

Map<String, dynamic> _$SendVerificationCodeResponseToJson(
        SendVerificationCodeResponse instance) =>
    <String, dynamic>{
      'verificationId': instance.verificationId,
      'maskedPhoneNumber': instance.maskedPhoneNumber,
      'expiresAt': instance.expiresAt,
      'maxAttempts': instance.maxAttempts,
      'message': instance.message,
    };

// _$CompleteCustomerAuthResponseFromJson was removed deliberately.
//
// It parsed a NESTED `user` object (`json['user'] as Map<String, dynamic>`), but the server sends this payload
// flat, so that cast received null and threw
//   type 'Null' is not a subtype of type 'Map<String, dynamic>'
// on every customer login. CompleteCustomerAuthResponse.fromJson in auth_models.dart now parses both shapes by
// hand; this generated version is dead and is not left behind as a trap for whoever next reads this file.
//
// Normally regenerating would restore it, but build_runner codegen is broken on this toolchain (see CLAUDE.md),
// so this file is effectively hand-maintained. If codegen is ever fixed, the annotation on the model must be
// reconciled with the real wire format before trusting a regenerated parser.

Map<String, dynamic> _$CompleteCustomerAuthResponseToJson(
        CompleteCustomerAuthResponse instance) =>
    <String, dynamic>{
      'accessToken': instance.accessToken,
      'refreshToken': instance.refreshToken,
      'userId': instance.userId,
      'customerId': instance.customerId,
      'user': instance.user,
      'customer': instance.customer,
      'expiresIn': instance.expiresIn,
    };

UserDto _$UserDtoFromJson(Map<String, dynamic> json) => UserDto(
      id: json['id'] as String,
      phoneNumber: json['phoneNumber'] as String,
      email: json['email'] as String?,
      firstName: json['firstName'] as String?,
      lastName: json['lastName'] as String?,
      profilePictureUrl: json['profilePictureUrl'] as String?,
      emailVerified: json['emailVerified'] as bool? ?? false,
      phoneVerified: json['phoneVerified'] as bool? ?? false,
      createdAt: DateTime.parse(json['createdAt'] as String),
      updatedAt: json['updatedAt'] == null
          ? null
          : DateTime.parse(json['updatedAt'] as String),
    );

Map<String, dynamic> _$UserDtoToJson(UserDto instance) => <String, dynamic>{
      'id': instance.id,
      'phoneNumber': instance.phoneNumber,
      'email': instance.email,
      'firstName': instance.firstName,
      'lastName': instance.lastName,
      'profilePictureUrl': instance.profilePictureUrl,
      'emailVerified': instance.emailVerified,
      'phoneVerified': instance.phoneVerified,
      'createdAt': instance.createdAt.toIso8601String(),
      'updatedAt': instance.updatedAt?.toIso8601String(),
    };

CustomerDto _$CustomerDtoFromJson(Map<String, dynamic> json) => CustomerDto(
      id: json['id'] as String,
      userId: json['userId'] as String,
      preferredLanguage: json['preferredLanguage'] as String?,
      favoriteProviders: (json['favoriteProviders'] as List<dynamic>?)
          ?.map((e) => e as String)
          .toList(),
      bookingCount: (json['bookingCount'] as num?)?.toInt() ?? 0,
      createdAt: DateTime.parse(json['createdAt'] as String),
      updatedAt: json['updatedAt'] == null
          ? null
          : DateTime.parse(json['updatedAt'] as String),
    );

Map<String, dynamic> _$CustomerDtoToJson(CustomerDto instance) =>
    <String, dynamic>{
      'id': instance.id,
      'userId': instance.userId,
      'preferredLanguage': instance.preferredLanguage,
      'favoriteProviders': instance.favoriteProviders,
      'bookingCount': instance.bookingCount,
      'createdAt': instance.createdAt.toIso8601String(),
      'updatedAt': instance.updatedAt?.toIso8601String(),
    };

RefreshTokenResponse _$RefreshTokenResponseFromJson(
        Map<String, dynamic> json) =>
    RefreshTokenResponse(
      accessToken: json['accessToken'] as String,
      refreshToken: json['refreshToken'] as String,
      expiresIn: (json['expiresIn'] as num).toInt(),
    );

Map<String, dynamic> _$RefreshTokenResponseToJson(
        RefreshTokenResponse instance) =>
    <String, dynamic>{
      'accessToken': instance.accessToken,
      'refreshToken': instance.refreshToken,
      'expiresIn': instance.expiresIn,
    };
