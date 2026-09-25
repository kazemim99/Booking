import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:booksy_customer_app/core/storage/secure_storage_service.dart';
import 'package:booksy_customer_app/features/auth/data/datasources/auth_api_service.dart';
import 'package:booksy_customer_app/features/auth/data/repositories/auth_repository_impl.dart';
import 'package:booksy_customer_app/features/auth/domain/entities/user.dart';
import 'package:booksy_customer_app/features/auth/presentation/bloc/auth_event.dart';
import 'package:booksy_customer_app/features/auth/presentation/bloc/auth_state.dart';

import '../../helpers/fake_auth_bloc.dart';

/// The session knows the customer's name (QA recording 2026-09-23 #9): a booking is only confirmed for a customer
/// with a real name, and one who has just given it must not be asked again — not on the next booking, and not after
/// the app is opened again. The session restored at a cold start used to carry no name at all.

/// Answers every request with [body], the way `ApiResponseMiddleware` wraps the login answer.
class _Adapter implements HttpClientAdapter {
  Object body;

  _Adapter(this.body);

  @override
  Future<ResponseBody> fetch(
      RequestOptions options, Stream<Uint8List>? requestStream, Future<void>? cancelFuture) async {
    return ResponseBody.fromString(jsonEncode(body), 200, headers: {
      Headers.contentTypeHeader: [Headers.jsonContentType],
    });
  }

  @override
  void close({bool force = false}) {}
}

/// The flat login answer (`CompleteCustomerAuthResponse`), with [fullName] as the server knows the person.
Map<String, dynamic> _login({String? fullName, String userId = 'user-1'}) => {
      'success': true,
      'data': {
        'accessToken': 'token',
        'refreshToken': 'refresh',
        'userId': userId,
        'customerId': 'c1',
        'phoneNumber': '+989384444636',
        if (fullName != null) 'fullName': fullName,
        'expiresIn': 3600,
      },
    };

void main() {
  group('a name given in the app', () {
    test('becomes the session user\'s name, and is kept on the device', () async {
      final auth = FakeAuthBloc()..signIn(sessionNamed('مشتری', '9384444636'));
      addTearDown(auth.close);

      auth.add(const UserNameChangedEvent(firstName: 'سارا', lastName: 'احمدی'));
      await pumpEventQueue();

      final state = auth.state;
      expect(state, isA<Authenticated>());
      final session = (state as Authenticated).session;
      expect(session.user.firstName, 'سارا');
      expect(session.user.lastName, 'احمدی');
      // Nothing else about the session changes.
      expect(session.accessToken, 'token');
      expect(session.user.id, 'user-1');
      expect(session.user.phoneNumber, '+989121234567');
      expect(auth.rememberedNames, [('سارا', 'احمدی')]);
    });

    test('is ignored when nobody is signed in', () async {
      final auth = FakeAuthBloc();
      addTearDown(auth.close);

      auth.add(const UserNameChangedEvent(firstName: 'سارا', lastName: 'احمدی'));
      await pumpEventQueue();

      expect(auth.state, isNot(isA<Authenticated>()));
      expect(auth.rememberedNames, isEmpty);
    });
  });

  group('the name on the device', () {
    late _Adapter adapter;
    late SecureStorageService storage;

    AuthRepositoryImpl repository() {
      final dio = Dio(BaseOptions(baseUrl: 'https://api.test'))..httpClientAdapter = adapter;
      return AuthRepositoryImpl(AuthApiService(dio), storage);
    }

    /// Opening the app again: a new repository over the same storage.
    Future<User> restoredUser() async {
      final restored = await repository().getCurrentSession();
      final session = restored.getOrElse(() => null);
      expect(session, isNotNull, reason: 'a stored session is restored');
      return session!.user;
    }

    setUp(() {
      FlutterSecureStorage.setMockInitialValues({});
      storage = SecureStorageService(const FlutterSecureStorage());
      adapter = _Adapter(_login(fullName: 'سارا احمدی'));
    });

    test('a sign-in keeps the name the server knows, and a restored session has it', () async {
      await repository().completeAuthentication(phoneNumber: '09384444636', code: '222222');

      final user = await restoredUser();
      expect(user.firstName, 'سارا');
      expect(user.lastName, 'احمدی');
    });

    test('a name given later outlives a restart', () async {
      adapter.body = _login(fullName: 'مشتری 9384444636');
      final repo = repository();
      await repo.completeAuthentication(phoneNumber: '09384444636', code: '222222');

      await repo.rememberUserName(firstName: 'سارا', lastName: 'احمدی');

      final user = await restoredUser();
      expect(user.firstName, 'سارا');
      expect(user.lastName, 'احمدی');
    });

    test('the next person to sign in on this device does not inherit the name', () async {
      await repository().completeAuthentication(phoneNumber: '09384444636', code: '222222');

      adapter.body = _login(userId: 'user-2');
      await repository().completeAuthentication(phoneNumber: '09120000000', code: '222222');

      final user = await restoredUser();
      expect(user.firstName, isNull);
      expect(user.lastName, isNull);
    });

    test('signing out forgets the name', () async {
      final repo = repository();
      await repo.completeAuthentication(phoneNumber: '09384444636', code: '222222');

      await repo.logout();

      expect(await storage.getFirstName(), isNull);
      expect(await storage.getLastName(), isNull);
    });
  });
}
