import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../home/domain/entities/provider_summary.dart';
import '../../domain/repositories/search_repository.dart';

// ---------- Events ----------

abstract class SearchEvent extends Equatable {
  const SearchEvent();

  @override
  List<Object?> get props => [];
}

/// Query text changed (already debounced by the UI).
class SearchQueryChanged extends SearchEvent {
  final String query;

  const SearchQueryChanged(this.query);

  @override
  List<Object?> get props => [query];
}

/// Category chip selected (null = all).
class SearchCategoryChanged extends SearchEvent {
  final String? category;

  const SearchCategoryChanged(this.category);

  @override
  List<Object?> get props => [category];
}

/// Retry after an error, or clear filters from the empty state.
class SearchRetried extends SearchEvent {
  const SearchRetried();
}

class SearchCleared extends SearchEvent {
  const SearchCleared();
}

// ---------- State ----------

enum SearchStatus { loading, loaded, empty, error }

class SearchState extends Equatable {
  final SearchStatus status;
  final String query;
  final String? category;
  final List<ProviderSummary> results;
  final String? errorMessage;

  const SearchState({
    this.status = SearchStatus.loading,
    this.query = '',
    this.category,
    this.results = const [],
    this.errorMessage,
  });

  SearchState copyWith({
    SearchStatus? status,
    String? query,
    String? Function()? category,
    List<ProviderSummary>? results,
    String? errorMessage,
  }) {
    return SearchState(
      status: status ?? this.status,
      query: query ?? this.query,
      category: category != null ? category() : this.category,
      results: results ?? this.results,
      errorMessage: errorMessage,
    );
  }

  @override
  List<Object?> get props => [status, query, category, results, errorMessage];
}

// ---------- Bloc ----------

/// Provider search. A monotonically increasing request id guards against
/// the stale-result race: an older in-flight response never overwrites a
/// newer query's results.
class SearchBloc extends Bloc<SearchEvent, SearchState> {
  final SearchRepository repository;
  int _requestId = 0;

  SearchBloc(this.repository) : super(const SearchState()) {
    on<SearchQueryChanged>(_onQueryChanged);
    on<SearchCategoryChanged>(_onCategoryChanged);
    on<SearchRetried>(_onRetried);
    on<SearchCleared>(_onCleared);
  }

  Future<void> _onQueryChanged(
    SearchQueryChanged event,
    Emitter<SearchState> emit,
  ) =>
      _search(emit, query: event.query, category: state.category);

  Future<void> _onCategoryChanged(
    SearchCategoryChanged event,
    Emitter<SearchState> emit,
  ) =>
      _search(emit, query: state.query, category: event.category);

  Future<void> _onRetried(
    SearchRetried event,
    Emitter<SearchState> emit,
  ) =>
      _search(emit, query: state.query, category: state.category);

  Future<void> _onCleared(
    SearchCleared event,
    Emitter<SearchState> emit,
  ) =>
      _search(emit, query: '', category: null);

  Future<void> _search(
    Emitter<SearchState> emit, {
    required String query,
    required String? category,
  }) async {
    final id = ++_requestId;
    emit(state.copyWith(
      status: SearchStatus.loading,
      query: query,
      category: () => category,
    ));

    final result = await repository.searchProviders(
      searchTerm: query.trim().isEmpty ? null : query.trim(),
      serviceCategory: category,
    );

    // A newer search superseded this one — drop the stale result.
    if (id != _requestId) return;

    result.fold(
      (failure) => emit(state.copyWith(
        status: SearchStatus.error,
        errorMessage: failure.message,
      )),
      (providers) => emit(state.copyWith(
        status:
            providers.isEmpty ? SearchStatus.empty : SearchStatus.loaded,
        results: providers,
      )),
    );
  }
}
