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
}
