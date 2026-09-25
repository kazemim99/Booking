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

  /// The customer lookup still on its way, if any: a heart tap made before
  /// it answers waits for it instead of failing.
  Future<void>? _signingIn;

  /// Bumped on every sign-out, so a lookup that answers after one is dropped
  /// rather than acting for a customer who has already left.
  int _session = 0;

  /// Per customer, not per page: the next customer to use this still-open
  /// page has a visit of their own.
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
  Future<void> customerSignedIn() {
    final session = _session;
    final signingIn = _lookUpCustomerId().then((id) {
      if (isClosed || session != _session || id == null || id.isEmpty) {
        return null;
      }
      _customerId = id;
      emit(state.copyWith(signedIn: true));
      return id;
    });
    _signingIn = signingIn;
    return signingIn.then((id) async {
      if (identical(_signingIn, signingIn)) _signingIn = null;
      if (id == null) return;

      if (!_visitRecorded) {
        _visitRecorded = true;
        // Not awaited, and its result dropped: a lost visit costs the
        // customer nothing, a stalled or failed page would.
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
    });
  }

  /// The customer id, or null when it cannot be read: the id comes from secure storage, which can throw, and the
  /// page neither awaits this nor can do anything with the error — an unknown customer is treated as a guest here.
  Future<String?> _lookUpCustomerId() async {
    try {
      return await customerId();
    } catch (_) {
      return null;
    }
  }

  void customerSignedOut() {
    _session++;
    _signingIn = null;
    _customerId = null;
    _customerChose = false;
    _visitRecorded = false;
    if (!isClosed) emit(const ProviderCustomerState());
  }

  /// Flips the heart at once and asks the server. Returns false when the
  /// change did not happen (refused, offline, or nobody signed in) — the heart
  /// is back where it was and the page says so.
  Future<bool> toggleFavorite() async {
    // A tap right after opening can come before the customer lookup answers:
    // wait for it rather than calling that a failure.
    final signingIn = _signingIn;
    if (_customerId == null && signingIn != null) await signingIn;
    if (isClosed) return true; // the page is gone; there is nobody to tell
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
