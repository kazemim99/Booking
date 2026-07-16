import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:get_it/get_it.dart';

import '../api/client/dio_client.dart';
import '../api/interceptors/auth_interceptor.dart';
import '../network/connectivity_service.dart';
import '../storage/secure_storage_service.dart';
import '../../features/auth/data/datasources/auth_api_service.dart';
import '../../features/auth/data/repositories/auth_repository_impl.dart';
import '../../features/auth/domain/repositories/auth_repository.dart';
import '../../features/auth/domain/usecases/complete_provider_authentication_usecase.dart';
import '../../features/auth/domain/usecases/send_verification_code_usecase.dart';
import '../../features/auth/presentation/bloc/auth_bloc.dart';
import '../../features/home/data/datasources/home_api_service.dart';
import '../../features/home/data/repositories/home_repository_impl.dart';
import '../../features/home/domain/repositories/home_repository.dart';
import '../../features/home/presentation/cubit/calendar_cubit.dart';
import '../../features/home/presentation/cubit/clients_cubit.dart';
import '../../features/home/presentation/cubit/composer_cubit.dart';
import '../../features/home/presentation/cubit/home_cubit.dart';
import '../../features/onboarding/data/datasources/geocoding_service.dart';
import '../../features/onboarding/data/datasources/location_api_service.dart';
import '../../features/onboarding/data/datasources/onboarding_api_service.dart';
import '../../features/onboarding/data/repositories/onboarding_repository_impl.dart';
import '../../features/onboarding/domain/repositories/onboarding_repository.dart';
import '../../features/onboarding/presentation/cubit/onboarding_cubit.dart';

final getIt = GetIt.instance;

/// Manual dependency wiring (no injectable codegen — see CLAUDE.md).
Future<void> configureDependencies() async {
  // ---- Infrastructure ----
  const secureStorage = FlutterSecureStorage(
    aOptions: AndroidOptions(encryptedSharedPreferences: true),
  );
  getIt.registerLazySingleton<SecureStorageService>(
    () => SecureStorageService(secureStorage),
  );
  getIt.registerLazySingleton<ConnectivityService>(
    () => ConnectivityService(Connectivity()),
  );

  // ---- HTTP ----
  // Unauthenticated Dio (auth endpoints + refresh).
  final authDio = DioFactory.createAuthDio();
  getIt.registerLazySingleton<Dio>(() => authDio, instanceName: 'authDio');

  // Authenticated Dio, sharing a single AuthInterceptor with single-flight
  // refresh (uses authDio for the refresh call).
  final authInterceptor = AuthInterceptor(
    getIt<SecureStorageService>(),
    authDio,
  );
  final authedDio = DioFactory.createAuthenticatedDio([authInterceptor]);
  getIt.registerLazySingleton<Dio>(
    () => authedDio,
    instanceName: 'authedDio',
  );

  // ---- Auth feature ----
  getIt.registerLazySingleton<AuthApiService>(
    () => AuthApiService(authDio, authedDio),
  );
  getIt.registerLazySingleton<AuthRepository>(
    () => AuthRepositoryImpl(getIt<AuthApiService>(), getIt<SecureStorageService>()),
  );
  getIt.registerLazySingleton<SendVerificationCodeUseCase>(
    () => SendVerificationCodeUseCase(getIt<AuthRepository>()),
  );
  getIt.registerLazySingleton<CompleteProviderAuthenticationUseCase>(
    () => CompleteProviderAuthenticationUseCase(getIt<AuthRepository>()),
  );

  // AuthBloc is a singleton: the router listens to it for session state.
  getIt.registerLazySingleton<AuthBloc>(
    () => AuthBloc(
      getIt<SendVerificationCodeUseCase>(),
      getIt<CompleteProviderAuthenticationUseCase>(),
      getIt<AuthRepository>(),
    ),
  );

  // ---- Onboarding feature ----
  getIt.registerLazySingleton<OnboardingApiService>(
    () => OnboardingApiService(authedDio),
  );
  getIt.registerLazySingleton<OnboardingRepository>(
    () => OnboardingRepositoryImpl(getIt<OnboardingApiService>()),
  );
  // Factory: the wizard owns a fresh cubit per entry.
  getIt.registerFactory<OnboardingCubit>(
    () => OnboardingCubit(getIt<OnboardingRepository>()),
  );

  // ---- Home (Today workspace) ----
  getIt.registerLazySingleton<HomeApiService>(
    () => HomeApiService(authedDio),
  );
  getIt.registerLazySingleton<HomeRepository>(
    () => HomeRepositoryImpl(getIt<HomeApiService>(), getIt<AuthRepository>()),
  );
  // Factory: the Home page owns a fresh cubit per entry. Polling (MVP refresh
  // strategy — resolved decision #2) while the Home is foregrounded.
  getIt.registerFactory<HomeCubit>(
    () => HomeCubit(
      getIt<HomeRepository>(),
      getIt<ConnectivityService>(),
      pollInterval: const Duration(seconds: 60),
    ),
  );
  // Factory: the booking composer owns a fresh cubit per entry; param1 is the
  // optional pre-set date (calendar-initiated creation).
  getIt.registerFactoryParam<ComposerCubit, DateTime?, void>(
    (initialDate, _) =>
        ComposerCubit(getIt<HomeRepository>(), initialDate: initialDate),
  );
  // Factory: the calendar owns a fresh cubit per entry.
  getIt.registerFactory<CalendarCubit>(
    () => CalendarCubit(getIt<HomeRepository>(), getIt<ConnectivityService>()),
  );
  // Factory: the clients tab owns a fresh cubit per entry.
  getIt.registerFactory<ClientsCubit>(
    () => ClientsCubit(getIt<HomeRepository>()),
  );

  // ---- Location (onboarding step 3) ----
  // City hierarchy comes from the (anonymous) ServiceCatalog endpoint; reuse the
  // authenticated Dio so we share timeouts/logging.
  getIt.registerLazySingleton<LocationApiService>(
    () => LocationApiService(authedDio),
  );
  // Geocoding (OSM/Nominatim) uses a plain Dio — no auth header, no app baseUrl.
  getIt.registerLazySingleton<GeocodingService>(
    () => GeocodingService(Dio()),
  );
}
