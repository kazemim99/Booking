import 'dart:async';

import 'package:dartz/dartz.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';

import 'package:asan_rezerve_customer_app/config/theme/app_theme.dart';
import 'package:asan_rezerve_customer_app/core/constants/app_strings.dart';
import 'package:asan_rezerve_customer_app/core/widgets/empty_state.dart';
import 'package:asan_rezerve_customer_app/core/widgets/error_state.dart';
import 'package:asan_rezerve_customer_app/core/widgets/skeleton_loader.dart';
import 'package:asan_rezerve_customer_app/core/errors/failures.dart';
import 'package:asan_rezerve_customer_app/features/notifications/domain/inbox_item.dart';
import 'package:asan_rezerve_customer_app/features/notifications/presentation/inbox_cubit.dart';
import 'package:asan_rezerve_customer_app/features/notifications/presentation/inbox_page.dart';

import 'inbox_cubit_test.dart' show FakeInboxRepository;

/// What a customer sees. The two properties worth a widget test: a failed load never reads as "no
/// notifications", and tapping a booking notice opens THAT appointment (this app has a screen for it).

/// The fixed "now" the page is given, so the relative times below never depend on the test clock.
final _now = DateTime(2026, 9, 23, 14, 30);

/// A page that never arrives: the inbox stays on its first load.
class _SlowInboxRepository extends FakeInboxRepository {
  final pending = Completer<Either<Failure, InboxResult>>();

  @override
  Future<Either<Failure, InboxResult>> fetchPage({int pageNumber = 1, int pageSize = 20}) {
    pageCalls++;
    return pending.future;
  }
}

InboxItem _item(String id, {bool read = false, bool actionable = true, DateTime? createdAt}) => InboxItem(
      id: id,
      subject: 'موضوع $id',
      body: 'متن $id',
      createdAt: createdAt ?? DateTime.utc(2026, 9, 21, 8),
      readAt: read ? DateTime.utc(2026, 9, 21, 9) : null,
      destinationKind: 'Booking',
      destinationId: 'booking-$id',
      isActionable: actionable,
    );

void main() {
  late FakeInboxRepository repo;
  late List<String> visited;

  setUp(() {
    repo = FakeInboxRepository();
    visited = [];
  });

  Future<InboxCubit> pump(WidgetTester tester, {bool settle = true}) async {
    final cubit = InboxCubit(repo);
    final router = GoRouter(
      initialLocation: '/inbox',
      routes: [
        GoRoute(
          path: '/inbox',
          builder: (_, __) => BlocProvider<InboxCubit>.value(value: cubit, child: InboxPage(now: () => _now)),
        ),
        GoRoute(
          path: '/appointments/:id',
          builder: (_, state) {
            visited.add(state.uri.toString());
            return const Scaffold(body: Text('appointment'));
          },
        ),
      ],
    );
    await tester.pumpWidget(MaterialApp.router(theme: AppTheme.light, routerConfig: router));
    if (settle) {
      await tester.pumpAndSettle();
    } else {
      await tester.pump();
    }
    return cubit;
  }

  testWidgets('says there is nothing only when there really is nothing', (tester) async {
    repo.givenInbox([]);

    await pump(tester);

    expect(find.byKey(const Key('inbox-empty')), findsOneWidget);
    expect(find.text(AppStrings.notificationsEmpty), findsOneWidget);
    expect(find.byType(EmptyState), findsOneWidget, reason: 'the shared empty state, with its icon');
  });

  testWidgets('the first load is a skeleton of rows, not a bare spinner', (tester) async {
    repo = _SlowInboxRepository();

    await pump(tester, settle: false);

    expect(find.byType(SkeletonLoader), findsOneWidget);
    expect(find.byType(CircularProgressIndicator), findsNothing);
    expect(find.byKey(const Key('inbox-empty')), findsNothing);
  });

  testWidgets('a failure offers to try again, and trying again loads', (tester) async {
    repo.pageResult = const Left(NetworkFailure('offline'));

    await pump(tester);
    expect(find.byType(ErrorState), findsOneWidget);
    expect(find.text(AppStrings.notificationsLoadFailed), findsOneWidget);

    repo.givenInbox([_item('a')]);
    await tester.tap(find.text(AppStrings.retry));
    await tester.pumpAndSettle();

    expect(repo.pageCalls, 2);
    expect(find.byKey(const Key('inbox-row-a')), findsOneWidget);
  });

  testWidgets('each row says how long ago it arrived', (tester) async {
    repo.givenInbox([
      _item('a', createdAt: _now.subtract(const Duration(minutes: 5))),
      _item('b', createdAt: _now.subtract(const Duration(hours: 2))),
      _item('c', createdAt: DateTime(2026, 9, 22, 9)),
    ]);

    await pump(tester);

    expect(find.text('۵ دقیقه پیش'), findsOneWidget);
    expect(find.text('۲ ساعت پیش'), findsOneWidget);
    expect(find.text('دیروز'), findsOneWidget);
  });

  testWidgets('"read all" is white on the blue app bar, not navy (1.41:1)', (tester) async {
    repo.givenInbox([_item('a')]);

    await pump(tester);

    final label = tester.widget<RichText>(find.descendant(
      of: find.byKey(const Key('inbox-mark-all')),
      matching: find.byType(RichText),
    ));
    expect(label.text.style?.color, AppTheme.light.appBarTheme.foregroundColor);
    expect(label.text.style?.color, Colors.white);
  });

  testWidgets('a failed load is shown as a failure, not as an empty inbox', (tester) async {
    repo.pageResult = const Left(NetworkFailure('offline'));

    await pump(tester);

    expect(find.byKey(const Key('inbox-error')), findsOneWidget);
    expect(find.byKey(const Key('inbox-empty')), findsNothing);
  });

  testWidgets('tapping a booking notice marks it read and opens that appointment', (tester) async {
    repo.givenInbox([_item('a')]);

    final cubit = await pump(tester);
    await tester.tap(find.byKey(const Key('inbox-row-a')));
    await tester.pumpAndSettle();

    expect(repo.markReadCalls, ['a']);
    expect(cubit.state.unreadCount, 0);
    expect(visited, ['/appointments/booking-a']);
  });

  testWidgets('a notice whose target is gone is still read, and goes nowhere', (tester) async {
    repo.givenInbox([_item('a', actionable: false)]);

    await pump(tester);
    await tester.tap(find.byKey(const Key('inbox-row-a')));
    await tester.pumpAndSettle();

    expect(repo.markReadCalls, ['a']);
    expect(visited, isEmpty);
  });
}
