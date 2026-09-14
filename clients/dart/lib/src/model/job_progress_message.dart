//
// AUTO-GENERATED FILE, DO NOT MODIFY!
//

// ignore_for_file: unused_element
import 'package:copy_with_extension/copy_with_extension.dart';
import 'package:json_annotation/json_annotation.dart';

part 'job_progress_message.g.dart';


@CopyWith()
@JsonSerializable(
  checked: true,
  createToJson: true,
  disallowUnrecognizedKeys: false,
  explicitToJson: true,
)
class JobProgressMessage {
  /// Returns a new [JobProgressMessage] instance.
  JobProgressMessage({

    required  this.type,

    required  this.jobId,

    required  this.status,

    required  this.progress,

    required  this.occurredAt,
  });

  @JsonKey(
    
    name: r'type',
    required: true,
    includeIfNull: false,
  )


  final JobProgressMessageTypeEnum type;



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


  final JobProgressMessageStatusEnum status;



          // minimum: 0
          // maximum: 100
  @JsonKey(
    
    name: r'progress',
    required: true,
    includeIfNull: false,
  )


  final int progress;



      /// RFC 3339, UTC, always suffixed `Z` — never a numeric offset such as `+00:00`. A spike caught one backend emitting the offset form while another emitted `Z` for the same instant; this pattern makes the offset form fail validation instead of merely looking inconsistent.
  @JsonKey(
    
    name: r'occurredAt',
    required: true,
    includeIfNull: false,
  )


  final DateTime occurredAt;





    @override
    bool operator ==(Object other) => identical(this, other) || other is JobProgressMessage &&
      other.type == type &&
      other.jobId == jobId &&
      other.status == status &&
      other.progress == progress &&
      other.occurredAt == occurredAt;

    @override
    int get hashCode =>
        type.hashCode +
        jobId.hashCode +
        status.hashCode +
        progress.hashCode +
        occurredAt.hashCode;

  factory JobProgressMessage.fromJson(Map<String, dynamic> json) => _$JobProgressMessageFromJson(json);

  Map<String, dynamic> toJson() => _$JobProgressMessageToJson(this);

  @override
  String toString() {
    return toJson().toString();
  }

}

enum JobProgressMessageTypeEnum {
@JsonValue(r'job.progress')
jobPeriodProgress(r'job.progress');

const JobProgressMessageTypeEnum(this.value);

final String value;

@override
String toString() => value;
}


enum JobProgressMessageStatusEnum {
@JsonValue(r'queued')
queued(r'queued'),
@JsonValue(r'running')
running(r'running'),
@JsonValue(r'succeeded')
succeeded(r'succeeded'),
@JsonValue(r'failed')
failed(r'failed');

const JobProgressMessageStatusEnum(this.value);

final String value;

@override
String toString() => value;
}


