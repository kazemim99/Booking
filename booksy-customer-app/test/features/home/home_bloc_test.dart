import 'package:dartz/dartz.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:booksy_customer_app/core/errors/failures.dart';
import 'package:booksy_customer_app/core/storage/secure_storage_service.dart';
import 'package:booksy_customer_app/features/home/domain/repositories/home_repository.dart';
import 'package:booksy_customer_app/features/home/domain/usecases/get_home_data.dart';
import 'package:booksy_customer_app/features/home/presentation/bloc/home_bloc.dart';
import 'package:booksy_customer_app/features/home/presentation/bloc/home_event.dart';
import 'package:booksy_customer_app/features/home/presentation/bloc/home_state.dart';

class FakeGetHomeData implements GetHomeData {
  Either<Failure, HomeData>? callResult;
  Either<Failure, HomeData>? retryResult;
  HomeSection? lastRetriedSection;

  @override
  Future<Either<Failure, HomeData>> call() async => callResult!;

  @override
  Future<Either<Failure, HomeData>> retrySection(
    HomeSection section,
    HomeData current,
  ) async {
    lastRetriedSection = section;
    return retryResult!;
  }

  @override
  HomeRepository get repository => throw UnimplementedError();

  @override
  SecureStorageService get storageService => throw UnimplementedError();
}

HomeData _data({Set<HomeSection> failed = const {}}) => HomeData(
      categories: const [],
      upcomingBookings: const [],
      topProviders: const [],
      promotions: const [],
      recentlyVisitedProviders: const [],
      favoriteProviders: const [],
      failedSections: failed,
    );

void main() {
  group('HomeBloc partial failure', () {
    test('renders loaded state with failed sections marked', () async {
      final usecase = FakeGetHomeData()
        ..callResult = Right(_data(failed: {HomeSection.topProviders}));
      final bloc = HomeBloc(usecase);

      bloc.add(const LoadHomeData());
      final state = await bloc.stream.firstWhere((s) => s is HomeLoaded);

      expect(
        (state as HomeLoaded).failedSections,
        {HomeSection.topProviders},
      );
      await bloc.close();
    });

    test('retrying a failed section clears it without reloading the rest',
        () async {
      final usecase = FakeGetHomeData()
        ..callResult = Right(_data(failed: {HomeSection.topProviders}))
        ..retryResult = Right(_data());
      final bloc = HomeBloc(usecase);

      bloc.add(const LoadHomeData());
      await bloc.stream.firstWhere((s) => s is HomeLoaded);

      bloc.add(const RetryHomeSection(HomeSection.topProviders));
      final state = await bloc.stream.firstWhere(
        (s) => s is HomeLoaded && s.failedSections.isEmpty,
      );

      expect(usecase.lastRetriedSection, HomeSection.topProviders);
      expect((state as HomeLoaded).failedSections, isEmpty);
      await bloc.close();
    });

    test('total failure emits HomeError', () async {
      final usecase = FakeGetHomeData()
        ..callResult = const Left(ServerFailure('down'));
      final bloc = HomeBloc(usecase);

      bloc.add(const LoadHomeData());
      final state = await bloc.stream.firstWhere((s) => s is HomeError);

      expect((state as HomeError).message, 'down');
      await bloc.close();
    });
  });
}
