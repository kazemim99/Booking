import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:injectable/injectable.dart';
import '../../domain/usecases/get_home_data.dart';
import 'home_event.dart';
import 'home_state.dart';

@injectable
class HomeBloc extends Bloc<HomeEvent, HomeState> {
  final GetHomeData getHomeData;

  HomeBloc(this.getHomeData) : super(const HomeInitial()) {
    on<LoadHomeData>(_onLoadHomeData);
    on<RefreshHomeData>(_onRefreshHomeData);
  }

  Future<void> _onLoadHomeData(
    LoadHomeData event,
    Emitter<HomeState> emit,
  ) async {
    emit(const HomeLoading());
    await _fetchData(emit);
  }

  Future<void> _onRefreshHomeData(
    RefreshHomeData event,
    Emitter<HomeState> emit,
  ) async {
    // Keep previous state while refreshing (better UX)
    await _fetchData(emit);
  }

  Future<void> _fetchData(Emitter<HomeState> emit) async {
    try {
      final result = await getHomeData();

      result.fold(
        (failure) => emit(HomeError(failure.message)),
        (homeData) => emit(HomeLoaded(
          categories: homeData.categories,
          upcomingBookings: homeData.upcomingBookings,
          topProviders: homeData.topProviders,
          promotions: homeData.promotions,
          recentlyVisitedProviders: homeData.recentlyVisitedProviders,
          favoriteProviders: homeData.favoriteProviders,
        )),
      );
    } catch (e) {
      // Ensure we never get stuck in loading state
      emit(HomeError('خطای غیرمنتظره: ${e.toString()}'));
    }
  }
}
