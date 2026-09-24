// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'notification_page.dart';

// **************************************************************************
// CopyWithGenerator
// **************************************************************************

abstract class _$NotificationPageCWProxy {
  NotificationPage page(int page);

  NotificationPage pageSize(int pageSize);

  NotificationPage totalItems(int totalItems);

  NotificationPage totalPages(int totalPages);

  NotificationPage items(List<Notification> items);

  /// Creates a new instance with the provided field values.
  /// Passing `null` to a nullable field nullifies it, while `null` for a non-nullable field is ignored. To update a single field use `NotificationPage(...).copyWith.fieldName(value)`.
  ///
  /// Example:
  /// ```dart
  /// NotificationPage(...).copyWith(id: 12, name: "My name")
  /// ```
  NotificationPage call({
    int page,
    int pageSize,
    int totalItems,
    int totalPages,
    List<Notification> items,
  });
}

/// Callable proxy for `copyWith` functionality.
/// Use as `instanceOfNotificationPage.copyWith(...)` or call `instanceOfNotificationPage.copyWith.fieldName(value)` for a single field.
class _$NotificationPageCWProxyImpl implements _$NotificationPageCWProxy {
  const _$NotificationPageCWProxyImpl(this._value);

  final NotificationPage _value;

  @override
  NotificationPage page(int page) => call(page: page);

  @override
  NotificationPage pageSize(int pageSize) => call(pageSize: pageSize);

  @override
  NotificationPage totalItems(int totalItems) => call(totalItems: totalItems);

  @override
  NotificationPage totalPages(int totalPages) => call(totalPages: totalPages);

  @override
  NotificationPage items(List<Notification> items) => call(items: items);

  /// Creates a new instance with the provided field values.
  /// Passing `null` to a nullable field nullifies it, while `null` for a non-nullable field is ignored. To update a single field use `NotificationPage(...).copyWith.fieldName(value)`.
  ///
  /// Example:
  /// ```dart
  /// NotificationPage(...).copyWith(id: 12, name: "My name")
  /// ```
  @override
  NotificationPage call({
    Object? page = const $CopyWithPlaceholder(),
    Object? pageSize = const $CopyWithPlaceholder(),
    Object? totalItems = const $CopyWithPlaceholder(),
    Object? totalPages = const $CopyWithPlaceholder(),
    Object? items = const $CopyWithPlaceholder(),
  }) {
    return NotificationPage(
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
          : items as List<Notification>,
    );
  }
}

extension $NotificationPageCopyWith on NotificationPage {
  /// Returns a callable class used to build a new instance with modified fields.
  /// Example: `instanceOfNotificationPage.copyWith(...)` or `instanceOfNotificationPage.copyWith.fieldName(...)`.
  // ignore: library_private_types_in_public_api
  _$NotificationPageCWProxy get copyWith => _$NotificationPageCWProxyImpl(this);
}

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

NotificationPage _$NotificationPageFromJson(Map<String, dynamic> json) =>
    $checkedCreate('NotificationPage', json, ($checkedConvert) {
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
      final val = NotificationPage(
        page: $checkedConvert('page', (v) => (v as num).toInt()),
        pageSize: $checkedConvert('pageSize', (v) => (v as num).toInt()),
        totalItems: $checkedConvert('totalItems', (v) => (v as num).toInt()),
        totalPages: $checkedConvert('totalPages', (v) => (v as num).toInt()),
        items: $checkedConvert(
          'items',
          (v) => (v as List<dynamic>)
              .map((e) => Notification.fromJson(e as Map<String, dynamic>))
              .toList(),
        ),
      );
      return val;
    });

Map<String, dynamic> _$NotificationPageToJson(NotificationPage instance) =>
    <String, dynamic>{
      'page': instance.page,
      'pageSize': instance.pageSize,
      'totalItems': instance.totalItems,
      'totalPages': instance.totalPages,
      'items': instance.items.map((e) => e.toJson()).toList(),
    };
