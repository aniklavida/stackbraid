//
// AUTO-GENERATED FILE, DO NOT MODIFY!
//

// ignore_for_file: unused_element
import 'package:copy_with_extension/copy_with_extension.dart';
import 'package:json_annotation/json_annotation.dart';

part 'assign_role_request.g.dart';


@CopyWith()
@JsonSerializable(
  checked: true,
  createToJson: true,
  disallowUnrecognizedKeys: false,
  explicitToJson: true,
)
class AssignRoleRequest {
  /// Returns a new [AssignRoleRequest] instance.
  AssignRoleRequest({

    required  this.roleId,
  });

  @JsonKey(
    
    name: r'roleId',
    required: true,
    includeIfNull: false,
  )


  final String roleId;





    @override
    bool operator ==(Object other) => identical(this, other) || other is AssignRoleRequest &&
      other.roleId == roleId;

    @override
    int get hashCode =>
        roleId.hashCode;

  factory AssignRoleRequest.fromJson(Map<String, dynamic> json) => _$AssignRoleRequestFromJson(json);

  Map<String, dynamic> toJson() => _$AssignRoleRequestToJson(this);

  @override
  String toString() {
    return toJson().toString();
  }

}

