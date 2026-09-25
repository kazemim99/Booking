import 'package:dartz/dartz.dart';
import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../core/errors/failures.dart';
import '../../../../core/utils/persian_text.dart';
import '../../../../core/utils/phone_number.dart';
import '../../domain/entities/provider_client.dart';
import '../../domain/entities/saved_customer.dart';
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

/// State for the Clients tab: the salon's saved customers and the customers
/// who booked online, as one book (spec: provider-customer-book); local
/// Persian-normalized search, pull-to-refresh, add/edit/remove/import.
class ClientsCubit extends Cubit<ClientsState> {
  final HomeRepository _repository;

  ClientsCubit(this._repository) : super(const ClientsState());

  Future<void> load() async {
    emit(state.copyWith(status: ClientsStatus.loading, error: () => null));
    await _fetch();
  }

  Future<void> refresh() => _fetch();

  void search(String query) => emit(state.copyWith(query: query));

  /// Saves a new customer; the failure's message, or null on success.
  Future<String?> add(CustomerDraft draft) =>
      _change(() => _repository.addCustomer(draft));

  Future<String?> update(String id, CustomerDraft draft) =>
      _change(() => _repository.updateCustomer(id, draft));

  Future<String?> remove(String id) =>
      _change(() => _repository.removeCustomer(id));

  /// Saves the contacts the provider ticked; the summary, or the failure's message.
  Future<(CustomerImportSummary?, String?)> importContacts(
      List<CustomerDraft> contacts) async {
    final result = await _repository.importCustomers(contacts);
    final summary = result.fold((_) => null, (s) => s);
    if (summary == null) return (null, result.fold((f) => f.message, (_) => null));
    await _fetch();
    return (summary, null);
  }

  Future<String?> _change<T>(
      Future<Either<Failure, T>> Function() call) async {
    final result = await call();
    if (result.isLeft()) return result.fold((f) => f.message, (_) => null);
    await _fetch();
    return null;
  }

  /// One row per person: a saved customer and an online client with the same
  /// number are one row carrying both their bookings. Saved customers first.
  static List<ProviderClient> merge(
      List<SavedCustomer> saved, List<ProviderClient> booked) {
    final byPhone = {
      for (final b in booked)
        if (b.phone.isNotEmpty) PhoneNumber.normalize(b.phone): b,
    };
    final merged = <String>{};
    final rows = <ProviderClient>[];
    for (final s in saved) {
      final phone = PhoneNumber.normalize(s.phone);
      final b = byPhone[phone];
      if (b != null) merged.add(b.customerId);
      rows.add(ProviderClient(
        customerId: s.id,
        name: s.fullName,
        phone: phone,
        totalBookings: s.totalBookings + (b?.totalBookings ?? 0),
        completedBookings: b?.completedBookings ?? 0,
        upcomingBookings: s.upcomingBookings + (b?.upcomingBookings ?? 0),
        lastVisitAt: b?.lastVisitAt,
        saved: s,
      ));
    }
    rows.addAll(booked.where((b) => !merged.contains(b.customerId)));
    return rows;
  }

  Future<void> _fetch() async {
    final savedF = _repository.fetchSavedCustomers();
    final bookedF = _repository.fetchClients();
    final savedOr = await savedF;
    final bookedOr = await bookedF;
    if (isClosed) return;

    // Either half alone is still a useful book; only both failing is an error.
    if (savedOr.isLeft() && bookedOr.isLeft()) {
      final message = bookedOr.fold((f) => f.message, (_) => null);
      emit(state.all.isEmpty
          ? state.copyWith(status: ClientsStatus.failed, error: () => message)
          : state); // keep showing the loaded book on refresh failure
      return;
    }
    final saved = savedOr.getOrElse(() => const []);
    final booked = bookedOr.getOrElse(() => const []);
    emit(state.copyWith(
      status: ClientsStatus.ready,
      all: merge(saved, booked),
      error: () => null,
    ));
  }
}
