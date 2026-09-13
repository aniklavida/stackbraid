// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'user_deactivated_message.dart';

// **************************************************************************
// CopyWithGenerator
// **************************************************************************

abstract class _$UserDeactivatedMessageCWProxy {
  UserDeactivatedMessage type(UserDeactivatedMessageTypeEnum type);

  UserDeactivatedMessage userId(String userId);

  UserDeactivatedMessage occurredAt(DateTime occurredAt);

  /// Creates a new instance with the provided field values.
  /// Passing `null` to a nullable field nullifies it, while `null` for a non-nullable field is ignored. To update a single field use `UserDeactivatedMessage(...).copyWith.fieldName(value)`.
  ///
  /// Example:
  /// ```dart
  /// UserDeactivatedMessage(...).copyWith(id: 12, name: "My name")
  /// ```
  UserDeactivatedMessage call({
    UserDeactivatedMessageTypeEnum type,
    String userId,
    DateTime occurredAt,
  });
}

/// Callable proxy for `copyWith` functionality.
/// Use as `instanceOfUserDeactivatedMessage.copyWith(...)` or call `instanceOfUserDeactivatedMessage.copyWith.fieldName(value)` for a single field.
class _$UserDeactivatedMessageCWProxyImpl
    implements _$UserDeactivatedMessageCWProxy {
  const _$UserDeactivatedMessageCWProxyImpl(this._value);

  final UserDeactivatedMessage _value;

  @override
  UserDeactivatedMessage type(UserDeactivatedMessageTypeEnum type) =>
      call(type: type);

  @override
  UserDeactivatedMessage userId(String userId) => call(userId: userId);

  @override
  UserDeactivatedMessage occurredAt(DateTime occurredAt) =>
      call(occurredAt: occurredAt);

  /// Creates a new instance with the provided field values.
  /// Passing `null` to a nullable field nullifies it, while `null` for a non-nullable field is ignored. To update a single field use `UserDeactivatedMessage(...).copyWith.fieldName(value)`.
  ///
  /// Example:
  /// ```dart
  /// UserDeactivatedMessage(...).copyWith(id: 12, name: "My name")
  /// ```
  @override
  UserDeactivatedMessage call({
    Object? type = const $CopyWithPlaceholder(),
    Object? userId = const $CopyWithPlaceholder(),
    Object? occurredAt = const $CopyWithPlaceholder(),
  }) {
    return UserDeactivatedMessage(
      type: type == const $CopyWithPlaceholder() || type == null
          ? _value.type
          // ignore: cast_nullable_to_non_nullable
          : type as UserDeactivatedMessageTypeEnum,
      userId: userId == const $CopyWithPlaceholder() || userId == null
          ? _value.userId
          // ignore: cast_nullable_to_non_nullable
          : userId as String,
      occurredAt:
          occurredAt == const $CopyWithPlaceholder() || occurredAt == null
          ? _value.occurredAt
          // ignore: cast_nullable_to_non_nullable
          : occurredAt as DateTime,
    );
  }
}

extension $UserDeactivatedMessageCopyWith on UserDeactivatedMessage {
  /// Returns a callable class used to build a new instance with modified fields.
  /// Example: `instanceOfUserDeactivatedMessage.copyWith(...)` or `instanceOfUserDeactivatedMessage.copyWith.fieldName(...)`.
  // ignore: library_private_types_in_public_api
  _$UserDeactivatedMessageCWProxy get copyWith =>
      _$UserDeactivatedMessageCWProxyImpl(this);
}

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

UserDeactivatedMessage _$UserDeactivatedMessageFromJson(
  Map<String, dynamic> json,
) => $checkedCreate('UserDeactivatedMessage', json, ($checkedConvert) {
  $checkKeys(json, requiredKeys: const ['type', 'userId', 'occurredAt']);
  final val = UserDeactivatedMessage(
    type: $checkedConvert(
      'type',
      (v) => $enumDecode(_$UserDeactivatedMessageTypeEnumEnumMap, v),
    ),
    userId: $checkedConvert('userId', (v) => v as String),
    occurredAt: $checkedConvert(
      'occurredAt',
      (v) => DateTime.parse(v as String),
    ),
  );
  return val;
});

Map<String, dynamic> _$UserDeactivatedMessageToJson(
  UserDeactivatedMessage instance,
) => <String, dynamic>{
  'type': _$UserDeactivatedMessageTypeEnumEnumMap[instance.type]!,
  'userId': instance.userId,
  'occurredAt': instance.occurredAt.toIso8601String(),
};

const _$UserDeactivatedMessageTypeEnumEnumMap = {
  UserDeactivatedMessageTypeEnum.userPeriodDeactivated: 'user.deactivated',
};
