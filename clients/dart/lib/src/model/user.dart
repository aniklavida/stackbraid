//
// AUTO-GENERATED FILE, DO NOT MODIFY!
//

// ignore_for_file: unused_element
import 'package:stackbraid_client/src/model/user_status.dart';
import 'package:stackbraid_client/src/model/role.dart';
import 'package:copy_with_extension/copy_with_extension.dart';
import 'package:json_annotation/json_annotation.dart';

part 'user.g.dart';


@CopyWith()
@JsonSerializable(
  checked: true,
  createToJson: true,
  disallowUnrecognizedKeys: false,
  explicitToJson: true,
)
class User {
  /// Returns a new [User] instance.
  User({

    required  this.id,

    required  this.email,

    required  this.displayName,

    required  this.status,

    required  this.roles,

    required  this.createdAt,

    required  this.updatedAt,

     this.lastLoginAt,

    required  this.deletedAt,
  });

  @JsonKey(
    
    name: r'id',
    required: true,
    includeIfNull: false,
  )


  final String id;



  @JsonKey(
    
    name: r'email',
    required: true,
    includeIfNull: false,
  )


  final String email;



  @JsonKey(
    
    name: r'displayName',
    required: true,
    includeIfNull: false,
  )


  final String displayName;



  @JsonKey(
    
    name: r'status',
    required: true,
    includeIfNull: false,
  )


  final UserStatus status;



  @JsonKey(
    
    name: r'roles',
    required: true,
    includeIfNull: false,
  )


  final List<Role> roles;



      /// RFC 3339, UTC, always suffixed `Z` — never a numeric offset such as `+00:00`. A spike caught one backend emitting the offset form while another emitted `Z` for the same instant; this pattern makes the offset form fail validation instead of merely looking inconsistent.
  @JsonKey(
    
    name: r'createdAt',
    required: true,
    includeIfNull: false,
  )


  final DateTime createdAt;



      /// RFC 3339, UTC, always suffixed `Z` — never a numeric offset such as `+00:00`. A spike caught one backend emitting the offset form while another emitted `Z` for the same instant; this pattern makes the offset form fail validation instead of merely looking inconsistent.
  @JsonKey(
    
    name: r'updatedAt',
    required: true,
    includeIfNull: false,
  )


  final DateTime updatedAt;



      /// Same rule as `UtcDateTime`; `null` means the event has not happened yet.
  @JsonKey(
    
    name: r'lastLoginAt',
    required: false,
    includeIfNull: false,
  )


  final DateTime? lastLoginAt;



      /// Set when the account is soft-deleted; null for an active account.
  @JsonKey(
    
    name: r'deletedAt',
    required: true,
    includeIfNull: true,
  )


  final DateTime? deletedAt;





    @override
    bool operator ==(Object other) => identical(this, other) || other is User &&
      other.id == id &&
      other.email == email &&
      other.displayName == displayName &&
      other.status == status &&
      other.roles == roles &&
      other.createdAt == createdAt &&
      other.updatedAt == updatedAt &&
      other.lastLoginAt == lastLoginAt &&
      other.deletedAt == deletedAt;

    @override
    int get hashCode =>
        id.hashCode +
        email.hashCode +
        displayName.hashCode +
        status.hashCode +
        roles.hashCode +
        createdAt.hashCode +
        updatedAt.hashCode +
        (lastLoginAt == null ? 0 : lastLoginAt.hashCode) +
        (deletedAt == null ? 0 : deletedAt.hashCode);

  factory User.fromJson(Map<String, dynamic> json) => _$UserFromJson(json);

  Map<String, dynamic> toJson() => _$UserToJson(this);

  @override
  String toString() {
    return toJson().toString();
  }

}

