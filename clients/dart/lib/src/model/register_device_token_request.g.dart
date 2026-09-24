// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'register_device_token_request.dart';

// **************************************************************************
// CopyWithGenerator
// **************************************************************************

abstract class _$RegisterDeviceTokenRequestCWProxy {
  RegisterDeviceTokenRequest token(String token);

  RegisterDeviceTokenRequest platform(DevicePlatform platform);

  RegisterDeviceTokenRequest appVersion(String? appVersion);

  /// Creates a new instance with the provided field values.
  /// Passing `null` to a nullable field nullifies it, while `null` for a non-nullable field is ignored. To update a single field use `RegisterDeviceTokenRequest(...).copyWith.fieldName(value)`.
  ///
  /// Example:
  /// ```dart
  /// RegisterDeviceTokenRequest(...).copyWith(id: 12, name: "My name")
  /// ```
  RegisterDeviceTokenRequest call({
    String token,
    DevicePlatform platform,
    String? appVersion,
  });
}

/// Callable proxy for `copyWith` functionality.
/// Use as `instanceOfRegisterDeviceTokenRequest.copyWith(...)` or call `instanceOfRegisterDeviceTokenRequest.copyWith.fieldName(value)` for a single field.
class _$RegisterDeviceTokenRequestCWProxyImpl
    implements _$RegisterDeviceTokenRequestCWProxy {
  const _$RegisterDeviceTokenRequestCWProxyImpl(this._value);

  final RegisterDeviceTokenRequest _value;

  @override
  RegisterDeviceTokenRequest token(String token) => call(token: token);

  @override
  RegisterDeviceTokenRequest platform(DevicePlatform platform) =>
      call(platform: platform);

  @override
  RegisterDeviceTokenRequest appVersion(String? appVersion) =>
      call(appVersion: appVersion);

  /// Creates a new instance with the provided field values.
  /// Passing `null` to a nullable field nullifies it, while `null` for a non-nullable field is ignored. To update a single field use `RegisterDeviceTokenRequest(...).copyWith.fieldName(value)`.
  ///
  /// Example:
  /// ```dart
  /// RegisterDeviceTokenRequest(...).copyWith(id: 12, name: "My name")
  /// ```
  @override
  RegisterDeviceTokenRequest call({
    Object? token = const $CopyWithPlaceholder(),
    Object? platform = const $CopyWithPlaceholder(),
    Object? appVersion = const $CopyWithPlaceholder(),
  }) {
    return RegisterDeviceTokenRequest(
      token: token == const $CopyWithPlaceholder() || token == null
          ? _value.token
          // ignore: cast_nullable_to_non_nullable
          : token as String,
      platform: platform == const $CopyWithPlaceholder() || platform == null
          ? _value.platform
          // ignore: cast_nullable_to_non_nullable
          : platform as DevicePlatform,
      appVersion: appVersion == const $CopyWithPlaceholder()
          ? _value.appVersion
          // ignore: cast_nullable_to_non_nullable
          : appVersion as String?,
    );
  }
}

extension $RegisterDeviceTokenRequestCopyWith on RegisterDeviceTokenRequest {
  /// Returns a callable class used to build a new instance with modified fields.
  /// Example: `instanceOfRegisterDeviceTokenRequest.copyWith(...)` or `instanceOfRegisterDeviceTokenRequest.copyWith.fieldName(...)`.
  // ignore: library_private_types_in_public_api
  _$RegisterDeviceTokenRequestCWProxy get copyWith =>
      _$RegisterDeviceTokenRequestCWProxyImpl(this);
}

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

RegisterDeviceTokenRequest _$RegisterDeviceTokenRequestFromJson(
  Map<String, dynamic> json,
) => $checkedCreate('RegisterDeviceTokenRequest', json, ($checkedConvert) {
  $checkKeys(json, requiredKeys: const ['token', 'platform']);
  final val = RegisterDeviceTokenRequest(
    token: $checkedConvert('token', (v) => v as String),
    platform: $checkedConvert(
      'platform',
      (v) => $enumDecode(_$DevicePlatformEnumMap, v),
    ),
    appVersion: $checkedConvert('appVersion', (v) => v as String?),
  );
  return val;
});

Map<String, dynamic> _$RegisterDeviceTokenRequestToJson(
  RegisterDeviceTokenRequest instance,
) => <String, dynamic>{
  'token': instance.token,
  'platform': _$DevicePlatformEnumMap[instance.platform]!,
  'appVersion': ?instance.appVersion,
};

const _$DevicePlatformEnumMap = {
  DevicePlatform.ios: 'ios',
  DevicePlatform.android: 'android',
  DevicePlatform.web: 'web',
};
