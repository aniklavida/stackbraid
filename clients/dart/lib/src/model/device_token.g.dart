// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'device_token.dart';

// **************************************************************************
// CopyWithGenerator
// **************************************************************************

abstract class _$DeviceTokenCWProxy {
  DeviceToken id(String id);

  DeviceToken platform(DevicePlatform platform);

  DeviceToken appVersion(String appVersion);

  DeviceToken registeredAt(DateTime registeredAt);

  DeviceToken updatedAt(DateTime updatedAt);

  /// Creates a new instance with the provided field values.
  /// Passing `null` to a nullable field nullifies it, while `null` for a non-nullable field is ignored. To update a single field use `DeviceToken(...).copyWith.fieldName(value)`.
  ///
  /// Example:
  /// ```dart
  /// DeviceToken(...).copyWith(id: 12, name: "My name")
  /// ```
  DeviceToken call({
    String id,
    DevicePlatform platform,
    String appVersion,
    DateTime registeredAt,
    DateTime updatedAt,
  });
}

/// Callable proxy for `copyWith` functionality.
/// Use as `instanceOfDeviceToken.copyWith(...)` or call `instanceOfDeviceToken.copyWith.fieldName(value)` for a single field.
class _$DeviceTokenCWProxyImpl implements _$DeviceTokenCWProxy {
  const _$DeviceTokenCWProxyImpl(this._value);

  final DeviceToken _value;

  @override
  DeviceToken id(String id) => call(id: id);

  @override
  DeviceToken platform(DevicePlatform platform) => call(platform: platform);

  @override
  DeviceToken appVersion(String appVersion) => call(appVersion: appVersion);

  @override
  DeviceToken registeredAt(DateTime registeredAt) =>
      call(registeredAt: registeredAt);

  @override
  DeviceToken updatedAt(DateTime updatedAt) => call(updatedAt: updatedAt);

  /// Creates a new instance with the provided field values.
  /// Passing `null` to a nullable field nullifies it, while `null` for a non-nullable field is ignored. To update a single field use `DeviceToken(...).copyWith.fieldName(value)`.
  ///
  /// Example:
  /// ```dart
  /// DeviceToken(...).copyWith(id: 12, name: "My name")
  /// ```
  @override
  DeviceToken call({
    Object? id = const $CopyWithPlaceholder(),
    Object? platform = const $CopyWithPlaceholder(),
    Object? appVersion = const $CopyWithPlaceholder(),
    Object? registeredAt = const $CopyWithPlaceholder(),
    Object? updatedAt = const $CopyWithPlaceholder(),
  }) {
    return DeviceToken(
      id: id == const $CopyWithPlaceholder() || id == null
          ? _value.id
          // ignore: cast_nullable_to_non_nullable
          : id as String,
      platform: platform == const $CopyWithPlaceholder() || platform == null
          ? _value.platform
          // ignore: cast_nullable_to_non_nullable
          : platform as DevicePlatform,
      appVersion:
          appVersion == const $CopyWithPlaceholder() || appVersion == null
          ? _value.appVersion
          // ignore: cast_nullable_to_non_nullable
          : appVersion as String,
      registeredAt:
          registeredAt == const $CopyWithPlaceholder() || registeredAt == null
          ? _value.registeredAt
          // ignore: cast_nullable_to_non_nullable
          : registeredAt as DateTime,
      updatedAt: updatedAt == const $CopyWithPlaceholder() || updatedAt == null
          ? _value.updatedAt
          // ignore: cast_nullable_to_non_nullable
          : updatedAt as DateTime,
    );
  }
}

extension $DeviceTokenCopyWith on DeviceToken {
  /// Returns a callable class used to build a new instance with modified fields.
  /// Example: `instanceOfDeviceToken.copyWith(...)` or `instanceOfDeviceToken.copyWith.fieldName(...)`.
  // ignore: library_private_types_in_public_api
  _$DeviceTokenCWProxy get copyWith => _$DeviceTokenCWProxyImpl(this);
}

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

DeviceToken _$DeviceTokenFromJson(Map<String, dynamic> json) =>
    $checkedCreate('DeviceToken', json, ($checkedConvert) {
      $checkKeys(
        json,
        requiredKeys: const [
          'id',
          'platform',
          'appVersion',
          'registeredAt',
          'updatedAt',
        ],
      );
      final val = DeviceToken(
        id: $checkedConvert('id', (v) => v as String),
        platform: $checkedConvert(
          'platform',
          (v) => $enumDecode(_$DevicePlatformEnumMap, v),
        ),
        appVersion: $checkedConvert('appVersion', (v) => v as String),
        registeredAt: $checkedConvert(
          'registeredAt',
          (v) => DateTime.parse(v as String),
        ),
        updatedAt: $checkedConvert(
          'updatedAt',
          (v) => DateTime.parse(v as String),
        ),
      );
      return val;
    });

Map<String, dynamic> _$DeviceTokenToJson(DeviceToken instance) =>
    <String, dynamic>{
      'id': instance.id,
      'platform': _$DevicePlatformEnumMap[instance.platform]!,
      'appVersion': instance.appVersion,
      'registeredAt': instance.registeredAt.toIso8601String(),
      'updatedAt': instance.updatedAt.toIso8601String(),
    };

const _$DevicePlatformEnumMap = {
  DevicePlatform.ios: 'ios',
  DevicePlatform.android: 'android',
  DevicePlatform.web: 'web',
};
