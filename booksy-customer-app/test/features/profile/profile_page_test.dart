import 'package:booksy_customer_app/config/theme/app_theme.dart';
import 'package:booksy_customer_app/core/constants/app_strings.dart';
import 'package:booksy_customer_app/core/di/injection.dart';
import 'package:booksy_customer_app/core/storage/secure_storage_service.dart';
import 'package:booksy_customer_app/features/auth/domain/entities/user.dart';
import 'package:booksy_customer_app/features/auth/presentation/bloc/auth_bloc.dart';
import 'package:booksy_customer_app/features/profile/data/datasources/profile_remote_datasource.dart';
import 'package:booksy_customer_app/features/profile/presentation/pages/profile_tab_page.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/fake_auth_bloc.dart';

/// The profile card names the customer — never by the «مشتری 9384444636» a skipped name prompt leaves behind
/// ("the number must never be written anywhere", production QA 2026-09-23). The phone shows as the phone.
class _Remote implements ProfileRemoteDataSource {
  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

class _Storage implements SecureStorageService {
  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

void main() {
  setUp(() {
    getIt
      ..registerSingleton<ProfileRemoteDataSource>(_Remote())
      ..registerSingleton<SecureStorageService>(_Storage());
  });

  tearDown(() => getIt.reset());

  Future<void> pump(WidgetTester tester, User user) async {
    final auth = FakeAuthBloc()..signIn();
    addTearDown(auth.close);
    await tester.pumpWidget(MaterialApp(
      theme: AppTheme.light,
      builder: (context, child) =>
          Directionality(textDirection: TextDirection.rtl, child: child!),
      home: BlocProvider<AuthBloc>.value(value: auth, child: ProfilePage(user: user)),
    ));
    await tester.pump();
  }

  User named(String? first, String? last) => User(
        id: 'user-1',
        phoneNumber: '+989121234567',
        firstName: first,
        lastName: last,
        createdAt: DateTime(2026, 1, 1),
      );

  testWidgets('a real name is shown', (tester) async {
    await pump(tester, named('سارا', 'احمدی'));

    expect(find.text('سارا احمدی'), findsOneWidget);
  });

  testWidgets('the sign-in placeholder is not a name, and the number is only the phone', (tester) async {
    await pump(tester, named('مشتری', '9121234567'));

    expect(find.textContaining('مشتری'), findsNothing);
    expect(find.textContaining('9121234567'), findsOneWidget,
        reason: 'once, as the phone line — never as the name');
    expect(find.text('+989121234567'), findsOneWidget);
  });

  testWidgets('editing starts from blank fields, not from the placeholder', (tester) async {
    await pump(tester, named('مشتری', '9121234567'));

    await tester.tap(find.text(AppStrings.profileEditTitle));
    await tester.pumpAndSettle();

    final fields = tester.widgetList<EditableText>(find.byType(EditableText)).toList();
    expect(fields, hasLength(2));
    expect(fields.map((f) => f.controller.text), everyElement(isEmpty));
  });
}
