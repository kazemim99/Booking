import 'package:equatable/equatable.dart';
import '../../domain/entities/category.dart';
import '../../domain/entities/upcoming_booking.dart';
import '../../domain/entities/provider_summary.dart';
import '../../domain/entities/promotion.dart';
import '../../domain/entities/recently_visited_provider.dart';
import '../../domain/entities/favorite_provider.dart';

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

/// Loaded state with data
class HomeLoaded extends HomeState {
  final List<Category> categories;
  final List<UpcomingBooking> upcomingBookings;
  final List<ProviderSummary> topProviders;
  final List<Promotion> promotions;
  final List<RecentlyVisitedProvider> recentlyVisitedProviders;
  final List<FavoriteProvider> favoriteProviders;

  const HomeLoaded({
    required this.categories,
    required this.upcomingBookings,
    required this.topProviders,
    required this.promotions,
    required this.recentlyVisitedProviders,
    required this.favoriteProviders,
  });

  @override
  List<Object?> get props => [
        categories,
        upcomingBookings,
        topProviders,
        promotions,
        recentlyVisitedProviders,
        favoriteProviders,
      ];
}

/// Error state
class HomeError extends HomeState {
  final String message;

  const HomeError(this.message);

  @override
  List<Object?> get props => [message];
}
