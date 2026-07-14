import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:dio/dio.dart';
import 'package:get_it/get_it.dart';
import 'package:injectable/injectable.dart';
import '../../features/booking/data/datasources/booking_remote_datasource.dart';
import '../../features/bookings/data/datasources/bookings_remote_datasource.dart';
import '../../features/bookings/data/repositories/bookings_repository_impl.dart';
import '../../features/bookings/domain/repositories/bookings_repository.dart';
import '../../features/bookings/presentation/bloc/appointments_bloc.dart';
import '../../features/booking/data/repositories/booking_repository_impl.dart';
import '../../features/booking/domain/repositories/booking_repository.dart';
import '../../features/booking/presentation/bloc/booking_bloc.dart';
import '../../features/profile/data/datasources/profile_remote_datasource.dart';
import '../../features/search/data/datasources/search_remote_datasource.dart';
import '../../features/search/data/repositories/search_repository_impl.dart';
import '../../features/search/domain/repositories/search_repository.dart';
import '../../features/search/presentation/bloc/provider_detail_cubit.dart';
import '../../features/search/presentation/bloc/search_bloc.dart';
import '../network/connectivity_service.dart';
import 'injection.config.dart';

final getIt = GetIt.instance;

@InjectableInit(
  initializerName: 'init',
  preferRelativeImports: true,
  asExtension: true,
)
Future<void> configureDependencies() async {
  getIt.init();

  // Registered manually: build_runner codegen is currently broken by a
  // retrofit_generator/SDK incompatibility, so these can't use @injectable.
  getIt.registerLazySingleton<ConnectivityService>(
    () => ConnectivityService(Connectivity()),
  );
  getIt.registerLazySingleton<SearchRemoteDataSource>(
    () => SearchRemoteDataSource(
      serviceCatalogDio: getIt<Dio>(instanceName: 'serviceCatalogDio'),
    ),
  );
  getIt.registerLazySingleton<SearchRepository>(
    () => SearchRepositoryImpl(remoteDataSource: getIt()),
  );
  getIt.registerFactory<SearchBloc>(() => SearchBloc(getIt()));
  getIt.registerLazySingleton<BookingRemoteDataSource>(
    () => BookingRemoteDataSource(
      serviceCatalogDio: getIt<Dio>(instanceName: 'serviceCatalogDio'),
    ),
  );
  getIt.registerLazySingleton<BookingRepository>(
    () => BookingRepositoryImpl(remoteDataSource: getIt()),
  );
  // Singleton on purpose: booking selections must survive the login
  // round-trip at the confirmation gate (see BookingStarted).
  getIt.registerLazySingleton<BookingBloc>(() => BookingBloc(getIt()));
  getIt.registerFactory<ProviderDetailCubit>(
    () => ProviderDetailCubit(getIt()),
  );
  getIt.registerLazySingleton<BookingsRemoteDataSource>(
    () => BookingsRemoteDataSource(
      serviceCatalogDio: getIt<Dio>(instanceName: 'serviceCatalogDio'),
    ),
  );
  getIt.registerLazySingleton<BookingsRepository>(
    () => BookingsRepositoryImpl(
      remoteDataSource: getIt(),
      storageService: getIt(),
    ),
  );
  getIt.registerFactory<AppointmentsBloc>(() => AppointmentsBloc(getIt()));
  getIt.registerLazySingleton<ProfileRemoteDataSource>(
    () => ProfileRemoteDataSource(
      userManagementDio: getIt<Dio>(instanceName: 'userManagementDio'),
    ),
  );
}
