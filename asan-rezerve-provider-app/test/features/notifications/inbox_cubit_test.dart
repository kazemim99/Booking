import 'package:asan_rezerve_provider_app/core/errors/failures.dart';
import 'package:asan_rezerve_provider_app/features/notifications/domain/inbox_item.dart';
import 'package:asan_rezerve_provider_app/features/notifications/domain/inbox_repository.dart';
import 'package:asan_rezerve_provider_app/features/notifications/presentation/inbox_cubit.dart';
import 'package:dartz/dartz.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

/// The salon's inbox and the bell's badge. They share one cubit so they cannot disagree — and these tests hold
/// the rules that keep the number true: reading an already-read notice changes nothing, a failed request is put
/// back exactly as it was, and a failed load is an error, never "you have no notifications".
class _MockRepo extends Mock implements InboxRepository {}

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

  int unreadInList(InboxCubit c) => c.state.items.where((i) => i.isUnread).length;

  test('loads the page and the count together', () async {
    givenInbox([_item('a'), _item('b', read: true)]);
    final cubit = InboxCubit(repo);

    await cubit.load();

    expect(cubit.state.items.map((i) => i.id), ['a', 'b']);
    expect(cubit.state.unreadCount, 1);
    await cubit.close();
  });

  test('is empty only once a load has succeeded and found nothing', () async {
    final cubit = InboxCubit(repo);
    expect(cubit.state.isEmpty, isFalse, reason: 'not loaded yet is "loading", not "empty"');

    givenInbox([]);
    await cubit.load();

    expect(cubit.state.isEmpty, isTrue);
    await cubit.close();
  });

  test('a failed load is an error, never an empty inbox', () async {
    when(() => repo.fetchPage(pageNumber: any(named: 'pageNumber'), pageSize: any(named: 'pageSize')))
        .thenAnswer((_) async => const Left(NetworkFailure('offline')));
    final cubit = InboxCubit(repo);

    await cubit.load();

    expect(cubit.state.error, isNotNull);
    expect(cubit.state.isEmpty, isFalse);
    await cubit.close();
  });

  test('reading an unread notice takes exactly one off the count', () async {
    givenInbox([_item('a'), _item('b')]);
    when(() => repo.markRead('a')).thenAnswer((_) async => const Right(null));
    final cubit = InboxCubit(repo);
    await cubit.load();

    await cubit.markRead('a');

    expect(cubit.state.unreadCount, 1);
    expect(cubit.state.unreadCount, unreadInList(cubit), reason: 'the badge and the list agree');
    await cubit.close();
  });

  test('reading an already-read notice changes nothing and asks the server nothing', () async {
    givenInbox([_item('a', read: true), _item('b')]);
    final cubit = InboxCubit(repo);
    await cubit.load();

    await cubit.markRead('a');

    expect(cubit.state.unreadCount, 1);
    verifyNever(() => repo.markRead(any()));
    await cubit.close();
  });

  test('a failed read is put back exactly as it was', () async {
    givenInbox([_item('a')]);
    when(() => repo.markRead('a')).thenAnswer((_) async => const Left(NetworkFailure('offline')));
    final cubit = InboxCubit(repo);
    await cubit.load();

    final ok = await cubit.markRead('a');

    expect(ok, isFalse);
    expect(cubit.state.unreadCount, 1);
    expect(cubit.state.items.single.isUnread, isTrue);
    await cubit.close();
  });

  test('marking everything read empties the count and reads every row', () async {
    givenInbox([_item('a'), _item('b'), _item('c', read: true)]);
    when(() => repo.markAllRead()).thenAnswer((_) async => const Right(null));
    final cubit = InboxCubit(repo);
    await cubit.load();

    await cubit.markAllRead();

    expect(cubit.state.unreadCount, 0);
    expect(unreadInList(cubit), 0);
    await cubit.close();
  });

  test('marking everything read twice is harmless and asks the server once', () async {
    givenInbox([_item('a')]);
    when(() => repo.markAllRead()).thenAnswer((_) async => const Right(null));
    final cubit = InboxCubit(repo);
    await cubit.load();

    await cubit.markAllRead();
    await cubit.markAllRead();

    verify(() => repo.markAllRead()).called(1);
    await cubit.close();
  });

  test('a failed mark-all is put back exactly as it was', () async {
    givenInbox([_item('a'), _item('b', read: true)]);
    when(() => repo.markAllRead()).thenAnswer((_) async => const Left(NetworkFailure('offline')));
    final cubit = InboxCubit(repo);
    await cubit.load();

    await cubit.markAllRead();

    expect(cubit.state.unreadCount, 1);
    expect(cubit.state.items.firstWhere((i) => i.id == 'a').isUnread, isTrue);
    expect(cubit.state.items.firstWhere((i) => i.id == 'b').isUnread, isFalse);
    await cubit.close();
  });

  test('refreshing the badge alone does not load the list', () async {
    when(() => repo.unreadCount()).thenAnswer((_) async => const Right(5));
    final cubit = InboxCubit(repo);

    await cubit.refreshCount();

    expect(cubit.state.unreadCount, 5);
    verifyNever(() => repo.fetchPage(pageNumber: any(named: 'pageNumber'), pageSize: any(named: 'pageSize')));
    await cubit.close();
  });

  test('a badge that cannot be read shows nothing rather than a guess', () async {
    when(() => repo.unreadCount()).thenAnswer((_) async => const Left(NetworkFailure('offline')));
    final cubit = InboxCubit(repo);

    await cubit.refreshCount();

    expect(cubit.state.unreadCount, 0);
    await cubit.close();
  });
}
