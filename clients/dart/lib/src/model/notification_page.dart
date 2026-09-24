//
// AUTO-GENERATED FILE, DO NOT MODIFY!
//

// ignore_for_file: unused_element
import 'package:stackbraid_client/src/model/notification.dart';
import 'package:copy_with_extension/copy_with_extension.dart';
import 'package:json_annotation/json_annotation.dart';

part 'notification_page.g.dart';


@CopyWith()
@JsonSerializable(
  checked: true,
  createToJson: true,
  disallowUnrecognizedKeys: false,
  explicitToJson: true,
)
class NotificationPage {
  /// Returns a new [NotificationPage] instance.
  NotificationPage({

    required  this.page,

    required  this.pageSize,

    required  this.totalItems,

    required  this.totalPages,

    required  this.items,
  });

          // minimum: 1
  @JsonKey(
    
    name: r'page',
    required: true,
    includeIfNull: false,
  )


  final int page;



          // minimum: 1
  @JsonKey(
    
    name: r'pageSize',
    required: true,
    includeIfNull: false,
  )


  final int pageSize;



          // minimum: 0
  @JsonKey(
    
    name: r'totalItems',
    required: true,
    includeIfNull: false,
  )


  final int totalItems;



          // minimum: 0
  @JsonKey(
    
    name: r'totalPages',
    required: true,
    includeIfNull: false,
  )


  final int totalPages;



  @JsonKey(
    
    name: r'items',
    required: true,
    includeIfNull: false,
  )


  final List<Notification> items;





    @override
    bool operator ==(Object other) => identical(this, other) || other is NotificationPage &&
      other.page == page &&
      other.pageSize == pageSize &&
      other.totalItems == totalItems &&
      other.totalPages == totalPages &&
      other.items == items;

    @override
    int get hashCode =>
        page.hashCode +
        pageSize.hashCode +
        totalItems.hashCode +
        totalPages.hashCode +
        items.hashCode;

  factory NotificationPage.fromJson(Map<String, dynamic> json) => _$NotificationPageFromJson(json);

  Map<String, dynamic> toJson() => _$NotificationPageToJson(this);

  @override
  String toString() {
    return toJson().toString();
  }

}

