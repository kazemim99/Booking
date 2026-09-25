import 'dart:async';

import 'package:dartz/dartz.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:asan_rezerve_customer_app/core/errors/failures.dart';
import 'package:asan_rezerve_customer_app/features/home/domain/repositories/home_repository.dart';
import 'package:asan_rezerve_customer_app/features/search/presentation/bloc/provider_customer_cubit.dart';

/// The signed-in customer's side of a salon profile (customer-app-ux-review-fixes
/// C.4/C.5): the visit is recorded once and silently, and the heart is
/// optimistic with a rollback.
class _FakeHomeRepository implements HomeRepository {
  final visits = <(String, String, String?)>[];
  final added = <(String, String)>[];
  final removed = <(String, String)>[];

  Either<Failure, void> visitResult = const Right(null);
  Future<Either<Failure, Set<String>>> Function() favorites =
      () async => const Right(<String>{});
  Completer<Either<Failure, Unit>>? pendingChange;
  Either<Failure, Unit> changeResult = const Right(unit);

  @override
  Future<Either<Failure, void>> recordProviderVisit(
      String customerId, String providerId,
      {String? viewSource}) async {
    visits.add((customerId, providerId, viewSource));
    return visitResult;
  }

  @override
  Future<Either<Failure, Set<String>>> getFavoriteProviderIds(
          String customerId) =>
      favorites();

  @override
  Future<Either<Failure, Unit>> addFavoriteProvider(
      String customerId, String providerId) {
    added.add((customerId, providerId));
    return pendingChange?.future ?? Future.value(changeResult);
  }

  @override
  Future<Either<Failure, Unit>> removeFavoriteProvider(
      String customerId, String providerId) {
    removed.add((customerId, providerId));
    return pendingChange?.future ?? Future.value(changeResult);
  }

  @override
  dynamic noSuchMethod(Invocation invocation) =>
      throw UnimplementedError('${invocation.memberName}');
}

ProviderCustomerCubit _cubit(_FakeHomeRepository repo, {String? customerId}) =>
    ProviderCustomerCubit(
      providerId: 'p1',
      repository: repo,
      customerId: () async => customerId,
    );

