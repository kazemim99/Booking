import 'package:dartz/dartz.dart';
import '../../../../core/errors/failures.dart';
import '../../../home/domain/entities/provider_summary.dart';

abstract class SearchRepository {
  Future<Either<Failure, List<ProviderSummary>>> searchProviders({
    String? searchTerm,
    String? serviceCategory,
    int pageNumber,
    int pageSize,
    double? latitude,
    double? longitude,
    double? radiusKm,
    String sortBy,
  });

  /// Providers around a point, each carrying its coordinates and a
  /// server-computed distance.
  ///
  /// Separate from [searchProviders] because it is backed by a different
  /// endpoint (`/Providers/by-location`): that is the only one that returns a
  /// position, and a map cannot place a pin without one.
  ///
  /// [serviceCategory] is the API's `ServiceCategory` enum name, never a
  /// Persian label.
  Future<Either<Failure, List<ProviderSummary>>> providersByLocation({
    required double latitude,
    required double longitude,
    double radiusKm,
    String? serviceCategory,
    int pageNumber,
    int pageSize,
  });
}
