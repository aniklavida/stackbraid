// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'audit_page.dart';

// **************************************************************************
// CopyWithGenerator
// **************************************************************************

abstract class _$AuditPageCWProxy {
  AuditPage items(List<AuditEntry> items);

  /// Creates a new instance with the provided field values.
  /// Passing `null` to a nullable field nullifies it, while `null` for a non-nullable field is ignored. To update a single field use `AuditPage(...).copyWith.fieldName(value)`.
  ///
  /// Example:
  /// ```dart
  /// AuditPage(...).copyWith(id: 12, name: "My name")
  /// ```
  AuditPage call({List<AuditEntry> items});
}

/// Callable proxy for `copyWith` functionality.
/// Use as `instanceOfAuditPage.copyWith(...)` or call `instanceOfAuditPage.copyWith.fieldName(value)` for a single field.
class _$AuditPageCWProxyImpl implements _$AuditPageCWProxy {
  const _$AuditPageCWProxyImpl(this._value);

  final AuditPage _value;

  @override
  AuditPage items(List<AuditEntry> items) => call(items: items);

  /// Creates a new instance with the provided field values.
  /// Passing `null` to a nullable field nullifies it, while `null` for a non-nullable field is ignored. To update a single field use `AuditPage(...).copyWith.fieldName(value)`.
  ///
  /// Example:
  /// ```dart
  /// AuditPage(...).copyWith(id: 12, name: "My name")
  /// ```
  @override
  AuditPage call({Object? items = const $CopyWithPlaceholder()}) {
    return AuditPage(
      items: items == const $CopyWithPlaceholder() || items == null
          ? _value.items
          // ignore: cast_nullable_to_non_nullable
          : items as List<AuditEntry>,
    );
  }
}

extension $AuditPageCopyWith on AuditPage {
  /// Returns a callable class used to build a new instance with modified fields.
  /// Example: `instanceOfAuditPage.copyWith(...)` or `instanceOfAuditPage.copyWith.fieldName(...)`.
  // ignore: library_private_types_in_public_api
  _$AuditPageCWProxy get copyWith => _$AuditPageCWProxyImpl(this);
}

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

AuditPage _$AuditPageFromJson(Map<String, dynamic> json) =>
    $checkedCreate('AuditPage', json, ($checkedConvert) {
      $checkKeys(json, requiredKeys: const ['items']);
      final val = AuditPage(
        items: $checkedConvert(
          'items',
          (v) => (v as List<dynamic>)
              .map((e) => AuditEntry.fromJson(e as Map<String, dynamic>))
              .toList(),
        ),
      );
      return val;
    });

Map<String, dynamic> _$AuditPageToJson(AuditPage instance) => <String, dynamic>{
  'items': instance.items.map((e) => e.toJson()).toList(),
};
