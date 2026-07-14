import 'package:dio/dio.dart';
import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../core/constants/app_strings.dart';
import '../../../../core/storage/secure_storage_service.dart';
import '../../data/datasources/profile_remote_datasource.dart';

enum ProfileEditStatus { idle, saving, success, failure }

class ProfileState extends Equatable {
  final String? firstName;
  final String? lastName;
  final ProfileEditStatus editStatus;
  final String? errorMessage;

  const ProfileState({
    this.firstName,
    this.lastName,
    this.editStatus = ProfileEditStatus.idle,
    this.errorMessage,
  });

  ProfileState copyWith({
    String? firstName,
    String? lastName,
    ProfileEditStatus? editStatus,
    String? errorMessage,
  }) {
    return ProfileState(
      firstName: firstName ?? this.firstName,
      lastName: lastName ?? this.lastName,
      editStatus: editStatus ?? this.editStatus,
      errorMessage: errorMessage,
    );
  }

  @override
  List<Object?> get props => [firstName, lastName, editStatus, errorMessage];
}

class ProfileCubit extends Cubit<ProfileState> {
  final ProfileRemoteDataSource remoteDataSource;
  final SecureStorageService storageService;

  ProfileCubit({
    required this.remoteDataSource,
    required this.storageService,
    String? initialFirstName,
    String? initialLastName,
  }) : super(ProfileState(
          firstName: initialFirstName,
          lastName: initialLastName,
        ));

  Future<void> saveProfile({
    required String firstName,
    required String lastName,
  }) async {
    emit(state.copyWith(editStatus: ProfileEditStatus.saving));
    try {
      final customerId = await storageService.getCustomerId();
      if (customerId == null) {
        emit(state.copyWith(
          editStatus: ProfileEditStatus.failure,
          errorMessage: AppStrings.genericError,
        ));
        return;
      }
      await remoteDataSource.updateProfile(
        customerId: customerId,
        firstName: firstName,
        lastName: lastName,
      );
      emit(state.copyWith(
        firstName: firstName,
        lastName: lastName,
        editStatus: ProfileEditStatus.success,
      ));
    } on DioException catch (e) {
      emit(state.copyWith(
        editStatus: ProfileEditStatus.failure,
        errorMessage: e.message ?? AppStrings.genericError,
      ));
    } catch (_) {
      emit(state.copyWith(
        editStatus: ProfileEditStatus.failure,
        errorMessage: AppStrings.genericError,
      ));
    }
  }
}
