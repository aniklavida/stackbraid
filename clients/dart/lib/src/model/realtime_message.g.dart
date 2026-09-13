// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'realtime_message.dart';

// **************************************************************************
// CopyWithGenerator
// **************************************************************************

abstract class _$RealtimeMessageCWProxy {
  RealtimeMessage type(RealtimeMessageTypeEnum type);

  RealtimeMessage userId(String userId);

  RealtimeMessage occurredAt(DateTime occurredAt);

  RealtimeMessage roles(List<Role> roles);

  RealtimeMessage jobId(String jobId);

  RealtimeMessage status(RealtimeMessageStatusEnum status);

  RealtimeMessage progress(int progress);

  /// Creates a new instance with the provided field values.
  /// Passing `null` to a nullable field nullifies it, while `null` for a non-nullable field is ignored. To update a single field use `RealtimeMessage(...).copyWith.fieldName(value)`.
  ///
  /// Example:
  /// ```dart
  /// RealtimeMessage(...).copyWith(id: 12, name: "My name")
  /// ```
  RealtimeMessage call({
    RealtimeMessageTypeEnum type,
    String userId,
    DateTime occurredAt,
    List<Role> roles,
    String jobId,
    RealtimeMessageStatusEnum status,
    int progress,
  });
}

/// Callable proxy for `copyWith` functionality.
/// Use as `instanceOfRealtimeMessage.copyWith(...)` or call `instanceOfRealtimeMessage.copyWith.fieldName(value)` for a single field.
class _$RealtimeMessageCWProxyImpl implements _$RealtimeMessageCWProxy {
  const _$RealtimeMessageCWProxyImpl(this._value);

  final RealtimeMessage _value;

  @override
  RealtimeMessage type(RealtimeMessageTypeEnum type) => call(type: type);

  @override
  RealtimeMessage userId(String userId) => call(userId: userId);

  @override
  RealtimeMessage occurredAt(DateTime occurredAt) =>
      call(occurredAt: occurredAt);

  @override
  RealtimeMessage roles(List<Role> roles) => call(roles: roles);

  @override
  RealtimeMessage jobId(String jobId) => call(jobId: jobId);

  @override
  RealtimeMessage status(RealtimeMessageStatusEnum status) =>
      call(status: status);

  @override
  RealtimeMessage progress(int progress) => call(progress: progress);

  /// Creates a new instance with the provided field values.
  /// Passing `null` to a nullable field nullifies it, while `null` for a non-nullable field is ignored. To update a single field use `RealtimeMessage(...).copyWith.fieldName(value)`.
  ///
  /// Example:
  /// ```dart
  /// RealtimeMessage(...).copyWith(id: 12, name: "My name")
  /// ```
  @override
  RealtimeMessage call({
    Object? type = const $CopyWithPlaceholder(),
    Object? userId = const $CopyWithPlaceholder(),
    Object? occurredAt = const $CopyWithPlaceholder(),
    Object? roles = const $CopyWithPlaceholder(),
    Object? jobId = const $CopyWithPlaceholder(),
    Object? status = const $CopyWithPlaceholder(),
    Object? progress = const $CopyWithPlaceholder(),
  }) {
    return RealtimeMessage(
      type: type == const $CopyWithPlaceholder() || type == null
          ? _value.type
          // ignore: cast_nullable_to_non_nullable
          : type as RealtimeMessageTypeEnum,
      userId: userId == const $CopyWithPlaceholder() || userId == null
          ? _value.userId
          // ignore: cast_nullable_to_non_nullable
          : userId as String,
      occurredAt:
          occurredAt == const $CopyWithPlaceholder() || occurredAt == null
          ? _value.occurredAt
          // ignore: cast_nullable_to_non_nullable
          : occurredAt as DateTime,
      roles: roles == const $CopyWithPlaceholder() || roles == null
          ? _value.roles
          // ignore: cast_nullable_to_non_nullable
          : roles as List<Role>,
      jobId: jobId == const $CopyWithPlaceholder() || jobId == null
          ? _value.jobId
          // ignore: cast_nullable_to_non_nullable
          : jobId as String,
      status: status == const $CopyWithPlaceholder() || status == null
          ? _value.status
          // ignore: cast_nullable_to_non_nullable
          : status as RealtimeMessageStatusEnum,
      progress: progress == const $CopyWithPlaceholder() || progress == null
          ? _value.progress
          // ignore: cast_nullable_to_non_nullable
          : progress as int,
    );
  }
}

extension $RealtimeMessageCopyWith on RealtimeMessage {
  /// Returns a callable class used to build a new instance with modified fields.
  /// Example: `instanceOfRealtimeMessage.copyWith(...)` or `instanceOfRealtimeMessage.copyWith.fieldName(...)`.
  // ignore: library_private_types_in_public_api
  _$RealtimeMessageCWProxy get copyWith => _$RealtimeMessageCWProxyImpl(this);
}

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

RealtimeMessage _$RealtimeMessageFromJson(Map<String, dynamic> json) =>
    $checkedCreate('RealtimeMessage', json, ($checkedConvert) {
      $checkKeys(
        json,
        requiredKeys: const [
          'type',
          'userId',
          'occurredAt',
          'roles',
          'jobId',
          'status',
          'progress',
        ],
      );
      final val = RealtimeMessage(
        type: $checkedConvert(
          'type',
          (v) => $enumDecode(_$RealtimeMessageTypeEnumEnumMap, v),
        ),
        userId: $checkedConvert('userId', (v) => v as String),
        occurredAt: $checkedConvert(
          'occurredAt',
          (v) => DateTime.parse(v as String),
        ),
        roles: $checkedConvert(
          'roles',
          (v) => (v as List<dynamic>)
              .map((e) => Role.fromJson(e as Map<String, dynamic>))
              .toList(),
        ),
        jobId: $checkedConvert('jobId', (v) => v as String),
        status: $checkedConvert(
          'status',
          (v) => $enumDecode(_$RealtimeMessageStatusEnumEnumMap, v),
        ),
        progress: $checkedConvert('progress', (v) => (v as num).toInt()),
      );
      return val;
    });

Map<String, dynamic> _$RealtimeMessageToJson(RealtimeMessage instance) =>
    <String, dynamic>{
      'type': _$RealtimeMessageTypeEnumEnumMap[instance.type]!,
      'userId': instance.userId,
      'occurredAt': instance.occurredAt.toIso8601String(),
      'roles': instance.roles.map((e) => e.toJson()).toList(),
      'jobId': instance.jobId,
      'status': _$RealtimeMessageStatusEnumEnumMap[instance.status]!,
      'progress': instance.progress,
    };

const _$RealtimeMessageTypeEnumEnumMap = {
  RealtimeMessageTypeEnum.userPeriodDeactivated: 'user.deactivated',
  RealtimeMessageTypeEnum.userPeriodRoleChanged: 'user.role_changed',
  RealtimeMessageTypeEnum.jobPeriodProgress: 'job.progress',
};

const _$RealtimeMessageStatusEnumEnumMap = {
  RealtimeMessageStatusEnum.queued: 'queued',
  RealtimeMessageStatusEnum.running: 'running',
  RealtimeMessageStatusEnum.succeeded: 'succeeded',
  RealtimeMessageStatusEnum.failed: 'failed',
};
