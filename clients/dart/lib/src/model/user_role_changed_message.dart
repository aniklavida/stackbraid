//
// AUTO-GENERATED FILE, DO NOT MODIFY!
//

// ignore_for_file: unused_element
import 'package:stackbraid_client/src/model/role.dart';
import 'package:copy_with_extension/copy_with_extension.dart';
import 'package:json_annotation/json_annotation.dart';

part 'user_role_changed_message.g.dart';


@CopyWith()
@JsonSerializable(
  checked: true,
  createToJson: true,
  disallowUnrecognizedKeys: false,
  explicitToJson: true,
)
class UserRoleChangedMessage {
  /// Returns a new [UserRoleChangedMessage] instance.
  UserRoleChangedMessage({

    required  this.type,

    required  this.userId,

    required  this.roles,

    required  this.occurredAt,
  });

  @JsonKey(
    
    name: r'type',
    required: true,
    includeIfNull: false,
  )


  final UserRoleChangedMessageTypeEnum type;



  @JsonKey(
    
    name: r'userId',
    required: true,
    includeIfNull: false,
  )


  final String userId;



      /// The user's full role set after the change, not a diff.
  @JsonKey(
    
    name: r'roles',
    required: true,
    includeIfNull: false,
  )


  final List<Role> roles;



      /// RFC 3339, UTC, always suffixed `Z` — never a numeric offset such as `+00:00`. A spike caught one backend emitting the offset form while another emitted `Z` for the same instant; this pattern makes the offset form fail validation instead of merely looking inconsistent.
  @JsonKey(
    
    name: r'occurredAt',
    required: true,
    includeIfNull: false,
  )


  final DateTime occurredAt;





    @override
    bool operator ==(Object other) => identical(this, other) || other is UserRoleChangedMessage &&
      other.type == type &&
      other.userId == userId &&
      other.roles == roles &&
      other.occurredAt == occurredAt;

    @override
    int get hashCode =>
        type.hashCode +
        userId.hashCode +
        roles.hashCode +
        occurredAt.hashCode;

  factory UserRoleChangedMessage.fromJson(Map<String, dynamic> json) => _$UserRoleChangedMessageFromJson(json);

  Map<String, dynamic> toJson() => _$UserRoleChangedMessageToJson(this);

  @override
  String toString() {
    return toJson().toString();
  }

}

enum UserRoleChangedMessageTypeEnum {
@JsonValue(r'user.role_changed')
userPeriodRoleChanged(r'user.role_changed');

const UserRoleChangedMessageTypeEnum(this.value);

final String value;

@override
String toString() => value;
}


