// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'assign_role_request.dart';

// **************************************************************************
// CopyWithGenerator
// **************************************************************************

abstract class _$AssignRoleRequestCWProxy {
  AssignRoleRequest roleId(String roleId);

  /// Creates a new instance with the provided field values.
  /// Passing `null` to a nullable field nullifies it, while `null` for a non-nullable field is ignored. To update a single field use `AssignRoleRequest(...).copyWith.fieldName(value)`.
  ///
  /// Example:
  /// ```dart
  /// AssignRoleRequest(...).copyWith(id: 12, name: "My name")
  /// ```
  AssignRoleRequest call({String roleId});
}

/// Callable proxy for `copyWith` functionality.
/// Use as `instanceOfAssignRoleRequest.copyWith(...)` or call `instanceOfAssignRoleRequest.copyWith.fieldName(value)` for a single field.
class _$AssignRoleRequestCWProxyImpl implements _$AssignRoleRequestCWProxy {
  const _$AssignRoleRequestCWProxyImpl(this._value);

  final AssignRoleRequest _value;

  @override
  AssignRoleRequest roleId(String roleId) => call(roleId: roleId);

  /// Creates a new instance with the provided field values.
  /// Passing `null` to a nullable field nullifies it, while `null` for a non-nullable field is ignored. To update a single field use `AssignRoleRequest(...).copyWith.fieldName(value)`.
  ///
  /// Example:
  /// ```dart
  /// AssignRoleRequest(...).copyWith(id: 12, name: "My name")
  /// ```
  @override
  AssignRoleRequest call({Object? roleId = const $CopyWithPlaceholder()}) {
    return AssignRoleRequest(
      roleId: roleId == const $CopyWithPlaceholder() || roleId == null
          ? _value.roleId
          // ignore: cast_nullable_to_non_nullable
          : roleId as String,
    );
  }
}

extension $AssignRoleRequestCopyWith on AssignRoleRequest {
  /// Returns a callable class used to build a new instance with modified fields.
  /// Example: `instanceOfAssignRoleRequest.copyWith(...)` or `instanceOfAssignRoleRequest.copyWith.fieldName(...)`.
  // ignore: library_private_types_in_public_api
  _$AssignRoleRequestCWProxy get copyWith =>
      _$AssignRoleRequestCWProxyImpl(this);
}

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

AssignRoleRequest _$AssignRoleRequestFromJson(Map<String, dynamic> json) =>
    $checkedCreate('AssignRoleRequest', json, ($checkedConvert) {
      $checkKeys(json, requiredKeys: const ['roleId']);
      final val = AssignRoleRequest(
        roleId: $checkedConvert('roleId', (v) => v as String),
      );
      return val;
    });

Map<String, dynamic> _$AssignRoleRequestToJson(AssignRoleRequest instance) =>
    <String, dynamic>{'roleId': instance.roleId};
