// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'user_page.dart';

// **************************************************************************
// CopyWithGenerator
// **************************************************************************

abstract class _$UserPageCWProxy {
  UserPage page(int page);

  UserPage pageSize(int pageSize);

  UserPage totalItems(int totalItems);

  UserPage totalPages(int totalPages);

  UserPage items(List<User> items);

  /// Creates a new instance with the provided field values.
  /// Passing `null` to a nullable field nullifies it, while `null` for a non-nullable field is ignored. To update a single field use `UserPage(...).copyWith.fieldName(value)`.
  ///
  /// Example:
  /// ```dart
  /// UserPage(...).copyWith(id: 12, name: "My name")
  /// ```
  UserPage call({
    int page,
    int pageSize,
    int totalItems,
    int totalPages,
    List<User> items,
  });
}

/// Callable proxy for `copyWith` functionality.
/// Use as `instanceOfUserPage.copyWith(...)` or call `instanceOfUserPage.copyWith.fieldName(value)` for a single field.
class _$UserPageCWProxyImpl implements _$UserPageCWProxy {
  const _$UserPageCWProxyImpl(this._value);

  final UserPage _value;

  @override
  UserPage page(int page) => call(page: page);

  @override
  UserPage pageSize(int pageSize) => call(pageSize: pageSize);

  @override
  UserPage totalItems(int totalItems) => call(totalItems: totalItems);

  @override
  UserPage totalPages(int totalPages) => call(totalPages: totalPages);

  @override
  UserPage items(List<User> items) => call(items: items);

  /// Creates a new instance with the provided field values.
  /// Passing `null` to a nullable field nullifies it, while `null` for a non-nullable field is ignored. To update a single field use `UserPage(...).copyWith.fieldName(value)`.
  ///
  /// Example:
  /// ```dart
  /// UserPage(...).copyWith(id: 12, name: "My name")
  /// ```
  @override
  UserPage call({
    Object? page = const $CopyWithPlaceholder(),
    Object? pageSize = const $CopyWithPlaceholder(),
    Object? totalItems = const $CopyWithPlaceholder(),
    Object? totalPages = const $CopyWithPlaceholder(),
    Object? items = const $CopyWithPlaceholder(),
  }) {
    return UserPage(
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
      items: items == const $CopyWithPlaceholder() || items == null
          ? _value.items
          // ignore: cast_nullable_to_non_nullable
          : items as List<User>,
    );
  }
}

extension $UserPageCopyWith on UserPage {
  /// Returns a callable class used to build a new instance with modified fields.
  /// Example: `instanceOfUserPage.copyWith(...)` or `instanceOfUserPage.copyWith.fieldName(...)`.
  // ignore: library_private_types_in_public_api
  _$UserPageCWProxy get copyWith => _$UserPageCWProxyImpl(this);
}

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

UserPage _$UserPageFromJson(Map<String, dynamic> json) =>
    $checkedCreate('UserPage', json, ($checkedConvert) {
      $checkKeys(
        json,
        requiredKeys: const [
          'page',
          'pageSize',
          'totalItems',
          'totalPages',
          'items',
        ],
      );
      final val = UserPage(
        page: $checkedConvert('page', (v) => (v as num).toInt()),
        pageSize: $checkedConvert('pageSize', (v) => (v as num).toInt()),
        totalItems: $checkedConvert('totalItems', (v) => (v as num).toInt()),
        totalPages: $checkedConvert('totalPages', (v) => (v as num).toInt()),
        items: $checkedConvert(
          'items',
          (v) => (v as List<dynamic>)
              .map((e) => User.fromJson(e as Map<String, dynamic>))
              .toList(),
        ),
      );
      return val;
    });

Map<String, dynamic> _$UserPageToJson(UserPage instance) => <String, dynamic>{
  'page': instance.page,
  'pageSize': instance.pageSize,
  'totalItems': instance.totalItems,
  'totalPages': instance.totalPages,
  'items': instance.items.map((e) => e.toJson()).toList(),
};
