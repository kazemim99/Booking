import 'dart:convert';

import 'package:asan_rezerve_provider_app/core/utils/jwt_decoder.dart';
import 'package:flutter_test/flutter_test.dart';

String _makeToken(Map<String, dynamic> payload) {
  String seg(Map<String, dynamic> m) =>
      base64Url.encode(utf8.encode(jsonEncode(m))).replaceAll('=', '');
  return '${seg({'alg': 'HS256', 'typ': 'JWT'})}.${seg(payload)}.sig';
}

void main() {
  group('JwtDecoder', () {
    test('decodes provider claims (backend key spellings)', () {
      final token = _makeToken({
        'sub': 'user-1',
        'email': 'p@x.com',
        'user_type': 'Provider',
        'role': ['Provider'],
        'providerId': 'prov-1',
        'provider_status': 'Active',
        'exp': DateTime.now()
                .add(const Duration(hours: 1))
                .millisecondsSinceEpoch ~/
            1000,
      });

      final claims = JwtDecoder.decode(token)!;
      expect(claims.userId, 'user-1');
      expect(claims.userType, 'Provider');
      expect(claims.providerId, 'prov-1');
      expect(claims.providerStatus, 'Active');
      expect(claims.isProvider, isTrue);
      expect(claims.isExpired, isFalse);
    });

    test('handles single role string and snake_case ids', () {
      final token = _makeToken({
        'nameid': 'u2',
        'role': 'ServiceProvider',
        'provider_id': 'p2',
      });
      final claims = JwtDecoder.decode(token)!;
      expect(claims.roles, ['ServiceProvider']);
      expect(claims.providerId, 'p2');
      expect(claims.isProvider, isTrue);
    });

    test('detects expired token', () {
      final token = _makeToken({
        'sub': 'u',
        'exp': DateTime.now()
                .subtract(const Duration(hours: 1))
                .millisecondsSinceEpoch ~/
            1000,
      });
      expect(JwtDecoder.decode(token)!.isExpired, isTrue);
    });

    test('returns null for malformed tokens', () {
      expect(JwtDecoder.decode(null), isNull);
      expect(JwtDecoder.decode(''), isNull);
      expect(JwtDecoder.decode('not.a.jwt.token'), isNull);
      expect(JwtDecoder.decode('onlyonepart'), isNull);
    });
  });

  group('JwtDecoder name claims', () {
    test('reads the ClaimTypes URIs the API writes', () {
      final claims = JwtDecoder.decode(_makeToken({
        'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname': 'مصطفی',
        'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname': 'کاظمی',
        'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name': 'مصطفی کاظمی',
      }))!;
      expect(claims.firstName, 'مصطفی');
      expect(claims.lastName, 'کاظمی');
      expect(claims.fullName, 'مصطفی کاظمی');
    });

    test('and the short spellings', () {
      final claims = JwtDecoder.decode(_makeToken({
        'given_name': 'مصطفی',
        'family_name': 'کاظمی',
        'unique_name': 'مصطفی کاظمی',
      }))!;
      expect(claims.firstName, 'مصطفی');
      expect(claims.lastName, 'کاظمی');
      expect(claims.fullName, 'مصطفی کاظمی');
    });
  });
}
