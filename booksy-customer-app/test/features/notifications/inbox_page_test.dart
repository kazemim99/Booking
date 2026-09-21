import 'package:dartz/dartz.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';

import 'package:booksy_customer_app/core/constants/app_strings.dart';
import 'package:booksy_customer_app/core/errors/failures.dart';
import 'package:booksy_customer_app/features/notifications/domain/inbox_item.dart';
import 'package:booksy_customer_app/features/notifications/presentation/inbox_cubit.dart';
import 'package:booksy_customer_app/features/notifications/presentation/inbox_page.dart';

import 'inbox_cubit_test.dart' show FakeInboxRepository;

/// What a customer sees. The two properties worth a widget test: a failed load never reads as "no
/// notifications", and tapping a booking notice opens THAT appointment (this app has a screen for it).

InboxItem _item(String id, {bool read = false, bool actionable = true}) => InboxItem(
      id: id,
      subject: 'موضوع $id',
      body: 'متن $id',
      createdAt: DateTime.utc(2026, 9, 21, 8),
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

  Future<InboxCubit> pump(WidgetTester tester) async {
    final cubit = InboxCubit(repo);
    final router = GoRouter(
      initialLocation: '/inbox',
      routes: [
        GoRoute(
          path: '/inbox',
          builder: (_, __) => BlocProvider<InboxCubit>.value(value: cubit, child: const InboxPage()),
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
    await tester.pumpWidget(MaterialApp.router(routerConfig: router));
    await tester.pumpAndSettle();
    return cubit;
  }

  testWidgets('says there is nothing only when there really is nothing', (tester) async {
    repo.givenInbox([]);

    await pump(tester);

    expect(find.byKey(const Key('inbox-empty')), findsOneWidget);
    expect(find.text(AppStrings.notificationsEmpty), findsOneWidget);
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
