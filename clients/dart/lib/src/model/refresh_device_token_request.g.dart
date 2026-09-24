// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'refresh_device_token_request.dart';

// **************************************************************************
// CopyWithGenerator
// **************************************************************************

abstract class _$RefreshDeviceTokenRequestCWProxy {
  RefreshDeviceTokenRequest token(String token);

  /// Creates a new instance with the provided field values.
  /// Passing `null` to a nullable field nullifies it, while `null` for a non-nullable field is ignored. To update a single field use `RefreshDeviceTokenRequest(...).copyWith.fieldName(value)`.
  ///
  /// Example:
  /// ```dart
  /// RefreshDeviceTokenRequest(...).copyWith(id: 12, name: "My name")
  /// ```
  RefreshDeviceTokenRequest call({String token});
}

/// Callable proxy for `copyWith` functionality.
/// Use as `instanceOfRefreshDeviceTokenRequest.copyWith(...)` or call `instanceOfRefreshDeviceTokenRequest.copyWith.fieldName(value)` for a single field.
class _$RefreshDeviceTokenRequestCWProxyImpl
    implements _$RefreshDeviceTokenRequestCWProxy {
  const _$RefreshDeviceTokenRequestCWProxyImpl(this._value);

  final RefreshDeviceTokenRequest _value;

  @override
  RefreshDeviceTokenRequest token(String token) => call(token: token);

  /// Creates a new instance with the provided field values.
  /// Passing `null` to a nullable field nullifies it, while `null` for a non-nullable field is ignored. To update a single field use `RefreshDeviceTokenRequest(...).copyWith.fieldName(value)`.
  ///
  /// Example:
  /// ```dart
  /// RefreshDeviceTokenRequest(...).copyWith(id: 12, name: "My name")
  /// ```
  @override
  RefreshDeviceTokenRequest call({
    Object? token = const $CopyWithPlaceholder(),
  }) {
    return RefreshDeviceTokenRequest(
      token: token == const $CopyWithPlaceholder() || token == null
          ? _value.token
          // ignore: cast_nullable_to_non_nullable
          : token as String,
    );
  }
}

extension $RefreshDeviceTokenRequestCopyWith on RefreshDeviceTokenRequest {
  /// Returns a callable class used to build a new instance with modified fields.
  /// Example: `instanceOfRefreshDeviceTokenRequest.copyWith(...)` or `instanceOfRefreshDeviceTokenRequest.copyWith.fieldName(...)`.
  // ignore: library_private_types_in_public_api
  _$RefreshDeviceTokenRequestCWProxy get copyWith =>
      _$RefreshDeviceTokenRequestCWProxyImpl(this);
}

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

RefreshDeviceTokenRequest _$RefreshDeviceTokenRequestFromJson(
  Map<String, dynamic> json,
) => $checkedCreate('RefreshDeviceTokenRequest', json, ($checkedConvert) {
  $checkKeys(json, requiredKeys: const ['token']);
  final val = RefreshDeviceTokenRequest(
    token: $checkedConvert('token', (v) => v as String),
  );
  return val;
});

Map<String, dynamic> _$RefreshDeviceTokenRequestToJson(
  RefreshDeviceTokenRequest instance,
) => <String, dynamic>{'token': instance.token};
