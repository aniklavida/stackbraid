// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'update_user_request.dart';

// **************************************************************************
// CopyWithGenerator
// **************************************************************************

abstract class _$UpdateUserRequestCWProxy {
  UpdateUserRequest displayName(String? displayName);

  UpdateUserRequest email(String? email);

  /// Creates a new instance with the provided field values.
  /// Passing `null` to a nullable field nullifies it, while `null` for a non-nullable field is ignored. To update a single field use `UpdateUserRequest(...).copyWith.fieldName(value)`.
  ///
  /// Example:
  /// ```dart
  /// UpdateUserRequest(...).copyWith(id: 12, name: "My name")
  /// ```
  UpdateUserRequest call({String? displayName, String? email});
}

/// Callable proxy for `copyWith` functionality.
/// Use as `instanceOfUpdateUserRequest.copyWith(...)` or call `instanceOfUpdateUserRequest.copyWith.fieldName(value)` for a single field.
class _$UpdateUserRequestCWProxyImpl implements _$UpdateUserRequestCWProxy {
  const _$UpdateUserRequestCWProxyImpl(this._value);

  final UpdateUserRequest _value;

  @override
  UpdateUserRequest displayName(String? displayName) =>
      call(displayName: displayName);

  @override
  UpdateUserRequest email(String? email) => call(email: email);

  /// Creates a new instance with the provided field values.
  /// Passing `null` to a nullable field nullifies it, while `null` for a non-nullable field is ignored. To update a single field use `UpdateUserRequest(...).copyWith.fieldName(value)`.
  ///
  /// Example:
  /// ```dart
  /// UpdateUserRequest(...).copyWith(id: 12, name: "My name")
  /// ```
  @override
  UpdateUserRequest call({
    Object? displayName = const $CopyWithPlaceholder(),
    Object? email = const $CopyWithPlaceholder(),
  }) {
    return UpdateUserRequest(
      displayName: displayName == const $CopyWithPlaceholder()
          ? _value.displayName
          // ignore: cast_nullable_to_non_nullable
          : displayName as String?,
      email: email == const $CopyWithPlaceholder()
          ? _value.email
          // ignore: cast_nullable_to_non_nullable
          : email as String?,
    );
  }
}

extension $UpdateUserRequestCopyWith on UpdateUserRequest {
  /// Returns a callable class used to build a new instance with modified fields.
  /// Example: `instanceOfUpdateUserRequest.copyWith(...)` or `instanceOfUpdateUserRequest.copyWith.fieldName(...)`.
  // ignore: library_private_types_in_public_api
  _$UpdateUserRequestCWProxy get copyWith =>
      _$UpdateUserRequestCWProxyImpl(this);
}

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

UpdateUserRequest _$UpdateUserRequestFromJson(Map<String, dynamic> json) =>
    $checkedCreate('UpdateUserRequest', json, ($checkedConvert) {
      final val = UpdateUserRequest(
        displayName: $checkedConvert('displayName', (v) => v as String?),
        email: $checkedConvert('email', (v) => v as String?),
      );
      return val;
    });

Map<String, dynamic> _$UpdateUserRequestToJson(UpdateUserRequest instance) =>
    <String, dynamic>{
      'displayName': ?instance.displayName,
      'email': ?instance.email,
    };
