import 'package:booksy_customer_app/config/theme/app_theme.dart';
import 'package:booksy_customer_app/core/constants/app_strings.dart';
import 'package:booksy_customer_app/core/di/injection.dart';
import 'package:booksy_customer_app/core/storage/secure_storage_service.dart';
import 'package:booksy_customer_app/features/auth/presentation/bloc/auth_bloc.dart';
import 'package:booksy_customer_app/features/auth/presentation/bloc/auth_event.dart';
import 'package:booksy_customer_app/features/auth/presentation/bloc/auth_state.dart';
import 'package:booksy_customer_app/features/profile/data/datasources/profile_remote_datasource.dart';
import 'package:booksy_customer_app/features/profile/presentation/pages/profile_tab_page.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/fake_auth_bloc.dart';

/// A name edited on the profile is the session's from then on (QA recording 2026-09-23 #9): the booking confirm
/// step asks a customer without a name for one, and must not ask someone who has just given it here.
class _Names implements ProfileRemoteDataSource {
  String? firstName;
  String? lastName;

  /// When set, the save fails with this.
  Object? failWith;

  @override
  Future<void> updateProfile({
    required String customerId,
    required String firstName,
    required String lastName,
  }) async {
    if (failWith != null) throw failWith!;
    this.firstName = firstName;
    this.lastName = lastName;
  }

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

class _Storage implements SecureStorageService {
  @override
  Future<String?> getCustomerId() async => 'c1';

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

void main() {
  late _Names names;
  late FakeAuthBloc auth;

  setUp(() {
    names = _Names();
    getIt
      ..registerSingleton<ProfileRemoteDataSource>(names)
      ..registerSingleton<SecureStorageService>(_Storage());
    auth = FakeAuthBloc()..signIn(sessionNamed('مشتری', '9384444636'));
  });

  tearDown(() async {
    await auth.close();
    await getIt.reset();
  });

  String? sessionName() {
    final state = auth.state;
    if (state is! Authenticated) return null;
    return '${state.session.user.firstName} ${state.session.user.lastName}';
  }

  Future<void> editName(WidgetTester tester, String first, String last) async {
    await tester.pumpWidget(BlocProvider<AuthBloc>.value(
      value: auth,
      child: MaterialApp(
        theme: AppTheme.light,
        home: const Directionality(
          textDirection: TextDirection.rtl,
          child: ProfileTabPage(),
        ),
      ),
    ));
    await tester.pumpAndSettle();

    await tester.tap(find.text(AppStrings.profileEditTitle));
    await tester.pumpAndSettle();
    final fields = find.byType(TextFormField);
    await tester.enterText(fields.at(0), first);
    await tester.enterText(fields.at(1), last);
    await tester.tap(find.text(AppStrings.save));
    await tester.pumpAndSettle();
  }

  testWidgets('a saved name is the session\'s from then on', (tester) async {
    await editName(tester, 'سارا', 'احمدی');

    expect(names.firstName, 'سارا');
    expect(sessionName(), 'سارا احمدی');
    expect(auth.rememberedNames, [('سارا', 'احمدی')]);
  });

  // QA 2026-09-24: the post-signup «نام شما» page saved the name (the session learned it), yet the profile tab — already
  // built underneath — kept showing only the phone, and «ویرایش پروفایل» opened with empty fields, as if nothing had
  // been saved. The tab follows the session's name.
  testWidgets('a name the session learns elsewhere shows on the open profile and prefills the edit sheet', (tester) async {
    await tester.pumpWidget(BlocProvider<AuthBloc>.value(
      value: auth,
      child: MaterialApp(
        theme: AppTheme.light,
        home: const Directionality(textDirection: TextDirection.rtl, child: ProfileTabPage()),
      ),
    ));
    await tester.pumpAndSettle();
    expect(find.text('ناصر عابدی'), findsNothing);

    auth.add(const UserNameChangedEvent(firstName: 'ناصر', lastName: 'عابدی'));
    // The bloc hands its new state to listeners a microtask later; frames alone do not wait for it.
    await tester.runAsync(() => Future<void>.delayed(Duration.zero));
    await tester.pumpAndSettle();

    expect(find.text('ناصر عابدی'), findsOneWidget);

    await tester.tap(find.text(AppStrings.profileEditTitle));
    await tester.pumpAndSettle();
    final fields = tester.widgetList<TextFormField>(find.byType(TextFormField)).toList();
    expect(fields[0].controller?.text, 'ناصر');
    expect(fields[1].controller?.text, 'عابدی');
  });

  testWidgets('a failed save leaves the session as it was', (tester) async {
    names.failWith = Exception('offline');
    await editName(tester, 'سارا', 'احمدی');

    expect(sessionName(), 'مشتری 9384444636');
    expect(auth.rememberedNames, isEmpty);
  });
}
