import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../home/domain/entities/composer_models.dart';
import '../../home/domain/repositories/home_repository.dart';
import '../domain/promotion.dart';
import '../domain/promotions_repository.dart';

class PromotionsState extends Equatable {
  final List<Promotion> promotions;
  final List<CampaignOffer> campaigns;

  /// The salon's services, for targeting a promotion at some of them. Empty when they could not be read — the
  /// form then offers "every service" only, rather than failing the whole page.
  final List<ComposerService> services;
  final bool loading;

  /// True once a load has SUCCEEDED — a failure is never shown as "no discounts".
  final bool loaded;
  final String? error;

  /// The promotion or campaign whose change is in flight.
  final String? busyId;

  const PromotionsState({
    this.promotions = const [],
    this.campaigns = const [],
    this.services = const [],
    this.loading = false,
    this.loaded = false,
    this.error,
    this.busyId,
  });

  PromotionsState copyWith({
    List<Promotion>? promotions,
    List<CampaignOffer>? campaigns,
    List<ComposerService>? services,
    bool? loading,
    bool? loaded,
    String? error,
    bool clearError = false,
    String? busyId,
    bool clearBusy = false,
  }) =>
      PromotionsState(
        promotions: promotions ?? this.promotions,
        campaigns: campaigns ?? this.campaigns,
        services: services ?? this.services,
        loading: loading ?? this.loading,
        loaded: loaded ?? this.loaded,
        error: clearError ? null : (error ?? this.error),
        busyId: clearBusy ? null : (busyId ?? this.busyId),
      );

  int get joinedCount => campaigns.where((c) => c.isJoined).length;

  @override
  List<Object?> get props => [promotions, campaigns, services, loading, loaded, error, busyId];
}

/// The salon's discounts and its platform campaigns (openspec/changes/add-discounts-and-campaigns). Every change takes
/// the state the server returned, never one the app assumed; a refusal returns the server's Persian reason.
class PromotionsCubit extends Cubit<PromotionsState> {
  final PromotionsRepository _repository;
  final HomeRepository _home;

  PromotionsCubit(this._repository, this._home) : super(const PromotionsState());

  Future<void> load() async {
    emit(state.copyWith(loading: true, clearError: true));
    final promotionsF = _repository.list();
    final campaignsF = _repository.campaigns();
    final servicesF = _home.fetchServices();
    final promotions = await promotionsF;
    final campaigns = await campaignsF;
    final services = await servicesF;
    if (isClosed) return;

    final failure = promotions.fold((f) => f, (_) => null) ?? campaigns.fold((f) => f, (_) => null);
    if (failure != null) {
      emit(state.copyWith(loading: false, error: failure.message));
      return;
    }
    emit(state.copyWith(
      promotions: promotions.getOrElse(() => const []),
      campaigns: campaigns.getOrElse(() => const []),
      services: services.getOrElse(() => const []),
      loading: false,
      loaded: true,
      clearError: true,
    ));
  }

  /// Creates, or edits [editingId]. Returns null on success, else the reason; the form keeps its input on failure.
  Future<String?> save(PromotionDraft draft, {String? editingId}) async {
    final invalid = draft.validate(DateTime.now().toUtc());
    if (invalid != null) return invalid;

    final result = editingId == null ? await _repository.create(draft) : await _repository.update(editingId, draft);
    if (isClosed) return null;
    return result.fold((failure) => failure.message, (saved) {
      final exists = state.promotions.any((p) => p.id == saved.id);
      emit(state.copyWith(
        promotions: exists
            ? [for (final p in state.promotions) p.id == saved.id ? saved : p]
            : [saved, ...state.promotions],
      ));
      return null;
    });
  }

  Future<String?> change(String promotionId, PromotionAction action) async {
    emit(state.copyWith(busyId: promotionId));
    final result = await _repository.change(promotionId, action);
    if (isClosed) return null;
    return result.fold((failure) {
      emit(state.copyWith(clearBusy: true));
      return failure.message;
    }, (updated) {
      emit(state.copyWith(
        promotions: [for (final p in state.promotions) p.id == updated.id ? updated : p],
        clearBusy: true,
      ));
      return null;
    });
  }

  Future<String?> setJoined(String campaignId, {required bool join}) async {
    emit(state.copyWith(busyId: campaignId));
    final result = await _repository.setJoined(campaignId, join: join);
    if (isClosed) return null;
    return result.fold((failure) {
      emit(state.copyWith(clearBusy: true));
      return failure.message;
    }, (offer) {
      emit(state.copyWith(
        campaigns: [
          for (final c in state.campaigns) c.campaign.id == campaignId ? c.withJoined(offer.isJoined) : c,
        ],
        clearBusy: true,
      ));
      return null;
    });
  }
}
