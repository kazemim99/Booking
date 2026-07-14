import '../../domain/entities/provider_session.dart';
import '../../domain/entities/provider_status.dart';

// ==================== Requests ====================

/// Send OTP request (AUTH_SPECIFICATION.md §5.1).
///
/// [countryCode] defaults to `"IR"` — a NON-DIGIT marker. The backend prepends
/// it to the phone before normalizing; a non-digit prefix is stripped, so the
/// stored `Value` equals the canonical `09…` used at complete-auth lookup
/// (§4.1 / E-1). Sending `"+98"` here would store `"+9809…"` and break login.
class SendVerificationCodeRequest {
  final String phoneNumber;
  final String countryCode;

  const SendVerificationCodeRequest({
    required this.phoneNumber,
    this.countryCode = 'IR',
  });

  Map<String, dynamic> toJson() => {
        'phoneNumber': phoneNumber,
        'countryCode': countryCode,
      };
}

/// Provider verify + login/register request (§5.2).
class CompleteProviderAuthRequest {
  final String phoneNumber;
  final String code;
  final String? firstName;
  final String? lastName;
  final String? email;

  const CompleteProviderAuthRequest({
    required this.phoneNumber,
    required this.code,
    this.firstName,
    this.lastName,
    this.email,
  });

  Map<String, dynamic> toJson() => {
        'phoneNumber': phoneNumber,
        'code': code,
        if (firstName != null) 'firstName': firstName,
        if (lastName != null) 'lastName': lastName,
        if (email != null) 'email': email,
      };
}

class RefreshTokenRequest {
  final String refreshToken;
  const RefreshTokenRequest({required this.refreshToken});
  Map<String, dynamic> toJson() => {'refreshToken': refreshToken};
}

// ==================== Responses ====================

class SendVerificationCodeResponse {
  final String verificationId;
  final String maskedPhoneNumber;
  final String? expiresAt;
  final int? maxAttempts;
  final String message;

  const SendVerificationCodeResponse({
    required this.verificationId,
    required this.maskedPhoneNumber,
    this.expiresAt,
    this.maxAttempts,
    required this.message,
  });

  factory SendVerificationCodeResponse.fromJson(Map<String, dynamic> json) {
    return SendVerificationCodeResponse(
      verificationId: (json['verificationId'] ?? '').toString(),
      maskedPhoneNumber: (json['maskedPhoneNumber'] ?? '').toString(),
      expiresAt: json['expiresAt']?.toString(),
      maxAttempts: (json['maxAttempts'] as num?)?.toInt(),
      message: (json['message'] ?? '').toString(),
    );
  }
}

/// Provider auth response (§5.2). Maps directly to the domain [ProviderSession].
class CompleteProviderAuthResponse {
  final bool isNewProvider;
  final String userId;
  final String? providerId;
  final String? providerStatus;
  final String phoneNumber;
  final String? email;
  final String fullName;
  final String accessToken;
  final String refreshToken;
  final int expiresIn;
  final bool requiresOnboarding;
  final String message;

  const CompleteProviderAuthResponse({
    required this.isNewProvider,
    required this.userId,
    this.providerId,
    this.providerStatus,
    required this.phoneNumber,
    this.email,
    required this.fullName,
    required this.accessToken,
    required this.refreshToken,
    required this.expiresIn,
    required this.requiresOnboarding,
    required this.message,
  });

  factory CompleteProviderAuthResponse.fromJson(Map<String, dynamic> json) {
    return CompleteProviderAuthResponse(
      isNewProvider: json['isNewProvider'] == true,
      userId: (json['userId'] ?? '').toString(),
      providerId: json['providerId']?.toString(),
      providerStatus: json['providerStatus']?.toString(),
      phoneNumber: (json['phoneNumber'] ?? '').toString(),
      email: json['email']?.toString(),
      fullName: (json['fullName'] ?? '').toString(),
      accessToken: (json['accessToken'] ?? '').toString(),
      refreshToken: (json['refreshToken'] ?? '').toString(),
      expiresIn: (json['expiresIn'] as num?)?.toInt() ?? 0,
      requiresOnboarding: json['requiresOnboarding'] == true,
      message: (json['message'] ?? '').toString(),
    );
  }

  ProviderSession toSession() {
    final names = fullName.trim().split(RegExp(r'\s+'));
    return ProviderSession(
      accessToken: accessToken,
      refreshToken: refreshToken,
      expiresIn: expiresIn,
      user: ProviderUser(
        id: userId,
        phoneNumber: phoneNumber,
        email: email,
        firstName: names.isNotEmpty ? names.first : null,
        lastName: names.length > 1 ? names.sublist(1).join(' ') : null,
        fullName: fullName,
      ),
      providerId: providerId,
      providerStatus: ProviderStatus.tryParse(providerStatus),
      isNewProvider: isNewProvider,
      requiresOnboarding: requiresOnboarding,
    );
  }
}

class RefreshTokenResponse {
  final String accessToken;
  final String refreshToken;
  final int expiresIn;

  const RefreshTokenResponse({
    required this.accessToken,
    required this.refreshToken,
    required this.expiresIn,
  });

  factory RefreshTokenResponse.fromJson(Map<String, dynamic> json) {
    return RefreshTokenResponse(
      accessToken: (json['accessToken'] ?? '').toString(),
      refreshToken: (json['refreshToken'] ?? '').toString(),
      expiresIn: (json['expiresIn'] as num?)?.toInt() ?? 0,
    );
  }
}
