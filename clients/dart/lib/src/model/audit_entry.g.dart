// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'audit_entry.dart';

// **************************************************************************
// CopyWithGenerator
// **************************************************************************

abstract class _$AuditEntryCWProxy {
  AuditEntry id(String id);

  AuditEntry entityType(String entityType);

  AuditEntry entityId(String entityId);

  AuditEntry action(String action);

  AuditEntry actorId(String? actorId);

  AuditEntry correlationId(String correlationId);

  AuditEntry occurredAt(DateTime occurredAt);

  AuditEntry details(String? details);

  /// Creates a new instance with the provided field values.
  /// Passing `null` to a nullable field nullifies it, while `null` for a non-nullable field is ignored. To update a single field use `AuditEntry(...).copyWith.fieldName(value)`.
  ///
  /// Example:
  /// ```dart
  /// AuditEntry(...).copyWith(id: 12, name: "My name")
  /// ```
  AuditEntry call({
    String id,
    String entityType,
    String entityId,
    String action,
    String? actorId,
    String correlationId,
    DateTime occurredAt,
    String? details,
  });
}

/// Callable proxy for `copyWith` functionality.
/// Use as `instanceOfAuditEntry.copyWith(...)` or call `instanceOfAuditEntry.copyWith.fieldName(value)` for a single field.
class _$AuditEntryCWProxyImpl implements _$AuditEntryCWProxy {
  const _$AuditEntryCWProxyImpl(this._value);

  final AuditEntry _value;

  @override
  AuditEntry id(String id) => call(id: id);

  @override
  AuditEntry entityType(String entityType) => call(entityType: entityType);

  @override
  AuditEntry entityId(String entityId) => call(entityId: entityId);

  @override
  AuditEntry action(String action) => call(action: action);

  @override
  AuditEntry actorId(String? actorId) => call(actorId: actorId);

  @override
  AuditEntry correlationId(String correlationId) =>
      call(correlationId: correlationId);

  @override
  AuditEntry occurredAt(DateTime occurredAt) => call(occurredAt: occurredAt);

  @override
  AuditEntry details(String? details) => call(details: details);

  /// Creates a new instance with the provided field values.
  /// Passing `null` to a nullable field nullifies it, while `null` for a non-nullable field is ignored. To update a single field use `AuditEntry(...).copyWith.fieldName(value)`.
  ///
  /// Example:
  /// ```dart
  /// AuditEntry(...).copyWith(id: 12, name: "My name")
  /// ```
  @override
  AuditEntry call({
    Object? id = const $CopyWithPlaceholder(),
    Object? entityType = const $CopyWithPlaceholder(),
    Object? entityId = const $CopyWithPlaceholder(),
    Object? action = const $CopyWithPlaceholder(),
    Object? actorId = const $CopyWithPlaceholder(),
    Object? correlationId = const $CopyWithPlaceholder(),
    Object? occurredAt = const $CopyWithPlaceholder(),
    Object? details = const $CopyWithPlaceholder(),
  }) {
    return AuditEntry(
      id: id == const $CopyWithPlaceholder() || id == null
          ? _value.id
          // ignore: cast_nullable_to_non_nullable
          : id as String,
      entityType:
          entityType == const $CopyWithPlaceholder() || entityType == null
          ? _value.entityType
          // ignore: cast_nullable_to_non_nullable
          : entityType as String,
      entityId: entityId == const $CopyWithPlaceholder() || entityId == null
          ? _value.entityId
          // ignore: cast_nullable_to_non_nullable
          : entityId as String,
      action: action == const $CopyWithPlaceholder() || action == null
          ? _value.action
          // ignore: cast_nullable_to_non_nullable
          : action as String,
      actorId: actorId == const $CopyWithPlaceholder()
          ? _value.actorId
          // ignore: cast_nullable_to_non_nullable
          : actorId as String?,
      correlationId:
          correlationId == const $CopyWithPlaceholder() || correlationId == null
          ? _value.correlationId
          // ignore: cast_nullable_to_non_nullable
          : correlationId as String,
      occurredAt:
          occurredAt == const $CopyWithPlaceholder() || occurredAt == null
          ? _value.occurredAt
          // ignore: cast_nullable_to_non_nullable
          : occurredAt as DateTime,
      details: details == const $CopyWithPlaceholder()
          ? _value.details
          // ignore: cast_nullable_to_non_nullable
          : details as String?,
    );
  }
}

extension $AuditEntryCopyWith on AuditEntry {
  /// Returns a callable class used to build a new instance with modified fields.
  /// Example: `instanceOfAuditEntry.copyWith(...)` or `instanceOfAuditEntry.copyWith.fieldName(...)`.
  // ignore: library_private_types_in_public_api
  _$AuditEntryCWProxy get copyWith => _$AuditEntryCWProxyImpl(this);
}

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

AuditEntry _$AuditEntryFromJson(Map<String, dynamic> json) =>
    $checkedCreate('AuditEntry', json, ($checkedConvert) {
      $checkKeys(
        json,
        requiredKeys: const [
          'id',
          'entityType',
          'entityId',
          'action',
          'correlationId',
          'occurredAt',
        ],
      );
      final val = AuditEntry(
        id: $checkedConvert('id', (v) => v as String),
        entityType: $checkedConvert('entityType', (v) => v as String),
        entityId: $checkedConvert('entityId', (v) => v as String),
        action: $checkedConvert('action', (v) => v as String),
        actorId: $checkedConvert('actorId', (v) => v as String?),
        correlationId: $checkedConvert('correlationId', (v) => v as String),
        occurredAt: $checkedConvert(
          'occurredAt',
          (v) => DateTime.parse(v as String),
        ),
        details: $checkedConvert('details', (v) => v as String?),
      );
      return val;
    });

Map<String, dynamic> _$AuditEntryToJson(AuditEntry instance) =>
    <String, dynamic>{
      'id': instance.id,
      'entityType': instance.entityType,
      'entityId': instance.entityId,
      'action': instance.action,
      'actorId': ?instance.actorId,
      'correlationId': instance.correlationId,
      'occurredAt': instance.occurredAt.toIso8601String(),
      'details': ?instance.details,
    };
