// GENERATED CODE - DO NOT MODIFY BY HAND

// **************************************************************************
// InjectableConfigGenerator
// **************************************************************************

// ignore_for_file: type=lint
// coverage:ignore-file

// ignore_for_file: no_leading_underscores_for_library_prefixes
import 'package:dio/dio.dart' as _i361;
import 'package:flutter_secure_storage/flutter_secure_storage.dart' as _i558;
import 'package:get_it/get_it.dart' as _i174;
import 'package:injectable/injectable.dart' as _i526;

import '../../features/auth/data/datasources/auth_api_service.dart' as _i156;
import '../../features/auth/data/repositories/auth_repository_impl.dart'
    as _i153;
import '../../features/auth/domain/repositories/auth_repository.dart' as _i787;
import '../../features/auth/domain/usecases/complete_authentication_usecase.dart'
    as _i336;
import '../../features/auth/domain/usecases/send_verification_code_usecase.dart'
    as _i480;
import '../../features/auth/presentation/bloc/auth_bloc.dart' as _i797;
import '../../features/home/data/datasources/home_remote_datasource.dart'
    as _i278;
import '../../features/home/data/repositories/home_repository_impl.dart'
    as _i76;
import '../../features/home/domain/repositories/home_repository.dart' as _i0;
import '../../features/home/domain/usecases/get_home_data.dart' as _i453;
import '../../features/home/presentation/bloc/home_bloc.dart' as _i202;
import '../api/client/dio_client.dart' as _i981;
import '../api/interceptors/auth_interceptor.dart' as _i861;
import '../api/interceptors/error_interceptor.dart' as _i581;
import '../storage/secure_storage_service.dart' as _i666;

extension GetItInjectableX on _i174.GetIt {
// initializes the registration of main-scope dependencies inside of GetIt
  _i174.GetIt init({
    String? environment,
    _i526.EnvironmentFilter? environmentFilter,
  }) {
    final gh = _i526.GetItHelper(
      this,
      environment,
      environmentFilter,
    );
    final dioModule = _$DioModule();
    gh.factory<_i581.ErrorInterceptor>(() => _i581.ErrorInterceptor());
    gh.lazySingleton<_i558.FlutterSecureStorage>(
        () => dioModule.provideSecureStorage());
    gh.lazySingleton<_i361.Dio>(
      () => dioModule.provideAuthDio(),
      instanceName: 'authDio',
    );
    gh.lazySingleton<_i666.SecureStorageService>(
        () => _i666.SecureStorageService(gh<_i558.FlutterSecureStorage>()));
    gh.factory<_i861.AuthInterceptor>(() => _i861.AuthInterceptor(
          gh<_i666.SecureStorageService>(),
          gh<_i361.Dio>(instanceName: 'authDio'),
        ));
    gh.lazySingleton<_i361.Dio>(
      () => dioModule.provideUserManagementDio(
        gh<_i861.AuthInterceptor>(),
        gh<_i581.ErrorInterceptor>(),
      ),
      instanceName: 'userManagementDio',
    );
    gh.factory<_i156.AuthApiService>(
        () => _i156.AuthApiService(gh<_i361.Dio>(instanceName: 'authDio')));
    gh.lazySingleton<_i787.AuthRepository>(() => _i153.AuthRepositoryImpl(
          gh<_i156.AuthApiService>(),
          gh<_i666.SecureStorageService>(),
        ));
    gh.factory<_i336.CompleteAuthenticationUseCase>(
        () => _i336.CompleteAuthenticationUseCase(gh<_i787.AuthRepository>()));
    gh.factory<_i480.SendVerificationCodeUseCase>(
        () => _i480.SendVerificationCodeUseCase(gh<_i787.AuthRepository>()));
    gh.lazySingleton<_i361.Dio>(
      () => dioModule.provideServiceCatalogDio(
        gh<_i861.AuthInterceptor>(),
        gh<_i581.ErrorInterceptor>(),
      ),
      instanceName: 'serviceCatalogDio',
    );
    gh.lazySingleton<_i278.HomeRemoteDataSource>(() =>
        _i278.HomeRemoteDataSource(
          serviceCatalogDio: gh<_i361.Dio>(instanceName: 'serviceCatalogDio'),
          userManagementDio: gh<_i361.Dio>(instanceName: 'userManagementDio'),
        ));
    gh.factory<_i797.AuthBloc>(() => _i797.AuthBloc(
          gh<_i480.SendVerificationCodeUseCase>(),
          gh<_i336.CompleteAuthenticationUseCase>(),
          gh<_i787.AuthRepository>(),
        ));
    gh.lazySingleton<_i0.HomeRepository>(
        () => _i76.HomeRepositoryImpl(gh<_i278.HomeRemoteDataSource>()));
    gh.lazySingleton<_i453.GetHomeData>(() => _i453.GetHomeData(
          gh<_i0.HomeRepository>(),
          gh<_i666.SecureStorageService>(),
        ));
    gh.factory<_i202.HomeBloc>(() => _i202.HomeBloc(gh<_i453.GetHomeData>()));
    return this;
  }
}

class _$DioModule extends _i981.DioModule {}
