import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:injectable/injectable.dart';
import 'package:pretty_dio_logger/pretty_dio_logger.dart';
import '../config/api_constants.dart';
import '../interceptors/auth_interceptor.dart';
import '../interceptors/error_interceptor.dart';

/// Dio Client Factory
/// Provides configured Dio instances for different microservices
@module
abstract class DioModule {
  /// Main Dio instance (without auth interceptor)
  /// Used for auth endpoints to avoid circular dependency
  @Named('authDio')
  @lazySingleton
  Dio provideAuthDio() {
    final dio = Dio(
      BaseOptions(
        baseUrl: ApiConstants.userManagementBaseUrl,
        connectTimeout: ApiConstants.connectTimeout,
        receiveTimeout: ApiConstants.receiveTimeout,
        sendTimeout: ApiConstants.sendTimeout,
        headers: {
          'Accept': 'application/json',
          'Content-Type': 'application/json',
          'Accept-Language': 'fa-IR',
        },
      ),
    );

    // Add logging in debug mode
    dio.interceptors.add(
      PrettyDioLogger(
        requestHeader: true,
        requestBody: true,
        responseBody: true,
        responseHeader: false,
        error: true,
        compact: true,
      ),
    );

    return dio;
  }

  /// User Management API Dio instance
  @Named('userManagementDio')
  @lazySingleton
  Dio provideUserManagementDio(
    AuthInterceptor authInterceptor,
    ErrorInterceptor errorInterceptor,
  ) {
    final dio = Dio(
      BaseOptions(
        baseUrl: ApiConstants.userManagementBaseUrl,
        connectTimeout: ApiConstants.connectTimeout,
        receiveTimeout: ApiConstants.receiveTimeout,
        sendTimeout: ApiConstants.sendTimeout,
        headers: {
          'Accept': 'application/json',
          'Content-Type': 'application/json',
          'Accept-Language': 'fa-IR',
        },
      ),
    );

    // Add interceptors in order
    dio.interceptors.addAll([
      authInterceptor,
      errorInterceptor,
      PrettyDioLogger(
        requestHeader: true,
        requestBody: true,
        responseBody: true,
        responseHeader: false,
        error: true,
        compact: true,
      ),
    ]);

    return dio;
  }

  /// Service Catalog API Dio instance
  @Named('serviceCatalogDio')
  @lazySingleton
  Dio provideServiceCatalogDio(
    AuthInterceptor authInterceptor,
    ErrorInterceptor errorInterceptor,
  ) {
    final dio = Dio(
      BaseOptions(
        baseUrl: ApiConstants.serviceCatalogBaseUrl,
        connectTimeout: ApiConstants.connectTimeout,
        receiveTimeout: ApiConstants.receiveTimeout,
        sendTimeout: ApiConstants.sendTimeout,
        headers: {
          'Accept': 'application/json',
          'Content-Type': 'application/json',
          'Accept-Language': 'fa-IR',
        },
      ),
    );

    // Add interceptors in order
    dio.interceptors.addAll([
      authInterceptor,
      errorInterceptor,
      PrettyDioLogger(
        requestHeader: true,
        requestBody: true,
        responseBody: true,
        responseHeader: false,
        error: true,
        compact: true,
      ),
    ]);

    return dio;
  }

  /// Flutter Secure Storage
  @lazySingleton
  FlutterSecureStorage provideSecureStorage() {
    return const FlutterSecureStorage(
      aOptions: AndroidOptions(
        encryptedSharedPreferences: true,
      ),
    );
  }
}
