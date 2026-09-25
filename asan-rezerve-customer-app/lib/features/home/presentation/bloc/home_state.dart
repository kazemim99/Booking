import 'package:equatable/equatable.dart';
import '../../domain/entities/category.dart';
import '../../domain/entities/upcoming_booking.dart';
import '../../domain/entities/provider_summary.dart';
import '../../domain/entities/promotion.dart';
import '../../domain/entities/recently_visited_provider.dart';
import '../../domain/entities/favorite_provider.dart';
import '../../domain/usecases/get_home_data.dart' show HomeData, HomeSection;

/// Base state for home screen
abstract class HomeState extends Equatable {
  const HomeState();

  @override
  List<Object?> get props => [];
}

/// Initial state
class HomeInitial extends HomeState {
  const HomeInitial();
}

/// Loading state
class HomeLoading extends HomeState {
  const HomeLoading();
}

/// Loaded state with data. Sections that failed are listed in
/// [failedSections] so the UI renders an inline retry for just those.
class HomeLoaded extends HomeState {
  final List<Category> categories;
  final List<UpcomingBooking> upcomingBookings;
  final List<ProviderSummary> topProviders;
  final List<Promotion> promotions;
  final List<RecentlyVisitedProvider> recentlyVisitedProviders;
  final List<FavoriteProvider> favoriteProviders;
  final Set<HomeSection> failedSections;

  const HomeLoaded({
    required this.categories,
    required this.upcomingBookings,
    required this.topProviders,
    required this.promotions,
    required this.recentlyVisitedProviders,
    required this.favoriteProviders,
    this.failedSections = const {},
  });

  factory HomeLoaded.fromData(HomeData data) => HomeLoaded(
        categories: data.categories,
        upcomingBookings: data.upcomingBookings,
        topProviders: data.topProviders,
        promotions: data.promotions,
        recentlyVisitedProviders: data.recentlyVisitedProviders,
        favoriteProviders: data.favoriteProviders,
        failedSections: data.failedSections,
      );

  HomeData toData() => HomeData(
        categories: categories,
        upcomingBookings: upcomingBookings,
        topProviders: topProviders,
        promotions: promotions,
        recentlyVisitedProviders: recentlyVisitedProviders,
        favoriteProviders: favoriteProviders,
        failedSections: failedSections,
      );

  @override
  List<Object?> get props => [
        categories,
        upcomingBookings,
        topProviders,
        promotions,
        recentlyVisitedProviders,
        favoriteProviders,
        failedSections,
      ];
}

/// Error state
class HomeError extends HomeState {
  final String message;

  const HomeError(this.message);

  @override
  List<Object?> get props => [message];
}
