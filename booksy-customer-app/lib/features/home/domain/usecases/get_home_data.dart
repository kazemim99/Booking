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

/// Response model for home screen data
class HomeData {
  final List<Category> categories;
  final List<UpcomingBooking> upcomingBookings;
  final List<ProviderSummary> topProviders;
  final List<Promotion> promotions;
  final List<RecentlyVisitedProvider> recentlyVisitedProviders;
  final List<FavoriteProvider> favoriteProviders;

  const HomeData({
    required this.categories,
    required this.upcomingBookings,
    required this.topProviders,
    required this.promotions,
    required this.recentlyVisitedProviders,
    required this.favoriteProviders,
  });
}

/// Use case to fetch all home screen data
@lazySingleton
class GetHomeData {
  final HomeRepository repository;
  final SecureStorageService storageService;

  GetHomeData(this.repository, this.storageService);

  Future<Either<Failure, HomeData>> call() async {
    try {
      print('🔄 GetHomeData: Starting data fetch...');

      // Get customer ID for authenticated users
      final customerId = await storageService.getCustomerId();
      print('👤 GetHomeData: Customer ID: ${customerId ?? "Guest"}');

      // Build futures list based on auth status
      final futures = <Future<Either<Failure, dynamic>>>[
        repository.getPopularCategories(),
        repository.getUpcomingBookings(limit: 2),
        repository.getTopProviders(limit: 10),
        repository.getPromotions(),
      ];

      // Add authenticated-only requests if user is logged in
      if (customerId != null) {
        futures.add(repository.getRecentlyVisitedProviders(customerId, limit: 10));
        futures.add(repository.getFavoriteProviders(customerId));
      }

      // Fetch all data in parallel for better performance
      // Use timeout to prevent infinite loading
      final results = await Future.wait(futures).timeout(
        const Duration(seconds: 15),
        onTimeout: () {
          print('⏱️ GetHomeData: Request timed out after 15 seconds');
          final baseFailures = [
            const Left(ServerFailure('خطا در بارگذاری دسته‌بندی‌ها')),
            const Left(ServerFailure('خطا در بارگذاری رزروها')),
            const Left(ServerFailure('خطا در بارگذاری ارائه‌دهندگان')),
            const Left(ServerFailure('خطا در بارگذاری تبلیغات')),
          ];
          if (customerId != null) {
            baseFailures.add(const Left(ServerFailure('خطا در بارگذاری بازدیدها')));
            baseFailures.add(const Left(ServerFailure('خطا در بارگذاری علاقه‌مندی‌ها')));
          }
          return baseFailures;
        },
      );

      print('✅ GetHomeData: Got ${results.length} results');

      // Extract successful results, use empty lists for failures
      // This allows partial data to be shown instead of complete failure
      final categories = results[0].getOrElse(() => <Category>[]);
      final upcomingBookings = results[1].getOrElse(() => <UpcomingBooking>[]);
      final topProviders = results[2].getOrElse(() => <ProviderSummary>[]);
      final promotions = results[3].getOrElse(() => <Promotion>[]);

      // Get recently visited and favorites if user is authenticated
      final recentlyVisitedProviders = customerId != null && results.length > 4
          ? results[4].getOrElse(() => <RecentlyVisitedProvider>[])
          : <RecentlyVisitedProvider>[];
      final favoriteProviders = customerId != null && results.length > 5
          ? results[5].getOrElse(() => <FavoriteProvider>[])
          : <FavoriteProvider>[];

      print('📊 GetHomeData: categories=${categories.length}, bookings=${upcomingBookings.length}, providers=${topProviders.length}, promos=${promotions.length}, recent=${recentlyVisitedProviders.length}, favorites=${favoriteProviders.length}');

      // Check if ALL requests actually failed (not just empty)
      final allFailed = results.every((result) => result.isLeft());

      if (allFailed) {
        print('❌ GetHomeData: All API requests failed');
        return const Left(ServerFailure('خطا در بارگذاری داده‌ها. لطفاً دوباره تلاش کنید.'));
      }

      // At least one request succeeded, return the data (even if some are empty)
      print('✅ GetHomeData: Returning HomeData (some requests succeeded)');
      return Right(HomeData(
        categories: categories as List<Category>,
        upcomingBookings: upcomingBookings as List<UpcomingBooking>,
        topProviders: topProviders as List<ProviderSummary>,
        promotions: promotions as List<Promotion>,
        recentlyVisitedProviders: recentlyVisitedProviders as List<RecentlyVisitedProvider>,
        favoriteProviders: favoriteProviders as List<FavoriteProvider>,
      ));
    } catch (e) {
      print('💥 GetHomeData: Exception caught: $e');
      return Left(ServerFailure('خطا در بارگذاری داده‌ها: ${e.toString()}'));
    }
  }
}
