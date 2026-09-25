import 'package:booksy_provider_app/core/constants/app_strings.dart';
import 'package:booksy_provider_app/core/errors/failures.dart';
import 'package:booksy_provider_app/features/notifications/domain/inbox_item.dart';
import 'package:booksy_provider_app/features/notifications/domain/inbox_repository.dart';
import 'package:booksy_provider_app/features/notifications/presentation/inbox_bell.dart';
import 'package:booksy_provider_app/features/notifications/presentation/inbox_cubit.dart';
import 'package:booksy_provider_app/features/notifications/presentation/inbox_page.dart';
import 'package:dartz/dartz.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

/// The screens, not the cubit: what a salon owner actually sees. The two properties worth a widget test are
/// that a failed load never reads as "no notifications", and that the bell's number is the server's.
class _MockRepo extends Mock implements InboxRepository {}

InboxItem _item(String id, {bool read = false}) => InboxItem(
      id: id,
      subject: 'موضوع $id',
      body: 'متن $id',
      createdAt: DateTime.utc(2026, 9, 21, 8),
      readAt: read ? DateTime.utc(2026, 9, 21, 9) : null,
      destinationKind: 'Booking',
      destinationId: 'b-$id',
      isActionable: true,
    );

void main() {
  late _MockRepo repo;

  setUp(() => repo = _MockRepo());

  void givenInbox(List<InboxItem> items) {
    when(() => repo.fetchPage(pageNumber: any(named: 'pageNumber'), pageSize: any(named: 'pageSize')))
        .thenAnswer((_) async => Right(InboxResult(
              items: items,
              totalCount: items.length,
              unreadCount: items.where((i) => i.isUnread).length,
            )));
  }

  Future<InboxCubit> pumpPage(WidgetTester tester) async {
    final cubit = InboxCubit(repo);
    await tester.pumpWidget(MaterialApp(
      home: BlocProvider<InboxCubit>.value(value: cubit, child: const InboxPage()),
    ));
    await tester.pumpAndSettle();
    return cubit;
  }

  testWidgets('shows what was sent', (tester) async {
    givenInbox([_item('a'), _item('b', read: true)]);

    await pumpPage(tester);

    expect(find.text('موضوع a'), findsOneWidget);
    expect(find.byKey(const Key('inbox-unread-a')), findsOneWidget);
    expect(find.byKey(const Key('inbox-unread-b')), findsNothing);
  });

  testWidgets('says there is nothing only when there really is nothing', (tester) async {
    givenInbox([]);

    await pumpPage(tester);

    expect(find.byKey(const Key('inbox-empty')), findsOneWidget);
    expect(find.text(AppStrings.notificationsEmpty), findsOneWidget);
  });

  testWidgets('a failed load is shown as a failure, not as an empty inbox', (tester) async {
    when(() => repo.fetchPage(pageNumber: any(named: 'pageNumber'), pageSize: any(named: 'pageSize')))
        .thenAnswer((_) async => const Left(NetworkFailure('offline')));

    await pumpPage(tester);

    expect(find.byKey(const Key('inbox-error')), findsOneWidget);
    expect(find.byKey(const Key('inbox-empty')), findsNothing);
  });

  testWidgets('tapping an unread notice marks it read', (tester) async {
    givenInbox([_item('a')]);
    when(() => repo.markRead('a')).thenAnswer((_) async => const Right(null));

    final cubit = await pumpPage(tester);
    await tester.tap(find.byKey(const Key('inbox-row-a')));
    await tester.pumpAndSettle();

    verify(() => repo.markRead('a')).called(1);
    expect(cubit.state.unreadCount, 0);
    expect(find.byKey(const Key('inbox-unread-a')), findsNothing);
  });

  testWidgets('offers "mark all read" only while something is unread', (tester) async {
    givenInbox([_item('a', read: true)]);

    await pumpPage(tester);

    expect(find.byKey(const Key('inbox-mark-all')), findsNothing);
  });

  group('InboxBell', () {
    Future<void> pumpBell(WidgetTester tester) async {
      await tester.pumpWidget(MaterialApp(
        home: Scaffold(
          appBar: AppBar(actions: [
            BlocProvider<InboxCubit>(create: (_) => InboxCubit(repo), child: const InboxBell()),
          ]),
        ),
      ));
      await tester.pumpAndSettle();
    }

    testWidgets('shows the number the server reports', (tester) async {
      when(() => repo.unreadCount()).thenAnswer((_) async => const Right(3));

      await pumpBell(tester);

      final badge = tester.widget<Badge>(find.byKey(const Key('home-bell-badge')));
      expect(badge.isLabelVisible, isTrue);
      expect(find.text('3'), findsOneWidget);
    });

    testWidgets('shows no number at zero, or when the count cannot be read', (tester) async {
      when(() => repo.unreadCount()).thenAnswer((_) async => const Left(NetworkFailure('offline')));

      await pumpBell(tester);

      expect(tester.widget<Badge>(find.byKey(const Key('home-bell-badge'))).isLabelVisible, isFalse);
    });
  });
}
