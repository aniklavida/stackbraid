//
// AUTO-GENERATED FILE, DO NOT MODIFY!
//

// ignore_for_file: unused_element
import 'package:stackbraid_client/src/model/user.dart';
import 'package:copy_with_extension/copy_with_extension.dart';
import 'package:json_annotation/json_annotation.dart';

part 'user_page.g.dart';


@CopyWith()
@JsonSerializable(
  checked: true,
  createToJson: true,
  disallowUnrecognizedKeys: false,
  explicitToJson: true,
)
class UserPage {
  /// Returns a new [UserPage] instance.
  UserPage({

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


  final List<User> items;





    @override
    bool operator ==(Object other) => identical(this, other) || other is UserPage &&
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

  factory UserPage.fromJson(Map<String, dynamic> json) => _$UserPageFromJson(json);

  Map<String, dynamic> toJson() => _$UserPageToJson(this);

  @override
  String toString() {
    return toJson().toString();
  }

}

