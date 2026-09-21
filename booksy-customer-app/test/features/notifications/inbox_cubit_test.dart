import 'package:dartz/dartz.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:booksy_customer_app/core/errors/failures.dart';
import 'package:booksy_customer_app/features/notifications/domain/inbox_item.dart';
import 'package:booksy_customer_app/features/notifications/domain/inbox_repository.dart';
import 'package:booksy_customer_app/features/notifications/presentation/inbox_cubit.dart';

/// The customer's inbox and the bell's badge share one cubit, so they cannot disagree. These hold the rules that
/// keep the number true: an already-read notice changes nothing, a failed request is put back exactly as it was,
/// and a failed load is an error — never "you have no notifications".

InboxItem _item(String id, {bool read = false}) => InboxItem(
      id: id,
      subject: 'موضوع $id',
      body: 'متن $id',
      createdAt: DateTime.utc(2026, 9, 21, 8),
      readAt: read ? DateTime.utc(2026, 9, 21, 9) : null,
      destinationKind: 'Booking',
      destinationId: 'booking-$id',
      isActionable: true,
    );

class FakeInboxRepository implements InboxRepository {
  Either<Failure, InboxResult>? pageResult;
  Either<Failure, int> countResult = const Right(0);
  Either<Failure, Unit> markReadResult = const Right(unit);
  Either<Failure, Unit> markAllResult = const Right(unit);
  final List<String> markReadCalls = [];
  int markAllCalls = 0;
  int pageCalls = 0;

  void givenInbox(List<InboxItem> items) => pageResult = Right(InboxResult(
        items: items,
        totalCount: items.length,
        unreadCount: items.where((i) => i.isUnread).length,
      ));

  @override
  Future<Either<Failure, InboxResult>> fetchPage({int pageNumber = 1, int pageSize = 20}) async {
    pageCalls++;
    return pageResult!;
  }

  @override
  Future<Either<Failure, int>> unreadCount() async => countResult;

  @override
  Future<Either<Failure, Unit>> markRead(String id) async {
    markReadCalls.add(id);
    return markReadResult;
  }

  @override
  Future<Either<Failure, Unit>> markAllRead() async {
    markAllCalls++;
    return markAllResult;
  }
}

void main() {
  late FakeInboxRepository repo;

  setUp(() => repo = FakeInboxRepository());

  int unreadInList(InboxCubit c) => c.state.items.where((i) => i.isUnread).length;

  test('loads the page and the count together', () async {
    repo.givenInbox([_item('a'), _item('b', read: true)]);
    final cubit = InboxCubit(repo);

    await cubit.load();

    expect(cubit.state.items.map((i) => i.id), ['a', 'b']);
    expect(cubit.state.unreadCount, 1);
    await cubit.close();
  });

  test('is empty only once a load has succeeded and found nothing', () async {
    final cubit = InboxCubit(repo);
    expect(cubit.state.isEmpty, isFalse);

    repo.givenInbox([]);
    await cubit.load();

    expect(cubit.state.isEmpty, isTrue);
    await cubit.close();
  });

  test('a failed load is an error, never an empty inbox', () async {
    repo.pageResult = const Left(NetworkFailure('offline'));
    final cubit = InboxCubit(repo);

    await cubit.load();

    expect(cubit.state.error, isNotNull);
    expect(cubit.state.isEmpty, isFalse);
    await cubit.close();
  });

  test('reading an unread notice takes exactly one off the count', () async {
    repo.givenInbox([_item('a'), _item('b')]);
    final cubit = InboxCubit(repo);
    await cubit.load();

    await cubit.markRead('a');

    expect(cubit.state.unreadCount, 1);
    expect(cubit.state.unreadCount, unreadInList(cubit));
    await cubit.close();
  });

  test('reading an already-read notice changes nothing and asks the server nothing', () async {
    repo.givenInbox([_item('a', read: true), _item('b')]);
    final cubit = InboxCubit(repo);
    await cubit.load();

    await cubit.markRead('a');

    expect(cubit.state.unreadCount, 1);
    expect(repo.markReadCalls, isEmpty);
    await cubit.close();
  });

  test('a failed read is put back exactly as it was', () async {
    repo.givenInbox([_item('a')]);
    repo.markReadResult = const Left(NetworkFailure('offline'));
    final cubit = InboxCubit(repo);
    await cubit.load();

    final ok = await cubit.markRead('a');

    expect(ok, isFalse);
    expect(cubit.state.unreadCount, 1);
    expect(cubit.state.items.single.isUnread, isTrue);
    await cubit.close();
  });

  test('marking everything read twice is harmless and asks the server once', () async {
    repo.givenInbox([_item('a'), _item('b', read: true)]);
    final cubit = InboxCubit(repo);
    await cubit.load();

    await cubit.markAllRead();
    await cubit.markAllRead();

    expect(cubit.state.unreadCount, 0);
    expect(unreadInList(cubit), 0);
    expect(repo.markAllCalls, 1);
    await cubit.close();
  });

  test('a failed mark-all is put back exactly as it was', () async {
    repo.givenInbox([_item('a'), _item('b', read: true)]);
    repo.markAllResult = const Left(NetworkFailure('offline'));
    final cubit = InboxCubit(repo);
    await cubit.load();

    await cubit.markAllRead();

    expect(cubit.state.unreadCount, 1);
    expect(cubit.state.items.firstWhere((i) => i.id == 'a').isUnread, isTrue);
    await cubit.close();
  });

  test('refreshing the badge alone does not load the list', () async {
    repo.countResult = const Right(5);
    final cubit = InboxCubit(repo);

    await cubit.refreshCount();

    expect(cubit.state.unreadCount, 5);
    expect(repo.pageCalls, 0);
    await cubit.close();
  });

  test('a badge that cannot be read shows nothing rather than a guess', () async {
    repo.countResult = const Left(NetworkFailure('offline'));
    final cubit = InboxCubit(repo);

    await cubit.refreshCount();

    expect(cubit.state.unreadCount, 0);
    await cubit.close();
  });
}
