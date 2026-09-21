import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../domain/inbox_item.dart';
import '../domain/inbox_repository.dart';

class InboxState extends Equatable {
  final List<InboxItem> items;
  final int unreadCount;
  final bool loading;

  /// True once a load has SUCCEEDED. Before that the inbox is "loading"; after a failure it is "failed" —
  /// neither of which may be shown as "you have no notifications".
  final bool loaded;
  final String? error;

  const InboxState({
    this.items = const [],
    this.unreadCount = 0,
    this.loading = false,
    this.loaded = false,
    this.error,
  });

  bool get isEmpty => loaded && error == null && items.isEmpty;

  InboxState copyWith({
    List<InboxItem>? items,
    int? unreadCount,
    bool? loading,
    bool? loaded,
    String? error,
    bool clearError = false,
  }) =>
      InboxState(
        items: items ?? this.items,
        unreadCount: unreadCount ?? this.unreadCount,
        loading: loading ?? this.loading,
        loaded: loaded ?? this.loaded,
        error: clearError ? null : (error ?? this.error),
      );

  @override
  List<Object?> get props => [items, unreadCount, loading, loaded, error];
}

/// The salon's inbox AND the bell's badge — one cubit for both, so the number on the bell and the list it
/// opens cannot disagree.
///
/// Reads are optimistic, because a badge that waits on the network lags the tap; every optimistic change is put
/// back exactly as it was if the server refuses, so a flaky connection cannot leave the number drifting.
class InboxCubit extends Cubit<InboxState> {
  final InboxRepository _repository;

  InboxCubit(this._repository) : super(const InboxState());

  Future<void> load({int pageNumber = 1, int pageSize = 20}) async {
    emit(state.copyWith(loading: true, clearError: true));

    final result = await _repository.fetchPage(pageNumber: pageNumber, pageSize: pageSize);

    result.fold(
      (failure) => emit(state.copyWith(loading: false, error: failure.message)),
      (page) => emit(state.copyWith(
        items: page.items,
        unreadCount: page.unreadCount,
        loading: false,
        loaded: true,
        clearError: true,
      )),
    );
  }

  /// The badge on its own. A failure leaves it at zero — a badge is a hint, and a guessed number is worse than
  /// none (the bell this replaces was deliberately left without a count for exactly that reason).
  Future<void> refreshCount() async {
    final result = await _repository.unreadCount();
    result.fold((_) {}, (count) => emit(state.copyWith(unreadCount: count)));
  }

  /// Returns false when the server refused, after putting the row and the badge back.
  Future<bool> markRead(String id) async {
    final index = state.items.indexWhere((i) => i.id == id);

    // Already read, or not on this page: nothing to change and nothing to tell the server. Decrementing for a
    // row that was already read is exactly how the badge drifts below the truth.
    if (index < 0 || !state.items[index].isUnread) return true;

    final before = state;
    final items = [...state.items]..[index] = state.items[index].markedRead(DateTime.now().toUtc());
    emit(state.copyWith(items: items, unreadCount: (state.unreadCount - 1).clamp(0, 1 << 30)));

    final result = await _repository.markRead(id);
    return result.fold((_) {
      emit(before);
      return false;
    }, (_) => true);
  }

  Future<bool> markAllRead() async {
    // Nothing unread: the second tap is a no-op, not a second request.
    if (state.unreadCount == 0 && state.items.every((i) => !i.isUnread)) return true;

    final before = state;
    final now = DateTime.now().toUtc();
    emit(state.copyWith(
      items: [for (final i in state.items) i.isUnread ? i.markedRead(now) : i],
      unreadCount: 0,
    ));

    final result = await _repository.markAllRead();
    return result.fold((_) {
      emit(before);
      return false;
    }, (_) => true);
  }
}
