import 'dart:convert';
import 'dart:typed_data';

import 'package:booksy_provider_app/features/onboarding/data/datasources/onboarding_api_service.dart';
import 'package:booksy_provider_app/features/onboarding/data/models/onboarding_models.dart';
import 'package:booksy_provider_app/features/onboarding/domain/entities/onboarding_data.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';

/// Records the path each call goes to and answers with a fixed body.
class _RecordingAdapter implements HttpClientAdapter {
  final List<String> paths = [];
  final Map<String, dynamic> body;
  _RecordingAdapter(this.body);

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    paths.add(options.path);
    return ResponseBody.fromString(
      jsonEncode(body),
      200,
      headers: {
        Headers.contentTypeHeader: ['application/json'],
      },
    );
  }

  @override
  void close({bool force = false}) {}
}

void main() {
  /// The backend removed POST /Providers/organizations with the provider-hierarchy
  /// model (commit cb3e7310); registration goes through the draft endpoint. The app
  /// kept calling the old path and production answered 405, so onboarding could not
  /// get past the address step.
  test('creating the draft posts to the endpoint the backend still serves', () async {
    final adapter = _RecordingAdapter({
      'data': {'providerId': 'p-1', 'registrationStep': 3},
    });
    final dio = Dio(BaseOptions(baseUrl: 'https://api.test'))..httpClientAdapter = adapter;

    final id = await OnboardingApiService(dio).registerOrganization(
      const RegisterOrganizationRequest(
        OnboardingData(
          businessInfo: BusinessInfo(
            businessName: 'سالن نهال',
            ownerFirstName: 'مصطفی',
            ownerLastName: 'کاظمی',
            phone: '+989123135143',
            description: 'با بیش از ۱۰ سال سابقه',
          ),
          categoryId: 'barbershop',
          address: OnboardingAddress(
            addressLine1: 'محله طالقانی، کوچه ۵ سهند',
            city: 'پارس آباد',
            province: 'اردبیل',
            latitude: 39.64,
            longitude: 47.89,
          ),
        ),
      ),
    );

    expect(adapter.paths.single, '/v1/Providers/draft');
    expect(id, 'p-1');
  });
}
