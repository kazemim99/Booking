import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../core/utils/persian_text.dart';
import '../../domain/entities/provider_client.dart';
import '../../domain/repositories/home_repository.dart';

enum ClientsStatus { loading, ready, failed }

class ClientsState extends Equatable {
  final ClientsStatus status;
  final List<ProviderClient> all;
  final String query;
  final String? error;

  const ClientsState({
    this.status = ClientsStatus.loading,
    this.all = const [],
    this.query = '',
    this.error,
  });

  /// Persian-normalized filter over name and phone (spec: provider-clients).
  List<ProviderClient> get filtered => query.trim().isEmpty
      ? all
      : all
          .where((c) =>
              PersianText.contains(c.name, query) ||
              PersianText.contains(c.phone, query))
          .toList();

  ClientsState copyWith({
    ClientsStatus? status,
    List<ProviderClient>? all,
    String? query,
    String? Function()? error,
  }) {
    return ClientsState(
      status: status ?? this.status,
      all: all ?? this.all,
      query: query ?? this.query,
      error: error != null ? error() : this.error,
    );
  }

  @override
  List<Object?> get props => [status, all, query, error];
}

/// State for the Clients tab: one load of the derived client book, local
/// Persian-normalized search, pull-to-refresh.
class ClientsCubit extends Cubit<ClientsState> {
  final HomeRepository _repository;

  ClientsCubit(this._repository) : super(const ClientsState());

  Future<void> load() async {
    emit(state.copyWith(status: ClientsStatus.loading, error: () => null));
    await _fetch();
  }

  Future<void> refresh() => _fetch();

  void search(String query) => emit(state.copyWith(query: query));

  Future<void> _fetch() async {
    final result = await _repository.fetchClients();
    if (isClosed) return;
    result.fold(
      (f) => emit(state.all.isEmpty
          ? state.copyWith(
              status: ClientsStatus.failed, error: () => f.message)
          : state), // keep showing the loaded book on refresh failure
      (clients) => emit(state.copyWith(
        status: ClientsStatus.ready,
        all: clients,
        error: () => null,
      )),
    );
  }
}
