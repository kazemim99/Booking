import 'dart:async';

import 'package:dartz/dartz.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:booksy_customer_app/core/errors/failures.dart';
import 'package:booksy_customer_app/features/home/domain/entities/provider_summary.dart';
import 'package:booksy_customer_app/features/search/domain/repositories/search_repository.dart';
import 'package:booksy_customer_app/features/search/presentation/bloc/search_bloc.dart';

ProviderSummary _provider(String id, String name) => ProviderSummary(
      id: id,
      name: name,
      rating: 4.5,
      reviewCount: 10,
      startingPrice: 100,
      isOpen: true,
    );

/// Repository whose responses complete under test control, so response
/// ordering can be inverted to reproduce the stale-result race.
class FakeSearchRepository implements SearchRepository {
  final _pending = <String?, Completer<Either<Failure, List<ProviderSummary>>>>{};
  final calls = <String?>[];

  Completer<Either<Failure, List<ProviderSummary>>> expect_(String? term) =>
      _pending[term] = Completer();

  @override
  Future<Either<Failure, List<ProviderSummary>>> searchProviders({
    String? searchTerm,
    String? serviceCategory,
    int pageNumber = 1,
    int pageSize = 20,
  }) {
    calls.add(searchTerm);
    return _pending[searchTerm]!.future;
  }
}

void main() {
  group('SearchBloc', () {
    test('stale in-flight result never overwrites a newer query', () async {
      final repo = FakeSearchRepository();
      final slow = repo.expect_('کوتاهی مو'); // first query, resolves LAST
      final fast = repo.expect_('ماساژ'); // second query, resolves first
      final bloc = SearchBloc(repo);

      bloc.add(const SearchQueryChanged('کوتاهی مو'));
      await Future<void>.delayed(Duration.zero);
      bloc.add(const SearchQueryChanged('ماساژ'));
      await Future<void>.delayed(Duration.zero);

      // Newer query resolves first...
      fast.complete(Right([_provider('2', 'ماساژ آرام')]));
      await bloc.stream.firstWhere((s) => s.status == SearchStatus.loaded);

      // ...then the stale first response arrives and must be dropped.
      slow.complete(Right([_provider('1', 'آرایشگاه قدیمی')]));
      await Future<void>.delayed(const Duration(milliseconds: 20));

      expect(bloc.state.results.single.id, '2');
      expect(bloc.state.query, 'ماساژ');
      await bloc.close();
    });

    test('empty results yield empty status; error yields error status',
        () async {
      final repo = FakeSearchRepository();
      final empty = repo.expect_('هیچ');
      final bloc = SearchBloc(repo);

      bloc.add(const SearchQueryChanged('هیچ'));
      empty.complete(const Right([]));
      final emptyState =
          await bloc.stream.firstWhere((s) => s.status != SearchStatus.loading);
      expect(emptyState.status, SearchStatus.empty);

      final err = repo.expect_('خطا');
      bloc.add(const SearchQueryChanged('خطا'));
      err.complete(const Left(ServerFailure('boom')));
      final errorState =
          await bloc.stream.firstWhere((s) => s.status == SearchStatus.error);
      expect(errorState.errorMessage, 'boom');
      await bloc.close();
    });

    test('category change re-searches with the active query preserved',
        () async {
      final repo = FakeSearchRepository();
      final first = repo.expect_(null);
      final bloc = SearchBloc(repo);

      bloc.add(const SearchCategoryChanged('ماساژ'));
      first.complete(Right([_provider('3', 'اسپا')]));
      final state =
          await bloc.stream.firstWhere((s) => s.status == SearchStatus.loaded);

      expect(state.category, 'ماساژ');
      expect(state.results.single.id, '3');
      await bloc.close();
    });
  });
}
