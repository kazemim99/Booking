import 'package:dartz/dartz.dart';
import '../../../../core/errors/failures.dart';
import '../entities/category.dart';
import '../entities/upcoming_booking.dart';
import '../entities/provider_summary.dart';
import '../entities/promotion.dart';
import '../entities/recently_visited_provider.dart';
import '../entities/favorite_provider.dart';

/// Repository interface for home screen data
abstract class HomeRepository {
  /// Get popular categories
  Future<Either<Failure, List<Category>>> getPopularCategories();

  /// Get upcoming bookings (limited to 2)
  Future<Either<Failure, List<UpcomingBooking>>> getUpcomingBookings({int limit = 2});

  /// Get top providers (recommended)
  Future<Either<Failure, List<ProviderSummary>>> getTopProviders({int limit = 10});

  /// The same salons, each carrying how soon it can be booked. Returns them
  /// unchanged when the availability call fails — a card without free times is
  /// still a card.
  Future<List<ProviderSummary>> withAvailability(List<ProviderSummary> providers);

  /// Get active promotions
  Future<Either<Failure, List<Promotion>>> getPromotions();

  /// Get recently visited providers for a customer
  Future<Either<Failure, List<RecentlyVisitedProvider>>> getRecentlyVisitedProviders(String customerId, {int limit = 10});

  /// Get favorite providers for a customer
  Future<Either<Failure, List<FavoriteProvider>>> getFavoriteProviders(String customerId);

  /// Record a provider visit
  Future<Either<Failure, void>> recordProviderVisit(String customerId, String providerId, {String? viewSource});

  /// The ids of the customer's favourite salons.
  Future<Either<Failure, Set<String>>> getFavoriteProviderIds(String customerId);

  /// Make the salon one of the customer's favourites. Already being one is
  /// success: the customer's wish holds either way.
  Future<Either<Failure, Unit>> addFavoriteProvider(String customerId, String providerId);

  /// Take the salon out of the customer's favourites. Not being one is success.
  Future<Either<Failure, Unit>> removeFavoriteProvider(String customerId, String providerId);
}
