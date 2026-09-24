//
// AUTO-GENERATED FILE, DO NOT MODIFY!
//

// ignore_for_file: unused_element
import 'package:copy_with_extension/copy_with_extension.dart';
import 'package:json_annotation/json_annotation.dart';

part 'audit_entry.g.dart';


@CopyWith()
@JsonSerializable(
  checked: true,
  createToJson: true,
  disallowUnrecognizedKeys: false,
  explicitToJson: true,
)
class AuditEntry {
  /// Returns a new [AuditEntry] instance.
  AuditEntry({

    required  this.id,

    required  this.entityType,

    required  this.entityId,

    required  this.action,

     this.actorId,

    required  this.correlationId,

    required  this.occurredAt,

     this.details,
  });

  @JsonKey(
    
    name: r'id',
    required: true,
    includeIfNull: false,
  )


  final String id;



  @JsonKey(
    
    name: r'entityType',
    required: true,
    includeIfNull: false,
  )


  final String entityType;



  @JsonKey(
    
    name: r'entityId',
    required: true,
    includeIfNull: false,
  )


  final String entityId;



  @JsonKey(
    
    name: r'action',
    required: true,
    includeIfNull: false,
  )


  final String action;



  @JsonKey(
    
    name: r'actorId',
    required: false,
    includeIfNull: false,
  )


  final String? actorId;



  @JsonKey(
    
    name: r'correlationId',
    required: true,
    includeIfNull: false,
  )


  final String correlationId;



      /// RFC 3339, UTC, always suffixed `Z` — never a numeric offset such as `+00:00`. A spike caught one backend emitting the offset form while another emitted `Z` for the same instant; this pattern makes the offset form fail validation instead of merely looking inconsistent.
  @JsonKey(
    
    name: r'occurredAt',
    required: true,
    includeIfNull: false,
  )


  final DateTime occurredAt;



  @JsonKey(
    
    name: r'details',
    required: false,
    includeIfNull: false,
  )


  final String? details;





    @override
    bool operator ==(Object other) => identical(this, other) || other is AuditEntry &&
      other.id == id &&
      other.entityType == entityType &&
      other.entityId == entityId &&
      other.action == action &&
      other.actorId == actorId &&
      other.correlationId == correlationId &&
      other.occurredAt == occurredAt &&
      other.details == details;

    @override
    int get hashCode =>
        id.hashCode +
        entityType.hashCode +
        entityId.hashCode +
        action.hashCode +
        (actorId == null ? 0 : actorId.hashCode) +
        correlationId.hashCode +
        occurredAt.hashCode +
        (details == null ? 0 : details.hashCode);

  factory AuditEntry.fromJson(Map<String, dynamic> json) => _$AuditEntryFromJson(json);

  Map<String, dynamic> toJson() => _$AuditEntryToJson(this);

  @override
  String toString() {
    return toJson().toString();
  }

}

