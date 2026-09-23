import 'dart:async';

import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../home/domain/repositories/home_repository.dart';

class ProviderCustomerState extends Equatable {
  /// A customer id is known, so the visit and the heart talk to the server.
  final bool signedIn;

  /// Whether this salon is one of the customer's favourites, as far as the
  /// page knows — the optimistic value while a change is [saving].
  final bool isFavorite;

  /// A favourite change is on its way to the server.
  final bool saving;

  const ProviderCustomerState({
    this.signedIn = false,
    this.isFavorite = false,
    this.saving = false,
  });

  ProviderCustomerState copyWith({
    bool? signedIn,
    bool? isFavorite,
    bool? saving,
  }) =>
      ProviderCustomerState(
        signedIn: signedIn ?? this.signedIn,
        isFavorite: isFavorite ?? this.isFavorite,
        saving: saving ?? this.saving,
      );

  @override
  List<Object?> get props => [signedIn, isFavorite, saving];
}

/// What one salon profile is to the signed-in customer looking at it: a visit
/// worth remembering (home's "recently visited") and, perhaps, a favourite.
///
/// Guests have no customer id, so nothing is sent for them. The visit is
/// fire-and-forget — recorded once per opening of the page, never awaited,
/// and its failure is invisible. The favourite is optimistic: the heart
/// changes on tap and changes back if the server refuses.
class ProviderCustomerCubit extends Cubit<ProviderCustomerState> {
  /// Sent as `viewSource`, so the server can tell profile visits apart.
  static const String viewSource = 'profile';

  final String providerId;
  final HomeRepository repository;

  /// The signed-in customer's id; null for a guest.
  final Future<String?> Function() customerId;

  String? _customerId;
  bool _visitRecorded = false;

  /// Set once the customer has tapped the heart: from then on their choice,
  /// not a favourites list fetched before it, decides the heart.
  bool _customerChose = false;

  ProviderCustomerCubit({
    required this.providerId,
    required this.repository,
    required this.customerId,
  }) : super(const ProviderCustomerState());

  /// The page opened with a session, or one began while it was open (the
  /// login round-trip can come back to this very page).
  Future<void> customerSignedIn() async {
    final id = await customerId();
    if (isClosed || id == null || id.isEmpty) return;
    _customerId = id;
    emit(state.copyWith(signedIn: true));

    if (!_visitRecorded) {
      _visitRecorded = true;
      // Not awaited, and its result dropped: a lost visit costs the customer
      // nothing, a stalled or failed page would.
      unawaited(repository
          .recordProviderVisit(id, providerId, viewSource: viewSource)
          .then((_) {}, onError: (Object _) {}));
    }

    final favorites = await repository.getFavoriteProviderIds(id);
    if (isClosed || _customerId != id || _customerChose) return;
    favorites.fold(
      // Unknown stays "not a favourite"; adding one that already is, is
      // harmless (the repository treats it as done).
      (_) {},
      (ids) => emit(state.copyWith(isFavorite: ids.contains(providerId))),
    );
  }

  void customerSignedOut() {
    _customerId = null;
    _customerChose = false;
    if (!isClosed) emit(const ProviderCustomerState());
  }

  /// Flips the heart at once and asks the server. Returns false when the
  /// change did not happen (refused, offline, or nobody signed in) — the heart
  /// is back where it was and the page says so.
  Future<bool> toggleFavorite() async {
    final id = _customerId;
    if (id == null) return false;
    if (state.saving) return true; // one change at a time; this tap is noise
    _customerChose = true;

    final wanted = !state.isFavorite;
    emit(state.copyWith(isFavorite: wanted, saving: true));

    final result = wanted
        ? await repository.addFavoriteProvider(id, providerId)
        : await repository.removeFavoriteProvider(id, providerId);
    if (isClosed) return result.isRight();

    return result.fold(
      (_) {
        emit(state.copyWith(isFavorite: !wanted, saving: false));
        return false;
      },
      (_) {
        emit(state.copyWith(saving: false));
        return true;
      },
    );
  }
}