void main() {
  group('the visit (C.4)', () {
    test('a signed-in customer\'s opening is recorded once', () async {
      final repo = _FakeHomeRepository();
      final cubit = _cubit(repo, customerId: 'c1');

      await cubit.customerSignedIn();
      await cubit.customerSignedIn(); // e.g. the auth state re-announces

      expect(repo.visits, [('c1', 'p1', ProviderCustomerCubit.viewSource)]);
    });

    test('a guest\'s opening is never recorded', () async {
      final repo = _FakeHomeRepository();
      final cubit = _cubit(repo);

      await cubit.customerSignedIn();

      expect(repo.visits, isEmpty);
      expect(cubit.state.signedIn, isFalse);
    });

    test('a failed recording changes nothing the customer sees', () async {
      final repo = _FakeHomeRepository()
        ..visitResult = const Left(ServerFailure('boom'))
        ..favorites = () async => const Right({'p1'});
      final cubit = _cubit(repo, customerId: 'c1');

      await cubit.customerSignedIn();

      expect(cubit.state.signedIn, isTrue);
      expect(cubit.state.isFavorite, isTrue);
    });

    test('the page does not wait for the recording', () async {
      final repo = _FakeHomeRepository();
      final never = Completer<Either<Failure, void>>();
      final cubit = ProviderCustomerCubit(
        providerId: 'p1',
        repository: _SlowVisitRepository(repo, never.future),
        customerId: () async => 'c1',
      );

      // Completes although the visit call never answers.
      await cubit.customerSignedIn();

      expect(cubit.state.signedIn, isTrue);
    });
  });

  group('the favourite (C.5)', () {
    test('starts from the customer\'s own favourites', () async {
      final repo = _FakeHomeRepository()
        ..favorites = () async => const Right({'p9', 'p1'});
      final cubit = _cubit(repo, customerId: 'c1');

      await cubit.customerSignedIn();

      expect(cubit.state.isFavorite, isTrue);
    });

    test('a salon not among them starts un-favourited', () async {
      final repo = _FakeHomeRepository()
        ..favorites = () async => const Right({'p9'});
      final cubit = _cubit(repo, customerId: 'c1');

      await cubit.customerSignedIn();

      expect(cubit.state.isFavorite, isFalse);
    });

    test('adding shows at once and stays when the server agrees', () async {
      final repo = _FakeHomeRepository()
        ..pendingChange = Completer<Either<Failure, Unit>>();
      final cubit = _cubit(repo, customerId: 'c1');
      await cubit.customerSignedIn();

      final toggled = cubit.toggleFavorite();
      await pumpEventQueue();

      expect(cubit.state.isFavorite, isTrue, reason: 'optimistic');
      expect(cubit.state.saving, isTrue);

      repo.pendingChange!.complete(const Right(unit));
      expect(await toggled, isTrue);
      expect(cubit.state.isFavorite, isTrue);
      expect(cubit.state.saving, isFalse);
      expect(repo.added, [('c1', 'p1')]);
    });

    test('removing a favourite calls DELETE for this salon', () async {
      final repo = _FakeHomeRepository()
        ..favorites = () async => const Right({'p1'});
      final cubit = _cubit(repo, customerId: 'c1');
      await cubit.customerSignedIn();

      expect(await cubit.toggleFavorite(), isTrue);

      expect(cubit.state.isFavorite, isFalse);
      expect(repo.removed, [('c1', 'p1')]);
      expect(repo.added, isEmpty);
    });

    test('a refused change is rolled back and reported', () async {
      final repo = _FakeHomeRepository()
        ..changeResult = const Left(ServerFailure('boom'));
      final cubit = _cubit(repo, customerId: 'c1');
      await cubit.customerSignedIn();

      expect(await cubit.toggleFavorite(), isFalse);

      expect(cubit.state.isFavorite, isFalse);
      expect(cubit.state.saving, isFalse);
    });

    test('a tap while a change is in flight is ignored', () async {
      final repo = _FakeHomeRepository()
        ..pendingChange = Completer<Either<Failure, Unit>>();
      final cubit = _cubit(repo, customerId: 'c1');
      await cubit.customerSignedIn();

      final first = cubit.toggleFavorite();
      await pumpEventQueue();
      await cubit.toggleFavorite();

      repo.pendingChange!.complete(const Right(unit));
      await first;
      expect(repo.added, hasLength(1));
      expect(repo.removed, isEmpty);
      expect(cubit.state.isFavorite, isTrue);
    });

    test('a late favourites answer does not undo the customer\'s tap',
        () async {
      final list = Completer<Either<Failure, Set<String>>>();
      final repo = _FakeHomeRepository()..favorites = () => list.future;
      final cubit = _cubit(repo, customerId: 'c1');

      final started = cubit.customerSignedIn();
      await pumpEventQueue();
      await cubit.toggleFavorite(); // adds
      list.complete(const Right(<String>{})); // the list from before the tap
      await started;

      expect(cubit.state.isFavorite, isTrue);
    });

    test('without a signed-in customer nothing is sent', () async {
      final repo = _FakeHomeRepository();
      final cubit = _cubit(repo);
      await cubit.customerSignedIn();

      expect(await cubit.toggleFavorite(), isFalse);
      expect(repo.added, isEmpty);
    });

    test('signing out forgets the customer', () async {
      final repo = _FakeHomeRepository()
        ..favorites = () async => const Right({'p1'});
      final cubit = _cubit(repo, customerId: 'c1');
      await cubit.customerSignedIn();

      cubit.customerSignedOut();

      expect(cubit.state.signedIn, isFalse);
      expect(cubit.state.isFavorite, isFalse);
    });

    test('a tap before the customer lookup answers waits for it, and is '
        'not reported as a failure', () async {
      final lookup = Completer<String?>();
      final repo = _FakeHomeRepository();
      final cubit = ProviderCustomerCubit(
        providerId: 'p1',
        repository: repo,
        customerId: () => lookup.future,
      );

      final started = cubit.customerSignedIn();
      final toggled = cubit.toggleFavorite(); // the page has only just opened
      await pumpEventQueue();
      expect(repo.added, isEmpty, reason: 'nobody known yet');

      lookup.complete('c1');
      await started;

      expect(await toggled, isTrue);
      expect(repo.added, [('c1', 'p1')]);
      expect(cubit.state.isFavorite, isTrue);
    });
  });

  // Review of the merged branch: the customer id is read from secure storage, which can throw. The page does not
  // await customerSignedIn, so its error went unhandled, and a heart tap waiting on the lookup rethrew it.
  group('a customer lookup that fails', () {
    test('the page treats the customer as unknown, and nothing goes unhandled', () async {
      final repo = _FakeHomeRepository();
      final cubit = ProviderCustomerCubit(
        providerId: 'p1',
        repository: repo,
        customerId: () async => throw StateError('storage unavailable'),
      );

      // Not awaited, as the page calls it.
      unawaited(cubit.customerSignedIn());
      await pumpEventQueue();

      expect(cubit.state.signedIn, isFalse);
      expect(repo.visits, isEmpty);
    });

    test('a heart tap waiting on it reports "not changed" instead of throwing', () async {
      final lookup = Completer<String?>();
      final repo = _FakeHomeRepository();
      final cubit = ProviderCustomerCubit(
        providerId: 'p1',
        repository: repo,
        customerId: () => lookup.future,
      );

      unawaited(cubit.customerSignedIn());
      final toggled = cubit.toggleFavorite();
      lookup.completeError(StateError('storage unavailable'));

      expect(await toggled, isFalse);
      expect(repo.added, isEmpty);
      expect(cubit.state.isFavorite, isFalse);
    });
  });

  group('signing out (P2 review)', () {
    test("the next customer's opening of the same page is a visit too",
        () async {
      var who = 'c1';
      final repo = _FakeHomeRepository();
      final cubit = ProviderCustomerCubit(
        providerId: 'p1',
        repository: repo,
        customerId: () async => who,
      );
      await cubit.customerSignedIn();

      cubit.customerSignedOut();
      who = 'c2';
      await cubit.customerSignedIn();

      expect(repo.visits, [
        ('c1', 'p1', ProviderCustomerCubit.viewSource),
        ('c2', 'p1', ProviderCustomerCubit.viewSource),
      ]);
    });

    test('a lookup that answers after signing out records nothing and '
        "shows nobody's favourite", () async {
      final lookup = Completer<String?>();
      final repo = _FakeHomeRepository()
        ..favorites = () async => const Right({'p1'});
      final cubit = ProviderCustomerCubit(
        providerId: 'p1',
        repository: repo,
        customerId: () => lookup.future,
      );

      final started = cubit.customerSignedIn();
      cubit.customerSignedOut();
      lookup.complete('c1');
      await started;

      expect(repo.visits, isEmpty);
      expect(cubit.state.signedIn, isFalse);
      expect(cubit.state.isFavorite, isFalse);
    });
  });
}

/// Answers everything like [inner] except the visit, which never returns.
class _SlowVisitRepository implements HomeRepository {
  final _FakeHomeRepository inner;
  final Future<Either<Failure, void>> visit;

  _SlowVisitRepository(this.inner, this.visit);

  @override
  Future<Either<Failure, void>> recordProviderVisit(
          String customerId, String providerId,
          {String? viewSource}) =>
      visit;

  @override
  Future<Either<Failure, Set<String>>> getFavoriteProviderIds(
          String customerId) =>
      inner.getFavoriteProviderIds(customerId);

  @override
  dynamic noSuchMethod(Invocation invocation) =>
      throw UnimplementedError('${invocation.memberName}');
}
