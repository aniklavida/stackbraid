//
// AUTO-GENERATED FILE, DO NOT MODIFY!
//

// ignore_for_file: unused_element
import 'package:stackbraid_client/src/model/job_progress_message.dart';
import 'package:stackbraid_client/src/model/role.dart';
import 'package:stackbraid_client/src/model/user_deactivated_message.dart';
import 'package:stackbraid_client/src/model/user_role_changed_message.dart';
import 'package:copy_with_extension/copy_with_extension.dart';
import 'package:json_annotation/json_annotation.dart';

part 'realtime_message.g.dart';


@CopyWith()
@JsonSerializable(
  checked: true,
  createToJson: true,
  disallowUnrecognizedKeys: false,
  explicitToJson: true,
)
class RealtimeMessage {
  /// Returns a new [RealtimeMessage] instance.
  RealtimeMessage({

    required  this.type,

    required  this.userId,

    required  this.occurredAt,

    required  this.roles,

    required  this.jobId,

    required  this.status,

    required  this.progress,
  });

  @JsonKey(
    
    name: r'type',
    required: true,
    includeIfNull: false,
  )


  final RealtimeMessageTypeEnum type;



  @JsonKey(
    
    name: r'userId',
    required: true,
    includeIfNull: false,
  )


  final String userId;



      /// RFC 3339, UTC, always suffixed `Z` — never a numeric offset such as `+00:00`. A spike caught one backend emitting the offset form while another emitted `Z` for the same instant; this pattern makes the offset form fail validation instead of merely looking inconsistent.
  @JsonKey(
    
    name: r'occurredAt',
    required: true,
    includeIfNull: false,
  )


  final DateTime occurredAt;



      /// The user's full role set after the change, not a diff.
  @JsonKey(
    
    name: r'roles',
    required: true,
    includeIfNull: false,
  )


  final List<Role> roles;



  @JsonKey(
    
    name: r'jobId',
    required: true,
    includeIfNull: false,
  )


  final String jobId;



  @JsonKey(
    
    name: r'status',
    required: true,
    includeIfNull: false,
  )


  final RealtimeMessageStatusEnum status;



          // minimum: 0
          // maximum: 100
  @JsonKey(
    
    name: r'progress',
    required: true,
    includeIfNull: false,
  )


  final int progress;





    @override
    bool operator ==(Object other) => identical(this, other) || other is RealtimeMessage &&
      other.type == type &&
      other.userId == userId &&
      other.occurredAt == occurredAt &&
      other.roles == roles &&
      other.jobId == jobId &&
      other.status == status &&
      other.progress == progress;

    @override
    int get hashCode =>
        type.hashCode +
        userId.hashCode +
        occurredAt.hashCode +
        roles.hashCode +
        jobId.hashCode +
        status.hashCode +
        progress.hashCode;

  factory RealtimeMessage.fromJson(Map<String, dynamic> json) => _$RealtimeMessageFromJson(json);

  Map<String, dynamic> toJson() => _$RealtimeMessageToJson(this);

  @override
  String toString() {
    return toJson().toString();
  }

}

enum RealtimeMessageTypeEnum {
@JsonValue(r'user.deactivated')
userPeriodDeactivated(r'user.deactivated'),
@JsonValue(r'user.role_changed')
userPeriodRoleChanged(r'user.role_changed'),
@JsonValue(r'job.progress')
jobPeriodProgress(r'job.progress');

const RealtimeMessageTypeEnum(this.value);

final String value;

@override
String toString() => value;
}


enum RealtimeMessageStatusEnum {
@JsonValue(r'queued')
queued(r'queued'),
@JsonValue(r'running')
running(r'running'),
@JsonValue(r'succeeded')
succeeded(r'succeeded'),
@JsonValue(r'failed')
failed(r'failed');

const RealtimeMessageStatusEnum(this.value);

final String value;

@override
String toString() => value;
}


