import 'package:dartz/dartz.dart';
import 'package:injectable/injectable.dart';
import '../../../../core/errors/failures.dart';
import '../../../../core/storage/secure_storage_service.dart';
import '../entities/category.dart';
import '../entities/upcoming_booking.dart';
import '../entities/provider_summary.dart';
import '../entities/promotion.dart';
import '../entities/recently_visited_provider.dart';
import '../entities/favorite_provider.dart';
import '../repositories/home_repository.dart';

/// Independently loadable home sections. Each can fail and be retried
/// without blanking the rest of the screen.
enum HomeSection {
  categories,
  upcomingBookings,
  topProviders,
  promotions,
  recentAndFavorites,
}

/// Response model for home screen data.
class HomeData {
  final List<Category> categories;
  final List<UpcomingBooking> upcomingBookings;
  final List<ProviderSummary> topProviders;
  final List<Promotion> promotions;
  final List<RecentlyVisitedProvider> recentlyVisitedProviders;
  final List<FavoriteProvider> favoriteProviders;

  /// Sections whose request failed (as opposed to legitimately empty).
  final Set<HomeSection> failedSections;

  const HomeData({
    required this.categories,
    required this.upcomingBookings,
    required this.topProviders,
    required this.promotions,
    required this.recentlyVisitedProviders,
    required this.favoriteProviders,
    this.failedSections = const {},
  });
}

/// Fetches all home sections in parallel. One failed section never fails
/// the whole screen; only a total failure returns Left.
@lazySingleton
class GetHomeData {
  final HomeRepository repository;
  final SecureStorageService storageService;

  GetHomeData(this.repository, this.storageService);

  Future<Either<Failure, HomeData>> call() async {
    try {
      final customerId = await storageService.getCustomerId();
      final isAuthenticated = customerId != null;

      final results = await Future.wait([
        repository.getPopularCategories(),
        repository.getUpcomingBookings(limit: 2),
        repository.getTopProviders(limit: 10),
        repository.getPromotions(),
        if (isAuthenticated)
          repository.getRecentlyVisitedProviders(customerId, limit: 10),
        if (isAuthenticated) repository.getFavoriteProviders(customerId),
      ]).timeout(const Duration(seconds: 15));

      final failed = <HomeSection>{
        if (results[0].isLeft()) HomeSection.categories,
        if (results[1].isLeft()) HomeSection.upcomingBookings,
        if (results[2].isLeft()) HomeSection.topProviders,
        if (results[3].isLeft()) HomeSection.promotions,
        if (isAuthenticated && (results[4].isLeft() || results[5].isLeft()))
          HomeSection.recentAndFavorites,
      };

      if (failed.length == HomeSection.values.length ||
          (!isAuthenticated && failed.length >= 4)) {
        return const Left(
          ServerFailure('خطا در بارگذاری داده‌ها. لطفاً دوباره تلاش کنید.'),
        );
      }

      return Right(HomeData(
        categories: results[0].getOrElse(() => <Category>[]) as List<Category>,
        upcomingBookings: results[1].getOrElse(() => <UpcomingBooking>[])
            as List<UpcomingBooking>,
        topProviders: results[2].getOrElse(() => <ProviderSummary>[])
            as List<ProviderSummary>,
        promotions:
            results[3].getOrElse(() => <Promotion>[]) as List<Promotion>,
        recentlyVisitedProviders: isAuthenticated
            ? results[4].getOrElse(() => <RecentlyVisitedProvider>[])
                as List<RecentlyVisitedProvider>
            : const [],
        favoriteProviders: isAuthenticated
            ? results[5].getOrElse(() => <FavoriteProvider>[])
                as List<FavoriteProvider>
            : const [],
        failedSections: failed,
      ));
    } catch (e) {
      return Left(ServerFailure('خطا در بارگذاری داده‌ها: ${e.toString()}'));
    }
  }

  /// Re-fetches a single failed section. Returns the fresh data merged into
  /// [current].
  Future<Either<Failure, HomeData>> retrySection(
    HomeSection section,
    HomeData current,
  ) async {
    switch (section) {
      case HomeSection.categories:
        final r = await repository.getPopularCategories();
        return r.map((v) => _merge(current, section, categories: v));
      case HomeSection.upcomingBookings:
        final r = await repository.getUpcomingBookings(limit: 2);
        return r.map((v) => _merge(current, section, upcomingBookings: v));
      case HomeSection.topProviders:
        final r = await repository.getTopProviders(limit: 10);
        return r.map((v) => _merge(current, section, topProviders: v));
      case HomeSection.promotions:
        final r = await repository.getPromotions();
        return r.map((v) => _merge(current, section, promotions: v));
      case HomeSection.recentAndFavorites:
        final customerId = await storageService.getCustomerId();
        if (customerId == null) {
          return Right(_merge(current, section));
        }
        final recent =
            await repository.getRecentlyVisitedProviders(customerId, limit: 10);
        final favorites = await repository.getFavoriteProviders(customerId);
        if (recent.isLeft()) return recent.map((_) => current);
        if (favorites.isLeft()) return favorites.map((_) => current);
        return Right(_merge(
          current,
          section,
          recentlyVisited: recent.getOrElse(() => []),
          favorites: favorites.getOrElse(() => []),
        ));
    }
  }

  HomeData _merge(
    HomeData current,
    HomeSection recovered, {
    List<Category>? categories,
    List<UpcomingBooking>? upcomingBookings,
    List<ProviderSummary>? topProviders,
    List<Promotion>? promotions,
    List<RecentlyVisitedProvider>? recentlyVisited,
    List<FavoriteProvider>? favorites,
  }) {
    return HomeData(
      categories: categories ?? current.categories,
      upcomingBookings: upcomingBookings ?? current.upcomingBookings,
      topProviders: topProviders ?? current.topProviders,
      promotions: promotions ?? current.promotions,
      recentlyVisitedProviders:
          recentlyVisited ?? current.recentlyVisitedProviders,
      favoriteProviders: favorites ?? current.favoriteProviders,
      failedSections: {...current.failedSections}..remove(recovered),
    );
  }
}
