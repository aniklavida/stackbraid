//
// AUTO-GENERATED FILE, DO NOT MODIFY!
//

// ignore_for_file: unused_element
import 'package:copy_with_extension/copy_with_extension.dart';
import 'package:json_annotation/json_annotation.dart';

part 'user_deactivated_message.g.dart';


@CopyWith()
@JsonSerializable(
  checked: true,
  createToJson: true,
  disallowUnrecognizedKeys: false,
  explicitToJson: true,
)
class UserDeactivatedMessage {
  /// Returns a new [UserDeactivatedMessage] instance.
  UserDeactivatedMessage({

    required  this.type,

    required  this.userId,

    required  this.occurredAt,
  });

  @JsonKey(
    
    name: r'type',
    required: true,
    includeIfNull: false,
  )


  final UserDeactivatedMessageTypeEnum type;



  @JsonKey(
    
    name: r'userId',
    required: true,
    includeIfNull: false,
  )


  final String userId;



      /// RFC 3339, UTC, always suffixed `Z` — never a numeric offset such as `+00:00`. A spike caught .NET emitting the offset form while Python emitted `Z` for the same instant; this pattern makes the offset form fail validation instead of merely looking inconsistent.
  @JsonKey(
    
    name: r'occurredAt',
    required: true,
    includeIfNull: false,
  )


  final DateTime occurredAt;





    @override
    bool operator ==(Object other) => identical(this, other) || other is UserDeactivatedMessage &&
      other.type == type &&
      other.userId == userId &&
      other.occurredAt == occurredAt;

    @override
    int get hashCode =>
        type.hashCode +
        userId.hashCode +
        occurredAt.hashCode;

  factory UserDeactivatedMessage.fromJson(Map<String, dynamic> json) => _$UserDeactivatedMessageFromJson(json);

  Map<String, dynamic> toJson() => _$UserDeactivatedMessageToJson(this);

  @override
  String toString() {
    return toJson().toString();
  }

}

enum UserDeactivatedMessageTypeEnum {
@JsonValue(r'user.deactivated')
userPeriodDeactivated(r'user.deactivated');

const UserDeactivatedMessageTypeEnum(this.value);

final String value;

@override
String toString() => value;
}


