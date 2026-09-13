// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'role.dart';

// **************************************************************************
// CopyWithGenerator
// **************************************************************************

abstract class _$RoleCWProxy {
  Role id(String id);

  Role name(String name);

  Role description(String? description);

  Role permissions(List<String> permissions);

  /// Creates a new instance with the provided field values.
  /// Passing `null` to a nullable field nullifies it, while `null` for a non-nullable field is ignored. To update a single field use `Role(...).copyWith.fieldName(value)`.
  ///
  /// Example:
  /// ```dart
  /// Role(...).copyWith(id: 12, name: "My name")
  /// ```
  Role call({
    String id,
    String name,
    String? description,
    List<String> permissions,
  });
}

/// Callable proxy for `copyWith` functionality.
/// Use as `instanceOfRole.copyWith(...)` or call `instanceOfRole.copyWith.fieldName(value)` for a single field.
class _$RoleCWProxyImpl implements _$RoleCWProxy {
  const _$RoleCWProxyImpl(this._value);

  final Role _value;

  @override
  Role id(String id) => call(id: id);

  @override
  Role name(String name) => call(name: name);

  @override
  Role description(String? description) => call(description: description);

  @override
  Role permissions(List<String> permissions) => call(permissions: permissions);

  /// Creates a new instance with the provided field values.
  /// Passing `null` to a nullable field nullifies it, while `null` for a non-nullable field is ignored. To update a single field use `Role(...).copyWith.fieldName(value)`.
  ///
  /// Example:
  /// ```dart
  /// Role(...).copyWith(id: 12, name: "My name")
  /// ```
  @override
  Role call({
    Object? id = const $CopyWithPlaceholder(),
    Object? name = const $CopyWithPlaceholder(),
    Object? description = const $CopyWithPlaceholder(),
    Object? permissions = const $CopyWithPlaceholder(),
  }) {
    return Role(
      id: id == const $CopyWithPlaceholder() || id == null
          ? _value.id
          // ignore: cast_nullable_to_non_nullable
          : id as String,
      name: name == const $CopyWithPlaceholder() || name == null
          ? _value.name
          // ignore: cast_nullable_to_non_nullable
          : name as String,
      description: description == const $CopyWithPlaceholder()
          ? _value.description
          // ignore: cast_nullable_to_non_nullable
          : description as String?,
      permissions:
          permissions == const $CopyWithPlaceholder() || permissions == null
          ? _value.permissions
          // ignore: cast_nullable_to_non_nullable
          : permissions as List<String>,
    );
  }
}

extension $RoleCopyWith on Role {
  /// Returns a callable class used to build a new instance with modified fields.
  /// Example: `instanceOfRole.copyWith(...)` or `instanceOfRole.copyWith.fieldName(...)`.
  // ignore: library_private_types_in_public_api
  _$RoleCWProxy get copyWith => _$RoleCWProxyImpl(this);
}

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

Role _$RoleFromJson(Map<String, dynamic> json) =>
    $checkedCreate('Role', json, ($checkedConvert) {
      $checkKeys(json, requiredKeys: const ['id', 'name', 'permissions']);
      final val = Role(
        id: $checkedConvert('id', (v) => v as String),
        name: $checkedConvert('name', (v) => v as String),
        description: $checkedConvert('description', (v) => v as String?),
        permissions: $checkedConvert(
          'permissions',
          (v) => (v as List<dynamic>).map((e) => e as String).toList(),
        ),
      );
      return val;
    });

Map<String, dynamic> _$RoleToJson(Role instance) => <String, dynamic>{
  'id': instance.id,
  'name': instance.name,
  'description': ?instance.description,
  'permissions': instance.permissions,
};
