// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'page.dart';

// **************************************************************************
// CopyWithGenerator
// **************************************************************************

abstract class _$PageCWProxy {
  Page page(int page);

  Page pageSize(int pageSize);

  Page totalItems(int totalItems);

  Page totalPages(int totalPages);

  /// Creates a new instance with the provided field values.
  /// Passing `null` to a nullable field nullifies it, while `null` for a non-nullable field is ignored. To update a single field use `Page(...).copyWith.fieldName(value)`.
  ///
  /// Example:
  /// ```dart
  /// Page(...).copyWith(id: 12, name: "My name")
  /// ```
  Page call({int page, int pageSize, int totalItems, int totalPages});
}

/// Callable proxy for `copyWith` functionality.
/// Use as `instanceOfPage.copyWith(...)` or call `instanceOfPage.copyWith.fieldName(value)` for a single field.
class _$PageCWProxyImpl implements _$PageCWProxy {
  const _$PageCWProxyImpl(this._value);

  final Page _value;

  @override
  Page page(int page) => call(page: page);

  @override
  Page pageSize(int pageSize) => call(pageSize: pageSize);

  @override
  Page totalItems(int totalItems) => call(totalItems: totalItems);

  @override
  Page totalPages(int totalPages) => call(totalPages: totalPages);

  /// Creates a new instance with the provided field values.
  /// Passing `null` to a nullable field nullifies it, while `null` for a non-nullable field is ignored. To update a single field use `Page(...).copyWith.fieldName(value)`.
  ///
  /// Example:
  /// ```dart
  /// Page(...).copyWith(id: 12, name: "My name")
  /// ```
  @override
  Page call({
    Object? page = const $CopyWithPlaceholder(),
    Object? pageSize = const $CopyWithPlaceholder(),
    Object? totalItems = const $CopyWithPlaceholder(),
    Object? totalPages = const $CopyWithPlaceholder(),
  }) {
    return Page(
      page: page == const $CopyWithPlaceholder() || page == null
          ? _value.page
          // ignore: cast_nullable_to_non_nullable
          : page as int,
      pageSize: pageSize == const $CopyWithPlaceholder() || pageSize == null
          ? _value.pageSize
          // ignore: cast_nullable_to_non_nullable
          : pageSize as int,
      totalItems:
          totalItems == const $CopyWithPlaceholder() || totalItems == null
          ? _value.totalItems
          // ignore: cast_nullable_to_non_nullable
          : totalItems as int,
      totalPages:
          totalPages == const $CopyWithPlaceholder() || totalPages == null
          ? _value.totalPages
          // ignore: cast_nullable_to_non_nullable
          : totalPages as int,
    );
  }
}

extension $PageCopyWith on Page {
  /// Returns a callable class used to build a new instance with modified fields.
  /// Example: `instanceOfPage.copyWith(...)` or `instanceOfPage.copyWith.fieldName(...)`.
  // ignore: library_private_types_in_public_api
  _$PageCWProxy get copyWith => _$PageCWProxyImpl(this);
}

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

Page _$PageFromJson(Map<String, dynamic> json) =>
    $checkedCreate('Page', json, ($checkedConvert) {
      $checkKeys(
        json,
        requiredKeys: const ['page', 'pageSize', 'totalItems', 'totalPages'],
      );
      final val = Page(
        page: $checkedConvert('page', (v) => (v as num).toInt()),
        pageSize: $checkedConvert('pageSize', (v) => (v as num).toInt()),
        totalItems: $checkedConvert('totalItems', (v) => (v as num).toInt()),
        totalPages: $checkedConvert('totalPages', (v) => (v as num).toInt()),
      );
      return val;
    });

Map<String, dynamic> _$PageToJson(Page instance) => <String, dynamic>{
  'page': instance.page,
  'pageSize': instance.pageSize,
  'totalItems': instance.totalItems,
  'totalPages': instance.totalPages,
};
