import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:dio/dio.dart';
import 'package:get_it/get_it.dart';
import 'package:injectable/injectable.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:url_launcher/url_launcher.dart';
import '../../features/checkout/data/datasources/checkout_attempt_store.dart';
import '../../features/checkout/data/datasources/checkout_remote_datasource.dart';
import '../../features/checkout/data/repositories/checkout_repository_impl.dart';
import '../../features/checkout/domain/repositories/checkout_repository.dart';
import '../../features/checkout/presentation/bloc/checkout_bloc.dart';
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
import '../../features/search/presentation/bloc/area_search_cubit.dart';
import '../../features/search/presentation/bloc/nearby_providers_cubit.dart';
import '../../features/search/presentation/bloc/provider_detail_cubit.dart';
import '../../features/search/presentation/bloc/search_bloc.dart';
import '../location/geocoding_service.dart';
import '../location/location_service.dart';
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

  // Awaited once here so consumers can resolve it synchronously (the checkout attempt store needs it).
  getIt.registerSingleton<SharedPreferences>(await SharedPreferences.getInstance());
  getIt.registerLazySingleton<SearchRemoteDataSource>(
    () => SearchRemoteDataSource(
      serviceCatalogDio: getIt<Dio>(instanceName: 'serviceCatalogDio'),
    ),
  );
  getIt.registerLazySingleton<SearchRepository>(
    () => SearchRepositoryImpl(remoteDataSource: getIt()),
  );
  getIt.registerLazySingleton<LocationService>(
    () => const GeolocatorLocationService(),
  );
  getIt.registerLazySingleton<GeocodingService>(
    () => NominatimGeocodingService(),
  );
  getIt.registerFactory<SearchBloc>(() => SearchBloc(getIt()));
  getIt.registerFactory<NearbyProvidersCubit>(
    () => NearbyProvidersCubit(
      locationService: getIt(),
      repository: getIt(),
    ),
  );
  getIt.registerFactory<AreaSearchCubit>(
    () => AreaSearchCubit(
      geocodingService: getIt(),
      repository: getIt(),
    ),
  );
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

  // ---- Checkout (deposit payment via the external-browser gateway flow) ----
  getIt.registerLazySingleton<CheckoutRemoteDataSource>(
    () => CheckoutRemoteDataSource(
      serviceCatalogDio: getIt<Dio>(instanceName: 'serviceCatalogDio'),
    ),
  );
  getIt.registerLazySingleton<CheckoutRepository>(
    () => CheckoutRepositoryImpl(remoteDataSource: getIt()),
  );
  // The attempt store persists the in-flight idempotency key + authority so an interrupted checkout resumes
  // instead of starting a second charge.
  getIt.registerLazySingleton<CheckoutAttemptStore>(
    () => SharedPrefsCheckoutAttemptStore(prefs: getIt<SharedPreferences>()),
  );
  // Factory, not singleton: each checkout screen drives one booking's attempt and is disposed with the page.
  getIt.registerFactory<CheckoutBloc>(
    () => CheckoutBloc(
      repository: getIt(),
      attemptStore: getIt(),
      launchPaymentUrl: (url) => launchUrl(
        Uri.parse(url),
        // A bank page must render in the real browser, with its address bar and TLS indicator visible — never
        // inside an in-app web view.
        mode: LaunchMode.externalApplication,
      ),
    ),
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
