// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'user_role_changed_message.dart';

// **************************************************************************
// CopyWithGenerator
// **************************************************************************

abstract class _$UserRoleChangedMessageCWProxy {
  UserRoleChangedMessage type(UserRoleChangedMessageTypeEnum type);

  UserRoleChangedMessage userId(String userId);

  UserRoleChangedMessage roles(List<Role> roles);

  UserRoleChangedMessage occurredAt(DateTime occurredAt);

  /// Creates a new instance with the provided field values.
  /// Passing `null` to a nullable field nullifies it, while `null` for a non-nullable field is ignored. To update a single field use `UserRoleChangedMessage(...).copyWith.fieldName(value)`.
  ///
  /// Example:
  /// ```dart
  /// UserRoleChangedMessage(...).copyWith(id: 12, name: "My name")
  /// ```
  UserRoleChangedMessage call({
    UserRoleChangedMessageTypeEnum type,
    String userId,
    List<Role> roles,
    DateTime occurredAt,
  });
}

/// Callable proxy for `copyWith` functionality.
/// Use as `instanceOfUserRoleChangedMessage.copyWith(...)` or call `instanceOfUserRoleChangedMessage.copyWith.fieldName(value)` for a single field.
class _$UserRoleChangedMessageCWProxyImpl
    implements _$UserRoleChangedMessageCWProxy {
  const _$UserRoleChangedMessageCWProxyImpl(this._value);

  final UserRoleChangedMessage _value;

  @override
  UserRoleChangedMessage type(UserRoleChangedMessageTypeEnum type) =>
      call(type: type);

  @override
  UserRoleChangedMessage userId(String userId) => call(userId: userId);

  @override
  UserRoleChangedMessage roles(List<Role> roles) => call(roles: roles);

  @override
  UserRoleChangedMessage occurredAt(DateTime occurredAt) =>
      call(occurredAt: occurredAt);

  /// Creates a new instance with the provided field values.
  /// Passing `null` to a nullable field nullifies it, while `null` for a non-nullable field is ignored. To update a single field use `UserRoleChangedMessage(...).copyWith.fieldName(value)`.
  ///
  /// Example:
  /// ```dart
  /// UserRoleChangedMessage(...).copyWith(id: 12, name: "My name")
  /// ```
  @override
  UserRoleChangedMessage call({
    Object? type = const $CopyWithPlaceholder(),
    Object? userId = const $CopyWithPlaceholder(),
    Object? roles = const $CopyWithPlaceholder(),
    Object? occurredAt = const $CopyWithPlaceholder(),
  }) {
    return UserRoleChangedMessage(
      type: type == const $CopyWithPlaceholder() || type == null
          ? _value.type
          // ignore: cast_nullable_to_non_nullable
          : type as UserRoleChangedMessageTypeEnum,
      userId: userId == const $CopyWithPlaceholder() || userId == null
          ? _value.userId
          // ignore: cast_nullable_to_non_nullable
          : userId as String,
      roles: roles == const $CopyWithPlaceholder() || roles == null
          ? _value.roles
          // ignore: cast_nullable_to_non_nullable
          : roles as List<Role>,
      occurredAt:
          occurredAt == const $CopyWithPlaceholder() || occurredAt == null
          ? _value.occurredAt
          // ignore: cast_nullable_to_non_nullable
          : occurredAt as DateTime,
    );
  }
}

extension $UserRoleChangedMessageCopyWith on UserRoleChangedMessage {
  /// Returns a callable class used to build a new instance with modified fields.
  /// Example: `instanceOfUserRoleChangedMessage.copyWith(...)` or `instanceOfUserRoleChangedMessage.copyWith.fieldName(...)`.
  // ignore: library_private_types_in_public_api
  _$UserRoleChangedMessageCWProxy get copyWith =>
      _$UserRoleChangedMessageCWProxyImpl(this);
}

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

UserRoleChangedMessage _$UserRoleChangedMessageFromJson(
  Map<String, dynamic> json,
) => $checkedCreate('UserRoleChangedMessage', json, ($checkedConvert) {
  $checkKeys(
    json,
    requiredKeys: const ['type', 'userId', 'roles', 'occurredAt'],
  );
  final val = UserRoleChangedMessage(
    type: $checkedConvert(
      'type',
      (v) => $enumDecode(_$UserRoleChangedMessageTypeEnumEnumMap, v),
    ),
    userId: $checkedConvert('userId', (v) => v as String),
    roles: $checkedConvert(
      'roles',
      (v) => (v as List<dynamic>)
          .map((e) => Role.fromJson(e as Map<String, dynamic>))
          .toList(),
    ),
    occurredAt: $checkedConvert(
      'occurredAt',
      (v) => DateTime.parse(v as String),
    ),
  );
  return val;
});

Map<String, dynamic> _$UserRoleChangedMessageToJson(
  UserRoleChangedMessage instance,
) => <String, dynamic>{
  'type': _$UserRoleChangedMessageTypeEnumEnumMap[instance.type]!,
  'userId': instance.userId,
  'roles': instance.roles.map((e) => e.toJson()).toList(),
  'occurredAt': instance.occurredAt.toIso8601String(),
};

const _$UserRoleChangedMessageTypeEnumEnumMap = {
  UserRoleChangedMessageTypeEnum.userPeriodRoleChanged: 'user.role_changed',
};
