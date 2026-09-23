import 'package:dartz/dartz.dart';
import 'package:dio/dio.dart';
import 'package:injectable/injectable.dart';
import '../../../../core/constants/app_strings.dart';
import '../../../../core/errors/failures.dart';
import '../../domain/entities/category.dart';
import '../../domain/entities/upcoming_booking.dart';
import '../../domain/entities/provider_summary.dart';
import '../../domain/entities/promotion.dart';
import '../../domain/entities/recently_visited_provider.dart';
import '../../domain/entities/favorite_provider.dart';
import '../../domain/repositories/home_repository.dart';
import '../datasources/home_remote_datasource.dart';
import '../models/category_model.dart';
import '../models/booking_model.dart';
import '../models/provider_model.dart';

@LazySingleton(as: HomeRepository)
class HomeRepositoryImpl implements HomeRepository {
  final HomeRemoteDataSource remoteDataSource;

  HomeRepositoryImpl(this.remoteDataSource);

  @override
  Future<Either<Failure, List<Category>>> getPopularCategories() async {
    try {
      final dtos = await remoteDataSource.getPopularCategories();
      final categories = dtos.map((dto) => dto.toEntity()).toList();
      return Right(categories);
    } on DioException catch (e) {
      return Left(_handleDioError(e));
    } catch (e) {
      return Left(ServerFailure('خطا در بارگذاری دسته‌بندی‌ها: ${e.toString()}'));
    }
  }

  @override
  Future<Either<Failure, List<UpcomingBooking>>> getUpcomingBookings({int limit = 2}) async {
    try {
      final dtos = await remoteDataSource.getUpcomingBookings(limit: limit);
      final bookings = dtos.map((dto) => dto.toUpcomingBooking()).toList();
      return Right(bookings);
    } on DioException catch (e) {
      // For guest users (401 Unauthorized), return empty list instead of error
      if (e.response?.statusCode == 401) {
        return const Right([]);
      }
      return Left(_handleDioError(e));
    } catch (e) {
      return Left(ServerFailure('خطا در بارگذاری رزروها: ${e.toString()}'));
    }
  }

  @override
  Future<Either<Failure, List<ProviderSummary>>> getTopProviders({int limit = 10}) async {
    try {
      final dtos = await remoteDataSource.getTopProviders(limit: limit);
      final providers = dtos.map((dto) => dto.toEntity()).toList();
      return Right(providers);
    } on DioException catch (e) {
      return Left(_handleDioError(e));
    } catch (e) {
      return Left(ServerFailure('خطا در بارگذاری ارائه‌دهندگان: ${e.toString()}'));
    }
  }

  @override
  Future<List<ProviderSummary>> withAvailability(
      List<ProviderSummary> providers) async {
    if (providers.isEmpty) return providers;
    try {
      final rows = await remoteDataSource
          .getAvailabilitySummary(providers.map((p) => p.id).toList());
      final byId = {
        for (final row in rows) row['providerId']?.toString(): row,
      };
      return providers.map((p) {
        final row = byId[p.id];
        if (row == null) return p;
        final date = row['date'] as String?;
        return p.withAvailability(
          date == null ? null : DateTime.tryParse(date),
          (row['freeSlotCount'] as num?)?.toInt() ?? 0,
        );
      }).toList();
    } catch (_) {
      // Free times are a nicety on a card; their absence must not empty the list.
      return providers;
    }
  }

  @override
  Future<Either<Failure, List<Promotion>>> getPromotions() async {
    try {
      final data = await remoteDataSource.getPromotions();
      // Convert to Promotion entities when backend is ready
      final promotions = data.map((json) => Promotion(
        id: json['id'] as String? ?? '',
        title: json['title'] as String? ?? '',
        description: json['description'] as String?,
        imageUrl: json['imageUrl'] as String? ?? '',
        actionUrl: json['actionUrl'] as String?,
        validUntil: json['validUntil'] != null
            ? DateTime.parse(json['validUntil'] as String)
            : null,
      )).toList();
      return Right(promotions);
    } catch (e) {
      // Return empty list for now since promotions are optional
      return const Right([]);
    }
  }

  @override
  Future<Either<Failure, List<RecentlyVisitedProvider>>> getRecentlyVisitedProviders(
      String customerId, {int limit = 10}) async {
    try {
      final dtos = await remoteDataSource.getRecentlyVisitedProviders(customerId, limit: limit);
      // A row without a salon name (a server from before the name was added)
      // has nothing to draw; it is left out so the section stays hidden
      // instead of showing its load error.
      final providers = dtos
          .where((dto) => _hasName(dto.providerName))
          .map((dto) => RecentlyVisitedProvider(
        providerId: dto.providerId,
        providerName: dto.providerName!,
        providerType: dto.providerType,
        logoUrl: dto.logoUrl,
        city: dto.city,
        state: dto.state,
        averageRating: dto.averageRating,
        totalReviews: dto.totalReviews,
        lastVisitedAt: dto.lastVisitedAt,
        visitCount: dto.visitCount,
      )).toList();
      return Right(providers);
    } on DioException catch (e) {
      // For guest users (401 Unauthorized), return empty list instead of error
      if (e.response?.statusCode == 401) {
        return const Right([]);
      }
      return Left(_handleDioError(e));
    } catch (e) {
      return Left(ServerFailure('خطا در بارگذاری بازدیدهای اخیر: ${e.toString()}'));
    }
  }

