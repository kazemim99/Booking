import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:get_it/get_it.dart';

import '../api/client/dio_client.dart';
import '../push/dio_device_token_api.dart';
import '../push/firebase_push_token_source.dart';
import '../push/push_registration.dart';
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
import '../../features/home/presentation/cubit/more_cubits.dart';
import '../../features/home/presentation/cubit/home_cubit.dart';
import '../../features/onboarding/data/datasources/device_location_service.dart';
import '../../features/onboarding/data/datasources/geocoding_service.dart';
import '../../features/onboarding/data/datasources/location_api_service.dart';
import '../../features/onboarding/data/datasources/onboarding_api_service.dart';
import '../../features/onboarding/data/repositories/onboarding_repository_impl.dart';
import '../../features/invitations/data/invitation_api_service.dart';
import '../../features/invitations/data/invitation_repository_impl.dart';
import '../../features/invitations/domain/invitation_repository.dart';
import '../../features/invitations/presentation/accept_invitation_cubit.dart';
import '../../features/invitations/presentation/register_and_accept_cubit.dart';
import '../../features/onboarding/domain/repositories/onboarding_repository.dart';
import '../../features/onboarding/presentation/cubit/onboarding_cubit.dart';
import '../../features/notifications/data/inbox_repository_impl.dart';
import '../../features/notifications/data/notification_api_service.dart';
import '../../features/notifications/domain/inbox_repository.dart';
import '../../features/notifications/presentation/inbox_cubit.dart';
import '../../features/notifications/presentation/push_permission_cubit.dart';
import '../../features/reviews/data/reviews_api_service.dart';
import '../../features/reviews/data/reviews_repository_impl.dart';
import '../../features/reviews/domain/reviews_repository.dart';
import '../../features/reviews/presentation/reviews_cubit.dart';

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
  getIt.registerLazySingleton<Dio>(() => authedDio, instanceName: 'authedDio');

  // ---- Auth feature ----
  getIt.registerLazySingleton<AuthApiService>(
    () => AuthApiService(authDio, authedDio),
  );
  getIt.registerLazySingleton<AuthRepository>(
    () => AuthRepositoryImpl(
      getIt<AuthApiService>(),
      getIt<SecureStorageService>(),
    ),
  );
  getIt.registerLazySingleton<SendVerificationCodeUseCase>(
    () => SendVerificationCodeUseCase(getIt<AuthRepository>()),
  );
  getIt.registerLazySingleton<CompleteProviderAuthenticationUseCase>(
    () => CompleteProviderAuthenticationUseCase(getIt<AuthRepository>()),
  );

  // ---- Push (device registration) ----
  // The Firebase source degrades to "unavailable" on a build without Firebase configuration, so wiring push
  // never makes the app depend on a file that is not in the repository.
  getIt.registerLazySingleton<FirebasePushTokenSource>(() => FirebasePushTokenSource());
  getIt.registerLazySingleton<PushRegistration>(
    () => PushRegistration(getIt<FirebasePushTokenSource>(), DioDeviceTokenApi(authedDio)),
  );
  // One shared instance, so turning notifications on from the More row hides the Home card and vice versa.
  getIt.registerLazySingleton<PushPermissionCubit>(
    () => PushPermissionCubit(getIt<PushRegistration>(), SecureStoragePushPromptMemory(secureStorage)),
  );

  // AuthBloc is a singleton: the router listens to it for session state.
  getIt.registerLazySingleton<AuthBloc>(
    () => AuthBloc(
      getIt<SendVerificationCodeUseCase>(),
      getIt<CompleteProviderAuthenticationUseCase>(),
      getIt<AuthRepository>(),
      push: getIt<PushRegistration>(),
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

  // ---- Invitations (accept from SMS link) ----
  getIt.registerLazySingleton<InvitationApiService>(
    () => InvitationApiService(authedDio),
  );
  getIt.registerLazySingleton<InvitationRepository>(
    () => InvitationRepositoryImpl(getIt<InvitationApiService>()),
  );
  // Factory: one cubit per accept screen, keyed on the invitation id.
  getIt.registerFactoryParam<AcceptInvitationCubit, String, void>(
    (invitationId, _) =>
        AcceptInvitationCubit(getIt<InvitationRepository>(), invitationId),
  );
  // Factory: one cubit per register-and-accept screen (new invitee, no account).
  getIt.registerFactoryParam<RegisterAndAcceptCubit, String, void>(
    (invitationId, _) =>
        RegisterAndAcceptCubit(getIt<InvitationRepository>(), invitationId),
  );

  // ---- Notifications inbox ----
  getIt.registerLazySingleton<NotificationApiService>(() => NotificationApiService(authedDio));
  getIt.registerLazySingleton<InboxRepository>(
    () => InboxRepositoryImpl(getIt<NotificationApiService>()),
  );
  // A SINGLETON, unlike the screen cubits above: the bell and the inbox page must read the same state, or the
  // badge and the list it opens can disagree.
  getIt.registerLazySingleton<InboxCubit>(() => InboxCubit(getIt<InboxRepository>()));

  // ---- Reviews (provider-reviews-and-ratings) ----
  getIt.registerLazySingleton<ReviewsApiService>(() => ReviewsApiService(authedDio));
  getIt.registerLazySingleton<ReviewsRepository>(
    () => ReviewsRepositoryImpl(getIt<ReviewsApiService>(), getIt<AuthRepository>()),
  );
  // Factory: the Home card and the reviews page each own one; the page's replies are re-read on return.
  getIt.registerFactory<ReviewsCubit>(() => ReviewsCubit(getIt<ReviewsRepository>()));

  // ---- Home (Today workspace) ----
  getIt.registerLazySingleton<HomeApiService>(() => HomeApiService(authedDio));
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
  // Factories: the More tab's read surfaces (insights/services/staff).
  getIt.registerFactory<InsightsCubit>(
    () => InsightsCubit(getIt<HomeRepository>()),
  );
  getIt.registerFactory<ServicesCubit>(
    () => ServicesCubit(getIt<HomeRepository>()),
  );
  getIt.registerFactory<StaffCubit>(() => StaffCubit(getIt<HomeRepository>()));
  getIt.registerFactory<PendingInvitationsCubit>(
    () => PendingInvitationsCubit(getIt<HomeRepository>()),
  );
  getIt.registerFactory<MembershipsCubit>(
    () => MembershipsCubit(getIt<HomeRepository>(), getIt<AuthRepository>()),
  );
  getIt.registerFactory<BusinessProfileCubit>(
    () => BusinessProfileCubit(getIt<HomeRepository>()),
  );
  getIt.registerFactory<BusinessHoursCubit>(
    () => BusinessHoursCubit(getIt<HomeRepository>()),
  );
  getIt.registerFactory<HolidaysCubit>(
    () => HolidaysCubit(getIt<HomeRepository>()),
  );
  getIt.registerFactory<GalleryCubit>(
    () => GalleryCubit(getIt<HomeRepository>()),
  );
  getIt.registerFactory<ExceptionsCubit>(
    () => ExceptionsCubit(getIt<HomeRepository>()),
  );

  // ---- Location (onboarding step 3) ----
  // City hierarchy comes from the (anonymous) ServiceCatalog endpoint; reuse the
  // authenticated Dio so we share timeouts/logging.
  getIt.registerLazySingleton<LocationApiService>(
    () => LocationApiService(authedDio),
  );
  // Geocoding goes through our own API (the server calls OpenStreetMap), so it
  // needs the app base URL. The endpoints are anonymous — onboarding picks a
  // location before a provider exists — so the unauthenticated client is right.
  getIt.registerLazySingleton<GeocodingService>(
    () => GeocodingService(authDio),
  );
  getIt.registerLazySingleton<DeviceLocationService>(
    () => DeviceLocationService(),
  );
}
