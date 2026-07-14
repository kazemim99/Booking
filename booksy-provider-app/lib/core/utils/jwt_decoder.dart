import 'dart:convert';

/// Minimal, dependency-free JWT payload decoder.
///
/// Mirrors the Vue `auth.store.decodeToken` tolerance for the backend's
/// multiple claim-key spellings (see AUTH_SPECIFICATION.md §10.2). Signature
/// is NOT verified here — the server is the source of truth; this only reads
/// claims for client-side routing (roles, provider status).
class JwtClaims {
  final String? userId;
  final String? email;
  final String? userType;
  final List<String> roles;
  final String? providerId;
  final String? providerStatus;
  final String? customerId;
  final DateTime? expiresAt;

  const JwtClaims({
    this.userId,
    this.email,
    this.userType,
    this.roles = const [],
    this.providerId,
    this.providerStatus,
    this.customerId,
    this.expiresAt,
  });

  bool get isProvider =>
      roles.contains('Provider') ||
      roles.contains('ServiceProvider') ||
      userType == 'Provider';

  bool get isExpired {
    final exp = expiresAt;
    if (exp == null) return true;
    return DateTime.now().isAfter(exp);
  }
}

class JwtDecoder {
  JwtDecoder._();

  /// Decodes a JWT's payload. Returns null on any malformed input.
  static JwtClaims? decode(String? token) {
    if (token == null || token.isEmpty) return null;
    final parts = token.split('.');
    if (parts.length != 3) return null;

    try {
      final payload = jsonDecode(_decodeBase64Url(parts[1])) as Map<String, dynamic>;

      final roles = _extractRoles(payload);

      final expRaw = payload['exp'];
      DateTime? expiresAt;
      if (expRaw is num) {
        expiresAt = DateTime.fromMillisecondsSinceEpoch(expRaw.toInt() * 1000);
      }

      return JwtClaims(
        userId: _firstString(payload, const [
          'sub',
          'nameid',
          'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier',
        ]),
        email: _firstString(payload, const [
          'email',
          'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress',
        ]),
        userType: _firstString(payload, const ['user_type']),
        roles: roles,
        providerId: _firstString(payload, const ['providerId', 'provider_id']),
        providerStatus: _firstString(
          payload,
          const ['provider_status', 'providerStatus'],
        ),
        customerId: _firstString(payload, const ['customerId', 'customer_id']),
        expiresAt: expiresAt,
      );
    } catch (_) {
      return null;
    }
  }

  static List<String> _extractRoles(Map<String, dynamic> payload) {
    final raw = payload['role'] ??
        payload['roles'] ??
        payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
    if (raw == null) return const [];
    if (raw is List) return raw.map((e) => e.toString()).toList();
    return [raw.toString()];
  }

  static String? _firstString(Map<String, dynamic> payload, List<String> keys) {
    for (final key in keys) {
      final v = payload[key];
      if (v != null && v.toString().isNotEmpty) return v.toString();
    }
    return null;
  }

  static String _decodeBase64Url(String input) {
    var output = input.replaceAll('-', '+').replaceAll('_', '/');
    switch (output.length % 4) {
      case 2:
        output += '==';
        break;
      case 3:
        output += '=';
        break;
    }
    return utf8.decode(base64.decode(output));
  }
}