  static bool _hasName(String? name) => name != null && name.trim().isNotEmpty;

  @override
  Future<Either<Failure, List<FavoriteProvider>>> getFavoriteProviders(String customerId) async {
    try {
      final dtos = await remoteDataSource.getFavoriteProviders(customerId);
      // Rows without a salon name are left out, as for recent visits.
      final providers = dtos
          .where((dto) => _hasName(dto.providerName))
          .map((dto) => FavoriteProvider(
        providerId: dto.providerId,
        providerName: dto.providerName!,
        providerType: dto.providerType,
        logoUrl: dto.logoUrl,
        city: dto.city,
        state: dto.state,
        averageRating: dto.averageRating,
        totalReviews: dto.totalReviews,
        addedAt: dto.addedAt,
        notes: dto.notes,
      )).toList();
      return Right(providers);
    } on DioException catch (e) {
      // For guest users (401 Unauthorized), return empty list instead of error
      if (e.response?.statusCode == 401) {
        return const Right([]);
      }
      return Left(_handleDioError(e));
    } catch (e) {
      return Left(ServerFailure('خطا در بارگذاری علاقه‌مندی‌ها: ${e.toString()}'));
    }
  }

  @override
  Future<Either<Failure, void>> recordProviderVisit(
      String customerId, String providerId, {String? viewSource}) async {
    try {
      await remoteDataSource.recordProviderVisit(customerId, providerId, viewSource: viewSource);
      return const Right(null);
    } on DioException catch (e) {
      return Left(_handleDioError(e));
    } catch (e) {
      return Left(ServerFailure('خطا در ثبت بازدید: ${e.toString()}'));
    }
  }

  @override
  Future<Either<Failure, Set<String>>> getFavoriteProviderIds(String customerId) async {
    try {
      return Right(await remoteDataSource.getFavoriteProviderIds(customerId));
    } on DioException catch (e) {
      return Left(_handleDioError(e));
    } catch (_) {
      return const Left(ServerFailure(AppStrings.genericError));
    }
  }

  @override
  Future<Either<Failure, Unit>> addFavoriteProvider(String customerId, String providerId) =>
      _favoriteChange(
        () => remoteDataSource.addFavoriteProvider(customerId, providerId),
        // 409: it already is a favourite — the state the customer asked for.
        alreadyDone: 409,
      );

  @override
  Future<Either<Failure, Unit>> removeFavoriteProvider(String customerId, String providerId) =>
      _favoriteChange(
        () => remoteDataSource.removeFavoriteProvider(customerId, providerId),
        // 404: it was not a favourite — again the state the customer asked for.
        alreadyDone: 404,
      );

  Future<Either<Failure, Unit>> _favoriteChange(
    Future<void> Function() call, {
    required int alreadyDone,
  }) async {
    try {
      await call();
      return const Right(unit);
    } on DioException catch (e) {
      if (e.response?.statusCode == alreadyDone) return const Right(unit);
      return Left(_handleDioError(e));
    } catch (_) {
      return const Left(ServerFailure(AppStrings.genericError));
    }
  }

  /// Handle Dio errors and convert to Failures
  Failure _handleDioError(DioException error) {
    switch (error.type) {
      case DioExceptionType.connectionTimeout:
      case DioExceptionType.sendTimeout:
      case DioExceptionType.receiveTimeout:
        return const NetworkFailure('اتصال به سرور برقرار نشد. لطفاً اینترنت خود را بررسی کنید');

      case DioExceptionType.badResponse:
        final statusCode = error.response?.statusCode;
        if (statusCode == 401) {
          return const UnauthorizedFailure('لطفاً دوباره وارد شوید');
        } else if (statusCode == 404) {
          return const NotFoundFailure('اطلاعات مورد نظر یافت نشد');
        } else {
          return ServerFailure('خطای سرور: ${error.response?.statusMessage ?? "نامشخص"}');
        }

      case DioExceptionType.cancel:
        return const ServerFailure('درخواست لغو شد');

      case DioExceptionType.connectionError:
        return const NetworkFailure('خطا در اتصال به اینترنت');

      default:
        return ServerFailure('خطای نامشخص: ${error.message}');
    }
  }
}
