// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'job_progress_message.dart';

// **************************************************************************
// CopyWithGenerator
// **************************************************************************

abstract class _$JobProgressMessageCWProxy {
  JobProgressMessage type(JobProgressMessageTypeEnum type);

  JobProgressMessage jobId(String jobId);

  JobProgressMessage status(JobProgressMessageStatusEnum status);

  JobProgressMessage progress(int progress);

  JobProgressMessage occurredAt(DateTime occurredAt);

  /// Creates a new instance with the provided field values.
  /// Passing `null` to a nullable field nullifies it, while `null` for a non-nullable field is ignored. To update a single field use `JobProgressMessage(...).copyWith.fieldName(value)`.
  ///
  /// Example:
  /// ```dart
  /// JobProgressMessage(...).copyWith(id: 12, name: "My name")
  /// ```
  JobProgressMessage call({
    JobProgressMessageTypeEnum type,
    String jobId,
    JobProgressMessageStatusEnum status,
    int progress,
    DateTime occurredAt,
  });
}

/// Callable proxy for `copyWith` functionality.
/// Use as `instanceOfJobProgressMessage.copyWith(...)` or call `instanceOfJobProgressMessage.copyWith.fieldName(value)` for a single field.
class _$JobProgressMessageCWProxyImpl implements _$JobProgressMessageCWProxy {
  const _$JobProgressMessageCWProxyImpl(this._value);

  final JobProgressMessage _value;

  @override
  JobProgressMessage type(JobProgressMessageTypeEnum type) => call(type: type);

  @override
  JobProgressMessage jobId(String jobId) => call(jobId: jobId);

  @override
  JobProgressMessage status(JobProgressMessageStatusEnum status) =>
      call(status: status);

  @override
  JobProgressMessage progress(int progress) => call(progress: progress);

  @override
  JobProgressMessage occurredAt(DateTime occurredAt) =>
      call(occurredAt: occurredAt);

  /// Creates a new instance with the provided field values.
  /// Passing `null` to a nullable field nullifies it, while `null` for a non-nullable field is ignored. To update a single field use `JobProgressMessage(...).copyWith.fieldName(value)`.
  ///
  /// Example:
  /// ```dart
  /// JobProgressMessage(...).copyWith(id: 12, name: "My name")
  /// ```
  @override
  JobProgressMessage call({
    Object? type = const $CopyWithPlaceholder(),
    Object? jobId = const $CopyWithPlaceholder(),
    Object? status = const $CopyWithPlaceholder(),
    Object? progress = const $CopyWithPlaceholder(),
    Object? occurredAt = const $CopyWithPlaceholder(),
  }) {
    return JobProgressMessage(
      type: type == const $CopyWithPlaceholder() || type == null
          ? _value.type
          // ignore: cast_nullable_to_non_nullable
          : type as JobProgressMessageTypeEnum,
      jobId: jobId == const $CopyWithPlaceholder() || jobId == null
          ? _value.jobId
          // ignore: cast_nullable_to_non_nullable
          : jobId as String,
      status: status == const $CopyWithPlaceholder() || status == null
          ? _value.status
          // ignore: cast_nullable_to_non_nullable
          : status as JobProgressMessageStatusEnum,
      progress: progress == const $CopyWithPlaceholder() || progress == null
          ? _value.progress
          // ignore: cast_nullable_to_non_nullable
          : progress as int,
      occurredAt:
          occurredAt == const $CopyWithPlaceholder() || occurredAt == null
          ? _value.occurredAt
          // ignore: cast_nullable_to_non_nullable
          : occurredAt as DateTime,
    );
  }
}

extension $JobProgressMessageCopyWith on JobProgressMessage {
  /// Returns a callable class used to build a new instance with modified fields.
  /// Example: `instanceOfJobProgressMessage.copyWith(...)` or `instanceOfJobProgressMessage.copyWith.fieldName(...)`.
  // ignore: library_private_types_in_public_api
  _$JobProgressMessageCWProxy get copyWith =>
      _$JobProgressMessageCWProxyImpl(this);
}

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

JobProgressMessage _$JobProgressMessageFromJson(Map<String, dynamic> json) =>
    $checkedCreate('JobProgressMessage', json, ($checkedConvert) {
      $checkKeys(
        json,
        requiredKeys: const [
          'type',
          'jobId',
          'status',
          'progress',
          'occurredAt',
        ],
      );
      final val = JobProgressMessage(
        type: $checkedConvert(
          'type',
          (v) => $enumDecode(_$JobProgressMessageTypeEnumEnumMap, v),
        ),
        jobId: $checkedConvert('jobId', (v) => v as String),
        status: $checkedConvert(
          'status',
          (v) => $enumDecode(_$JobProgressMessageStatusEnumEnumMap, v),
        ),
        progress: $checkedConvert('progress', (v) => (v as num).toInt()),
        occurredAt: $checkedConvert(
          'occurredAt',
          (v) => DateTime.parse(v as String),
        ),
      );
      return val;
    });

Map<String, dynamic> _$JobProgressMessageToJson(JobProgressMessage instance) =>
    <String, dynamic>{
      'type': _$JobProgressMessageTypeEnumEnumMap[instance.type]!,
      'jobId': instance.jobId,
      'status': _$JobProgressMessageStatusEnumEnumMap[instance.status]!,
      'progress': instance.progress,
      'occurredAt': instance.occurredAt.toIso8601String(),
    };

const _$JobProgressMessageTypeEnumEnumMap = {
  JobProgressMessageTypeEnum.jobPeriodProgress: 'job.progress',
};

const _$JobProgressMessageStatusEnumEnumMap = {
  JobProgressMessageStatusEnum.queued: 'queued',
  JobProgressMessageStatusEnum.running: 'running',
  JobProgressMessageStatusEnum.succeeded: 'succeeded',
  JobProgressMessageStatusEnum.failed: 'failed',
};
