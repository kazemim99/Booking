import 'dart:async';

import 'package:booksy_customer_app/config/theme/app_theme.dart';
import 'package:booksy_customer_app/core/constants/app_strings.dart';
import 'package:booksy_customer_app/core/storage/secure_storage_service.dart';
import 'package:booksy_customer_app/features/profile/data/datasources/profile_remote_datasource.dart';
import 'package:booksy_customer_app/features/profile/presentation/bloc/profile_cubit.dart';
import 'package:booksy_customer_app/features/profile/presentation/pages/complete_name_page.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';

/// Asking a new customer for their name (QA walkthrough 2026-09-22: the profile read «ارائه‌دهنده 9384444636»
/// and nothing ever asked). Worth asking once; never worth blocking the journey over.
class _RecordingDataSource implements ProfileRemoteDataSource {
  String? firstName;
  String? lastName;
  var calls = 0;

  /// When set, the save fails with this.
  Object? failWith;

  /// When set, the save waits for it.
  Completer<void>? gate;

  @override
  Future<void> updateProfile({
    required String customerId,
    required String firstName,
    required String lastName,
  }) async {
    calls++;
    if (gate != null) await gate!.future;
    if (failWith != null) throw failWith!;
    this.firstName = firstName;
    this.lastName = lastName;
  }

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

/// Enough of the storage for the cubit: it reads the customer id before saving.
class _FakeStorage implements SecureStorageService {
  @override
  Future<String?> getCustomerId() async => 'c1';

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

void main() {
  late _RecordingDataSource remote;
  late ProfileCubit cubit;
  late String landedOn;

  setUp(() {
    remote = _RecordingDataSource();
    cubit = ProfileCubit(remoteDataSource: remote, storageService: _FakeStorage());
    landedOn = '';
  });

  Future<void> pump(WidgetTester tester, {String? redirect}) async {
    final router = GoRouter(
      initialLocation: '/profile/name',
      routes: [
        GoRoute(
          path: '/profile/name',
          builder: (_, __) => CompleteNamePage(redirect: redirect, cubit: cubit),
        ),
        GoRoute(path: '/home', builder: (_, __) {
          landedOn = '/home';
          return const Scaffold(body: Text('home'));
        }),
        GoRoute(path: '/providers/p1/book', builder: (_, __) {
          landedOn = '/providers/p1/book';
          return const Scaffold(body: Text('booking'));
        }),
      ],
    );

    await tester.pumpWidget(MaterialApp.router(
      theme: AppTheme.light,
      routerConfig: router,
      builder: (context, child) => Directionality(
        textDirection: TextDirection.rtl,
        child: child ?? const SizedBox.shrink(),
      ),
    ));
    await tester.pumpAndSettle();
  }

  testWidgets('saves the name and carries on to where they were going', (tester) async {
    await pump(tester, redirect: Uri.encodeComponent('/providers/p1/book'));

    await tester.enterText(find.byKey(const Key('complete-name-first')), 'سارا');
    await tester.enterText(find.byKey(const Key('complete-name-last')), 'احمدی');
    await tester.tap(find.byKey(const Key('complete-name-save')));
    await tester.pumpAndSettle();

    expect(remote.firstName, 'سارا');
    expect(remote.lastName, 'احمدی');
    expect(landedOn, '/providers/p1/book', reason: 'the booking they were in the middle of');
  });

  testWidgets('a name is asked for, not demanded', (tester) async {
    await pump(tester);

    await tester.tap(find.byKey(const Key('complete-name-skip')));
    await tester.pumpAndSettle();

    expect(remote.calls, 0);
    expect(landedOn, '/home');
  });

  testWidgets('saving nothing says what is missing and goes nowhere', (tester) async {
    await pump(tester);

    await tester.tap(find.byKey(const Key('complete-name-save')));
    await tester.pumpAndSettle();

    expect(find.text(AppStrings.firstNameRequired), findsOneWidget);
    expect(remote.calls, 0);
    expect(landedOn, '');
  });

  testWidgets('"later" is white on the blue app bar, not navy (1.41:1) — it is the only way to skip',
      (tester) async {
    await pump(tester);

    final label = tester.widget<RichText>(find.descendant(
      of: find.byKey(const Key('complete-name-skip')),
      matching: find.byType(RichText),
    ));
    expect(label.text.style?.color, AppTheme.light.appBarTheme.foregroundColor);
    expect(label.text.style?.color, Colors.white);
  });

  testWidgets('it waits for the save, showing it is busy, before moving on', (tester) async {
    remote.gate = Completer<void>();
    await pump(tester, redirect: Uri.encodeComponent('/providers/p1/book'));

    await tester.enterText(find.byKey(const Key('complete-name-first')), 'سارا');
    await tester.tap(find.byKey(const Key('complete-name-save')));
    await tester.pump();

    expect(landedOn, '', reason: 'nothing is known yet — the name may not be saved');
    expect(
      find.descendant(
          of: find.byKey(const Key('complete-name-save')),
          matching: find.byType(CircularProgressIndicator)),
      findsOneWidget,
    );

    remote.gate!.complete();
    await tester.pumpAndSettle();
    expect(landedOn, '/providers/p1/book');
  });

  testWidgets('a failed save is said in place and keeps what they typed', (tester) async {
    remote.failWith = Exception('offline');
    await pump(tester, redirect: Uri.encodeComponent('/providers/p1/book'));

    await tester.enterText(find.byKey(const Key('complete-name-first')), 'سارا');
    await tester.enterText(find.byKey(const Key('complete-name-last')), 'احمدی');
    await tester.tap(find.byKey(const Key('complete-name-save')));
    await tester.pumpAndSettle();

    expect(landedOn, '', reason: 'leaving would lose the name without a word');
    expect(find.byType(CompleteNamePage), findsOneWidget);
    expect(find.text(AppStrings.completeNameSaveFailed), findsOneWidget);
    expect(find.text('سارا'), findsOneWidget);
    expect(find.text('احمدی'), findsOneWidget);

    // And trying again, once it works, carries on.
    remote.failWith = null;
    await tester.tap(find.byKey(const Key('complete-name-save')));
    await tester.pumpAndSettle();
    expect(landedOn, '/providers/p1/book');
  });
}
